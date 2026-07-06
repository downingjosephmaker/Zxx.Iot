using CenBoCommon.Zxx;
using Npgsql;
using System.Collections.Concurrent;

namespace IotWebApi.Services
{
    /// <summary>
    /// 点位映射解析器((设备,参数编码)→point_id,内存缓存首见UPSERT进iot_ts.point_map;
    /// 遥测写入器与最新值刷新共用同一份缓存)
    /// </summary>
    public class TelemetryPointMap
    {
        /// <summary>
        /// 点位映射内存缓存(首见落库终身复用)
        /// </summary>
        private readonly ConcurrentDictionary<(long DeviceId, string ParamCode), int> _cache = new();

        /// <summary>
        /// 取点位ID(仅查内存缓存,未命中说明映射尚未落库)
        /// </summary>
        public bool TryGet(long deviceid, string paramcode, out int pointid)
        {
            return _cache.TryGetValue((deviceid, paramcode), out pointid);
        }

        /// <summary>
        /// 补齐一批点位映射(缓存未命中的(设备,参数编码)UPSERT进point_map并回填point_id)
        /// </summary>
        public async Task EnsureAsync(NpgsqlConnection conn, IEnumerable<TelemetryPoint> points, CancellationToken token)
        {
            foreach (var group in points.GroupBy(t => (t.DeviceId, t.ParamCode)))
            {
                if (group.Key.ParamCode.IsZxxNullOrEmpty() || _cache.ContainsKey(group.Key)) continue;
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO iot_ts.point_map (device_id, param_code, param_name) VALUES (@did, @code, @name) " +
                    "ON CONFLICT (device_id, param_code) DO UPDATE SET param_name = EXCLUDED.param_name RETURNING point_id", conn);
                cmd.Parameters.AddWithValue("did", group.Key.DeviceId);
                cmd.Parameters.AddWithValue("code", group.Key.ParamCode);
                cmd.Parameters.AddWithValue("name", group.First().ParamName ?? "");
                var result = await cmd.ExecuteScalarAsync(token);
                if (result != null) _cache[group.Key] = Convert.ToInt32(result);
            }
        }
    }
}
