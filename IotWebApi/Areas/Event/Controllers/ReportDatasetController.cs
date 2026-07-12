using CenBoCommon.Zxx;
using IotModel;
using IotWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IotWebApi.Controllers
{
    /// <summary>
    /// 低代码报表数据集(D-7:JimuReport旁挂经API数据源取数,不直连业务库;
    /// 数据集端点[Token]鉴权走全局租户过滤,VerifyToken供报表服务端回调验真)
    /// </summary>
    [ApiController]
    [ControllSort("25-5-8")]
    public class ReportDatasetController : ControllerBaseApi
    {
        /// <summary>
        /// 报表数据集:按日设备用量(基于telemetry_1h日末表码差分,适用电能等累积点位;
        /// 设备集经DeviceInfoDAO取得,租户过滤自动生效——时序库无租户列不能直查)
        /// </summary>
        /// <param name="paramcode">参数编码(累积表码点位,如正向有功电能)</param>
        /// <param name="starttime">开始日期(含当日,如2026-07-01)</param>
        /// <param name="endtime">结束日期(含当日)</param>
        /// <param name="deviceid">设备ID(0=当前租户全部设备)</param>
        /// <param name="queryService">遥测历史查询服务(DI注入)</param>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public async Task<List<DailyEnergyRow>> GetDailyEnergy(string paramcode, string starttime, string endtime,
            int deviceid, [FromServices] TelemetryQueryService queryService)
        {
            Status = false;
            var rows = new List<DailyEnergyRow>();
            if (!queryService.Enabled)
            {
                Message = "时序库未配置,日用量数据集不可用。";
                return rows;
            }
            if (paramcode.IsZxxNullOrEmpty() || !DateTime.TryParse(starttime, out var st)
                || !DateTime.TryParse(endtime, out var et) || et < st)
            {
                Message = "参数无效。";
                return rows;
            }
            if (et.Date.AddDays(1) - st.Date > TimeSpan.FromDays(400))
            {
                Message = "时间跨度过大(最多400天)。";
                return rows;
            }

            var devices = deviceid > 0
                ? DeviceInfoDAO.Instance.GetListBy(t => t.DeviceId == deviceid)
                : DeviceInfoDAO.Instance.GetList();
            if (!devices.IsZxxAny())
            {
                Message = "无可见设备。";
                return rows;
            }

            var usage = await queryService.QueryDailyUsageAsync(
                devices.Select(t => (long)t.DeviceId).ToList(), paramcode, st, et);
            var namemap = devices.ToDictionary(t => (long)t.DeviceId, t => t.DeviceName);
            rows = usage.Select(t => new DailyEnergyRow
            {
                Day = t.Day,
                DeviceId = t.DeviceId,
                DeviceName = namemap.TryGetValue(t.DeviceId, out var name) ? name : "",
                Value = t.Value
            }).ToList();

            Status = true;
            TotalCount = rows.Count;
            return rows;
        }

        /// <summary>
        /// 报表数据集:按日×等级告警计数(零值补齐;等级全集=AlarmConfig配置∪窗口内实际出现;
        /// 排除"离线"事件与现有统计口径一致;时间过滤走SnowId区间,租户过滤自动生效)
        /// </summary>
        /// <param name="starttime">开始日期(含当日,如2026-07-01)</param>
        /// <param name="endtime">结束日期(含当日)</param>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public List<AlarmDailyRow> GetAlarmDaily(string starttime, string endtime)
        {
            Status = false;
            var rows = new List<AlarmDailyRow>();
            if (!DateTime.TryParse(starttime, out var st) || !DateTime.TryParse(endtime, out var et) || et < st)
            {
                Message = "时间范围无效。";
                return rows;
            }
            if (et.Date.AddDays(1) - st.Date > TimeSpan.FromDays(400))
            {
                Message = "时间跨度过大(最多400天)。";
                return rows;
            }

            long minsnowid = SnowModel.Instance.GetId(st.Date);
            long maxsnowid = SnowModel.Instance.GetId(et.Date.AddDays(1));
            var alarms = EventAlarmDAO.Instance.GetListBy(
                t => t.SnowId >= minsnowid && t.SnowId < maxsnowid && t.EventType != "离线");

            var valid = alarms.Where(t => !t.AlarmGrade.IsZxxNullOrEmpty()
                && t.EventTime != null && t.EventTime.Length >= 10).ToList();
            var grades = AlarmConfigDAO.Instance.GetList()
                .Select(t => t.AlarmGrade).Where(t => !t.IsZxxNullOrEmpty())
                .Union(valid.Select(t => t.AlarmGrade))
                .Distinct().ToList();
            var countmap = valid
                .GroupBy(t => new { Day = t.EventTime.Substring(0, 10), t.AlarmGrade })
                .ToDictionary(g => (g.Key.Day, g.Key.AlarmGrade), g => g.Count());

            for (var day = st.Date; day <= et.Date; day = day.AddDays(1))
            {
                string daystr = day.ToString("yyyy-MM-dd");
                foreach (string grade in grades)
                {
                    countmap.TryGetValue((daystr, grade), out int count);
                    rows.Add(new AlarmDailyRow { Day = daystr, AlarmGrade = grade, AlarmCount = count });
                }
            }

            Status = true;
            TotalCount = rows.Count;
            return rows;
        }

        /// <summary>
        /// Token校验回调(供JimuReport等旁挂服务的JmReportTokenServiceI服务端验真;
        /// 无[Token]=匿名可调,只回答token是否有效,不签发任何凭据;
        /// 校验口径与TokenAuthorizationFilter同源:DES解密+LoginTime时限,另加用户在库且启用;
        /// 失败文案统一不区分原因,避免匿名端点成为token状态探测oracle)
        /// </summary>
        /// <param name="token">平台登录token(优先读请求头token,查询参数仅作回落——避免有效token进反代访问日志)</param>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [ApiGroup(ApiGroupNames.Event)]
        public TokenVerifyResult VerifyToken(string token)
        {
            Status = false;
            Message = "token验证不通过。";
            var result = new TokenVerifyResult();
            if (Request.Headers.TryGetValue("token", out var headertoken)
                && !string.IsNullOrWhiteSpace(headertoken))
            {
                token = headertoken.ToString();
            }
            if (token.IsZxxNullOrEmpty())
            {
                return result;
            }
            try
            {
                string json = EncryptsHelper.Decrypt(token);
                var model = JsonConvert.DeserializeObject<OperatorModelLogin>(json);
                if (model == null || model.UserID <= 0)
                {
                    return result;
                }
                string timeouthour = AppSetting.GetConfig("DefaultValues:tokentimeouthour");
                if (!string.IsNullOrWhiteSpace(timeouthour)
                    && model.LoginTime.AddHours(timeouthour.ToZxxInt()) < DateTime.Now)
                {
                    return result;
                }
                var user = SysUserDAO.Instance.GetOneBy(t => t.UserId == model.UserID && t.IsEnable == 1);
                if (user == null)
                {
                    return result;
                }
                result.Valid = true;
                result.UserId = model.UserID;
                result.UserName = model.UserName;
                result.TenantId = model.TenantId;
                Status = true;
                Message = "验证通过。";
            }
            catch (Exception)
            {
                // 解密/反序列化异常与非法token同文案,不暴露失败阶段
            }
            return result;
        }
    }

    /// <summary>日用量数据集行</summary>
    public class DailyEnergyRow
    {
        /// <summary>日期(yyyy-MM-dd)</summary>
        public string Day { get; set; } = "";
        /// <summary>设备ID</summary>
        public long DeviceId { get; set; }
        /// <summary>设备名称</summary>
        public string DeviceName { get; set; } = "";
        /// <summary>当日用量(累积表码日末差分;首日无基准或表码回退为null)</summary>
        public double? Value { get; set; }
    }

    /// <summary>告警日统计数据集行</summary>
    public class AlarmDailyRow
    {
        /// <summary>日期(yyyy-MM-dd)</summary>
        public string Day { get; set; } = "";
        /// <summary>报警等级</summary>
        public string AlarmGrade { get; set; } = "";
        /// <summary>告警数</summary>
        public int AlarmCount { get; set; }
    }

    /// <summary>Token校验回调结果</summary>
    public class TokenVerifyResult
    {
        /// <summary>是否有效</summary>
        public bool Valid { get; set; }
        /// <summary>用户ID</summary>
        public int UserId { get; set; }
        /// <summary>用户名</summary>
        public string UserName { get; set; } = "";
        /// <summary>租户ID</summary>
        public int TenantId { get; set; }
    }
}
