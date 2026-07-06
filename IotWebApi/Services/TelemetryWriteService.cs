using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using Npgsql;
using NpgsqlTypes;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace IotWebApi.Services
{
    /// <summary>
    /// 遥测点位(对应 iot_ts.telemetry 窄表一行)
    /// </summary>
    public class TelemetryPoint
    {
        /// <summary>
        /// 设备主键
        /// </summary>
        public long DeviceId { get; set; }
        /// <summary>
        /// 参数编码
        /// </summary>
        public string ParamCode { get; set; }
        /// <summary>
        /// 参数名称(仅点位映射首建时落库)
        /// </summary>
        public string ParamName { get; set; }
        /// <summary>
        /// 采集时间(UTC,timestamptz列要求UTC Kind)
        /// </summary>
        public DateTime Ts { get; set; }
        /// <summary>
        /// 数值型点位值
        /// </summary>
        public double? Value { get; set; }
        /// <summary>
        /// 状态/字符串型点位值
        /// </summary>
        public string ValueStr { get; set; }
        /// <summary>
        /// 质量戳(0=正常)
        /// </summary>
        public short Quality { get; set; }
    }

    /// <summary>
    /// 遥测批量写入服务(有界队列攒批,Npgsql Binary COPY 写 TimescaleDB 遥测窄表;
    /// 未配置 DbSetting.TimescaleConString 时整体不启用,不影响现有 MySQL/Tidb 链路)
    /// </summary>
    public class TelemetryWriteService : IHostedService
    {
        private const string Service_CATEGORY = "遥测写入服务";

        /// <summary>
        /// 队列容量上限(遥测可丢:队列满丢最旧,保护数据库不被瞬时洪峰压垮)
        /// </summary>
        private const int QueueCapacity = 50000;

        /// <summary>
        /// 单批最大行数(与攒批时长先到为准,严禁逐行INSERT)
        /// </summary>
        private const int BatchSize = 5000;

        /// <summary>
        /// 攒批最长等待时长
        /// </summary>
        private static readonly TimeSpan BatchWindow = TimeSpan.FromSeconds(2);

        /// <summary>
        /// 遥测点位有界队列
        /// </summary>
        private readonly Channel<TelemetryPoint> _channel;

        /// <summary>
        /// 点位映射内存缓存((设备,参数编码)→point_id,首见落库终身复用)
        /// </summary>
        private readonly ConcurrentDictionary<(long DeviceId, string ParamCode), int> _pointCache = new();

        /// <summary>
        /// Timescale连接字符串(空=服务未启用)
        /// </summary>
        private readonly string _connString;

        /// <summary>
        /// 消费任务取消令牌
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// 后台消费任务
        /// </summary>
        private Task _consumeTask;

        public TelemetryWriteService()
        {
            _connString = DbSetting.Current.TimescaleConString;
            _channel = Channel.CreateBounded<TelemetryPoint>(new BoundedChannelOptions(QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
        }

        /// <summary>
        /// 服务是否启用(已配置Timescale连接字符串)
        /// </summary>
        public bool Enabled => !_connString.IsZxxNullOrEmpty();

        #region 服务生命周期

        /// <summary>
        /// 启动后台消费任务(未配置连接字符串时不启动)
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Enabled)
            {
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "未配置Timescale连接字符串,遥测写入服务未启用", Service_CATEGORY);
                return Task.CompletedTask;
            }
            _cts = new CancellationTokenSource();
            _consumeTask = Task.Run(() => ConsumeLoopAsync(_cts.Token));
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "遥测写入服务已启动", Service_CATEGORY);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止后台消费任务(等待正在写入的批次完成)
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!Enabled) return;
            _channel.Writer.TryComplete();
            _cts?.Cancel();
            if (_consumeTask != null)
            {
                try { await _consumeTask; } catch (OperationCanceledException) { }
            }
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "遥测写入服务已停止", Service_CATEGORY);
        }

        #endregion

        #region 消息入队

        /// <summary>
        /// 遥测点位批量入队(队列满时自动丢弃最旧点位;服务未启用时直接忽略)
        /// </summary>
        public void Enqueue(List<TelemetryPoint> points)
        {
            if (!Enabled || !points.IsZxxAny()) return;
            foreach (var point in points)
            {
                _channel.Writer.TryWrite(point);
            }
        }

        #endregion

        #region 批量写入

        /// <summary>
        /// 消费主循环(攒够BatchSize行或到达BatchWindow时长,先到为准即写一批)
        /// </summary>
        private async Task ConsumeLoopAsync(CancellationToken token)
        {
            var reader = _channel.Reader;
            try
            {
                while (await reader.WaitToReadAsync(token))
                {
                    var batch = new List<TelemetryPoint>(BatchSize);
                    var deadline = DateTime.UtcNow.Add(BatchWindow);
                    while (batch.Count < BatchSize)
                    {
                        while (batch.Count < BatchSize && reader.TryRead(out var point))
                        {
                            batch.Add(point);
                        }
                        if (batch.Count >= BatchSize) break;

                        var remaining = deadline - DateTime.UtcNow;
                        if (remaining <= TimeSpan.Zero) break;
                        using var window = CancellationTokenSource.CreateLinkedTokenSource(token);
                        window.CancelAfter(remaining);
                        try
                        {
                            if (!await reader.WaitToReadAsync(window.Token)) break;
                        }
                        catch (OperationCanceledException) when (!token.IsCancellationRequested)
                        {
                            break;  // 攒批窗口到时，按当前批量写入
                        }
                    }
                    if (!batch.IsZxxAny()) continue;
                    try
                    {
                        await WriteBatchAsync(batch, token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"批量写入失败(丢弃{batch.Count}行)：{ex}", Service_CATEGORY);
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
        /// 一批遥测点位经Binary COPY写入telemetry(先补齐点位映射)
        /// </summary>
        private async Task WriteBatchAsync(List<TelemetryPoint> batch, CancellationToken token)
        {
            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync(token);
            await EnsurePointIdsAsync(conn, batch, token);

            // Npgsql 5.x(SqlSugar传递引用)无BeginBinaryImportAsync(6.0新增),同步开启COPY后仍可异步写行
            await using var writer = conn.BeginBinaryImport(
                "COPY iot_ts.telemetry (device_id, point_id, ts, value, value_str, quality) FROM STDIN (FORMAT BINARY)");
            foreach (var point in batch)
            {
                if (!_pointCache.TryGetValue((point.DeviceId, point.ParamCode), out int pointid)) continue;
                await writer.StartRowAsync(token);
                await writer.WriteAsync(point.DeviceId, NpgsqlDbType.Bigint, token);
                await writer.WriteAsync(pointid, NpgsqlDbType.Integer, token);
                await writer.WriteAsync(point.Ts, NpgsqlDbType.TimestampTz, token);
                if (point.Value.HasValue) await writer.WriteAsync(point.Value.Value, NpgsqlDbType.Double, token);
                else await writer.WriteNullAsync(token);
                if (point.ValueStr.IsZxxNullOrEmpty()) await writer.WriteNullAsync(token);
                else await writer.WriteAsync(point.ValueStr, NpgsqlDbType.Text, token);
                await writer.WriteAsync(point.Quality, NpgsqlDbType.Smallint, token);
            }
            await writer.CompleteAsync(token);
        }

        /// <summary>
        /// 补齐本批点位映射(缓存未命中的(设备,参数编码)UPSERT进point_map并回填point_id)
        /// </summary>
        private async Task EnsurePointIdsAsync(NpgsqlConnection conn, List<TelemetryPoint> batch, CancellationToken token)
        {
            foreach (var group in batch.GroupBy(t => (t.DeviceId, t.ParamCode)))
            {
                if (group.Key.ParamCode.IsZxxNullOrEmpty() || _pointCache.ContainsKey(group.Key)) continue;
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO iot_ts.point_map (device_id, param_code, param_name) VALUES (@did, @code, @name) " +
                    "ON CONFLICT (device_id, param_code) DO UPDATE SET param_name = EXCLUDED.param_name RETURNING point_id", conn);
                cmd.Parameters.AddWithValue("did", group.Key.DeviceId);
                cmd.Parameters.AddWithValue("code", group.Key.ParamCode);
                cmd.Parameters.AddWithValue("name", group.First().ParamName ?? "");
                var result = await cmd.ExecuteScalarAsync(token);
                if (result != null) _pointCache[group.Key] = Convert.ToInt32(result);
            }
        }

        #endregion
    }
}
