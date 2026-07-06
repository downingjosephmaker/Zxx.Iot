using CenBoCommon.Zxx;
using IotModel;
using System.Collections.Concurrent;

namespace IotWebApi.Services
{
    /// <summary>
    /// 采集侧三级异常值过滤链(合理范围→变化幅度→连续异常容错,仅数值型且值变化时执行;
    /// 范围越界一律丢弃,幅度异常连续N次认定真实阶跃接受,防真实突变被永久过滤)
    /// </summary>
    public class ValueFilterService
    {
        /// <summary>
        /// 物模型配置缓存刷新周期(点表低频变更,过期懒加载重建)
        /// </summary>
        private static readonly TimeSpan ConfigTtl = TimeSpan.FromSeconds(60);

        /// <summary>
        /// 点位过滤运行时状态(前值+连续异常计数)
        /// </summary>
        private class FilterState
        {
            public double LastValue;
            public bool HasLast;
            public int AbnormalCount;
        }

        /// <summary>
        /// 点位过滤状态((设备,参数编码)→状态)
        /// </summary>
        private readonly ConcurrentDictionary<(int DeviceId, string ParamCode), FilterState> _states = new();

        /// <summary>
        /// 物模型配置快照(类型编码|参数编码→点表配置)
        /// </summary>
        private volatile Dictionary<string, DeviceTypeParam> _configs = new();

        /// <summary>
        /// 配置快照生成时间
        /// </summary>
        private DateTime _configTime = DateTime.MinValue;

        /// <summary>
        /// 配置重建锁
        /// </summary>
        private readonly object _configLock = new();

        /// <summary>
        /// 判定是否接受该采集值(true=接受,false=毛刺/越界丢弃)
        /// </summary>
        public bool Accept(string typecode, int deviceid, string paramcode, string paramvalue)
        {
            if (paramcode.IsZxxNullOrEmpty() || !double.TryParse(paramvalue, out double value)) return true;
            var config = GetConfig(typecode, paramcode);
            if (config == null || (!config.RangeFilterEnable && !config.AmplitudeFilterEnable)) return true;

            var state = _states.GetOrAdd((deviceid, paramcode), _ => new FilterState());
            lock (state)
            {
                // 值未变化不参与过滤(§7.2:仅在值变化时执行)
                if (state.HasLast && value == state.LastValue) return true;

                // 1.合理范围:越界丢弃(物理合法值域,不参与连续容错)
                if (config.RangeFilterEnable && (value < (double)config.ParamMinValue || value > (double)config.ParamMaxValue))
                {
                    return false;
                }

                // 2.变化幅度:单次跳变超限视为毛刺(绝对差与百分比任一超限即异常)
                bool abnormal = false;
                if (config.AmplitudeFilterEnable && state.HasLast)
                {
                    var diff = Math.Abs(value - state.LastValue);
                    if (config.ParamChangeValue > 0 && diff > (double)config.ParamChangeValue) abnormal = true;
                    if (!abnormal && config.MaxAmplitudePercent > 0 && state.LastValue != 0
                        && diff / Math.Abs(state.LastValue) * 100 > (double)config.MaxAmplitudePercent) abnormal = true;
                }

                if (!abnormal)
                {
                    state.LastValue = value;
                    state.HasLast = true;
                    state.AbnormalCount = 0;
                    return true;
                }

                // 3.连续异常容错:连续N次幅度异常认定为真实阶跃,接受该值
                if (config.ContinuousFilterEnable)
                {
                    state.AbnormalCount++;
                    int maxcount = config.MaxContinuousCount > 0 ? config.MaxContinuousCount : 3;
                    if (state.AbnormalCount >= maxcount)
                    {
                        state.LastValue = value;
                        state.HasLast = true;
                        state.AbnormalCount = 0;
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 取物模型点表配置(快照过期时整体重建)
        /// </summary>
        private DeviceTypeParam GetConfig(string typecode, string paramcode)
        {
            if (DateTime.Now - _configTime > ConfigTtl)
            {
                lock (_configLock)
                {
                    if (DateTime.Now - _configTime > ConfigTtl)
                    {
                        var configs = new Dictionary<string, DeviceTypeParam>();
                        var list = DeviceTypeParamDAO.Instance.GetList();
                        if (list.IsZxxAny())
                        {
                            foreach (var item in list)
                            {
                                configs.TryAdd($"{item.DeviceTypeCode}|{item.ParamCode}", item);
                            }
                        }
                        _configs = configs;
                        _configTime = DateTime.Now;
                    }
                }
            }
            return _configs.TryGetValue($"{typecode}|{paramcode}", out var config) ? config : null;
        }
    }
}
