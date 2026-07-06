using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using Npgsql;
using NpgsqlTypes;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace IotWebApi.Services
{
    /// <summary>
    /// 最新值缓存服务(内存ConcurrentDictionary实时更新,每2秒把变化点位批量刷
    /// Redis设备哈希键与iot_ts.telemetry_latest;实时值查询走本服务不扫时序表)
    /// </summary>
    public class TelemetryLatestService : IHostedService
    {
        private const string Service_CATEGORY = "最新值缓存服务";

        /// <summary>
        /// 刷新周期(攒2秒内的变化点位一次性落库)
        /// </summary>
        private static readonly TimeSpan FlushWindow = TimeSpan.FromSeconds(2);

        /// <summary>
        /// 最新值内存缓存((设备,参数编码)→最新点位)
        /// </summary>
        private readonly ConcurrentDictionary<(long DeviceId, string ParamCode), TelemetryPoint> _latest = new();

        /// <summary>
        /// 待刷新脏点位集合(上次刷新后有变化的键)
        /// </summary>
        private readonly ConcurrentDictionary<(long DeviceId, string ParamCode), byte> _dirty = new();

        /// <summary>
        /// 点位映射解析器(与遥测写入服务共用同一份缓存)
        /// </summary>
        private readonly TelemetryPointMap _pointMap;

        /// <summary>
        /// Timescale连接字符串(空=不刷telemetry_latest,内存与Redis仍然可用)
        /// </summary>
        private readonly string _connString;

        /// <summary>
        /// 刷新任务取消令牌
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// 后台刷新任务
        /// </summary>
        private Task _flushTask;

        public TelemetryLatestService(TelemetryPointMap pointMap)
        {
            _pointMap = pointMap;
            _connString = DbSetting.Current.TimescaleConString;
        }

        #region 服务生命周期

        /// <summary>
        /// 启动后台刷新任务(内存缓存始终可用,Redis/PG在刷新时各自判断可用性)
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _flushTask = Task.Run(() => FlushLoopAsync(_cts.Token));
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "最新值缓存服务已启动", Service_CATEGORY);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止后台刷新任务
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            if (_flushTask != null)
            {
                try { await _flushTask; } catch (OperationCanceledException) { }
            }
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "最新值缓存服务已停止", Service_CATEGORY);
        }

        #endregion

        #region 内存缓存读写

        /// <summary>
        /// 批量更新最新值(按采集时间比较,乱序到达的旧值不覆盖新值)
        /// </summary>
        public void Update(List<TelemetryPoint> points)
        {
            if (!points.IsZxxAny()) return;
            foreach (var point in points)
            {
                if (point.ParamCode.IsZxxNullOrEmpty()) continue;
                var key = (point.DeviceId, point.ParamCode);
                _latest.AddOrUpdate(key, point, (_, exist) => point.Ts >= exist.Ts ? point : exist);
                _dirty[key] = 1;
            }
        }

        /// <summary>
        /// 查询一台设备的全部点位最新值
        /// </summary>
        public List<TelemetryPoint> GetLatest(long deviceid)
        {
            return _latest.Where(t => t.Key.DeviceId == deviceid).Select(t => t.Value).ToList();
        }

        /// <summary>
        /// 查询单个点位最新值(无数据返回null)
        /// </summary>
        public TelemetryPoint GetLatest(long deviceid, string paramcode)
        {
            return _latest.TryGetValue((deviceid, paramcode), out var point) ? point : null;
        }

        #endregion

        #region 批量刷新

        /// <summary>
        /// 刷新主循环(每轮取走全部脏点位,分别刷Redis与telemetry_latest,互不拖累)
        /// </summary>
        private async Task FlushLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(FlushWindow, token);
                    var keys = _dirty.Keys.ToList();
                    if (!keys.IsZxxAny()) continue;

                    var points = new List<TelemetryPoint>(keys.Count);
                    foreach (var key in keys)
                    {
                        // 先摘脏标记再取值：取值后新到的更新会重新置脏，不丢变化
                        _dirty.TryRemove(key, out _);
                        if (_latest.TryGetValue(key, out var point)) points.Add(point);
                    }
                    if (!points.IsZxxAny()) continue;

                    try
                    {
                        await FlushRedisAsync(points);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"刷新Redis失败：{ex}", Service_CATEGORY);
                    }
                    try
                    {
                        await FlushPgAsync(points, token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"刷新telemetry_latest失败：{ex}", Service_CATEGORY);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 服务停止，正常退出
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"刷新循环意外退出：{ex}", Service_CATEGORY);
            }
        }

        /// <summary>
        /// 变化点位刷入Redis(设备哈希键iot:latest:{deviceId},field=参数编码;
        /// RedisService自带熔断,不可用时返回null直接跳过)
        /// </summary>
        private static async Task FlushRedisAsync(List<TelemetryPoint> points)
        {
            var redis = RedisHelper.RedisService;
            if (redis == null) return;
            foreach (var group in points.GroupBy(t => t.DeviceId))
            {
                var entries = group.Select(t => new HashEntry(t.ParamCode, t.ToJson())).ToArray();
                await redis.HashSetAsync($"iot:latest:{group.Key}", entries);
            }
        }

        /// <summary>
        /// 变化点位经unnest数组一条UPSERT刷入telemetry_latest(带ts守卫,旧值不回退)
        /// </summary>
        private async Task FlushPgAsync(List<TelemetryPoint> points, CancellationToken token)
        {
            if (_connString.IsZxxNullOrEmpty()) return;

            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync(token);
            await _pointMap.EnsureAsync(conn, points, token);

            var rows = points.Where(t => _pointMap.TryGet(t.DeviceId, t.ParamCode, out _)).ToList();
            if (!rows.IsZxxAny()) return;

            var dids = new long[rows.Count];
            var pids = new int[rows.Count];
            var tss = new DateTime[rows.Count];
            var vals = new double?[rows.Count];
            var strs = new string[rows.Count];
            var quals = new short[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                _pointMap.TryGet(rows[i].DeviceId, rows[i].ParamCode, out int pointid);
                dids[i] = rows[i].DeviceId;
                pids[i] = pointid;
                tss[i] = rows[i].Ts;
                vals[i] = rows[i].Value;
                strs[i] = rows[i].ValueStr;
                quals[i] = rows[i].Quality;
            }

            await using var cmd = new NpgsqlCommand(
                "INSERT INTO iot_ts.telemetry_latest (device_id, point_id, ts, value, value_str, quality) " +
                "SELECT d, p, t, v, s, q FROM unnest(@dids, @pids, @tss, @vals, @strs, @quals) AS x(d, p, t, v, s, q) " +
                "ON CONFLICT (device_id, point_id) DO UPDATE " +
                "SET ts = EXCLUDED.ts, value = EXCLUDED.value, value_str = EXCLUDED.value_str, quality = EXCLUDED.quality " +
                "WHERE telemetry_latest.ts <= EXCLUDED.ts", conn);
            cmd.Parameters.Add(new NpgsqlParameter("dids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = dids });
            cmd.Parameters.Add(new NpgsqlParameter("pids", NpgsqlDbType.Array | NpgsqlDbType.Integer) { Value = pids });
            cmd.Parameters.Add(new NpgsqlParameter("tss", NpgsqlDbType.Array | NpgsqlDbType.TimestampTz) { Value = tss });
            cmd.Parameters.Add(new NpgsqlParameter("vals", NpgsqlDbType.Array | NpgsqlDbType.Double) { Value = vals });
            cmd.Parameters.Add(new NpgsqlParameter("strs", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = strs });
            cmd.Parameters.Add(new NpgsqlParameter("quals", NpgsqlDbType.Array | NpgsqlDbType.Smallint) { Value = quals });
            await cmd.ExecuteNonQueryAsync(token);
        }

        #endregion
    }
}
