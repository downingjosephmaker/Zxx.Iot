using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using System.Collections.Concurrent;

namespace IotWebApi.Services
{
    /// <summary>
    /// 告警触发/恢复信息(引擎判定成立后经FireHandler交数据入库服务落库+推送)
    /// </summary>
    public class AlarmFireInfo
    {
        /// <summary>设备ID</summary>
        public int DeviceId { get; set; }

        /// <summary>事件类型(设备告警/告警恢复)</summary>
        public string EventType { get; set; } = "";

        /// <summary>告警等级(取自AlarmConfig字典)</summary>
        public string AlarmGrade { get; set; } = "";

        /// <summary>告警内容(文字模板渲染结果)</summary>
        public string Content { get; set; } = "";

        /// <summary>触发值快照(参数=值,入EventValue)</summary>
        public string ValueText { get; set; } = "";

        /// <summary>是否通知(北向/短信等通知通道)</summary>
        public bool IsNote { get; set; }

        /// <summary>规则ID(§9.2生命周期去重键:同设备同规则同时仅一个活动告警;离线告警恒为0)</summary>
        public long RuleId { get; set; }

        /// <summary>报警类型(AlarmConfig字典AlarmType,入EventAlarm)</summary>
        public string AlarmType { get; set; } = "";

        /// <summary>事件类别(AlarmConfig字典EventType,入EventAlarm.EventType;
        /// 现有统计口径以"离线"排除离线告警,离线告警未配置字典时默认"离线")</summary>
        public string AlarmEventType { get; set; } = "";

        /// <summary>触发公式(入EventAlarm拓展快照)</summary>
        public string Formula { get; set; } = "";

        /// <summary>首参数编码(入EventAlarm拓展快照)</summary>
        public string ParamCode { get; set; } = "";
    }

    /// <summary>
    /// 告警引擎(§9:平台侧唯一裁决者,基于最新值缓存评估两级告警规则——
    /// 设备级DeviceAlarmConfig优先(TypeSnowId标记覆盖关系),未覆盖回落产品级DeviceTypeAlarmConfig,
    /// 防抖参数经AlarmConfigId取自AlarmConfig字典;三型防抖:次数型(连续/累计+第一次/最后一次)、
    /// 时长型(疑似态持续确认,期间恢复静默取消)、屏蔽型(直接丢弃,后续由alarm_mask取代);
    /// 告警事件走独立通道不经推送节流,落库/推送经FireHandler回调数据入库服务)
    /// </summary>
    public class AlarmEngineService : IHostedService
    {
        private const string Service_CATEGORY = "告警引擎";

        /// <summary>
        /// 规则缓存刷新周期(配置低频变更,过期懒加载重建)
        /// </summary>
        private static readonly TimeSpan ConfigTtl = TimeSpan.FromSeconds(60);

        /// <summary>
        /// 扫描周期(时长型确认与次数型"最后一次"补发粒度)
        /// </summary>
        private static readonly TimeSpan ScanWindow = TimeSpan.FromSeconds(2);

        /// <summary>
        /// 合并后的有效告警规则
        /// </summary>
        private class EffectiveRule
        {
            public long RuleId;
            public List<string> ParamCodes = new();
            public string Formula = "";
            public string RecoverFormula = "";   // 恢复公式(高低水位滞回,空=触发公式取反)
            public string RestrainFormula = "";  // 抑制公式(联锁抑制,false则抑制,空=不抑制)
            public string TextTemplate = "";
            public bool IsNote;
            public string AlarmGrade = "";
            public string AlarmType = "";      // 报警类型(字典AlarmType,入EventAlarm)
            public string EventCategory = "";  // 事件类别(字典EventType,入EventAlarm.EventType)
            public DebounceTypeEnum DebounceType = DebounceTypeEnum.次数型;
            public bool IsDebounce;
            public int DebounceSeconds = 60;
            public DebounceModeEnum DebounceMode = DebounceModeEnum.累计;
            public int DebounceCount = 3;
            public DebounceActionEnum DebounceAction = DebounceActionEnum.第一次;
            public int AlarmConfirmSeconds;
        }

        /// <summary>
        /// 点位告警运行时状态((设备,规则)粒度)
        /// </summary>
        private class AlarmState
        {
            public bool Active;                 // 当前告警是否成立(边沿检测)
            public DateTime WindowStart;        // 次数型窗口起点
            public DateTime LastHitTime;        // 次数型最近一次触发
            public int HitCount;                // 次数型窗口内计数
            public bool FiredInWindow;          // 次数型"第一次"窗口内已发标记
            public AlarmFireInfo? Buffered;     // 次数型"最后一次"缓冲的末条
            public DateTime BufferDue;          // 缓冲补发到期时刻
            public DateTime SuspectSince;       // 时长型疑似态起点
            public bool Suspecting;             // 时长型疑似中
            public AlarmFireInfo? SuspectInfo;  // 时长型疑似缓冲
        }

        /// <summary>设备级规则(设备ID→规则)</summary>
        private volatile Dictionary<int, List<EffectiveRule>> _deviceRules = new();

        /// <summary>产品级规则(类型编码→规则)</summary>
        private volatile Dictionary<string, List<EffectiveRule>> _typeRules = new();

        /// <summary>设备已覆盖的产品规则ID(设备ID→被覆盖TypeSnowId集合)</summary>
        private volatile Dictionary<int, HashSet<long>> _overrides = new();

        /// <summary>设备→类型编码映射(产品级规则回落用,随规则快照重建)</summary>
        private volatile Dictionary<int, string> _deviceTypeMap = new();

        private DateTime _configTime = DateTime.MinValue;
        private readonly object _configLock = new();

        /// <summary>屏蔽型防抖废弃提示已输出(每进程一次防刷屏)</summary>
        private bool _maskDeprecationLogged;

        /// <summary>((设备,规则)→运行时状态)</summary>
        private readonly ConcurrentDictionary<(int DeviceId, long RuleId), AlarmState> _states = new();

        /// <summary>
        /// 最新值缓存(公式取数来源)
        /// </summary>
        private readonly TelemetryLatestService _latestService;

        /// <summary>
        /// 告警成立/恢复回调(数据入库服务注册:EventSignal落库+SignalR alarm组推送+通知通道)
        /// </summary>
        public Action<List<AlarmFireInfo>>? FireHandler { get; set; }

        private CancellationTokenSource? _cts;
        private Task? _scanTask;

        public AlarmEngineService(TelemetryLatestService latestService)
        {
            _latestService = latestService;
        }

        #region 服务生命周期

        /// <summary>
        /// 启动扫描循环
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _scanTask = Task.Run(() => ScanLoopAsync(_cts.Token));
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "告警引擎已启动", Service_CATEGORY);
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
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "告警引擎已停止", Service_CATEGORY);
        }

        /// <summary>
        /// 清空规则缓存(StrategyChangedEvent等配置变更时热重载)
        /// </summary>
        public void Reload()
        {
            lock (_configLock) { _configTime = DateTime.MinValue; }
        }

        #endregion

        #region 规则加载与合并

        /// <summary>
        /// 取设备的有效规则(设备级优先,产品级中被TypeSnowId覆盖的剔除)
        /// </summary>
        private List<EffectiveRule> GetRules(int deviceid)
        {
            EnsureConfig();
            var result = new List<EffectiveRule>();
            if (_deviceRules.TryGetValue(deviceid, out var devrules)) result.AddRange(devrules);
            _deviceTypeMap.TryGetValue(deviceid, out var typecode);
            if (_typeRules.TryGetValue(typecode ?? "", out var typerules))
            {
                _overrides.TryGetValue(deviceid, out var overridden);
                result.AddRange(typerules.Where(t => overridden == null || !overridden.Contains(t.RuleId)));
            }
            return result;
        }

        /// <summary>
        /// 规则快照过期时整体重建(设备级/产品级/字典三表联装)
        /// </summary>
        private void EnsureConfig()
        {
            if (DateTime.Now - _configTime <= ConfigTtl) return;
            lock (_configLock)
            {
                if (DateTime.Now - _configTime <= ConfigTtl) return;
                try
                {
                    var dict = (AlarmConfigDAO.Instance.GetList() ?? new List<AlarmConfig>())
                        .ToDictionary(t => t.Id, t => t);

                    // DebounceType=3屏蔽型已废弃(§9.4):按等价"永久+完全屏蔽"的alarm_mask规则执行,提示迁移
                    if (!_maskDeprecationLogged)
                    {
                        var deprecated = dict.Values.Where(t => t.IsDebounce && t.DebounceType == DebounceTypeEnum.屏蔽).ToList();
                        if (deprecated.IsZxxAny())
                        {
                            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                                $"检测到{deprecated.Count}个告警类型使用已废弃的屏蔽型防抖:[{string.Join(",", deprecated.Select(t => $"{t.Id}:{t.AlarmType}"))}],新配置请迁移到alarm_mask屏蔽规则", Service_CATEGORY);
                            _maskDeprecationLogged = true;
                        }
                    }
                    var typelist = DeviceTypeAlarmConfigDAO.Instance.GetList() ?? new List<DeviceTypeAlarmConfig>();
                    var devlist = DeviceAlarmConfigDAO.Instance.GetList() ?? new List<DeviceAlarmConfig>();

                    var typerules = new Dictionary<string, List<EffectiveRule>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var item in typelist.Where(t => t.IsFormulaEnable == 1 && !t.JisuanFormula.IsZxxNullOrEmpty()))
                    {
                        var rule = BuildRule(item.SnowId, item.ParamCode, item.JisuanFormula, item.TextTemplate,
                            item.IsNote, dict.TryGetValue(item.AlarmConfigId, out var d1) ? d1 : null);
                        if (rule == null) continue;
                        rule.RecoverFormula = item.RecoverFormula ?? "";
                        rule.RestrainFormula = item.RestrainFormula ?? "";
                        if (!typerules.TryGetValue(item.DeviceTypeCode ?? "", out var list))
                        {
                            list = new List<EffectiveRule>();
                            typerules[item.DeviceTypeCode ?? ""] = list;
                        }
                        list.Add(rule);
                    }

                    var devrules = new Dictionary<int, List<EffectiveRule>>();
                    var overrides = new Dictionary<int, HashSet<long>>();
                    foreach (var item in devlist.Where(t => t.IsFormulaEnable == 1 && !t.JisuanFormula.IsZxxNullOrEmpty()))
                    {
                        var rule = BuildRule(item.SnowId, item.ParamCode, item.JisuanFormula, item.TextTemplate,
                            item.IsNote, dict.TryGetValue(item.AlarmConfigId, out var d2) ? d2 : null);
                        if (rule == null) continue;
                        rule.RecoverFormula = item.RecoverFormula ?? "";
                        rule.RestrainFormula = item.RestrainFormula ?? "";
                        if (!devrules.TryGetValue(item.DeviceId, out var list))
                        {
                            list = new List<EffectiveRule>();
                            devrules[item.DeviceId] = list;
                        }
                        list.Add(rule);
                        // 设备行由产品行派生(TypeSnowId>0)即视为覆盖,对应产品规则对该设备失效
                        if (item.TypeSnowId > 0)
                        {
                            if (!overrides.TryGetValue(item.DeviceId, out var set))
                            {
                                set = new HashSet<long>();
                                overrides[item.DeviceId] = set;
                            }
                            set.Add(item.TypeSnowId);
                        }
                    }

                    var devices = DeviceInfoDAO.Instance.GetList()?.Cast<DeviceInfo>().ToList() ?? new List<DeviceInfo>();
                    _deviceTypeMap = devices
                        .GroupBy(t => t.DeviceId)
                        .ToDictionary(g => g.Key, g => g.First().DeviceTypeCode ?? "");

                    // 点表告警源(§9.6:is_alarm_source=1的点位=设备自报告警状态位,
                    // 自动合成"非0即告警"规则并入产品级,等级/通知/防抖/模板经alarm_config_id继承,
                    // 规则ID取负点位主键防与配置行冲突;平台引擎是唯一裁决者,协议层不建第二套告警逻辑)
                    var alarmsources = (DeviceTypeParamDAO.Instance.GetList() ?? new List<DeviceTypeParamEntity>())
                        .Where(t => t.IsAlarmSource && t.AlarmConfigId > 0 && !t.ParamCode.IsZxxNullOrEmpty()).ToList();
                    foreach (var item in alarmsources)
                    {
                        dict.TryGetValue(item.AlarmConfigId, out var srcdict);
                        var rule = BuildRule(-item.SnowId, item.ParamCode, $"{item.ParamCode} > 0",
                            srcdict?.TextTemplate ?? "", srcdict?.IsNote ?? false, srcdict);
                        if (rule == null) continue;
                        if (!typerules.TryGetValue(item.DeviceTypeCode ?? "", out var srclist))
                        {
                            srclist = new List<EffectiveRule>();
                            typerules[item.DeviceTypeCode ?? ""] = srclist;
                        }
                        srclist.Add(rule);
                    }

                    // 离线告警字典行(§9.6:AlarmType或EventType含"离线"即认定,等级/通知/确认时长由此继承)
                    _offlineConfig = dict.Values.FirstOrDefault(t =>
                        t.AlarmType?.Contains("离线") == true || t.EventType?.Contains("离线") == true);

                    _typeRules = typerules;
                    _deviceRules = devrules;
                    _overrides = overrides;
                    _configTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"告警规则加载失败：{ex}", Service_CATEGORY);
                    _configTime = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// 装配单条有效规则(防抖参数从字典继承;字典缺失按无防抖直发处理)
        /// </summary>
        private static EffectiveRule? BuildRule(long ruleid, string paramcode, string formula,
            string template, bool isnote, AlarmConfig? dict)
        {
            var codes = (paramcode ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            if (!codes.IsZxxAny()) return null;
            var rule = new EffectiveRule
            {
                RuleId = ruleid,
                ParamCodes = codes,
                Formula = formula,
                TextTemplate = template ?? "",
                IsNote = isnote
            };
            if (dict != null)
            {
                rule.AlarmGrade = dict.AlarmGrade ?? "";
                rule.AlarmType = dict.AlarmType ?? "";
                rule.EventCategory = dict.EventType ?? "";
                rule.IsDebounce = dict.IsDebounce;
                rule.DebounceType = dict.DebounceType;
                rule.DebounceSeconds = dict.DebounceSeconds;
                rule.DebounceMode = dict.DebounceMode;
                rule.DebounceCount = dict.DebounceCount;
                rule.DebounceAction = dict.DebounceAction;
                rule.AlarmConfirmSeconds = dict.AlarmConfirmSeconds;
            }
            return rule;
        }

        #endregion

        #region 评估入口

        /// <summary>
        /// 评估设备的告警规则(数据入库管道在最新值缓存更新后调用;
        /// changedcodes为本轮变化参数,只评估涉及这些参数的规则)
        /// </summary>
        public void Evaluate(int deviceid, HashSet<string> changedcodes)
        {
            try
            {
                var rules = GetRules(deviceid);
                if (!rules.IsZxxAny()) return;
                var fires = new List<AlarmFireInfo>();
                foreach (var rule in rules)
                {
                    if (!rule.ParamCodes.Any(changedcodes.Contains)) continue;
                    EvaluateRule(deviceid, rule, fires);
                }
                Dispatch(fires);
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"设备[{deviceid}]告警评估失败：{ex}", Service_CATEGORY);
            }
        }

        /// <summary>
        /// 评估单条规则:公式代入最新值→边沿检测→防抖流水线
        /// </summary>
        private void EvaluateRule(int deviceid, EffectiveRule rule, List<AlarmFireInfo> fires)
        {
            var variables = new Dictionary<string, double>();
            var valuetexts = new List<string>();
            foreach (var code in rule.ParamCodes)
            {
                var point = _latestService.GetLatest(deviceid, code);
                if (point == null || !point.Value.HasValue) return; // 值不齐不评估
                variables[code] = point.Value.Value;
                valuetexts.Add($"{code}={point.Value.Value}");
            }
            // 经IotModel的公式引擎求值(规避DynamicExpresso.Core与本地项目的程序集重名冲突;公式异常返回false不触发)
            bool triggered = ExpressoFormula.CalculateMultiple(rule.Formula, variables);

            var state = _states.GetOrAdd((deviceid, rule.RuleId), _ => new AlarmState());
            lock (state)
            {
                // 高低水位滞回(§9.3):恢复公式非空时,已告警态的解除以"恢复公式为真"为准,
                // 触发/恢复阈值成对配置(80触发/60恢复)防临界值震荡;空则退化为触发公式取反
                bool active;
                if (state.Active && !rule.RecoverFormula.IsZxxNullOrEmpty())
                {
                    active = !ExpressoFormula.CalculateMultiple(rule.RecoverFormula, variables);
                }
                else
                {
                    active = triggered;
                }

                if (active == state.Active) return; // 无边沿不动作
                if (!active)
                {
                    // 下降沿:恢复——时长型疑似静默取消不产生事件,已成立告警发恢复
                    bool wassuspecting = state.Suspecting;
                    state.Suspecting = false;
                    state.SuspectInfo = null;
                    state.Active = false;
                    if (!wassuspecting)
                    {
                        fires.Add(BuildFire(deviceid, rule, "告警恢复", valuetexts));
                    }
                    return;
                }

                // 联锁抑制(§9.3):告警命中后再评估抑制公式(可引用同设备其他点位,
                // 如"设备运行中才允许低温告警"),false则抑制本次触发且不置告警态
                if (!rule.RestrainFormula.IsZxxNullOrEmpty())
                {
                    var restrainvars = new Dictionary<string, double>(variables);
                    foreach (var code in ExtractRestrainCodes(rule.RestrainFormula, deviceid))
                    {
                        if (restrainvars.ContainsKey(code.Key)) continue;
                        restrainvars[code.Key] = code.Value;
                    }
                    if (!ExpressoFormula.CalculateMultiple(rule.RestrainFormula, restrainvars)) return;
                }

                // 上升沿:进防抖流水线
                var info = BuildFire(deviceid, rule, "设备告警", valuetexts);
                if (!rule.IsDebounce)
                {
                    state.Active = true;
                    fires.Add(info);
                    return;
                }
                switch (rule.DebounceType)
                {
                    case DebounceTypeEnum.屏蔽:
                        // 屏蔽型(deprecated§9.4):按等价"永久+完全屏蔽"的alarm_mask规则执行——
                        // 直接丢弃不入库,标active防重复评估抖动;新配置一律使用alarm_mask
                        state.Active = true;
                        return;
                    case DebounceTypeEnum.时长型:
                        // 疑似态:持续满AlarmConfirmSeconds且未恢复才由扫描循环补发
                        state.Active = true;
                        state.Suspecting = true;
                        state.SuspectSince = DateTime.Now;
                        state.SuspectInfo = info;
                        return;
                    default:
                        EvaluateCountDebounce(state, rule, info, fires);
                        return;
                }
            }
        }

        /// <summary>
        /// 次数型防抖(连续:间隔超窗重置计数;累计:固定窗口;
        /// 达阈值后第一次=立即发且窗口内屏蔽后续,最后一次=缓冲至窗口末补发末条)
        /// </summary>
        private static void EvaluateCountDebounce(AlarmState state, EffectiveRule rule, AlarmFireInfo info, List<AlarmFireInfo> fires)
        {
            var now = DateTime.Now;
            state.Active = true;
            if (rule.DebounceMode == DebounceModeEnum.连续)
            {
                if (state.HitCount > 0 && (now - state.LastHitTime).TotalSeconds > rule.DebounceSeconds)
                {
                    state.HitCount = 0;
                    state.FiredInWindow = false;
                    state.Buffered = null;
                }
                if (state.HitCount == 0) state.WindowStart = now;
            }
            else
            {
                if (state.HitCount == 0 || (now - state.WindowStart).TotalSeconds > rule.DebounceSeconds)
                {
                    state.WindowStart = now;
                    state.HitCount = 0;
                    state.FiredInWindow = false;
                    state.Buffered = null;
                }
            }
            state.LastHitTime = now;
            state.HitCount++;
            if (state.HitCount < rule.DebounceCount) return;

            if (rule.DebounceAction == DebounceActionEnum.第一次)
            {
                if (state.FiredInWindow) return;
                state.FiredInWindow = true;
                fires.Add(info);
                return;
            }
            // 最后一次:缓冲末条,窗口末由扫描循环补发
            state.Buffered = info;
            state.BufferDue = state.WindowStart.AddSeconds(rule.DebounceSeconds);
        }

        /// <summary>
        /// 抽取抑制公式引用的同设备其他点位当前值(标识符正则解析,true/false字面量排除;
        /// 取不到值的标识符跳过——公式对缺失变量求值异常返回false即抑制,语义安全)
        /// </summary>
        private Dictionary<string, double> ExtractRestrainCodes(string formula, int deviceid)
        {
            var result = new Dictionary<string, double>();
            foreach (System.Text.RegularExpressions.Match match in
                System.Text.RegularExpressions.Regex.Matches(formula, @"[A-Za-z_][A-Za-z0-9_]*"))
            {
                var code = match.Value;
                if (string.Equals(code, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(code, "false", StringComparison.OrdinalIgnoreCase)) continue;
                if (result.ContainsKey(code)) continue;
                var point = _latestService.GetLatest(deviceid, code);
                if (point != null && point.Value.HasValue) result[code] = point.Value.Value;
            }
            return result;
        }

        /// <summary>
        /// 构造告警/恢复信息(文字模板渲染:{参数编码}替换为当前值,空模板用默认格式)
        /// </summary>
        private AlarmFireInfo BuildFire(int deviceid, EffectiveRule rule, string eventtype, List<string> valuetexts)
        {
            string valuetext = string.Join(",", valuetexts);
            string content = rule.TextTemplate;
            if (content.IsZxxNullOrEmpty())
            {
                content = eventtype == "告警恢复"
                    ? $"公式[{rule.Formula}]恢复正常({valuetext})"
                    : $"公式[{rule.Formula}]触发告警({valuetext})";
            }
            else
            {
                foreach (var text in valuetexts)
                {
                    var pair = text.Split('=');
                    if (pair.Length == 2) content = content.Replace($"{{{pair[0]}}}", pair[1]);
                }
                if (eventtype == "告警恢复") content = $"[恢复]{content}";
            }
            return new AlarmFireInfo
            {
                DeviceId = deviceid,
                EventType = eventtype,
                AlarmGrade = rule.AlarmGrade,
                Content = content,
                ValueText = valuetext,
                IsNote = rule.IsNote && eventtype != "告警恢复",
                RuleId = rule.RuleId,
                AlarmType = rule.AlarmType,
                AlarmEventType = rule.EventCategory,
                Formula = rule.Formula,
                ParamCode = rule.ParamCodes.FirstOrDefault() ?? ""
            };
        }

        #endregion

        #region 离线告警(§9.6:与上下线判定共用同一结果)

        /// <summary>
        /// 离线告警字典行(AlarmType/EventType含"离线"的告警类型,未配置时按默认等级/不通知处理)
        /// </summary>
        private volatile AlarmConfig? _offlineConfig;

        /// <summary>
        /// 设备离线告警活动状态(边沿检测:已发离线告警的设备恢复时才发恢复事件)
        /// </summary>
        private readonly ConcurrentDictionary<int, bool> _offlineActive = new();

        /// <summary>
        /// 离线确认时长(上下线判定服务经此读离线字典行的AlarmConfirmSeconds——
        /// 时长型防抖的典型用例;字典缺失或非正数回落判定服务兜底值)
        /// </summary>
        public int GetOfflineConfirmSeconds()
        {
            EnsureConfig();
            var cfg = _offlineConfig;
            return cfg != null && cfg.AlarmConfirmSeconds > 0 ? cfg.AlarmConfirmSeconds : OfflineDebounceService.ConfirmSeconds;
        }

        /// <summary>
        /// 设备离线告警(§7.5/§9.6:疑似离线已由上下线判定服务按确认时长完成时长型防抖,
        /// 此处共用该判定结果直接成告警不重复防抖;经FireHandler走屏蔽/落库/通知同一链路)
        /// </summary>
        public void FireOffline(int deviceid, string reason)
        {
            try
            {
                EnsureConfig();
                if (_offlineActive.TryGetValue(deviceid, out bool active) && active) return; // 无边沿不重复告警
                _offlineActive[deviceid] = true;
                var cfg = _offlineConfig;
                string content = cfg != null && !cfg.TextTemplate.IsZxxNullOrEmpty() ? cfg.TextTemplate : "设备通信中断离线";
                Dispatch(new List<AlarmFireInfo>
                {
                    new AlarmFireInfo
                    {
                        DeviceId = deviceid,
                        EventType = "设备告警",
                        AlarmGrade = cfg?.AlarmGrade ?? "",
                        Content = $"{content}({reason})",
                        ValueText = "",
                        IsNote = cfg?.IsNote ?? false,
                        RuleId = 0,
                        AlarmType = cfg?.AlarmType ?? "",
                        AlarmEventType = cfg != null && !cfg.EventType.IsZxxNullOrEmpty() ? cfg.EventType : "离线"
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"设备[{deviceid}]离线告警失败：{ex}", Service_CATEGORY);
            }
        }

        /// <summary>
        /// 设备离线告警恢复(上线判定即时生效;仅对已发离线告警的设备发恢复,
        /// 恢复事件不参与屏蔽也不外发通知)
        /// </summary>
        public void FireOfflineRecover(int deviceid)
        {
            try
            {
                if (!_offlineActive.TryRemove(deviceid, out bool active) || !active) return; // 未发过离线告警
                var cfg = _offlineConfig;
                Dispatch(new List<AlarmFireInfo>
                {
                    new AlarmFireInfo
                    {
                        DeviceId = deviceid,
                        EventType = "告警恢复",
                        AlarmGrade = cfg?.AlarmGrade ?? "",
                        Content = "[恢复]设备通信恢复上线",
                        ValueText = "",
                        IsNote = false,
                        RuleId = 0,
                        AlarmType = cfg?.AlarmType ?? "",
                        AlarmEventType = cfg != null && !cfg.EventType.IsZxxNullOrEmpty() ? cfg.EventType : "离线"
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"设备[{deviceid}]离线恢复失败：{ex}", Service_CATEGORY);
            }
        }

        #endregion

        #region 补发扫描

        /// <summary>
        /// 扫描主循环(时长型疑似确认到期补发;次数型"最后一次"窗口末补发)
        /// </summary>
        private async Task ScanLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(ScanWindow, token);
                    var now = DateTime.Now;
                    var fires = new List<AlarmFireInfo>();
                    foreach (var pair in _states)
                    {
                        var state = pair.Value;
                        lock (state)
                        {
                            if (state.Suspecting && state.SuspectInfo != null)
                            {
                                var rules = _deviceRules.TryGetValue(pair.Key.DeviceId, out var list)
                                    ? list.FirstOrDefault(t => t.RuleId == pair.Key.RuleId) : null;
                                rules ??= _typeRules.Values.SelectMany(t => t).FirstOrDefault(t => t.RuleId == pair.Key.RuleId);
                                int confirm = rules?.AlarmConfirmSeconds ?? 0;
                                if ((now - state.SuspectSince).TotalSeconds >= Math.Max(1, confirm))
                                {
                                    fires.Add(state.SuspectInfo);
                                    state.Suspecting = false;
                                    state.SuspectInfo = null;
                                }
                            }
                            if (state.Buffered != null && now >= state.BufferDue)
                            {
                                fires.Add(state.Buffered);
                                state.Buffered = null;
                                state.FiredInWindow = true;
                            }
                        }
                    }
                    Dispatch(fires);
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

        /// <summary>
        /// 调度回调(落库/推送由数据入库服务承接,异常只记日志)
        /// </summary>
        private void Dispatch(List<AlarmFireInfo> fires)
        {
            if (!fires.IsZxxAny()) return;
            var handler = FireHandler;
            if (handler == null) return;
            try
            {
                handler(fires);
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"告警回调{fires.Count}条失败：{ex}", Service_CATEGORY);
            }
        }

        #endregion
    }
}
