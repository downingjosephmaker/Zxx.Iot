using System.Collections.Concurrent;
using System.Text;
using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using MQTTnet;
using MQTTnet.Protocol;
using SqlSugar;

namespace IotWebApi.Services
{
    /// <summary>
    /// 北向转发服务(§10.2:Connector/Sink抽象,MQTT/HTTP Webhook目的地,Kafka预留;
    /// 三段式断线续传=内存ConcurrentQueue(10万条)→溢出/失败落盘每目的地独立SQLite→后台2000条批重传成功才删;
    /// 缓存文件1GB滚动删除;失败日志只在状态翻转时记录防刷盘;
    /// 转发面暂与SignalR推送面共用同一push_strategy节流判定(接线于DataPointIngestService过闸之后))
    /// </summary>
    public class NorthboundForwardService : IHostedService
    {
        private const string Service_CATEGORY = "北向转发";

        /// <summary>内存队列上限(条)</summary>
        private const int MemLimit = 100_000;

        /// <summary>单轮内存直发批大小</summary>
        private const int SendBatchSize = 500;

        /// <summary>落盘重传批大小</summary>
        private const int RetryBatchSize = 2_000;

        /// <summary>缓存文件上限(字节,超限滚动删除旧半)</summary>
        private const long CacheFileLimit = 1_073_741_824;

        /// <summary>目的地配置刷新周期</summary>
        private static readonly TimeSpan ConfigTtl = TimeSpan.FromSeconds(60);

        /// <summary>SinkId→工作器</summary>
        private readonly ConcurrentDictionary<long, SinkWorker> _workers = new();

        private CancellationTokenSource _cts;
        private Task _refreshTask;
        private volatile bool _reloadRequested;

        #region 生命周期

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _refreshTask = Task.Run(() => RefreshLoopAsync(_cts.Token));
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "北向转发服务已启动", Service_CATEGORY);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _cts?.Cancel();
                if (_refreshTask != null) await Task.WhenAny(_refreshTask, Task.Delay(3000, cancellationToken));
                foreach (var worker in _workers.Values) worker.Dispose();
                _workers.Clear();
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.Message, Service_CATEGORY); }
        }

        /// <summary>
        /// 请求立即刷新目的地配置(管理接口保存后调用)
        /// </summary>
        public void Reload() => _reloadRequested = true;

        /// <summary>
        /// 配置刷新循环(变更检测:行JSON签名变化才重建工作器,避免无谓断开长连接)
        /// </summary>
        private async Task RefreshLoopAsync(CancellationToken token)
        {
            var lastrefresh = DateTime.MinValue;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_reloadRequested || DateTime.Now - lastrefresh >= ConfigTtl)
                    {
                        _reloadRequested = false;
                        lastrefresh = DateTime.Now;
                        RefreshWorkers();
                    }
                }
                catch (Exception ex) { LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.Message, Service_CATEGORY); }
                try { await Task.Delay(1000, token); }
                catch (OperationCanceledException) { break; }
            }
        }

        /// <summary>
        /// 按最新配置增删改工作器
        /// </summary>
        private void RefreshWorkers()
        {
            var sinks = (NorthboundSinkDAO.Instance.GetList()?.Cast<NorthboundSink>().ToList() ?? new List<NorthboundSink>())
                .Where(t => t.IsEnable).ToList();
            var aliveids = new HashSet<long>(sinks.Select(t => t.SnowId));

            foreach (var pair in _workers)
            {
                if (!aliveids.Contains(pair.Key) && _workers.TryRemove(pair.Key, out var removed))
                {
                    removed.Dispose();
                    LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"目的地[{removed.Sink.SinkName}]已停用,工作器销毁", Service_CATEGORY);
                }
            }
            foreach (var sink in sinks)
            {
                string signature = sink.ToJson();
                if (_workers.TryGetValue(sink.SnowId, out var exist))
                {
                    if (exist.Signature == signature) continue;
                    exist.Dispose();
                    _workers.TryRemove(sink.SnowId, out _);
                }
                var worker = new SinkWorker(sink, signature);
                if (_workers.TryAdd(sink.SnowId, worker)) worker.Start();
                else worker.Dispose();
            }
        }

        #endregion

        #region 转发入口

        /// <summary>
        /// 转发一批遥测点(由入库管道在push_strategy过闸后调用,入队即返回不阻塞)
        /// </summary>
        public void ForwardTelemetry(int deviceid, string typecode, List<TelemetryPoint> points)
        {
            if (_workers.IsEmpty || !points.IsZxxAny()) return;
            foreach (var worker in _workers.Values)
            {
                if (worker.Sink.ContentMode == 2) continue;
                if (!worker.MatchScope(deviceid, typecode)) continue;
                worker.AcceptTelemetry(deviceid, points);
            }
        }

        /// <summary>
        /// 转发一条告警事件(由入库管道在屏蔽裁决后调用,入队即返回不阻塞)
        /// </summary>
        public void ForwardAlarm(int deviceid, string typecode, EventSignal signal)
        {
            if (_workers.IsEmpty || signal == null) return;
            foreach (var worker in _workers.Values)
            {
                if (worker.Sink.ContentMode == 1) continue;
                if (!worker.MatchScope(deviceid, typecode)) continue;
                worker.AcceptAlarm(deviceid, signal);
            }
        }

        /// <summary>
        /// 北向设备状态事件出口（与遥测/告警并列的第三出口）：上线/离线/掉电作为客观事实转发，
        /// 不查告警字典、不进告警生命周期、不受屏蔽规则影响。devicestate: 2=在线,1=掉电,0=离线。
        /// </summary>
        public void ForwardDeviceState(int deviceid, string typecode, int devicestate, string reason)
        {
            if (_workers.IsEmpty) return;
            foreach (var worker in _workers.Values)
            {
                if (worker.Sink.ContentMode == 1) continue;              // 1=仅遥测,状态事件属事件类,跳过
                if (!worker.MatchScope(deviceid, typecode)) continue;
                worker.AcceptDeviceState(deviceid, devicestate, reason);
            }
        }

        /// <summary>构造设备状态北向报文（纯函数，供 AcceptDeviceState 复用并可单测）。</summary>
        public static string BuildDeviceStatePayload(int deviceid, int devicestate, string reason)
            => new { msgType = "deviceState", deviceId = deviceid, deviceState = devicestate, reason }.ToJson();

        /// <summary>
        /// 队列水位快照(每目的地:在线状态/内存积压/落盘积压/累计计数)
        /// </summary>
        public List<SinkStatus> GetStatus()
        {
            return _workers.Values.Select(t => t.Snapshot()).ToList();
        }

        #endregion

        #region 测试连接与样例报文

        /// <summary>
        /// 测试结果("测试连接/发送样例报文"返回体,含将发出的JSON预览)
        /// </summary>
        public class SinkTestResult
        {
            /// <summary>是否成功</summary>
            public bool Success { get; set; }
            /// <summary>结果说明</summary>
            public string Message { get; set; }
            /// <summary>样例主题(仅MQTT)</summary>
            public string SampleTopic { get; set; }
            /// <summary>样例报文JSON预览</summary>
            public string SamplePayload { get; set; }
        }

        /// <summary>
        /// 构建样例报文预览(§10.2向导④:干跑不发送,展示将发出的JSON)
        /// </summary>
        public SinkTestResult BuildSample(long sinksnowid)
        {
            var sink = NorthboundSinkDAO.Instance.GetOneBy(t => t.SnowId == sinksnowid);
            if (sink == null) return new SinkTestResult { Success = false, Message = "目的地不存在" };
            var item = BuildSampleItem(sink);
            return new SinkTestResult { Success = true, Message = "样例预览", SampleTopic = item.Topic, SamplePayload = item.Payload };
        }

        /// <summary>
        /// 测试连接并实际发送一条样例报文(临时发送器即用即弃,不影响工作器状态)
        /// </summary>
        public async Task<SinkTestResult> TestSendAsync(long sinksnowid)
        {
            var sink = NorthboundSinkDAO.Instance.GetOneBy(t => t.SnowId == sinksnowid);
            if (sink == null) return new SinkTestResult { Success = false, Message = "目的地不存在" };
            var item = BuildSampleItem(sink);
            ISinkSender sender = sink.SinkType == 1
                ? new MqttSinkSender(sink.ConnConfig?.ToObject<SinkMqttConfig>() ?? new SinkMqttConfig())
                : new HttpSinkSender(sink.ConnConfig?.ToObject<SinkHttpConfig>() ?? new SinkHttpConfig());
            try
            {
                bool ok = await sender.SendAsync(new List<ForwardItem> { item }, CancellationToken.None);
                return new SinkTestResult
                {
                    Success = ok,
                    Message = ok ? "样例报文发送成功" : "连接或发送失败,请检查地址与认证配置",
                    SampleTopic = item.Topic,
                    SamplePayload = item.Payload
                };
            }
            finally { sender.Dispose(); }
        }

        /// <summary>
        /// 构建样例遥测消息(deviceId=0占位)
        /// </summary>
        private static ForwardItem BuildSampleItem(NorthboundSink sink)
        {
            var mqttcfg = sink.SinkType == 1 ? sink.ConnConfig?.ToObject<SinkMqttConfig>() : null;
            string datatopic = mqttcfg?.DataTopic.IsZxxNullOrEmpty() == false ? mqttcfg.DataTopic : "iot/data/{deviceId}";
            return new ForwardItem
            {
                Topic = datatopic.Replace("{deviceId}", "0"),
                Payload = new
                {
                    msgType = "telemetry",
                    deviceId = 0,
                    paramCode = "sample",
                    ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    value = 1.0,
                    valueStr = "",
                    quality = 0
                }.ToJson()
            };
        }

        #endregion

        #region 状态模型

        /// <summary>
        /// 目的地水位快照
        /// </summary>
        public class SinkStatus
        {
            /// <summary>目的地主键</summary>
            public string SinkId { get; set; }
            /// <summary>目的地名称</summary>
            public string SinkName { get; set; }
            /// <summary>是否在线(最近一次发送成功)</summary>
            public bool Online { get; set; }
            /// <summary>内存队列积压条数</summary>
            public int MemCount { get; set; }
            /// <summary>落盘缓存积压条数</summary>
            public long CacheCount { get; set; }
            /// <summary>缓存文件字节数</summary>
            public long CacheBytes { get; set; }
            /// <summary>累计入队</summary>
            public long Enqueued { get; set; }
            /// <summary>累计发出</summary>
            public long Sent { get; set; }
            /// <summary>累计落盘</summary>
            public long Spilled { get; set; }
            /// <summary>累计重传成功</summary>
            public long Retransmitted { get; set; }
            /// <summary>累计滚动丢弃</summary>
            public long Dropped { get; set; }
        }

        /// <summary>
        /// 待转发消息(Topic仅MQTT使用,HTTP整批POST)
        /// </summary>
        private class ForwardItem
        {
            public string Topic;
            public string Payload;
        }

        /// <summary>
        /// SQLite落盘缓存行(每目的地独立文件Config/NorthboundCache/sink_{id}.db)
        /// </summary>
        [SugarTable(TableName = "forward_cache")]
        public class NorthboundCacheRow
        {
            [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
            public long Id { get; set; }
            [SugarColumn(IsNullable = true, ColumnDataType = "varchar(300)")]
            public string Topic { get; set; }
            [SugarColumn(IsNullable = true, ColumnDataType = "text")]
            public string Payload { get; set; }
            [SugarColumn(IsNullable = true, ColumnDataType = "varchar(30)")]
            public string CreateTime { get; set; }
        }

        #endregion

        #region 目的地工作器

        /// <summary>
        /// 单目的地工作器:独立内存队列+SQLite缓存+发送循环(互不拖累);
        /// 循环序=溢出落盘→内存直发(失败落盘)→在线时重传落盘积压→周期性1GB滚动
        /// </summary>
        private class SinkWorker : IDisposable
        {
            public NorthboundSink Sink { get; }
            public string Signature { get; }

            private readonly ConcurrentQueue<ForwardItem> _queue = new();
            private int _memCount;
            private readonly ISinkSender _sender;
            private readonly HashSet<string> _scopeTypeCodes;
            private readonly HashSet<int> _scopeDeviceIds;
            private readonly string _dataTopic;
            private readonly string _eventTopic;
            private readonly string _cachePath;
            private SqlSugarClient _cacheDb;
            private CancellationTokenSource _cts;
            private bool _online;
            private bool _statusLogged;
            private int _rollCounter;

            private long _enqueued, _sent, _spilled, _retransmitted, _dropped;

            public SinkWorker(NorthboundSink sink, string signature)
            {
                Sink = sink;
                Signature = signature;
                _scopeTypeCodes = new HashSet<string>(ParseScopeStrings(sink, 1), StringComparer.OrdinalIgnoreCase);
                _scopeDeviceIds = new HashSet<int>(ParseScopeInts(sink));
                var mqttcfg = sink.SinkType == 1 ? sink.ConnConfig?.ToObject<SinkMqttConfig>() : null;
                _dataTopic = mqttcfg?.DataTopic.IsZxxNullOrEmpty() == false ? mqttcfg.DataTopic : "iot/data/{deviceId}";
                _eventTopic = mqttcfg?.EventTopic.IsZxxNullOrEmpty() == false ? mqttcfg.EventTopic : "iot/event/{deviceId}";
                _sender = sink.SinkType == 1
                    ? new MqttSinkSender(mqttcfg ?? new SinkMqttConfig())
                    : new HttpSinkSender(sink.ConnConfig?.ToObject<SinkHttpConfig>() ?? new SinkHttpConfig());
                _cachePath = Path.Combine(AppContext.BaseDirectory, "Config", "NorthboundCache", $"sink_{sink.SnowId}.db");
            }

            private static List<string> ParseScopeStrings(NorthboundSink sink, int scopetype)
            {
                if (sink.ScopeType != scopetype || sink.ScopeJson.IsZxxNullOrEmpty()) return new List<string>();
                try { return sink.ScopeJson.ToObject<List<string>>() ?? new List<string>(); }
                catch { return new List<string>(); }
            }

            private static List<int> ParseScopeInts(NorthboundSink sink)
            {
                if (sink.ScopeType != 2 || sink.ScopeJson.IsZxxNullOrEmpty()) return new List<int>();
                try { return sink.ScopeJson.ToObject<List<int>>() ?? new List<int>(); }
                catch { return new List<int>(); }
            }

            public void Start()
            {
                _cts = new CancellationTokenSource();
                _ = Task.Run(() => RunLoopAsync(_cts.Token));
            }

            /// <summary>
            /// 范围过滤(0全部/1按产品类型编码/2按设备ID)
            /// </summary>
            public bool MatchScope(int deviceid, string typecode)
            {
                return Sink.ScopeType switch
                {
                    1 => _scopeTypeCodes.Contains(typecode ?? ""),
                    2 => _scopeDeviceIds.Contains(deviceid),
                    _ => true
                };
            }

            public void AcceptTelemetry(int deviceid, List<TelemetryPoint> points)
            {
                string topic = _dataTopic.Replace("{deviceId}", deviceid.ToString());
                foreach (var point in points)
                {
                    Enqueue(new ForwardItem
                    {
                        Topic = topic,
                        Payload = new
                        {
                            msgType = "telemetry",
                            deviceId = point.DeviceId,
                            paramCode = point.ParamCode,
                            ts = point.Ts.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                            value = point.Value,
                            valueStr = point.ValueStr,
                            quality = point.Quality
                        }.ToJson()
                    });
                }
            }

            public void AcceptAlarm(int deviceid, EventSignal signal)
            {
                Enqueue(new ForwardItem
                {
                    Topic = _eventTopic.Replace("{deviceId}", deviceid.ToString()),
                    Payload = new { msgType = "alarm", data = signal }.ToJson()
                });
            }

            public void AcceptDeviceState(int deviceid, int devicestate, string reason)
            {
                Enqueue(new ForwardItem
                {
                    Topic = _eventTopic.Replace("{deviceId}", deviceid.ToString()),
                    Payload = NorthboundForwardService.BuildDeviceStatePayload(deviceid, devicestate, reason)
                });
            }

            private void Enqueue(ForwardItem item)
            {
                _queue.Enqueue(item);
                Interlocked.Increment(ref _memCount);
                Interlocked.Increment(ref _enqueued);
            }

            /// <summary>
            /// 发送主循环(200ms节拍,异常只记一次错误日志不退出)
            /// </summary>
            private async Task RunLoopAsync(CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        SpillOverflow();
                        var batch = Drain(SendBatchSize);
                        if (batch.Count > 0)
                        {
                            bool ok = await _sender.SendAsync(batch, token);
                            if (ok)
                            {
                                Interlocked.Add(ref _sent, batch.Count);
                                SetOnline(true, "");
                            }
                            else
                            {
                                Spill(batch);
                                SetOnline(false, "直发失败,已落盘缓存");
                            }
                        }
                        if (_online) await RetransmitAsync(token);
                        if (++_rollCounter >= 300)
                        {
                            _rollCounter = 0;
                            RollCacheIfOversize();
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        SetOnline(false, ex.Message);
                    }
                    try { await Task.Delay(200, token); }
                    catch (OperationCanceledException) { break; }
                }
            }

            private List<ForwardItem> Drain(int max)
            {
                var list = new List<ForwardItem>();
                while (list.Count < max && _queue.TryDequeue(out var item))
                {
                    Interlocked.Decrement(ref _memCount);
                    list.Add(item);
                }
                return list;
            }

            /// <summary>
            /// 内存超限落盘(整批2000条搬运,溢出由工作循环执行,瞬时越限容忍)
            /// </summary>
            private void SpillOverflow()
            {
                while (Volatile.Read(ref _memCount) > MemLimit)
                {
                    var batch = Drain(RetryBatchSize);
                    if (batch.Count == 0) break;
                    Spill(batch);
                }
            }

            /// <summary>
            /// 一批消息落盘SQLite
            /// </summary>
            private void Spill(List<ForwardItem> batch)
            {
                var db = EnsureCacheDb();
                string now = DateTime.Now.ToDateTimeString();
                var rows = batch.Select(t => new NorthboundCacheRow { Topic = t.Topic, Payload = t.Payload, CreateTime = now }).ToList();
                db.Insertable(rows).ExecuteCommand();
                Interlocked.Add(ref _spilled, rows.Count);
            }

            /// <summary>
            /// 重传落盘积压(最老2000条一批,发送成功才删)
            /// </summary>
            private async Task RetransmitAsync(CancellationToken token)
            {
                if (!File.Exists(_cachePath)) return;
                var db = EnsureCacheDb();
                var rows = db.Queryable<NorthboundCacheRow>().OrderBy(t => t.Id).Take(RetryBatchSize).ToList();
                if (!rows.IsZxxAny()) return;
                var batch = rows.Select(t => new ForwardItem { Topic = t.Topic, Payload = t.Payload }).ToList();
                bool ok = await _sender.SendAsync(batch, token);
                if (ok)
                {
                    db.Deleteable<NorthboundCacheRow>().In(rows.Select(t => t.Id).ToList()).ExecuteCommand();
                    Interlocked.Add(ref _retransmitted, rows.Count);
                }
                else
                {
                    SetOnline(false, "重传失败,等待下轮");
                }
            }

            /// <summary>
            /// 缓存文件超1GB滚动删除旧半并VACUUM回收
            /// </summary>
            private void RollCacheIfOversize()
            {
                if (!File.Exists(_cachePath) || new FileInfo(_cachePath).Length <= CacheFileLimit) return;
                var db = EnsureCacheDb();
                long minid = db.Queryable<NorthboundCacheRow>().Min(t => t.Id);
                long maxid = db.Queryable<NorthboundCacheRow>().Max(t => t.Id);
                long cut = minid + (maxid - minid) / 2;
                int dropped = db.Deleteable<NorthboundCacheRow>().Where(t => t.Id <= cut).ExecuteCommand();
                db.Ado.ExecuteCommand("VACUUM");
                Interlocked.Add(ref _dropped, dropped);
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"目的地[{Sink.SinkName}]缓存超限,滚动丢弃最旧{dropped}条", Service_CATEGORY);
            }

            /// <summary>
            /// 懒建SQLite缓存连接(仅工作循环线程访问)
            /// </summary>
            private SqlSugarClient EnsureCacheDb()
            {
                if (_cacheDb != null) return _cacheDb;
                Directory.CreateDirectory(Path.GetDirectoryName(_cachePath));
                _cacheDb = new SqlSugarClient(new ConnectionConfig
                {
                    ConnectionString = $"Data Source={_cachePath}",
                    DbType = SqlSugar.DbType.Sqlite,
                    IsAutoCloseConnection = true,
                    InitKeyType = InitKeyType.Attribute
                });
                _cacheDb.CodeFirst.InitTables<NorthboundCacheRow>();
                return _cacheDb;
            }

            /// <summary>
            /// 在线状态翻转才记日志(防失败刷盘)
            /// </summary>
            private void SetOnline(bool online, string reason)
            {
                if (_online == online && _statusLogged) return;
                _online = online;
                _statusLogged = true;
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"目的地[{Sink.SinkName}]{(online ? "连通" : $"中断({reason})")}", Service_CATEGORY);
            }

            public SinkStatus Snapshot()
            {
                long cachecount = 0, cachebytes = 0;
                try
                {
                    if (File.Exists(_cachePath))
                    {
                        cachebytes = new FileInfo(_cachePath).Length;
                        cachecount = _cacheDb?.Queryable<NorthboundCacheRow>().Count() ?? 0;
                    }
                }
                catch { }
                return new SinkStatus
                {
                    SinkId = Sink.SnowId.ToString(),
                    SinkName = Sink.SinkName,
                    Online = _online,
                    MemCount = Volatile.Read(ref _memCount),
                    CacheCount = cachecount,
                    CacheBytes = cachebytes,
                    Enqueued = Interlocked.Read(ref _enqueued),
                    Sent = Interlocked.Read(ref _sent),
                    Spilled = Interlocked.Read(ref _spilled),
                    Retransmitted = Interlocked.Read(ref _retransmitted),
                    Dropped = Interlocked.Read(ref _dropped)
                };
            }

            public void Dispose()
            {
                try { _cts?.Cancel(); } catch { }
                try { _sender?.Dispose(); } catch { }
                try { _cacheDb?.Dispose(); } catch { }
            }
        }

        #endregion

        #region 发送器

        /// <summary>
        /// 目的地发送器抽象(整批成功才true;失败/异常false由工作器落盘)
        /// </summary>
        private interface ISinkSender : IDisposable
        {
            Task<bool> SendAsync(List<ForwardItem> batch, CancellationToken token);
        }

        /// <summary>
        /// MQTT发送器(MQTTnet客户端,懒连接,逐条按Topic发布QoS1;异常整批判失败并弃连接待重建)
        /// </summary>
        private class MqttSinkSender : ISinkSender
        {
            private readonly SinkMqttConfig _config;
            private IMqttClient _client;

            public MqttSinkSender(SinkMqttConfig config) => _config = config;

            public async Task<bool> SendAsync(List<ForwardItem> batch, CancellationToken token)
            {
                if (_config.Host.IsZxxNullOrEmpty()) return false;
                try
                {
                    if (_client == null || !_client.IsConnected)
                    {
                        _client?.Dispose();
                        _client = new MqttClientFactory().CreateMqttClient();
                        var builder = new MqttClientOptionsBuilder()
                            .WithTcpServer(_config.Host, _config.Port)
                            .WithClientId(_config.ClientId.IsZxxNullOrEmpty() ? $"ZxxIotNorthbound_{Guid.NewGuid():N}" : _config.ClientId)
                            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                            .WithTimeout(TimeSpan.FromSeconds(10));
                        if (!_config.UserName.IsZxxNullOrEmpty()) builder = builder.WithCredentials(_config.UserName, _config.Password);
                        await _client.ConnectAsync(builder.Build(), token);
                    }
                    foreach (var item in batch)
                    {
                        var message = new MqttApplicationMessageBuilder()
                            .WithTopic(item.Topic)
                            .WithPayload(Encoding.UTF8.GetBytes(item.Payload ?? ""))
                            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                            .Build();
                        await _client.PublishAsync(message, token);
                    }
                    return true;
                }
                catch (OperationCanceledException) { throw; }
                catch
                {
                    try { _client?.Dispose(); } catch { }
                    _client = null;
                    return false;
                }
            }

            public void Dispose()
            {
                try { _client?.Dispose(); } catch { }
            }
        }

        /// <summary>
        /// HTTP Webhook发送器(整批POST JSON数组,2xx即成功)
        /// </summary>
        private class HttpSinkSender : ISinkSender
        {
            private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
            private readonly SinkHttpConfig _config;

            public HttpSinkSender(SinkHttpConfig config) => _config = config;

            public async Task<bool> SendAsync(List<ForwardItem> batch, CancellationToken token)
            {
                if (_config.Url.IsZxxNullOrEmpty()) return false;
                try
                {
                    string body = "[" + string.Join(",", batch.Select(t => t.Payload)) + "]";
                    using var request = new HttpRequestMessage(HttpMethod.Post, _config.Url)
                    {
                        Content = new StringContent(body, Encoding.UTF8, "application/json")
                    };
                    if (_config.Headers.IsZxxAny())
                    {
                        foreach (var pair in _config.Headers) request.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
                    }
                    using var response = await Http.SendAsync(request, token);
                    return response.IsSuccessStatusCode;
                }
                catch (OperationCanceledException) { throw; }
                catch { return false; }
            }

            public void Dispose() { }
        }

        #endregion
    }
}
