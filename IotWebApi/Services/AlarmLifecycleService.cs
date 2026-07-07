using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using System.Collections.Concurrent;

namespace IotWebApi.Services
{
    /// <summary>
    /// 告警生命周期服务(§9.2:借鉴ThingsBoard四态模型 Active_UnAck/Active_Ack/Cleared_UnAck/Cleared_Ack,
    /// 映射现有EventAlarm字段——激活维=IsRestore(未恢复/已恢复),确认维=CheckResult(未处理/已处理);
    /// Ack动作由EventAlarmDbController.PostHandleAlarm承接,Ack即静默:确认后告警保持Active直至条件恢复;
    /// 去重键(设备,规则)同时只允许一个活动告警——重复触发原地更新当前值/等级不新建记录,天然免刷屏;
    /// 恢复回写RestoreTime与AlarmTimeRange(分);登记表暖启动装载近两月未恢复行,重启不丢活动态)
    /// </summary>
    public class AlarmLifecycleService
    {
        private const string Service_CATEGORY = "告警生命周期";

        /// <summary>
        /// 活动告警登记表((设备,规则)→EventAlarm主键)
        /// </summary>
        private readonly ConcurrentDictionary<(int DeviceId, long RuleId), long> _actives = new();

        private volatile bool _warmed;
        private readonly object _warmLock = new();

        /// <summary>
        /// 告警成立/恢复驱动四态流转(数据入库服务在屏蔽裁决后调用;
        /// 完全屏蔽的告警不进生命周期,静默告警照常流转仅不推送不通知)
        /// </summary>
        public void Apply(AlarmFireInfo fire, DeviceInfo device, List<BasicunitInfoEntity> unitlist,
            List<BuildInfo> buildlist, List<DeptInfo> deptlist, List<DeviceTypeEntity> typelist)
        {
            try
            {
                WarmUp();
                var key = (fire.DeviceId, fire.RuleId);
                if (fire.EventType == "告警恢复")
                {
                    Restore(key);
                    return;
                }

                if (_actives.TryGetValue(key, out long existid))
                {
                    // 去重(§9.2):重复触发原地更新当前值,级别升级同步等级——不新建记录;
                    // 不动CheckResult(Ack即静默:已确认的活动告警保持已处理态直至恢复)
                    var exist = EventAlarmDAO.Instance.GetOneBy(t => t.SnowId == existid);
                    if (exist != null)
                    {
                        exist.AlarmValue = fire.Content;
                        if (!fire.AlarmGrade.IsZxxNullOrEmpty()) exist.AlarmGrade = fire.AlarmGrade;
                        EventAlarmDAO.Instance.Update(exist);
                        return;
                    }
                    _actives.TryRemove(key, out _); // 登记项对应行已被人工删除,清脏后按新告警落库
                }

                var alarm = new EventAlarmEntity
                {
                    SnowId = SnowModel.Instance.NewId(),
                    EventTime = DateTime.Now.ToDateTimeString(),
                    EventType = fire.AlarmEventType,
                    AlarmGrade = fire.AlarmGrade,
                    AlarmType = fire.AlarmType,
                    AlarmValue = fire.Content,
                    CheckResult = "未处理",
                    IsRestore = "未恢复",
                    ExpandObject = new List<Expand_EventAlarm>
                    {
                        new Expand_EventAlarm
                        {
                            ParamCode = fire.ParamCode,
                            JisuanFormula = fire.Formula,
                            RuleId = fire.RuleId
                        }
                    }
                };
                DataPointIngestService.FillEventBase(alarm, device, unitlist, buildlist, deptlist, typelist);
                EventAlarmDAO.Instance.InsertRange(new List<EventAlarmEntity> { alarm });
                _actives[key] = alarm.SnowId;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"设备[{fire.DeviceId}]规则[{fire.RuleId}]生命周期流转失败：{ex}", Service_CATEGORY);
            }
        }

        /// <summary>
        /// 恢复流转(Active→Cleared:回写恢复时间与告警时长,确认维不变;无活动告警的恢复静默)
        /// </summary>
        private void Restore((int DeviceId, long RuleId) key)
        {
            if (!_actives.TryRemove(key, out long snowid)) return;
            var active = EventAlarmDAO.Instance.GetOneBy(t => t.SnowId == snowid);
            if (active == null) return;
            active.IsRestore = "已恢复";
            active.RestoreTime = DateTime.Now.ToDateTimeString();
            if (DateTime.TryParse(active.EventTime, out DateTime firsttime))
            {
                active.AlarmTimeRange = (int)Math.Max(0, (DateTime.Now - firsttime).TotalMinutes);
            }
            EventAlarmDAO.Instance.Update(active);
        }

        /// <summary>
        /// 暖启动(近两月未恢复的活动告警装回登记表,重启不丢活动态;
        /// 规则ID取拓展快照首行,历史行无RuleId且事件类别非离线的无法参与去重,跳过)
        /// </summary>
        private void WarmUp()
        {
            if (_warmed) return;
            lock (_warmLock)
            {
                if (_warmed) return;
                try
                {
                    long minid = SnowModel.Instance.GetId(DateTime.Now.AddMonths(-2));
                    var list = EventAlarmDAO.Instance.GetListBy(t => t.SnowId >= minid && t.IsRestore == "未恢复")
                        ?? new List<EventAlarmEntity>();
                    foreach (var item in list)
                    {
                        long ruleid = item.ExpandObject?.FirstOrDefault()?.RuleId ?? 0;
                        if (ruleid == 0 && item.EventType != "离线") continue;
                        _actives.TryAdd((item.DeviceId, ruleid), item.SnowId);
                    }
                    if (_actives.Count > 0)
                    {
                        LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                            $"暖启动装载活动告警{_actives.Count}条", Service_CATEGORY);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"暖启动装载失败：{ex}", Service_CATEGORY);
                }
                _warmed = true;
            }
        }
    }
}
