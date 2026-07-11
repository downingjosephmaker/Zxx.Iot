using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using Npgsql;
using NpgsqlTypes;

namespace IotWebApi.Services
{
    /// <summary>
    /// 遥测历史查询服务(设备中心历史曲线tab/组态C-6历史数据集:
    /// iot_ts.telemetry原始点(保留30天)+telemetry_1h小时聚合(长期);
    /// Timescale无ORM,走裸Npgsql参数化;point_id经SQL JOIN point_map解析,
    /// 不依赖TelemetryPointMap内存缓存——重启后冷缓存也可查)
    /// </summary>
    public class TelemetryQueryService
    {
        private const string CATEGORY = "遥测历史查询";

        /// <summary>原始表保留窗口(与V0002 retention策略一致,超窗只有聚合)</summary>
        private static readonly TimeSpan RawRetention = TimeSpan.FromDays(30);

        /// <summary>auto模式跨度不超过该值走原始点,否则走1h聚合</summary>
        private static readonly TimeSpan AutoRawSpan = TimeSpan.FromHours(48);

        /// <summary>单次原始点查询上限(48h/秒级也不过17万,截断护栏防拖库)</summary>
        private const int RawLimit = 50000;

        private readonly string _connString = DbSetting.Current.TimescaleConString;

        /// <summary>Timescale未配置时禁用(与TelemetryWriteService同口径)</summary>
        public bool Enabled => !_connString.IsZxxNullOrEmpty();

        /// <summary>历史点(原始模式=Ts/Value/ValueStr;聚合模式=Value取avg_v,另带Min/Max/Last/Cnt)</summary>
        public sealed class HistoryPoint
        {
            /// <summary>本地时间(原始=采集时刻,聚合=小时桶起点)</summary>
            public string Ts { get; set; } = "";
            /// <summary>数值(聚合模式=桶内均值,便于曲线直接用)</summary>
            public double? Value { get; set; }
            /// <summary>字符串值(仅原始模式)</summary>
            public string ValueStr { get; set; }
            /// <summary>桶内最小值(仅聚合模式)</summary>
            public double? Min { get; set; }
            /// <summary>桶内最大值(仅聚合模式)</summary>
            public double? Max { get; set; }
            /// <summary>桶内末值(仅聚合模式)</summary>
            public double? Last { get; set; }
            /// <summary>桶内点数(仅聚合模式)</summary>
            public long Cnt { get; set; }
        }

        /// <summary>历史查询结果</summary>
        public sealed class HistoryResult
        {
            /// <summary>实际执行的模式(raw=原始点/hour=小时聚合)</summary>
            public string Mode { get; set; } = "raw";
            /// <summary>点位序列(按时间正序)</summary>
            public List<HistoryPoint> Points { get; set; } = new();
        }

        /// <summary>
        /// 查询单设备单参数历史(mode:auto=跨度≤48h且起点在30天保留窗内走原始点,否则1h聚合;
        /// raw/hour=显式指定(显式raw超保留窗如实返回空);入参本地时间,内部转UTC匹配timestamptz)
        /// </summary>
        public async Task<HistoryResult> QueryAsync(long deviceid, string paramcode,
            DateTime startLocal, DateTime endLocal, string mode)
        {
            var result = new HistoryResult();
            if (!Enabled || deviceid <= 0 || paramcode.IsZxxNullOrEmpty() || endLocal <= startLocal)
            {
                return result;
            }

            var stUtc = DateTime.SpecifyKind(startLocal, DateTimeKind.Local).ToUniversalTime();
            var etUtc = DateTime.SpecifyKind(endLocal, DateTimeKind.Local).ToUniversalTime();

            bool useRaw = string.Equals(mode, "raw", StringComparison.OrdinalIgnoreCase);
            if (mode.IsZxxNullOrEmpty() || string.Equals(mode, "auto", StringComparison.OrdinalIgnoreCase))
            {
                useRaw = etUtc - stUtc <= AutoRawSpan && stUtc >= DateTime.UtcNow - RawRetention;
            }
            result.Mode = useRaw ? "raw" : "hour";

            try
            {
                await using var conn = new NpgsqlConnection(_connString);
                await conn.OpenAsync();
                string sql = useRaw
                    ? "SELECT t.ts, t.value, t.value_str FROM iot_ts.telemetry t " +
                      "JOIN iot_ts.point_map m ON m.point_id = t.point_id AND m.device_id = t.device_id " +
                      "WHERE t.device_id = @did AND m.param_code = @code AND t.ts >= @st AND t.ts < @et " +
                      $"ORDER BY t.ts LIMIT {RawLimit}"
                    : "SELECT h.bucket, h.avg_v, h.min_v, h.max_v, h.last_v, h.cnt FROM iot_ts.telemetry_1h h " +
                      "JOIN iot_ts.point_map m ON m.point_id = h.point_id AND m.device_id = h.device_id " +
                      "WHERE h.device_id = @did AND m.param_code = @code AND h.bucket >= @st AND h.bucket < @et " +
                      "ORDER BY h.bucket";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("did", deviceid);
                cmd.Parameters.AddWithValue("code", paramcode.Trim());
                // Npgsql 5.x的AddWithValue(DateTime)无视Kind按无时区timestamp发送,PG会按会话时区错位8小时,必须显式TimestampTz
                cmd.Parameters.Add(new NpgsqlParameter("st", NpgsqlDbType.TimestampTz) { Value = stUtc });
                cmd.Parameters.Add(new NpgsqlParameter("et", NpgsqlDbType.TimestampTz) { Value = etUtc });
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (useRaw)
                    {
                        result.Points.Add(new HistoryPoint
                        {
                            Ts = reader.GetDateTime(0).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                            Value = reader.IsDBNull(1) ? null : reader.GetDouble(1),
                            ValueStr = reader.IsDBNull(2) ? null : reader.GetString(2)
                        });
                    }
                    else
                    {
                        result.Points.Add(new HistoryPoint
                        {
                            Ts = reader.GetDateTime(0).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                            Value = reader.IsDBNull(1) ? null : reader.GetDouble(1),
                            Min = reader.IsDBNull(2) ? null : reader.GetDouble(2),
                            Max = reader.IsDBNull(3) ? null : reader.GetDouble(3),
                            Last = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                            Cnt = reader.IsDBNull(5) ? 0 : reader.GetInt64(5)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), CATEGORY);
            }
            return result;
        }

        /// <summary>日用量行(Day=本地日期;Value=当日末表码-上日末表码,首日无基准或表码回退(换表/清零)时为null)</summary>
        public sealed class DailyUsageRow
        {
            /// <summary>本地日期(yyyy-MM-dd)</summary>
            public string Day { get; set; } = "";
            /// <summary>设备ID</summary>
            public long DeviceId { get; set; }
            /// <summary>当日用量(累积表码日末差分)</summary>
            public double? Value { get; set; }
        }

        /// <summary>
        /// 查询多设备单参数按日用量(D-7报表数据集:基于telemetry_1h长期聚合的日末last_v差分,
        /// 适用电能等累积表码点位;窗口前移一天取前日末值作首日差分基准;
        /// bucket是UTC timestamptz,归日必须AT TIME ZONE本地时区,否则日边界错8小时)
        /// </summary>
        public async Task<List<DailyUsageRow>> QueryDailyUsageAsync(List<long> deviceids, string paramcode,
            DateTime startLocal, DateTime endLocal)
        {
            var rows = new List<DailyUsageRow>();
            if (!Enabled || !deviceids.IsZxxAny() || paramcode.IsZxxNullOrEmpty() || endLocal < startLocal)
            {
                return rows;
            }

            var dayFrom = startLocal.Date;
            var stUtc = DateTime.SpecifyKind(dayFrom.AddDays(-1), DateTimeKind.Local).ToUniversalTime();
            var etUtc = DateTime.SpecifyKind(endLocal.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();

            try
            {
                await using var conn = new NpgsqlConnection(_connString);
                await conn.OpenAsync();
                const string sql =
                    "SELECT device_id, day, day_last - lag(day_last) OVER (PARTITION BY device_id ORDER BY day) AS usage " +
                    "FROM (SELECT h.device_id, date_trunc('day', h.bucket AT TIME ZONE 'Asia/Shanghai') AS day, " +
                    "             last(h.last_v, h.bucket) AS day_last " +
                    "      FROM iot_ts.telemetry_1h h " +
                    "      JOIN iot_ts.point_map m ON m.point_id = h.point_id AND m.device_id = h.device_id " +
                    "      WHERE m.param_code = @code AND h.device_id = ANY(@ids) AND h.bucket >= @st AND h.bucket < @et " +
                    "      GROUP BY h.device_id, day) d " +
                    "ORDER BY device_id, day";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("code", paramcode.Trim());
                cmd.Parameters.AddWithValue("ids", deviceids.ToArray());
                // Npgsql 5.x的AddWithValue(DateTime)无视Kind按无时区timestamp发送,PG会按会话时区错位8小时,必须显式TimestampTz
                cmd.Parameters.Add(new NpgsqlParameter("st", NpgsqlDbType.TimestampTz) { Value = stUtc });
                cmd.Parameters.Add(new NpgsqlParameter("et", NpgsqlDbType.TimestampTz) { Value = etUtc });
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var day = reader.GetDateTime(1);
                    if (day < dayFrom) continue; // 前移的基准日只参与差分不出行
                    double? usage = reader.IsDBNull(2) ? null : reader.GetDouble(2);
                    if (usage < 0) usage = null; // 表码回退(换表/清零)不计当日
                    rows.Add(new DailyUsageRow
                    {
                        Day = day.ToString("yyyy-MM-dd"),
                        DeviceId = Convert.ToInt64(reader.GetValue(0)),
                        Value = usage
                    });
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), CATEGORY);
            }
            return rows;
        }
    }
}
