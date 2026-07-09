using CenBoCommon.Zxx;
using IotLog;
using IotModel;

namespace IotWebApi.Services
{
    /// <summary>
    /// 屏蔽裁决结果
    /// </summary>
    public enum AlarmMaskVerdict
    {
        /// <summary>不屏蔽,正常入库+通知</summary>
        通过 = 0,
        /// <summary>完全屏蔽,不入库不通知</summary>
        完全屏蔽 = 1,
        /// <summary>静默,入库打标不通知(默认动作)</summary>
        静默 = 2,
        /// <summary>降级,改写等级后正常入库+通知</summary>
        降级 = 3,
    }

    /// <summary>
    /// 告警屏蔽引擎(§9.4:在"告警产生之后、入库通知之前"过滤;
    /// 六种屏蔽对象(全局/单位/建筑/设备类型/单设备/告警等级)×三种模式(永久/一次性/周期窗)×
    /// 三种动作(完全屏蔽/静默/降级);ExpireAt到期自动失效防"忘了解除";
    /// 多条规则命中时取动作最重者(完全屏蔽>静默>降级);告警恢复事件不参与屏蔽)
    /// </summary>
    public class AlarmMaskService
    {
        private const string Service_CATEGORY = "告警屏蔽引擎";

        /// <summary>
        /// 规则缓存刷新周期
        /// </summary>
        private static readonly TimeSpan ConfigTtl = TimeSpan.FromSeconds(60);

        /// <summary>
        /// 规则快照
        /// </summary>
        private volatile List<AlarmMask> _masks = new();

        private DateTime _configTime = DateTime.MinValue;
        private readonly object _configLock = new();

        /// <summary>
        /// 屏蔽裁决(fire为待发布告警,device用于scope匹配;
        /// 返回裁决结果,降级时同步改写fire.AlarmGrade)
        /// </summary>
        public AlarmMaskVerdict Apply(AlarmFireInfo fire, DeviceInfo device)
        {
            try
            {
                // 告警恢复走独立语义,不参与屏蔽(屏蔽期内产生的告警,其恢复同样已被屏蔽掉源头)
                if (fire.EventType == "告警恢复") return AlarmMaskVerdict.通过;
                EnsureConfig();
                var now = DateTime.Now;
                var verdict = AlarmMaskVerdict.通过;
                string downgrade = "";
                foreach (var mask in _masks)
                {
                    if (!MatchScope(mask, fire, device)) continue;
                    if (!MatchTime(mask, now)) continue;
                    // 动作最重者生效:完全屏蔽(1)>静默(2)>降级(3)
                    var action = mask.MaskAction switch
                    {
                        1 => AlarmMaskVerdict.完全屏蔽,
                        3 => AlarmMaskVerdict.降级,
                        _ => AlarmMaskVerdict.静默
                    };
                    if (action == AlarmMaskVerdict.完全屏蔽) return AlarmMaskVerdict.完全屏蔽;
                    if (action == AlarmMaskVerdict.静默)
                    {
                        verdict = AlarmMaskVerdict.静默;
                    }
                    else if (verdict == AlarmMaskVerdict.通过)
                    {
                        verdict = AlarmMaskVerdict.降级;
                        downgrade = mask.DowngradeGrade ?? "";
                    }
                }
                if (verdict == AlarmMaskVerdict.降级 && !downgrade.IsZxxNullOrEmpty())
                {
                    fire.AlarmGrade = downgrade;
                }
                return verdict;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"屏蔽裁决失败：{ex}", Service_CATEGORY);
                return AlarmMaskVerdict.通过;
            }
        }

        /// <summary>
        /// 清空规则缓存(配置变更热重载)
        /// </summary>
        public void Reload()
        {
            lock (_configLock) { _configTime = DateTime.MinValue; }
        }

        /// <summary>
        /// 屏蔽对象匹配(1全局/2单位/4设备类型/5单设备/6告警等级)
        /// </summary>
        private static bool MatchScope(AlarmMask mask, AlarmFireInfo fire, DeviceInfo device)
        {
            var scopeid = mask.ScopeId ?? "";
            return mask.MaskScopeType switch
            {
                2 => scopeid == device.UnitId.ToString(),
                4 => string.Equals(scopeid, device.DeviceTypeCode, StringComparison.OrdinalIgnoreCase),
                5 => scopeid == device.DeviceId.ToString(),
                6 => string.Equals(scopeid, fire.AlarmGrade, StringComparison.OrdinalIgnoreCase),
                _ => true // 全局
            };
        }

        /// <summary>
        /// 时间窗匹配(1永久恒真;2一次性按起止时间;3周期窗按星期+时刻;均受ExpireAt约束)
        /// </summary>
        private static bool MatchTime(AlarmMask mask, DateTime now)
        {
            if (!mask.ExpireAt.IsZxxNullOrEmpty()
                && DateTime.TryParse(mask.ExpireAt, out var expire) && now > expire)
            {
                return false; // 到期自动失效
            }
            switch (mask.MaskMode)
            {
                case 2:
                    if (!DateTime.TryParse(mask.StartTime ?? "", out var start)
                        || !DateTime.TryParse(mask.EndTime ?? "", out var end)) return false;
                    return now >= start && now <= end;
                case 3:
                    if (mask.TimeRanges.IsZxxNullOrEmpty()) return false;
                    List<AlarmMaskTimeRange>? ranges;
                    try { ranges = mask.TimeRanges.ToObject<List<AlarmMaskTimeRange>>(); }
                    catch { return false; }
                    if (ranges == null) return false;
                    int today = (int)now.DayOfWeek;
                    var moment = now.TimeOfDay;
                    foreach (var range in ranges)
                    {
                        if (range.Days.IsZxxAny() && !range.Days.Contains(today)) continue;
                        if (!TimeSpan.TryParse(range.Start, out var from)
                            || !TimeSpan.TryParse(range.End, out var to)) continue;
                        if (moment >= from && moment <= to) return true;
                    }
                    return false;
                default:
                    return true; // 永久
            }
        }

        /// <summary>
        /// 规则快照过期时整体重建(仅加载启用中的规则)
        /// </summary>
        private void EnsureConfig()
        {
            if (DateTime.Now - _configTime <= ConfigTtl) return;
            lock (_configLock)
            {
                if (DateTime.Now - _configTime <= ConfigTtl) return;
                try
                {
                    _masks = (AlarmMaskDAO.Instance.GetList() ?? new List<AlarmMask>())
                        .Where(t => t.IsEnable).ToList();
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"屏蔽规则加载失败：{ex}", Service_CATEGORY);
                }
                _configTime = DateTime.Now;
            }
        }
    }
}
