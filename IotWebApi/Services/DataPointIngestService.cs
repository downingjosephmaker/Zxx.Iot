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

        public DataPointIngestService(TelemetryWriteService telemetryService, TelemetryLatestService latestService, IHubContext<ChatServer> hubContext)
        {
            _telemetryService = telemetryService;
            _latestService = latestService;
            _hubContext = hubContext;
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
                var unitlist = BasicunitInfoDAO.Instance.GetList();
                var buildlist = BuildInfoDAO.Instance.GetList();
                var deptlist = DeptInfoDAO.Instance.GetList();
                var typelist = DeviceTypeDAO.Instance.GetList();

                if (protocoldatas.IsZxxAny()) SaveProtocolData(protocoldatas, unitlist, buildlist, deptlist, typelist);
                if (runstatedatas.IsZxxAny()) SaveRunState(runstatedatas, unitlist, buildlist, deptlist, typelist);
                if (controlresults.IsZxxAny()) SaveControlResult(controlresults, unitlist, buildlist, deptlist, typelist);
            }
        }

        #endregion

        #region 数据入库

        /// <summary>
        /// 协议解析数据入库(合并更新设备参数最新值与设备在线状态,写入历史记录快照)
        /// </summary>
        private void SaveProtocolData(List<DeviceData> datas, List<BasicunitInfoEntity> unitlist, List<BuildInfo> buildlist, List<DeptInfo> deptlist, List<DeviceTypeEntity> typelist)
        {
            var validlist = datas.Where(t => t.device != null && t.deviceparam.IsZxxAny()).ToList();
            if (!validlist.IsZxxAny()) return;

            var deviceids = validlist.Select(t => t.DeviceId).Distinct().ToList();
            var paramlist = DeviceParamDAO.Instance.GetListBy(t => deviceids.Contains(t.DeviceId));

            var paramupdates = new Dictionary<int, DeviceParamEntity>();
            var deviceupdates = new Dictionary<int, DeviceInfoEntity>();
            var historylist = new List<EventHistoryEntity>();
            var telemetrypoints = new List<TelemetryPoint>();

            foreach (var data in validlist)
            {
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

                // 2.更新设备在线状态(插件上报的device副本已带最新状态)
                deviceupdates[data.DeviceId] = data.device;

                // 3.生成历史记录快照(一台设备一次采集一行)
                var history = new EventHistoryEntity
                {
                    SnowId = SnowModel.Instance.NewId(),
                    EventTime = data.deviceparam.FirstOrDefault(t => !t.CollectTime.IsZxxNullOrEmpty())?.CollectTime ?? DateTime.Now.ToDateTimeString(),
                    ExpandObject = data.deviceparam.Select(t => new Expand_EventHistory
                    {
                        ParamCode = t.ParamCode,
                        ParamName = t.ParamName,
                        ParamValue = t.ParamValue,
                        ValueUnit = t.ValueUnit,
                        IsAlarm = t.IsAlarm
                    }).ToList()
                };
                FillEventBase(history, data.device, unitlist, buildlist, deptlist, typelist);
                historylist.Add(history);

                // 4.投递遥测窄表写入队列(未配置Timescale连接串时服务内部直接忽略)
                telemetrypoints.AddRange(BuildTelemetryPoints(data));
            }

            if (paramupdates.Count > 0) DeviceParamDAO.Instance.UpdateColumns(paramupdates.Values.ToList(), it => new { it.ExpandJson });
            if (deviceupdates.Count > 0) DeviceInfoDAO.Instance.UpdateColumns(deviceupdates.Values.ToList(), it => new { it.DeviceState, it.LastOnlineTime, it.DeviceAlarm });
            if (historylist.IsZxxAny()) EventHistoryDAO.Instance.InsertRange(historylist);
            if (telemetrypoints.IsZxxAny())
            {
                _telemetryService.Enqueue(telemetrypoints);
                _latestService.Update(telemetrypoints);
                BroadcastDeviceData(telemetrypoints);
            }
        }

        /// <summary>
        /// 运行状态入库(设备上下线状态变化时更新设备状态并写入运行日志)
        /// </summary>
        private void SaveRunState(List<DeviceData> datas, List<BasicunitInfoEntity> unitlist, List<BuildInfo> buildlist, List<DeptInfo> deptlist, List<DeviceTypeEntity> typelist)
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
                if (dbdev == null || dbdev.DeviceState == data.device.DeviceState) continue;

                dbdev.DeviceState = data.device.DeviceState;
                if (!data.device.LastOnlineTime.IsZxxNullOrEmpty()) dbdev.LastOnlineTime = data.device.LastOnlineTime;
                deviceupdates[dbdev.DeviceId] = dbdev;

                var run = new EventRun
                {
                    SnowId = SnowModel.Instance.NewId(),
                    EventTime = DateTime.Now.ToDateTimeString(),
                    EventType = data.device.DeviceState == 2 ? "设备通信恢复" : "设备离线",
                    EventContent = $"设备[{dbdev.DeviceName}]{(data.device.DeviceState == 2 ? "通信恢复上线" : "通信中断离线")}"
                };
                FillEventBase(run, dbdev, unitlist, buildlist, deptlist, typelist);
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
        /// 控制结果入库(逐台设备写入控制日志)
        /// </summary>
        private void SaveControlResult(List<PluginControlResultMessage> results, List<BasicunitInfoEntity> unitlist, List<BuildInfo> buildlist, List<DeptInfo> deptlist, List<DeviceTypeEntity> typelist)
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
                        FillEventBase(control, dbdev, unitlist, buildlist, deptlist, typelist);
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
        /// 填充记录基础字段(设备归属的单位/建筑/部门/设备类型信息)
        /// </summary>
        private static void FillEventBase(EventBase evt, DeviceInfo device, List<BasicunitInfoEntity> unitlist, List<BuildInfo> buildlist, List<DeptInfo> deptlist, List<DeviceTypeEntity> typelist)
        {
            evt.DeviceId = device.DeviceId;
            evt.DeviceName = device.DeviceName;
            evt.DeviceTypeCode = device.DeviceTypeCode;
            evt.UnitId = device.UnitId;
            evt.BuildId = device.BuildId;
            evt.DeptId = device.DeptId;
            var unit = unitlist.FirstOrDefault(t => t.UnitId == device.UnitId);
            if (unit != null) evt.UnitName = unit.UnitName;
            var build = buildlist.FirstOrDefault(t => t.BuildId == device.BuildId);
            if (build != null) evt.BuildName = build.FullName.BeautifyFullName();
            var dept = deptlist.FirstOrDefault(t => t.DeptId == device.DeptId);
            if (dept != null) evt.DeptName = dept.FullName.BeautifyFullName();
            var devtype = typelist.FirstOrDefault(t => t.TypeCode == device.DeviceTypeCode);
            if (devtype != null) evt.DeviceTypeName = devtype.TypeName;
        }

        #endregion
    }
}
