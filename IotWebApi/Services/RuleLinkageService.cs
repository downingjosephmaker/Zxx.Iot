using CenboEventBus;
using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using NewLife.Threading;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace IotWebApi.Services
{
    /// <summary>
    /// 规则联动引擎(§10.1:扁平"触发-条件-动作"模型,吸取IoTSharp FlowRuleEngine教训明确不做BPMN;
    /// 触发:点位变化/告警产生或恢复/定时cron/设备上下线;
    /// 条件:表达式(裸参数编码=触发设备点位,d{设备ID}_{参数编码}=跨设备)+时间窗+冷却防连发;
    /// 动作:下发命令(白名单+PluginCommandEvent总线)/写虚拟点位/发通知(渠道复用)/Webhook;
    /// 工程化:每规则漏斗指标(matched/passed/failed/action计数)+执行审计只记一条汇总日志+异常捕获隔离)
    /// </summary>
    public class RuleLinkageService : IHostedService
    {
        private const string Service_CATEGORY = "规则联动";

        /// <summary>
        /// 命令白名单(§6.3:动作下发仅允许既有协议控制类型,阀控类默认由插件侧白名单再把一道关)
        /// </summary>
        private static readonly HashSet<string> CommandWhitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            "netmodbuswrite", "netdlt645timesync", "netdlt645read", "netcjt188read", "netcjt188valve",
            "nets7write", "netopcuawrite"
        };

        /// <summary>
        /// 规则缓存刷新周期
        /// </summary>
        private static readonly TimeSpan ConfigTtl = TimeSpan.FromSeconds(60);

        /// <summary>
        /// 定时型规则扫描周期
        /// </summary>
        private static readonly TimeSpan CronScanWindow = TimeSpan.FromSeconds(5);

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

        /// <summary>
        /// 规则漏斗指标(§10.1工程化三件套之一)
        /// </summary>
        public class RuleMetrics
        {
            /// <summary>触发命中数</summary>
            public long Matched;
            /// <summary>条件通过数</summary>
            public long Passed;
            /// <summary>条件未过数(含时间窗外)</summary>
            public long Failed;
            /// <summary>动作成功数</summary>
            public long ActionOk;
            /// <summary>动作失败数</summary>
            public long ActionFail;
        }

        /// <summary>规则快照</summary>
        private volatile List<LinkageRule> _rules = new();

        private DateTime _configTime = DateTime.MinValue;
        private readonly object _configLock = new();

        /// <summary>规则→漏斗指标</summary>
        private readonly ConcurrentDictionary<long, RuleMetrics> _metrics = new();

        /// <summary>规则→最近动作时刻(冷却防连发)</summary>
        private readonly ConcurrentDictionary<long, DateTime> _lastRun = new();

        /// <summary>定时型规则→下次触发时刻</summary>
        private readonly ConcurrentDictionary<long, DateTime> _cronNext = new();

        private readonly TelemetryLatestService _latestService;
        private readonly TelemetryWriteService _telemetryService;
        private readonly AlarmNotifyService _notifyService;
        private readonly IEventBus<PluginCommandEvent> _commandBus;

        private CancellationTokenSource _cts;
        private Task _cronTask;

        public RuleLinkageService(TelemetryLatestService latestService, TelemetryWriteService telemetryService,
            AlarmNotifyService notifyService, IEventBus<PluginCommandEvent> commandBus)
        {
            _latestService = latestService;
            _telemetryService = telemetryService;
            _notifyService = notifyService;
            _commandBus = commandBus;
        }

        #region 服务生命周期

        /// <summary>
        /// 启动定时型规则扫描
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _cronTask = Task.Run(() => CronLoopAsync(_cts.Token));
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "规则联动引擎已启动", Service_CATEGORY);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止定时型规则扫描
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            if (_cronTask != null)
            {
                try { await _cronTask; } catch (OperationCanceledException) { }
            }
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "规则联动引擎已停止", Service_CATEGORY);
        }

        /// <summary>
        /// 清空规则缓存(配置变更热重载)
        /// </summary>
        public void Reload()
        {
            lock (_configLock) { _configTime = DateTime.MinValue; }
        }

        /// <summary>
        /// 漏斗指标快照(试运行/监控接口用)
        /// </summary>
        public Dictionary<long, RuleMetrics> GetMetrics()
        {
            return _metrics.ToDictionary(t => t.Key, t => t.Value);
        }

        /// <summary>
        /// 试运行结果(§10.1工程化:干跑无副作用——不计指标/不记冷却/不执行动作)
        /// </summary>
        public class LinkageDryRunResult
        {
            /// <summary>规则是否存在且启用</summary>
            public bool Found { get; set; }
            /// <summary>规则名称</summary>
            public string RuleName { get; set; } = "";
            /// <summary>当前是否在生效时间窗内</summary>
            public bool InWindow { get; set; }
            /// <summary>条件变量取值快照(最新值缓存缺失的变量不出现)</summary>
            public Dictionary<string, double> Variables { get; set; } = new();
            /// <summary>条件表达式当前求值结果(空条件=恒真)</summary>
            public bool ConditionPass { get; set; }
            /// <summary>冷却剩余秒数(0=可执行)</summary>
            public int CooldownRemainSeconds { get; set; }
            /// <summary>动作类型(1命令2虚拟点位3通知4Webhook)</summary>
            public int ActionType { get; set; }
        }

        /// <summary>
        /// 规则试运行(按当前最新值评估时间窗/条件/冷却并返回变量快照,
        /// 不执行动作不计指标不记冷却——前端保存前预览命中情况)
        /// </summary>
        public LinkageDryRunResult DryRun(long rulesnowid, int triggerdeviceid)
        {
            var result = new LinkageDryRunResult();
            var rule = GetRules().FirstOrDefault(t => t.SnowId == rulesnowid);
            if (rule == null) return result;
            result.Found = true;
            result.RuleName = rule.RuleName ?? "";
            result.ActionType = rule.ActionType;
            result.InWindow = InTimeWindow(rule);
            if (rule.ConditionFormula.IsZxxNullOrEmpty())
            {
                result.ConditionPass = true;
            }
            else
            {
                result.Variables = BuildVariables(rule.ConditionFormula, triggerdeviceid);
                result.ConditionPass = ExpressoFormula.CalculateMultiple(rule.ConditionFormula, result.Variables);
            }
            if (_lastRun.TryGetValue(rule.SnowId, out var last))
            {
                double remain = Math.Max(1, rule.CooldownSeconds) - (DateTime.Now - last).TotalSeconds;
                result.CooldownRemainSeconds = remain > 0 ? (int)Math.Ceiling(remain) : 0;
            }
            return result;
        }

        #endregion

        #region 触发入口

        /// <summary>
        /// 点位变化触发(数据入库服务在最新值缓存更新后调用)
        /// </summary>
        public void OnPointChanged(int deviceid, HashSet<string> changedcodes)
        {
            try
            {
                foreach (var rule in GetRules().Where(t => t.TriggerType == 1))
                {
                    if (rule.TriggerDeviceId > 0 && rule.TriggerDeviceId != deviceid) continue;
                    if (!rule.TriggerParamCode.IsZxxNullOrEmpty() && !changedcodes.Contains(rule.TriggerParamCode)) continue;
                    Execute(rule, deviceid, "点位变化");
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"设备[{deviceid}]点位触发失败：{ex}", Service_CATEGORY);
            }
        }

        /// <summary>
        /// 告警产生/恢复触发(数据入库服务在告警回调中调用,完全屏蔽的告警不触发)
        /// </summary>
        public void OnAlarm(int deviceid, bool recovered)
        {
            try
            {
                int triggertype = recovered ? 3 : 2;
                foreach (var rule in GetRules().Where(t => t.TriggerType == triggertype))
                {
                    if (rule.TriggerDeviceId > 0 && rule.TriggerDeviceId != deviceid) continue;
                    Execute(rule, deviceid, recovered ? "告警恢复" : "告警产生");
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"设备[{deviceid}]告警触发失败：{ex}", Service_CATEGORY);
            }
        }

        /// <summary>
        /// 设备上下线触发(上线即时/离线经确认防抖后,与§7.5判定共用同一结果)
        /// </summary>
        public void OnDeviceState(int deviceid, bool online)
        {
            try
            {
                int triggertype = online ? 5 : 6;
                foreach (var rule in GetRules().Where(t => t.TriggerType == triggertype))
                {
                    if (rule.TriggerDeviceId > 0 && rule.TriggerDeviceId != deviceid) continue;
                    Execute(rule, deviceid, online ? "设备上线" : "设备离线");
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"设备[{deviceid}]上下线触发失败：{ex}", Service_CATEGORY);
            }
        }

        /// <summary>
        /// 定时型规则扫描循环(NewLife Cron解析,到点触发并推算下一次)
        /// </summary>
        private async Task CronLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(CronScanWindow, token);
                    var now = DateTime.Now;
                    foreach (var rule in GetRules().Where(t => t.TriggerType == 4 && !t.TriggerCron.IsZxxNullOrEmpty()))
                    {
                        try
                        {
                            var cron = new Cron();
                            if (!cron.Parse(rule.TriggerCron)) continue;
                            if (!_cronNext.TryGetValue(rule.SnowId, out var next))
                            {
                                _cronNext[rule.SnowId] = cron.GetNext(now);
                                continue;
                            }
                            if (now < next) continue;
                            _cronNext[rule.SnowId] = cron.GetNext(now);
                            Execute(rule, rule.TriggerDeviceId, "定时触发");
                        }
                        catch (Exception ex)
                        {
                            LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"规则[{rule.SnowId}]定时触发失败：{ex}", Service_CATEGORY);
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

        #region 条件与执行

        /// <summary>
        /// 执行单条规则:时间窗→条件表达式→冷却→动作(异常隔离不影响数据主流程),
        /// 执行审计只记一条汇总日志(§10.1:不逐节点入库)
        /// </summary>
        private void Execute(LinkageRule rule, int triggerdeviceid, string triggerinfo)
        {
            var metrics = _metrics.GetOrAdd(rule.SnowId, _ => new RuleMetrics());
            Interlocked.Increment(ref metrics.Matched);
            try
            {
                if (!InTimeWindow(rule))
                {
                    Interlocked.Increment(ref metrics.Failed);
                    return;
                }
                if (!rule.ConditionFormula.IsZxxNullOrEmpty())
                {
                    var variables = BuildVariables(rule.ConditionFormula, triggerdeviceid);
                    // 缺失变量求值异常返回false即不执行,语义安全
                    if (!ExpressoFormula.CalculateMultiple(rule.ConditionFormula, variables))
                    {
                        Interlocked.Increment(ref metrics.Failed);
                        return;
                    }
                }
                Interlocked.Increment(ref metrics.Passed);

                // 冷却防连发(点位高频变化下条件持续为真时按冷却窗限流)
                var now = DateTime.Now;
                if (_lastRun.TryGetValue(rule.SnowId, out var last) && (now - last).TotalSeconds < Math.Max(1, rule.CooldownSeconds)) return;
                _lastRun[rule.SnowId] = now;

                bool ok = RunAction(rule, triggerdeviceid);
                if (ok) Interlocked.Increment(ref metrics.ActionOk);
                else Interlocked.Increment(ref metrics.ActionFail);
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"规则[{rule.SnowId}:{rule.RuleName}]由[{triggerinfo},设备{triggerdeviceid}]触发,动作{(ok ? "成功" : "失败")}(matched={Interlocked.Read(ref metrics.Matched)},passed={Interlocked.Read(ref metrics.Passed)},actionok={Interlocked.Read(ref metrics.ActionOk)})", Service_CATEGORY);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref metrics.ActionFail);
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"规则[{rule.SnowId}:{rule.RuleName}]执行异常：{ex}", Service_CATEGORY);
            }
        }

        /// <summary>
        /// 时间窗判定(复用AlarmMaskTimeRange结构,空=全天生效;解析失败按全天处理)
        /// </summary>
        private static bool InTimeWindow(LinkageRule rule)
        {
            if (rule.TimeRanges.IsZxxNullOrEmpty()) return true;
            List<AlarmMaskTimeRange> ranges;
            try { ranges = rule.TimeRanges.ToObject<List<AlarmMaskTimeRange>>(); }
            catch { return true; }
            if (!ranges.IsZxxAny()) return true;
            var now = DateTime.Now;
            int day = (int)now.DayOfWeek;
            var time = now.TimeOfDay;
            foreach (var range in ranges)
            {
                if (range.Days.IsZxxAny() && !range.Days.Contains(day)) continue;
                if (TimeSpan.TryParse(range.Start, out var start) && TimeSpan.TryParse(range.End, out var end)
                    && time >= start && time <= end)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 抽取条件变量(标识符正则解析:d{设备ID}_{参数编码}跨设备取值,
        /// 裸参数编码取触发设备点位;true/false字面量排除,取不到值的跳过)
        /// </summary>
        private Dictionary<string, double> BuildVariables(string formula, int triggerdeviceid)
        {
            var result = new Dictionary<string, double>();
            foreach (Match match in Regex.Matches(formula, @"[A-Za-z_][A-Za-z0-9_]*"))
            {
                var name = match.Value;
                if (string.Equals(name, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "false", StringComparison.OrdinalIgnoreCase)) continue;
                if (result.ContainsKey(name)) continue;
                var cross = Regex.Match(name, @"^d(\d+)_(.+)$");
                var point = cross.Success
                    ? _latestService.GetLatest(int.Parse(cross.Groups[1].Value), cross.Groups[2].Value)
                    : (triggerdeviceid > 0 ? _latestService.GetLatest(triggerdeviceid, name) : null);
                if (point != null && point.Value.HasValue) result[name] = point.Value.Value;
            }
            return result;
        }

        #endregion

        #region 动作

        /// <summary>
        /// 动作分发(按类型解析JSON配置执行)
        /// </summary>
        private bool RunAction(LinkageRule rule, int triggerdeviceid)
        {
            switch (rule.ActionType)
            {
                case 1:
                    return RunCommand(rule, triggerdeviceid);
                case 2:
                    return RunVirtualPoint(rule, triggerdeviceid);
                case 3:
                    var notify = rule.ActionConfig?.ToObject<LinkageActionNotify>();
                    if (notify == null || notify.Content.IsZxxNullOrEmpty()) return false;
                    _notifyService.NotifyText($"规则[{rule.RuleName}]：{notify.Content}");
                    return true;
                default:
                    return RunWebhook(rule);
            }
        }

        /// <summary>
        /// 下发设备命令(ClassName过白名单;PluginGuid空=广播全部已加载插件,
        /// 协议插件对不支持的控制类型自行忽略;控制结果经既有PluginControlResultMessage链路回流审计)
        /// </summary>
        private bool RunCommand(LinkageRule rule, int triggerdeviceid)
        {
            var cfg = rule.ActionConfig?.ToObject<LinkageActionCommand>();
            if (cfg == null || cfg.ClassName.IsZxxNullOrEmpty() || cfg.ConContent.IsZxxNullOrEmpty()) return false;
            if (!CommandWhitelist.Contains(cfg.ClassName.Trim()))
            {
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"规则[{rule.SnowId}]的控制类型[{cfg.ClassName}]不在白名单,已拒绝下发", Service_CATEGORY);
                return false;
            }
            var deviceids = cfg.DeviceIds.IsZxxAny() ? cfg.DeviceIds : new List<int> { triggerdeviceid };
            deviceids = deviceids.Where(t => t > 0).Distinct().ToList();
            if (!deviceids.IsZxxAny()) return false;

            var message = new PluginMessage
            {
                MessageType = PluginMessageEnum.设备控制,
                MessageJson = new
                {
                    CommandId = SnowModel.Instance.NewId().ToString(),
                    ClassName = cfg.ClassName,
                    ConContent = cfg.ConContent,
                    DeviceIds = deviceids
                }.ToJson()
            };
            if (!cfg.PluginGuid.IsZxxNullOrEmpty())
            {
                _commandBus.Publish(new PluginCommandEvent(cfg.PluginGuid, message));
            }
            else
            {
                foreach (var guid in OperatorCommon.DicPlugins.Keys.ToList())
                {
                    _commandBus.Publish(new PluginCommandEvent(guid, message));
                }
            }
            return true;
        }

        /// <summary>
        /// 写虚拟点位(进最新值缓存与遥测管道,可被其他规则/告警公式引用)
        /// </summary>
        private bool RunVirtualPoint(LinkageRule rule, int triggerdeviceid)
        {
            var cfg = rule.ActionConfig?.ToObject<LinkageActionVirtualPoint>();
            if (cfg == null || cfg.ParamCode.IsZxxNullOrEmpty()) return false;
            int deviceid = cfg.DeviceId > 0 ? cfg.DeviceId : triggerdeviceid;
            if (deviceid <= 0) return false;
            var point = new TelemetryPoint
            {
                DeviceId = deviceid,
                ParamCode = cfg.ParamCode,
                ParamName = cfg.ParamCode,
                Ts = DateTime.UtcNow,
                Quality = 0
            };
            if (double.TryParse(cfg.ParamValue, out double value)) point.Value = value;
            else point.ValueStr = cfg.ParamValue;
            var points = new List<TelemetryPoint> { point };
            _latestService.Update(points);
            _telemetryService.Enqueue(points);
            return true;
        }

        /// <summary>
        /// 调用Webhook(POST JSON异步外发,失败只记日志)
        /// </summary>
        private bool RunWebhook(LinkageRule rule)
        {
            var cfg = rule.ActionConfig?.ToObject<LinkageActionWebhook>();
            if (cfg == null || cfg.Url.IsZxxNullOrEmpty()) return false;
            string body = cfg.Body.IsZxxNullOrEmpty()
                ? new { rule = rule.RuleName, time = DateTime.Now.ToDateTimeString() }.ToJson()
                : cfg.Body;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var content = new StringContent(body, Encoding.UTF8, "application/json");
                    using var response = await _http.PostAsync(cfg.Url, content);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"规则[{rule.SnowId}]Webhook外发失败：{ex.Message}", Service_CATEGORY);
                }
            });
            return true;
        }

        #endregion

        #region 规则加载

        /// <summary>
        /// 取启用中的规则(快照过期懒加载重建)
        /// </summary>
        private List<LinkageRule> GetRules()
        {
            EnsureConfig();
            return _rules;
        }

        /// <summary>
        /// 规则快照过期时整体重建(仅装载启用规则)
        /// </summary>
        private void EnsureConfig()
        {
            if (DateTime.Now - _configTime <= ConfigTtl) return;
            lock (_configLock)
            {
                if (DateTime.Now - _configTime <= ConfigTtl) return;
                try
                {
                    _rules = (LinkageRuleDAO.Instance.GetList() ?? new List<LinkageRule>())
                        .Where(t => t.IsEnable).ToList();
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"联动规则加载失败：{ex}", Service_CATEGORY);
                }
                _configTime = DateTime.Now;
            }
        }

        #endregion
    }
}
