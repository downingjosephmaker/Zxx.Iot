using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using System.Collections.Concurrent;

namespace IotWebApi.Services
{
    /// <summary>
    /// 点位生效采集策略(三级合并后的最终值)
    /// </summary>
    public class EffectiveCollectStrategy
    {
        /// <summary>
        /// 采集周期毫秒
        /// </summary>
        public int CollectCycleMs { get; set; }
        /// <summary>
        /// 采集cron表达式(非空时优先于采集周期)
        /// </summary>
        public string CollectCron { get; set; }
        /// <summary>
        /// 上报最大周期毫秒(0=不限,采集即上报)
        /// </summary>
        public int ReportCycleMs { get; set; }
    }

    /// <summary>
    /// 点位生效推送策略(三级合并后的最终值)
    /// </summary>
    public class EffectivePushStrategy
    {
        /// <summary>
        /// 推送模式(1=收到即报,2=变化上报,3=定时上报,4=变化上报+最大静默周期兜底)
        /// </summary>
        public int ReportMode { get; set; }
        /// <summary>
        /// 死区类型(0=严格不等,1=绝对死区,2=百分比死区)
        /// </summary>
        public int DeadbandType { get; set; }
        /// <summary>
        /// 死区值(绝对值或百分比)
        /// </summary>
        public decimal DeadbandValue { get; set; }
        /// <summary>
        /// 最小推送间隔毫秒(节流窗口,0=不节流)
        /// </summary>
        public int MinPushIntervalMs { get; set; }
        /// <summary>
        /// 最大静默周期毫秒(强制上报兜底)
        /// </summary>
        public int MaxSilentMs { get; set; }
        /// <summary>
        /// 关键属性点位集合(变化立即冲刷不参与合并节流)
        /// </summary>
        public HashSet<string> DebounceIgnoreKeys { get; set; } = new HashSet<string>();
    }

    /// <summary>
    /// 策略合并服务(collect_strategy/push_strategy三级挂靠,按点位>设备>产品优先级逐字段合并;
    /// 底层清单走EntityCache,合并结果本地缓存,收到StrategyChangedEvent整体失效实现热重载)
    /// </summary>
    public class StrategyMergeService
    {
        private const string Service_CATEGORY = "策略合并服务";

        /// <summary>
        /// 默认采集周期毫秒(三级均未配置时生效)
        /// </summary>
        public const int DefaultCollectCycleMs = 5000;

        /// <summary>
        /// 默认推送模式(4=变化上报+最大静默周期兜底)
        /// </summary>
        public const int DefaultReportMode = 4;

        /// <summary>
        /// 默认最大静默周期毫秒(15分钟,曲线连续性兜底)
        /// </summary>
        public const int DefaultMaxSilentMs = 900000;

        /// <summary>
        /// 采集策略合并结果缓存(键:产品编码|设备ID|参数编码)
        /// </summary>
        private readonly ConcurrentDictionary<string, EffectiveCollectStrategy> _collectCache = new();

        /// <summary>
        /// 推送策略合并结果缓存(键:产品编码|设备ID|参数编码)
        /// </summary>
        private readonly ConcurrentDictionary<string, EffectivePushStrategy> _pushCache = new();

        #region 策略取用

        /// <summary>
        /// 取点位生效采集策略(合并结果有缓存,策略变更事件后重建)
        /// </summary>
        public EffectiveCollectStrategy GetCollectStrategy(string typecode, int deviceid, string paramcode)
        {
            return _collectCache.GetOrAdd($"{typecode}|{deviceid}|{paramcode}", _ => MergeCollect(typecode, deviceid, paramcode));
        }

        /// <summary>
        /// 取点位生效推送策略(合并结果有缓存,策略变更事件后重建)
        /// </summary>
        public EffectivePushStrategy GetPushStrategy(string typecode, int deviceid, string paramcode)
        {
            return _pushCache.GetOrAdd($"{typecode}|{deviceid}|{paramcode}", _ => MergePush(typecode, deviceid, paramcode));
        }

        /// <summary>
        /// 清空合并结果缓存(策略变更后由事件处理器调用,下次取用时重新合并,无需重启插件)
        /// </summary>
        public void Reload()
        {
            _collectCache.Clear();
            _pushCache.Clear();
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "策略合并缓存已清空,下次取用时重新合并", Service_CATEGORY);
        }

        #endregion

        #region 三级合并

        /// <summary>
        /// 合并采集策略(点位>设备>产品逐字段回落,三级均未设置用默认值)
        /// </summary>
        private static EffectiveCollectStrategy MergeCollect(string typecode, int deviceid, string paramcode)
        {
            var list = CollectStrategyDAO.Instance.GetList();
            var devicekey = deviceid.ToString();
            var product = list.FirstOrDefault(t => t.ScopeType == 1 && t.ScopeId == typecode);
            var device = list.FirstOrDefault(t => t.ScopeType == 2 && t.ScopeId == devicekey);
            var point = list.FirstOrDefault(t => t.ScopeType == 3 && t.ScopeId == devicekey && t.ParamCode == paramcode);

            return new EffectiveCollectStrategy
            {
                CollectCycleMs = point?.CollectCycleMs ?? device?.CollectCycleMs ?? product?.CollectCycleMs ?? DefaultCollectCycleMs,
                CollectCron = FirstNotEmpty(point?.CollectCron, device?.CollectCron, product?.CollectCron),
                ReportCycleMs = point?.ReportCycleMs ?? device?.ReportCycleMs ?? product?.ReportCycleMs ?? 0
            };
        }

        /// <summary>
        /// 合并推送策略(点位>设备>产品逐字段回落,三级均未设置用默认值)
        /// </summary>
        private static EffectivePushStrategy MergePush(string typecode, int deviceid, string paramcode)
        {
            var list = PushStrategyDAO.Instance.GetList();
            var devicekey = deviceid.ToString();
            var product = list.FirstOrDefault(t => t.ScopeType == 1 && t.ScopeId == typecode);
            var device = list.FirstOrDefault(t => t.ScopeType == 2 && t.ScopeId == devicekey);
            var point = list.FirstOrDefault(t => t.ScopeType == 3 && t.ScopeId == devicekey && t.ParamCode == paramcode);

            var ignorekeys = FirstNotEmpty(point?.DebounceIgnoreKeys, device?.DebounceIgnoreKeys, product?.DebounceIgnoreKeys);
            return new EffectivePushStrategy
            {
                ReportMode = point?.ReportMode ?? device?.ReportMode ?? product?.ReportMode ?? DefaultReportMode,
                DeadbandType = point?.DeadbandType ?? device?.DeadbandType ?? product?.DeadbandType ?? 0,
                DeadbandValue = point?.DeadbandValue ?? device?.DeadbandValue ?? product?.DeadbandValue ?? 0,
                MinPushIntervalMs = point?.MinPushIntervalMs ?? device?.MinPushIntervalMs ?? product?.MinPushIntervalMs ?? 0,
                MaxSilentMs = point?.MaxSilentMs ?? device?.MaxSilentMs ?? product?.MaxSilentMs ?? DefaultMaxSilentMs,
                DebounceIgnoreKeys = ignorekeys.IsZxxNullOrEmpty()
                    ? new HashSet<string>()
                    : ignorekeys.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet()
            };
        }

        /// <summary>
        /// 逐级取第一个非空字符串(与数值字段的??回落语义一致)
        /// </summary>
        private static string FirstNotEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!value.IsZxxNullOrEmpty()) return value;
            }
            return "";
        }

        #endregion
    }
}
