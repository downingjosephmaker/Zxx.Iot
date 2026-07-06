using CenBoCommon.Zxx;
using IotLog;
using System.Collections.Concurrent;

namespace IotWebApi.Services
{
    /// <summary>
    /// 推送策略引擎(管道入口统一执行:四模式+死区+最小推送间隔+最大静默兜底+关键属性冲刷;
    /// 只约束遥测数据流的对外发布(历史/遥测/SignalR),最新值缓存永远即时更新,
    /// 告警/上下线事件走独立事件通道不经本引擎)
    /// </summary>
    public class PushGateService : IHostedService
    {
        private const string Service_CATEGORY = "推送策略引擎";

        /// <summary>
        /// 挂起点位扫描周期(节流窗口到期与静默兜底的补发粒度)
        /// </summary>
        private static readonly TimeSpan ScanWindow = TimeSpan.FromSeconds(1);

        /// <summary>
        /// 点位推送运行时状态
        /// </summary>
        private class PushState
        {
            public string TypeCode;              // 设备类型编码(扫描循环取策略用)
            public double? LastPushedValue;      // 上次对外发布的数值
            public string LastPushedStr;         // 上次对外发布的字符串值
            public bool HasPushed;               // 是否已发布过(首值必发做曲线基线)
            public DateTime LastPushTime;        // 上次对外发布时刻(UTC)
            public TelemetryPoint Pending;       // 被扣下的最新点位(丢中间保最新)
            public bool PendingPublishable;      // 扣下原因(true=判定要发但被节流,false=未变化/定时模式仅静默兜底)
        }

        /// <summary>
        /// 点位推送状态((设备,参数编码)→状态)
        /// </summary>
        private readonly ConcurrentDictionary<(long DeviceId, string ParamCode), PushState> _states = new();

        /// <summary>
        /// 策略合并服务
        /// </summary>
        private readonly StrategyMergeService _mergeService;

        /// <summary>
        /// 挂起点位补发回调(由数据入库服务注册:补写历史/遥测/SignalR)
        /// </summary>
        public Action<List<TelemetryPoint>> FlushHandler { get; set; }

        private CancellationTokenSource _cts;
        private Task _scanTask;

        public PushGateService(StrategyMergeService mergeService)
        {
            _mergeService = mergeService;
        }

        #region 服务生命周期

        /// <summary>
        /// 启动扫描循环
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _scanTask = Task.Run(() => ScanLoopAsync(_cts.Token));
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "推送策略引擎已启动", Service_CATEGORY);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止扫描循环
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            if (_scanTask != null)
            {
                try { await _scanTask; } catch (OperationCanceledException) { }
            }
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "推送策略引擎已停止", Service_CATEGORY);
        }

        #endregion

        #region 推送判定

        /// <summary>
        /// 判定点位是否立即对外发布(false=扣下,由扫描循环按节流窗口/静默周期补发)
        /// </summary>
        public bool ShouldPublish(string typecode, TelemetryPoint point)
        {
            var strategy = _mergeService.GetPushStrategy(typecode, (int)point.DeviceId, point.ParamCode);
            var state = _states.GetOrAdd((point.DeviceId, point.ParamCode), _ => new PushState());
            lock (state)
            {
                state.TypeCode = typecode;
                var now = DateTime.UtcNow;

                // 首值必发(曲线基线);关键属性变化立即冲刷,不参与合并节流
                if (!state.HasPushed || strategy.DebounceIgnoreKeys.Contains(point.ParamCode))
                {
                    MarkPushed(state, point, now);
                    return true;
                }

                bool changed = IsChanged(state, point, strategy);
                bool publish = strategy.ReportMode switch
                {
                    1 => true,        // 收到即报
                    2 => changed,     // 变化上报
                    3 => false,       // 定时上报(只靠静默周期补发)
                    _ => changed      // 4=变化上报+最大静默兜底(默认)
                };

                // 最小推送间隔:节流窗口内多次变化只留最新一条
                bool throttled = publish && strategy.MinPushIntervalMs > 0
                    && (now - state.LastPushTime).TotalMilliseconds < strategy.MinPushIntervalMs;

                if (publish && !throttled)
                {
                    MarkPushed(state, point, now);
                    return true;
                }

                // 扣下最新值(节流的可补发;未变化/定时模式的仅供静默兜底取用)
                state.Pending = point;
                state.PendingPublishable = state.PendingPublishable || throttled;
                return false;
            }
        }

        /// <summary>
        /// 死区变化判定(0=严格不等,1=绝对死区,2=百分比死区;字符串值恒按严格不等)
        /// </summary>
        private static bool IsChanged(PushState state, TelemetryPoint point, EffectivePushStrategy strategy)
        {
            if (!point.Value.HasValue || !state.LastPushedValue.HasValue)
            {
                return point.ValueStr != state.LastPushedStr || point.Value != state.LastPushedValue;
            }
            var diff = Math.Abs(point.Value.Value - state.LastPushedValue.Value);
            return strategy.DeadbandType switch
            {
                1 => diff > (double)strategy.DeadbandValue,
                2 => state.LastPushedValue.Value != 0 && diff / Math.Abs(state.LastPushedValue.Value) * 100 > (double)strategy.DeadbandValue,
                _ => diff > 0
            };
        }

        /// <summary>
        /// 记录发布状态并清空挂起
        /// </summary>
        private static void MarkPushed(PushState state, TelemetryPoint point, DateTime now)
        {
            state.LastPushedValue = point.Value;
            state.LastPushedStr = point.ValueStr;
            state.HasPushed = true;
            state.LastPushTime = now;
            state.Pending = null;
            state.PendingPublishable = false;
        }

        #endregion

        #region 补发扫描

        /// <summary>
        /// 扫描主循环(节流窗口到期补发扣下的最新值;静默周期到期强制上报兜底)
        /// </summary>
        private async Task ScanLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(ScanWindow, token);
                    var flushlist = new List<TelemetryPoint>();
                    var now = DateTime.UtcNow;
                    foreach (var pair in _states)
                    {
                        var state = pair.Value;
                        lock (state)
                        {
                            if (state.TypeCode.IsZxxNullOrEmpty() || !state.HasPushed) continue;
                            var strategy = _mergeService.GetPushStrategy(state.TypeCode, (int)pair.Key.DeviceId, pair.Key.ParamCode);
                            var elapsedms = (now - state.LastPushTime).TotalMilliseconds;

                            // 1.节流窗口到期:补发被扣下的可发布点位
                            if (state.Pending != null && state.PendingPublishable && elapsedms >= strategy.MinPushIntervalMs)
                            {
                                flushlist.Add(state.Pending);
                                MarkPushed(state, state.Pending, now);
                                continue;
                            }
                            // 2.最大静默兜底:超时强制上报最新值(定时模式3的唯一发布通道)
                            if ((strategy.ReportMode == 3 || strategy.ReportMode == 4) && strategy.MaxSilentMs > 0 && elapsedms >= strategy.MaxSilentMs)
                            {
                                var point = state.Pending ?? new TelemetryPoint
                                {
                                    DeviceId = pair.Key.DeviceId,
                                    ParamCode = pair.Key.ParamCode,
                                    Ts = now,
                                    Value = state.LastPushedValue,
                                    ValueStr = state.LastPushedStr,
                                    Quality = 0
                                };
                                flushlist.Add(point);
                                MarkPushed(state, point, now);
                            }
                        }
                    }
                    if (flushlist.IsZxxAny() && FlushHandler != null)
                    {
                        try
                        {
                            FlushHandler(flushlist);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"补发{flushlist.Count}个点位失败：{ex}", Service_CATEGORY);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 服务停止，正常退出
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"扫描循环意外退出：{ex}", Service_CATEGORY);
            }
        }

        #endregion
    }
}
