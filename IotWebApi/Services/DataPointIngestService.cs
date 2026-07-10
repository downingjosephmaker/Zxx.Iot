using CenBoCommon.Zxx;
using CenboEventBus;
using IotLog;
using IotModel;
using IotWebApi.Services.Jobs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Channels;

namespace IotWebApi.Services
{
    /// <summary>
    /// 数据入库服务(统一消费插件上行事件,攒批写入数据库)
    /// </summary>
    public class DataPointIngestService : IHostedService
    {
        private const string Service_CATEGORY = "数据入库服务";

        /// <summary>
        /// 队列容量上限(队列满时丢弃最旧消息,保护数据库不被瞬时洪峰压垮)
        /// </summary>
        private const int QueueCapacity = 10000;

        /// <summary>
        /// 单批最大处理数量(攒批集中写库,避免逐行INSERT)
        /// </summary>
        private const int BatchSize = 200;

        /// <summary>
        /// 插件上行消息有界队列
        /// </summary>
        private readonly Channel<PluginEvent> _channel;

        /// <summary>
        /// 消费任务取消令牌
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// 后台消费任务
        /// </summary>
        private Task _consumeTask;

        /// <summary>
        /// 遥测批量写入服务(协议解析数据同步投递TimescaleDB遥测窄表)
        /// </summary>
        private readonly TelemetryWriteService _telemetryService;

        /// <summary>
        /// 最新值缓存服务(协议解析数据同步更新内存最新值)
        /// </summary>
        private readonly TelemetryLatestService _latestService;

        /// <summary>
        /// SignalR中心上下文(按device:{id}分组推送实时数据)
        /// </summary>
        private readonly IHubContext<ChatServer> _hubContext;

        /// <summary>
        /// 采集侧异常值过滤链(范围/幅度/连续容错)
        /// </summary>
        private readonly ValueFilterService _valueFilterService;

        /// <summary>
        /// 推送策略引擎(对外发布节流,最新值不受约束)
        /// </summary>
        private readonly PushGateService _pushGateService;

        /// <summary>
        /// 上下线判定服务(离线疑似中间态防抖)
        /// </summary>
        private readonly OfflineDebounceService _offlineDebounceService;

        /// <summary>
        /// 告警引擎(基于最新值缓存评估两级告警规则)
        /// </summary>
        private readonly AlarmEngineService _alarmEngineService;

        /// <summary>
        /// 告警屏蔽引擎(告警产生之后、入库通知之前裁决)
        /// </summary>
        private readonly AlarmMaskService _alarmMaskService;

        /// <summary>
        /// 告警通知服务(IsNote告警按渠道外发)
        /// </summary>
        private readonly AlarmNotifyService _alarmNotifyService;

        /// <summary>
        /// 告警生命周期服务(§9.2四态流转:去重/恢复回写/Ack映射)
        /// </summary>
        private readonly AlarmLifecycleService _alarmLifecycleService;

        /// <summary>
        /// 告警升级链服务(§9.5:未Ack未恢复渐进重复升级)
        /// </summary>
        private readonly AlarmEscalationService _alarmEscalationService;

        /// <summary>
        /// 规则联动引擎(§10.1:触发-条件-动作)
        /// </summary>
        private readonly RuleLinkageService _ruleLinkageService;

        /// <summary>
        /// 北向转发服务(§10.2:过闸遥测与未屏蔽告警转发第三方,入队即返回)
        /// </summary>
        private readonly NorthboundForwardService _northboundForwardService;

        public DataPointIngestService(TelemetryWriteService telemetryService, TelemetryLatestService latestService, IHubContext<ChatServer> hubContext, ValueFilterService valueFilterService, PushGateService pushGateService, OfflineDebounceService offlineDebounceService, AlarmEngineService alarmEngineService, AlarmMaskService alarmMaskService, AlarmNotifyService alarmNotifyService, AlarmLifecycleService alarmLifecycleService, AlarmEscalationService alarmEscalationService, RuleLinkageService ruleLinkageService, NorthboundForwardService northboundForwardService)
        {
            _alarmMaskService = alarmMaskService;
            _alarmNotifyService = alarmNotifyService;
            _alarmLifecycleService = alarmLifecycleService;
            _alarmEscalationService = alarmEscalationService;
            _ruleLinkageService = ruleLinkageService;
            _northboundForwardService = northboundForwardService;
            _telemetryService = telemetryService;
            _latestService = latestService;
            _hubContext = hubContext;
            _valueFilterService = valueFilterService;
            _pushGateService = pushGateService;
            _pushGateService.FlushHandler = FlushHeldPoints;  //节流/静默到期的点位由本服务补写历史/遥测/SignalR
            _offlineDebounceService = offlineDebounceService;
            _offlineDebounceService.ConfirmHandler = ConfirmOffline;  //疑似离线确认后由本服务落库+推送
            _alarmEngineService = alarmEngineService;
            _alarmEngineService.FireHandler = HandleAlarmFired;  //告警成立/恢复由本服务落库+推送
            _offlineDebounceService.ConfirmSecondsProvider = _alarmEngineService.GetOfflineConfirmSeconds;  //离线确认时长改读告警字典AlarmConfirmSeconds(§9.6)
            _channel = Channel.CreateBounded<PluginEvent>(new BoundedChannelOptions(QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
        }

        #region 服务生命周期

        /// <summary>
        /// 启动后台消费任务
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _consumeTask = Task.Run(() => ConsumeLoopAsync(_cts.Token));
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "数据入库服务已启动", Service_CATEGORY);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止后台消费任务(等待正在处理的批次完成)
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _channel.Writer.TryComplete();
            _cts?.Cancel();
            if (_consumeTask != null)
            {
                try { await _consumeTask; } catch (OperationCanceledException) { }
            }
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "数据入库服务已停止", Service_CATEGORY);
        }

        #endregion

        #region 消息入队

        /// <summary>
        /// 上行消息入队(队列满时自动丢弃最旧消息,仅在通道关闭后返回false)
        /// </summary>
        public bool Enqueue(PluginEvent @event)
        {
            return _channel.Writer.TryWrite(@event);
        }

        #endregion

        #region 批量消费

        /// <summary>
        /// 消费主循环(每轮取出当前积压的一批消息集中处理)
        /// </summary>
        private async Task ConsumeLoopAsync(CancellationToken token)
        {
            var reader = _channel.Reader;
            try
            {
                while (await reader.WaitToReadAsync(token))
                {
                    var batch = new List<PluginEvent>();
                    while (batch.Count < BatchSize && reader.TryRead(out var evt))
                    {
                        batch.Add(evt);
                    }
                    if (!batch.IsZxxAny()) continue;
                    try
                    {
                        ProcessBatch(batch);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 服务停止，正常退出
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"消费循环意外退出：{ex}", Service_CATEGORY);
            }
        }

        /// <summary>
        /// 按消息类型分流处理一批上行消息
        /// </summary>
        private void ProcessBatch(List<PluginEvent> batch)
        {
            var protocoldatas = new List<DeviceData>();
            var runstatedatas = new List<DeviceData>();
            var controlresults = new List<PluginControlResultMessage>();

            foreach (var evt in batch)
            {
                if (evt?.Message == null) continue;
                switch (evt.Message.MessageType)
                {
                    case PluginMessageEnum.协议解析:
                        var datas = evt.Message.MessageJson.ToObject<List<DeviceData>>();
                        if (datas.IsZxxAny()) protocoldatas.AddRange(datas);
                        break;
                    case PluginMessageEnum.运行状态:
                        var states = evt.Message.MessageJson.ToObject<List<DeviceData>>();
                        if (states.IsZxxAny()) runstatedatas.AddRange(states);
                        break;
                    case PluginMessageEnum.控制结果:
                        var result = evt.Message.MessageJson.ToObject<PluginControlResultMessage>();
                        if (result != null) controlresults.Add(result);
                        break;
                    case PluginMessageEnum.心跳:
                        LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件[{evt.PluginGuid}]心跳：{evt.Message.MessageJson}", Service_CATEGORY);
                        break;
                    default:
                        LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件[{evt.PluginGuid}]的【{evt.Message.MessageType}】消息暂无入库处理逻辑，已忽略。", Service_CATEGORY);
                        break;
                }
            }

            if (protocoldatas.IsZxxAny() || runstatedatas.IsZxxAny() || controlresults.IsZxxAny())
            {
                var typelist = DeviceTypeDAO.Instance.GetList();

                if (protocoldatas.IsZxxAny()) SaveProtocolData(protocoldatas, typelist);
                if (runstatedatas.IsZxxAny()) SaveRunState(runstatedatas, typelist);
                if (controlresults.IsZxxAny()) SaveControlResult(controlresults, typelist);
            }
        }

        #endregion

        #region 数据入库

        /// <summary>
        /// 协议解析数据入库(合并更新设备参数最新值与设备在线状态,写入历史记录快照)
        /// </summary>
        private void SaveProtocolData(List<DeviceData> datas, List<DeviceTypeEntity> typelist)
        {
            var validlist = datas.Where(t => t.device != null && t.deviceparam.IsZxxAny()).ToList();
            if (!validlist.IsZxxAny()) return;

            var deviceids = validlist.Select(t => t.DeviceId).Distinct().ToList();
            var paramlist = DeviceParamDAO.Instance.GetListBy(t => deviceids.Contains(t.DeviceId));

            var paramupdates = new Dictionary<int, DeviceParamEntity>();
            var deviceupdates = new Dictionary<int, DeviceInfoEntity>();
            var historylist = new List<EventHistoryEntity>();
            var telemetrypoints = new List<TelemetryPoint>();
            var latestpoints = new List<TelemetryPoint>();

            foreach (var data in validlist)
            {
                // 0.采集侧三级异常值过滤(范围/幅度/连续容错,被滤除的参数本轮整体丢弃)
                data.deviceparam = data.deviceparam.Where(t => _valueFilterService.Accept(data.device.DeviceTypeCode, data.DeviceId, t.ParamCode, t.ParamValue)).ToList();
                if (!data.deviceparam.IsZxxAny()) continue;

                // 1.按ParamCode合并更新设备参数最新值(设备参数表是实时值的唯一事实来源)
                var param = paramlist.FirstOrDefault(t => t.DeviceId == data.DeviceId);
                if (param != null && param.ExpandObjects.IsZxxAny())
                {
                    foreach (var income in data.deviceparam)
                    {
                        var exist = param.ExpandObjects.FirstOrDefault(t => t.ParamCode == income.ParamCode);
                        if (exist == null) continue;
                        exist.ParamLastValue = exist.ParamValue;
                        exist.ParamValue = income.ParamValue;
                        exist.CollectTime = income.CollectTime;
                        exist.IsAlarm = income.IsAlarm;
                    }
                    paramupdates[param.DeviceId] = param;
                }

                // 2.更新设备在线状态(插件上报的device副本已带最新状态;
                // 协议数据到达即存活,看门狗语义重置疑似离线计时)
                deviceupdates[data.DeviceId] = data.device;
                _offlineDebounceService.CancelSuspect(data.DeviceId);

                // 3.推送策略判定:通过的点位才对外发布,扣下的由引擎按节流窗口/静默周期补发
                var devicepoints = BuildTelemetryPoints(data);
                latestpoints.AddRange(devicepoints);
                var publishpoints = devicepoints.Where(t => _pushGateService.ShouldPublish(data.device.DeviceTypeCode, t)).ToList();
                if (!publishpoints.IsZxxAny()) continue;
                telemetrypoints.AddRange(publishpoints);
                // 北向转发(§10.2:转发面与推送面共用同一过闸结果,入队即返回不阻塞)
                _northboundForwardService.ForwardTelemetry(data.DeviceId, data.device.DeviceTypeCode, publishpoints);

                // 4.生成历史记录快照(只含本轮对外发布的点位)
                var pubcodes = publishpoints.Select(t => t.ParamCode).ToHashSet();
                var history = new EventHistoryEntity
                {
                    SnowId = SnowModel.Instance.NewId(),
                    EventTime = data.deviceparam.FirstOrDefault(t => !t.CollectTime.IsZxxNullOrEmpty())?.CollectTime ?? DateTime.Now.ToDateTimeString(),
                    ExpandObject = data.deviceparam.Where(t => pubcodes.Contains(t.ParamCode)).Select(t => new Expand_EventHistory
                    {
                        ParamCode = t.ParamCode,
                        ParamName = t.ParamName,
                        ParamValue = t.ParamValue,
                        ValueUnit = t.ValueUnit,
                        IsAlarm = t.IsAlarm
                    }).ToList()
                };
                FillEventBase(history, data.device, typelist);
                historylist.Add(history);
            }

            if (paramupdates.Count > 0) DeviceParamDAO.Instance.UpdateColumns(paramupdates.Values.ToList(), it => new { it.ExpandJson });
            if (deviceupdates.Count > 0) DeviceInfoDAO.Instance.UpdateColumns(deviceupdates.Values.ToList(), it => new { it.DeviceState, it.LastOnlineTime, it.DeviceAlarm });
            if (historylist.IsZxxAny()) EventHistoryDAO.Instance.InsertRange(historylist);
            if (latestpoints.IsZxxAny())
            {
                _latestService.Update(latestpoints);  //最新值缓存不受推送策略约束,永远即时更新
                // 告警评估与规则联动基于最新值缓存,走独立事件通道不受推送节流影响(§7.3硬规则)
                foreach (var group in latestpoints.GroupBy(t => (int)t.DeviceId))
                {
                    var changedcodes = group.Select(t => t.ParamCode).ToHashSet();
                    _alarmEngineService.Evaluate(group.Key, changedcodes);
                    _ruleLinkageService.OnPointChanged(group.Key, changedcodes);
                }
            }
            if (telemetrypoints.IsZxxAny())
            {
                _telemetryService.Enqueue(telemetrypoints);
                BroadcastDeviceData(telemetrypoints);
            }
        }

        /// <summary>
        /// 推送策略补发回调(节流窗口/静默周期到期的点位,补写历史/遥测/SignalR)
        /// </summary>
        private void FlushHeldPoints(List<TelemetryPoint> points)
        {
            try
            {
                if (!points.IsZxxAny()) return;
                _telemetryService.Enqueue(points);
                _latestService.Update(points);
                BroadcastDeviceData(points);

                var deviceids = points.Select(t => (int)t.DeviceId).Distinct().ToList();
                var devlist = DeviceInfoDAO.Instance.GetListBy(t => deviceids.Contains(t.DeviceId));
                if (!devlist.IsZxxAny()) return;
                var typelist = DeviceTypeDAO.Instance.GetList();

                var historylist = new List<EventHistoryEntity>();
                foreach (var group in points.GroupBy(t => t.DeviceId))
                {
                    var dbdev = devlist.FirstOrDefault(t => t.DeviceId == group.Key);
                    if (dbdev == null) continue;
                    var history = new EventHistoryEntity
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        EventTime = DateTime.Now.ToDateTimeString(),
                        ExpandObject = group.Select(t => new Expand_EventHistory
                        {
                            ParamCode = t.ParamCode,
                            ParamName = t.ParamName,
                            ParamValue = t.Value.HasValue ? t.Value.Value.ToString() : t.ValueStr
                        }).ToList()
                    };
                    FillEventBase(history, dbdev, typelist);
                    historylist.Add(history);
                }
                if (historylist.IsZxxAny()) EventHistoryDAO.Instance.InsertRange(historylist);
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
            }
        }

        /// <summary>
        /// 运行状态入库(设备上下线状态变化时更新设备状态并写入运行日志)
        /// </summary>
        private void SaveRunState(List<DeviceData> datas, List<DeviceTypeEntity> typelist)
        {
            var validlist = datas.Where(t => t.device != null).ToList();
            if (!validlist.IsZxxAny()) return;

            var deviceids = validlist.Select(t => t.DeviceId).Distinct().ToList();
            var devlist = DeviceInfoDAO.Instance.GetListBy(t => deviceids.Contains(t.DeviceId));
            if (!devlist.IsZxxAny()) return;

            var deviceupdates = new Dictionary<int, DeviceInfoEntity>();
            var runlist = new List<EventRun>();

            foreach (var data in validlist)
            {
                var dbdev = devlist.FirstOrDefault(t => t.DeviceId == data.DeviceId);
                if (dbdev == null) continue;

                // 离线不即时落库:进入疑似离线中间态,超过确认时长由回调正式判离线(判定级防抖)
                if (data.device.DeviceState != 2)
                {
                    if (dbdev.DeviceState != data.device.DeviceState)
                    {
                        _offlineDebounceService.OnSuspectOffline(data.DeviceId, data.device.DeviceState);
                    }
                    continue;
                }

                // 任何在线上报都取消疑似离线(疑似期间恢复=静默取消不产生事件;
                // 库内状态未变时也必须取消,否则疑似态残留会在确认时长后误判离线)
                _offlineDebounceService.CancelSuspect(data.DeviceId);
                if (dbdev.DeviceState == data.device.DeviceState) continue;

                // 上线即时生效,离线告警共用判定结果即时恢复(§9.6);上线通知同设备60秒限频
                bool notify = _offlineDebounceService.OnOnline(data.DeviceId);
                _alarmEngineService.FireOfflineRecover(data.DeviceId);
                _ruleLinkageService.OnDeviceState(data.DeviceId, true);  //规则联动:设备上线触发(§10.1)
                dbdev.DeviceState = data.device.DeviceState;
                if (!data.device.LastOnlineTime.IsZxxNullOrEmpty()) dbdev.LastOnlineTime = data.device.LastOnlineTime;
                deviceupdates[dbdev.DeviceId] = dbdev;
                if (!notify) continue;

                var run = new EventRun
                {
                    SnowId = SnowModel.Instance.NewId(),
                    EventTime = DateTime.Now.ToDateTimeString(),
                    EventType = "设备通信恢复",
                    EventContent = $"设备[{dbdev.DeviceName}]通信恢复上线"
                };
                FillEventBase(run, dbdev, typelist);
                runlist.Add(run);
            }

            if (deviceupdates.Count > 0) DeviceInfoDAO.Instance.UpdateColumns(deviceupdates.Values.ToList(), it => new { it.DeviceState, it.LastOnlineTime });
            if (runlist.IsZxxAny())
            {
                EventRunDAO.Instance.InsertRange(runlist);
                BroadcastDeviceState(runlist);
            }
        }

        /// <summary>
        /// 离线确认回调(疑似离线超过确认时长的设备正式落库并推送,事件携带原因)
        /// </summary>
        private void ConfirmOffline(List<(int DeviceId, int DeviceState, string Reason)> confirms)
        {
            try
            {
                if (!confirms.IsZxxAny()) return;
                var deviceids = confirms.Select(t => t.DeviceId).Distinct().ToList();
                var devlist = DeviceInfoDAO.Instance.GetListBy(t => deviceids.Contains(t.DeviceId));
                if (!devlist.IsZxxAny()) return;
                var typelist = DeviceTypeDAO.Instance.GetList();

                var deviceupdates = new List<DeviceInfoEntity>();
                var runlist = new List<EventRun>();
                var offlinefires = new List<(int DeviceId, string Reason)>();
                foreach (var confirm in confirms)
                {
                    var dbdev = devlist.FirstOrDefault(t => t.DeviceId == confirm.DeviceId);
                    if (dbdev == null || dbdev.DeviceState == confirm.DeviceState) continue;
                    dbdev.DeviceState = confirm.DeviceState;
                    deviceupdates.Add(dbdev);
                    offlinefires.Add((confirm.DeviceId, confirm.Reason));

                    var run = new EventRun
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        EventTime = DateTime.Now.ToDateTimeString(),
                        EventType = "设备离线",
                        EventContent = $"设备[{dbdev.DeviceName}]通信中断离线,原因:{confirm.Reason}"
                    };
                    FillEventBase(run, dbdev, typelist);
                    runlist.Add(run);
                }
                if (deviceupdates.IsZxxAny()) DeviceInfoDAO.Instance.UpdateColumns(deviceupdates, it => new { it.DeviceState });
                if (runlist.IsZxxAny())
                {
                    EventRunDAO.Instance.InsertRange(runlist);
                    BroadcastDeviceState(runlist);
                }
                // 离线告警与上下线事件共用同一判定结果(§9.6:确认时长即时长型防抖,
                // 引擎不重复防抖;告警经FireHandler回流本服务走屏蔽/落库/通知链路)
                foreach (var fire in offlinefires)
                {
                    _alarmEngineService.FireOffline(fire.DeviceId, fire.Reason);
                    _ruleLinkageService.OnDeviceState(fire.DeviceId, false);  //规则联动:设备离线触发(§10.1)
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
            }
        }

        /// <summary>
        /// 控制结果入库(逐台设备写入控制日志)
        /// </summary>
        private void SaveControlResult(List<PluginControlResultMessage> results, List<DeviceTypeEntity> typelist)
        {
            var deviceids = results.SelectMany(t => t.DeviceResults).Select(t => t.DeviceId).Distinct().ToList();
            if (!deviceids.IsZxxAny()) return;
            var devlist = DeviceInfoDAO.Instance.GetListBy(t => deviceids.Contains(t.DeviceId));

            var controllist = new List<EventControl>();
            foreach (var result in results)
            {
                foreach (var item in result.DeviceResults)
                {
                    var control = new EventControl
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        EventTime = item.ResultTime.IsZxxNullOrEmpty() ? DateTime.Now.ToDateTimeString() : item.ResultTime,
                        OptResult = item.Success ? "成功" : "失败",
                        OptContent = item.Message,
                        OptJosn = item.ToJson(),
                        OptBatch = result.DeviceResults.Count > 1 ? 1 : 0,
                        OptBatchId = long.TryParse(result.CommandId, out long batchid) ? batchid : 0,
                        EventUserId = 0,
                        EventUserName = "",
                        SourceType = "",
                        LinkType = "Service"
                    };
                    var dbdev = devlist.FirstOrDefault(t => t.DeviceId == item.DeviceId);
                    if (dbdev != null)
                    {
                        FillEventBase(control, dbdev, typelist);
                    }
                    else
                    {
                        control.DeviceId = item.DeviceId;
                        control.DeviceName = item.DeviceName;
                    }
                    controllist.Add(control);
                }
            }

            if (controllist.IsZxxAny()) EventControlDAO.Instance.InsertRange(controllist);
        }

        /// <summary>
        /// 按设备分组推送实时数据(组名device:{deviceId},客户端监听ReceiveDeviceData;
        /// 尽力而为不阻塞入库,推送失败仅记日志)
        /// </summary>
        private void BroadcastDeviceData(List<TelemetryPoint> points)
        {
            foreach (var group in points.GroupBy(t => t.DeviceId))
            {
                var deviceid = group.Key;
                _hubContext.Clients.Group($"device:{deviceid}").SendAsync("ReceiveDeviceData", group.ToList().ToJson())
                    .ContinueWith(t => LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"设备[{deviceid}]实时数据推送失败：{t.Exception}", Service_CATEGORY), TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        /// <summary>
        /// 按设备分组推送上下线状态变化(客户端监听ReceiveDeviceState)
        /// </summary>
        private void BroadcastDeviceState(List<EventRun> runlist)
        {
            foreach (var run in runlist)
            {
                _hubContext.Clients.Group($"device:{run.DeviceId}").SendAsync("ReceiveDeviceState", run.ToJson())
                    .ContinueWith(t => LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"设备[{run.DeviceId}]状态推送失败：{t.Exception}", Service_CATEGORY), TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        /// <summary>
        /// 告警成立/恢复回调(告警事件走独立通道不经推送节流:EventSignal落库+SignalR告警组推送)
        /// </summary>
        private void HandleAlarmFired(List<AlarmFireInfo> fires)
        {
            try
            {
                if (!fires.IsZxxAny()) return;
                var deviceids = fires.Select(t => t.DeviceId).Distinct().ToList();
                var devlist = DeviceInfoDAO.Instance.GetListBy(t => deviceids.Contains(t.DeviceId));
                if (!devlist.IsZxxAny()) return;
                var typelist = DeviceTypeDAO.Instance.GetList();

                var signallist = new List<EventSignal>();
                var pushlist = new List<EventSignal>();
                var notifylist = new List<EventSignal>();
                var alarmflagupdates = new Dictionary<int, DeviceInfoEntity>();
                foreach (var fire in fires)
                {
                    var dbdev = devlist.FirstOrDefault(t => t.DeviceId == fire.DeviceId);
                    if (dbdev == null) continue;
                    // 屏蔽裁决(§9.4:完全屏蔽不入库不通知;静默入库打标不通知;降级改写等级)
                    var verdict = _alarmMaskService.Apply(fire, dbdev);
                    if (verdict == AlarmMaskVerdict.完全屏蔽) continue;
                    // 四态生命周期流转(§9.2:EventAlarm去重登记/恢复回写;静默告警照常流转)
                    long alarmid = _alarmLifecycleService.Apply(fire, dbdev, typelist);
                    // 设备告警标志:成立置位,恢复且无其他活动告警才清零(Ack清零由处理接口承接)
                    if (fire.EventType == "设备告警" && dbdev.DeviceAlarm != 1)
                    {
                        dbdev.DeviceAlarm = 1;
                        alarmflagupdates[dbdev.DeviceId] = dbdev;
                    }
                    else if (fire.EventType == "告警恢复" && dbdev.DeviceAlarm != 0 && !_alarmLifecycleService.HasActive(fire.DeviceId))
                    {
                        dbdev.DeviceAlarm = 0;
                        alarmflagupdates[dbdev.DeviceId] = dbdev;
                    }
                    string content = fire.AlarmGrade.IsZxxNullOrEmpty() ? fire.Content : $"[{fire.AlarmGrade}]{fire.Content}";
                    var signal = new EventSignal
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        EventTime = DateTime.Now.ToDateTimeString(),
                        EventType = fire.EventType,
                        EventValue = fire.ValueText,
                        EventContent = verdict == AlarmMaskVerdict.静默 ? $"[已屏蔽]{content}" : content
                    };
                    FillEventBase(signal, dbdev, typelist);
                    signallist.Add(signal);
                    if (verdict != AlarmMaskVerdict.静默)
                    {
                        pushlist.Add(signal);
                        if (fire.IsNote)
                        {
                            notifylist.Add(signal);
                            // 升级链登记(§9.5:第一梯队由Notify立即外发,未Ack未恢复的后续梯队由升级链驱动)
                            if (fire.EventType == "设备告警" && alarmid > 0) _alarmEscalationService.Track(alarmid, signal);
                        }
                    }
                }
                if (alarmflagupdates.Count > 0) DeviceInfoDAO.Instance.UpdateColumns(alarmflagupdates.Values.ToList(), it => new { it.DeviceAlarm });
                if (!signallist.IsZxxAny()) return;
                EventSignalDAO.Instance.InsertRange(signallist);
                if (notifylist.IsZxxAny()) _alarmNotifyService.Notify(notifylist);
                foreach (var signal in pushlist)
                {
                    // 北向转发(§10.2:静默/完全屏蔽的不外发,与SignalR推送同口径)
                    _northboundForwardService.ForwardAlarm(signal.DeviceId, signal.DeviceTypeCode, signal);
                    _hubContext.Clients.Group($"alarm:{signal.TenantId}").SendAsync("ReceiveAlarm", signal.ToJson())
                        .ContinueWith(t => LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"设备[{signal.DeviceId}]告警推送失败：{t.Exception}", Service_CATEGORY), TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"告警落库推送失败：{ex}", Service_CATEGORY);
            }
        }

        /// <summary>
        /// 将一台设备的采集参数转换为遥测点位(数值型进value,其余进value_str)
        /// </summary>
        private static List<TelemetryPoint> BuildTelemetryPoints(DeviceData data)
        {
            var points = new List<TelemetryPoint>();
            foreach (var income in data.deviceparam)
            {
                if (income.ParamCode.IsZxxNullOrEmpty()) continue;
                var point = new TelemetryPoint
                {
                    DeviceId = data.DeviceId,
                    ParamCode = income.ParamCode,
                    ParamName = income.ParamName,
                    Ts = ToUtcTime(income.CollectTime),
                    Quality = 0
                };
                if (double.TryParse(income.ParamValue, out double value)) point.Value = value;
                else point.ValueStr = income.ParamValue;
                points.Add(point);
            }
            return points;
        }

        /// <summary>
        /// 采集时间(本地时间字符串)转UTC(telemetry的timestamptz列要求UTC Kind)
        /// </summary>
        private static DateTime ToUtcTime(string collecttime)
        {
            if (!collecttime.IsZxxNullOrEmpty() && DateTime.TryParse(collecttime, out DateTime time))
            {
                return DateTime.SpecifyKind(time, DateTimeKind.Local).ToUniversalTime();
            }
            return DateTime.UtcNow;
        }

        /// <summary>
        /// 填充记录基础字段(设备归属的租户/设备类型信息;
        /// internal供告警生命周期服务复用同一填充逻辑)
        /// </summary>
        internal static void FillEventBase(EventBase evt, DeviceInfo device, List<DeviceTypeEntity> typelist)
        {
            evt.DeviceId = device.DeviceId;
            evt.DeviceName = device.DeviceName;
            evt.DeviceTypeCode = device.DeviceTypeCode;
            evt.TenantId = device.TenantId;
            var devtype = typelist.FirstOrDefault(t => t.TypeCode == device.DeviceTypeCode);
            if (devtype != null) evt.DeviceTypeName = devtype.TypeName;
        }

        #endregion
    }
}
