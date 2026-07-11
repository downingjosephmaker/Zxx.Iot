using CenBoCommon.Zxx;
using IotLog;
using NewLife;

namespace IotDriverCore
{
    /// <summary>
    /// 通道指令调度引擎(自GuoXiang插件"指令调度"骨架上提,协议无关:
    /// 每端点独立发送循环+同端点单飞行指令(严格一问一答)+控制优先于采集+超时重发;
    /// 采集指令常驻队列按毫秒周期/cron双模循环(即采集调度器的落点),超时重排到下一周期永不删除;
    /// 物理发送经IChannelTransport,收帧由驱动调用MatchResponse完成回执关联)
    /// </summary>
    public class ChannelCommandEngine : IDisposable
    {
        /// <summary>
        /// 同端点发送循环的轮询间隔(毫秒,兼作RS-485从站喘息时间)
        /// </summary>
        private readonly int _sendIntervalMs;

        /// <summary>
        /// 通道物理发送器
        /// </summary>
        private readonly IChannelTransport _transport;

        /// <summary>
        /// 指令队列锁
        /// </summary>
        private readonly object _cmdLock = new();

        /// <summary>
        /// 端点→指令队列
        /// </summary>
        private readonly Dictionary<string, List<DriverCommand>> _queues = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 发送循环表锁
        /// </summary>
        private readonly object _workerLock = new();

        /// <summary>
        /// 端点→发送循环任务
        /// </summary>
        private readonly Dictionary<string, Task> _workers = new(StringComparer.OrdinalIgnoreCase);

        private CancellationTokenSource? _cts;

        /// <summary>
        /// 已停止标志(区分"未启动"与"已停止":Start前入队采集指令是合法模式,不能只判_cts)
        /// </summary>
        private volatile bool _stopped;

        /// <summary>
        /// 控制指令最终超时回调(驱动发布超时控制结果用)
        /// </summary>
        public Action<DriverCommand>? TimeoutHandler { get; set; }

        public ChannelCommandEngine(IChannelTransport transport, int sendintervalms = 300)
        {
            _transport = transport;
            _sendIntervalMs = Math.Max(10, sendintervalms);
        }

        #region 引擎生命周期

        /// <summary>
        /// 启动引擎(为已有队列的端点拉起发送循环,之后Enqueue自动补拉)
        /// </summary>
        public void Start()
        {
            _stopped = false;
            _cts ??= new CancellationTokenSource();
            List<string> endpoints;
            lock (_cmdLock) { endpoints = _queues.Keys.ToList(); }
            foreach (var endpoint in endpoints) EnsureWorker(endpoint);
        }

        /// <summary>
        /// 停止引擎(取消并等待所有端点发送循环安全退出)
        /// </summary>
        public async Task StopAsync()
        {
            _stopped = true;
            var cts = _cts;
            _cts = null;
            cts?.Cancel();
            Task[] tasks;
            lock (_workerLock)
            {
                tasks = _workers.Values.ToArray();
                _workers.Clear();
            }
            if (tasks.Length > 0)
            {
                try { await Task.WhenAll(tasks); } catch { }
            }
            cts?.Dispose();
        }

        public void Dispose()
        {
            _stopped = true;
            _cts?.Cancel();
        }

        #endregion

        #region 指令入队与回执匹配

        /// <summary>
        /// 指令入队(队列不存在自动创建,并确保端点发送循环已启动);
        /// 引擎已停止时控制指令立即走超时回调返回失败回执,不再静默滞留于无发送循环的队列
        /// </summary>
        public void Enqueue(DriverCommand cmd)
        {
            if (cmd.Endpoint.IsZxxNullOrEmpty()) return;
            if (_stopped && cmd.CmdKind == DriverCommand.KindControl)
            {
                FailControlCommand(cmd);
                return;
            }
            lock (_cmdLock)
            {
                if (!_queues.TryGetValue(cmd.Endpoint, out var list))
                {
                    list = new List<DriverCommand>();
                    _queues[cmd.Endpoint] = list;
                }
                list.Add(cmd);
            }
            EnsureWorker(cmd.Endpoint);
        }

        /// <summary>
        /// 原子替换端点队列(设备配置刷新时全量重建采集指令)
        /// </summary>
        public void ReplaceQueue(string endpoint, List<DriverCommand> cmds)
        {
            if (endpoint.IsZxxNullOrEmpty()) return;
            lock (_cmdLock) { _queues[endpoint] = cmds; }
            EnsureWorker(endpoint);
        }

        /// <summary>
        /// 清空全部队列(插件停止时调用);在队未完成的控制指令逐条走超时回调,
        /// 让调用方(手动下发/规则联动)收到失败回执而非无回执无超时的静默丢失
        /// </summary>
        public void ClearAll()
        {
            List<DriverCommand> pending;
            lock (_cmdLock)
            {
                pending = _queues.Values.SelectMany(t => t)
                    .Where(c => c.CmdKind == DriverCommand.KindControl && c.State != 2 && c.State != 3)
                    .ToList();
                _queues.Clear();
            }
            foreach (var cmd in pending) FailControlCommand(cmd);
        }

        /// <summary>
        /// 控制指令失败出局:置废弃态并回调TimeoutHandler(引擎停止/清队兜底,与ProcessTimeouts超限废弃同款回执路径)
        /// </summary>
        private void FailControlCommand(DriverCommand cmd)
        {
            cmd.State = 3;
            var handler = TimeoutHandler;
            if (handler != null) _ = Task.Run(() => handler(cmd));
        }

        /// <summary>
        /// 入站帧回执匹配:命中该端点飞行中指令则标记已回执并返回,未命中返回null
        /// (匹配条件:飞行中+等待应答+ResponseMatcher命中或为空)
        /// </summary>
        public DriverCommand? MatchResponse(string endpoint, byte[] frame)
        {
            lock (_cmdLock)
            {
                if (!_queues.TryGetValue(endpoint, out var list)) return null;
                var cmd = list.FirstOrDefault(t =>
                    t.State == 1 && t.WaitForResponse &&
                    (t.ResponseMatcher == null || t.ResponseMatcher(frame)));
                if (cmd == null) return null;
                cmd.State = 2;
                cmd.TimeoutCount = 0;
                cmd.ReceiveTime = DateTime.Now;
                cmd.ResponseFrame = frame;
                return cmd;
            }
        }

        /// <summary>
        /// 从队列移除指令(驱动处理完一次性回执后调用)
        /// </summary>
        public void Remove(DriverCommand cmd)
        {
            lock (_cmdLock)
            {
                if (_queues.TryGetValue(cmd.Endpoint, out var list)) list.Remove(cmd);
            }
        }

        /// <summary>
        /// 加速采集:把端点下采集指令的下次发送时刻重置为当前(控制成功后即时刷新状态);
        /// deviceaddr>=0时仅加速该设备地址的采集指令
        /// </summary>
        public void AccelerateCollect(string endpoint, int deviceaddr = -1)
        {
            lock (_cmdLock)
            {
                if (!_queues.TryGetValue(endpoint, out var list)) return;
                var now = DateTime.Now;
                foreach (var c in list.Where(t => t.CmdKind == DriverCommand.KindCollect
                    && (deviceaddr < 0 || t.DeviceAddr == deviceaddr)))
                {
                    c.NextSendTime = now;
                    if (c.State == 2) c.State = 0;
                }
            }
        }

        #endregion

        #region 发送循环

        /// <summary>
        /// 确保指定端点的发送循环已运行(不存在或已完成则重新拉起;引擎停止中不再拉起)
        /// </summary>
        private void EnsureWorker(string endpoint)
        {
            var cts = _cts;
            if (cts == null || cts.IsCancellationRequested) return;
            lock (_workerLock)
            {
                if (_workers.TryGetValue(endpoint, out var task) && !task.IsCompleted) return;
                _workers[endpoint] = Task.Run(() => RunSendLoopAsync(endpoint, cts.Token));
            }
        }

        /// <summary>
        /// 端点发送主循环(每隔SendIntervalMs尝试发送一条;异常退出后由EnsureWorker再次拉起)
        /// </summary>
        private async Task RunSendLoopAsync(string endpoint, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    TrySend(endpoint);
                    await Task.Delay(_sendIntervalMs, token);
                }
            }
            catch (OperationCanceledException)
            {
                // 引擎停止，正常退出
            }
            catch (Exception ex) { LogHelper.Error(ex); }
            finally
            {
                lock (_workerLock)
                {
                    if (_workers.TryGetValue(endpoint, out var task) && task.IsCompleted)
                        _workers.Remove(endpoint);
                }
            }
        }

        /// <summary>
        /// 尝试发送一条指令:处理超时→选取(单飞行约束+控制优先)→标记已发→物理发送,
        /// 发送失败回置待发下轮重试
        /// </summary>
        private void TrySend(string endpoint)
        {
            try
            {
                DriverCommand? cmd;
                lock (_cmdLock)
                {
                    if (!_queues.TryGetValue(endpoint, out var list) || !list.IsZxxAny()) return;
                    ProcessTimeouts(list);
                    cmd = SelectNext(list);
                    if (cmd == null) { ResetCompleted(list); return; }
                    MarkSent(cmd, DateTime.Now);
                }
                bool ok = false;
                try { ok = _transport.Send(endpoint, cmd.Payload); }
                catch (Exception ex) { LogHelper.Error(ex); }
                if (!ok)
                {
                    lock (_cmdLock) { cmd.State = 0; }
                    return;
                }
                LogHelper.Info($"通道[{endpoint}]{(cmd.CmdKind == DriverCommand.KindControl ? "控制" : "采集")}指令：{cmd.Payload.ToHex()}，设备[{cmd.DeviceId}]");
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        /// <summary>
        /// 处理已超时指令:采集指令重排到下一周期永不删除(设备恢复后自动恢复采集);
        /// 控制指令未达重试上限立即重发,超限废弃并回调TimeoutHandler
        /// </summary>
        private void ProcessTimeouts(List<DriverCommand> list)
        {
            var now = DateTime.Now;
            foreach (var t in list.Where(c => c.State == 1 && c.WaitForResponse && c.SendDeadline < now).ToList())
            {
                t.State = 0;
                t.TimeoutCount++;
                if (t.CmdKind == DriverCommand.KindCollect)
                {
                    t.NextSendTime = t.ComputeNextSendTime(now);
                    continue;
                }
                if (t.RetryLimit > t.TimeoutCount)
                {
                    t.NextSendTime = now;
                    LogHelper.Info($"通道[{t.Endpoint}]控制指令超时重试，设备[{t.DeviceId}] addr[{t.DeviceAddr}]");
                    continue;
                }
                t.State = 3;
                var handler = TimeoutHandler;
                if (handler != null) _ = Task.Run(() => handler(t));
            }
        }

        /// <summary>
        /// 选取下一条待发指令(前置:同端点无飞行中指令;排序:控制优先→设备地址升序)
        /// </summary>
        private static DriverCommand? SelectNext(List<DriverCommand> list)
        {
            if (list.Any(t => t.State == 1 && t.WaitForResponse)) return null;
            var now = DateTime.Now;
            return list.Where(c => c.State == 0 && c.NextSendTime <= now)
                       .OrderByDescending(c => c.CmdKind)
                       .ThenBy(c => c.DeviceAddr)
                       .FirstOrDefault();
        }

        /// <summary>
        /// 整理队列:已回执的一次性指令删除,循环指令回置待发等下一周期;废弃指令删除
        /// </summary>
        private static void ResetCompleted(List<DriverCommand> list)
        {
            foreach (var c in list.ToList())
            {
                if (c.State != 2) continue;
                if (c.OneShot) { list.Remove(c); continue; }
                c.State = 0;
            }
            list.RemoveAll(c => c.State == 3);
        }

        /// <summary>
        /// 标记指令已发送:等待应答置飞行中,更新发送/超时/下一周期时刻
        /// </summary>
        private static void MarkSent(DriverCommand c, DateTime now)
        {
            c.State = c.WaitForResponse ? 1 : 2;
            c.SendCount++;
            c.SendTime = now;
            c.SendDeadline = now.AddSeconds(c.TimeoutSeconds);
            c.NextSendTime = c.ComputeNextSendTime(now);
            c.ResponseFrame = Array.Empty<byte>();
        }

        #endregion
    }
}
