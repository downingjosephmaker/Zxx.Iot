using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using IotModel;
using IotWebApi.Areas.Event.Models;

namespace IotWebApi.Controllers
{
    /// <summary>
    /// 告警记录（关系型分表版本，按周分表）
    /// </summary>
    [ApiController]
    [ControllSort("25-5-2")]
    public class EventAlarmDbController : ControllerBaseApi
    {
        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public EventAlarmEntity GetInfoByPk(long _SnowId)
        {
            var entity = EventAlarmDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
            return entity;
        }

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name="model">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public List<EventAlarmEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = EventAlarmDAO.Instance.GetListByPage(model, ref totalNumber);
            if (list.Count > 0)
            {
                list.ForEach(t =>
                {
                    t.DeviceName = t.DeviceName.BeautifyFullName();
                });
            }
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 告警处理接口
        /// </summary>
        /// <param name="model">告警处理模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public string PostHandleAlarm(HandleAlarm model)
        {
            Status = false;
            Message = "告警处理失败";
            if (model == null) return Message;
            var optmdl = Request.GetToken();

            var alarmty = EventAlarmDAO.Instance.GetOneBy(t => t.SnowId == model.SnowId);
            if (alarmty != null)
            {
                alarmty.CheckResult = "已处理";
                alarmty.CheckUserId = optmdl.UserID;
                alarmty.CheckUserName = optmdl.UserName;
                alarmty.CheckTime = model.CheckTime.IsZxxNullOrEmpty() ? DateTime.Now.ToDateTimeString() : model.CheckTime;
                alarmty.CheckRemark = model.CheckRemark;
                alarmty.AlarmOptCount++;
                Status = EventAlarmDAO.Instance.Update(alarmty);
                if (Status)
                {
                    if (alarmty.ExpandObject.IsZxxAny())
                    {
                        var deviceParam = DeviceParamDAO.Instance.GetOneBy(t => t.DeviceId == alarmty.DeviceId);
                        if (deviceParam != null)
                        {
                            foreach (var alarmparam in alarmty.ExpandObject)
                            {
                                if (!alarmparam.ParamCode.IsZxxNullOrEmpty())
                                {
                                    var _deviceParam = deviceParam.ExpandObjects.Find(t => t.ParamCode == alarmparam.ParamCode);
                                    if (_deviceParam != null) _deviceParam.IsAlarm = 0;
                                }
                            }
                            deviceParam.ExpandJson = deviceParam.ExpandObjects.ToJson();
                            DeviceInfo dev = new DeviceInfo
                            {
                                DeviceId = 0,
                            };
                            if (!deviceParam.ExpandObjects.Any(t => t.IsAlarm == 1))
                            {
                                dev.DeviceId = alarmty.DeviceId;
                                dev.DeviceAlarm = 0;
                            }
                            SysCommonDAO<DeviceParam>.Instance.TranAction(() =>
                            {
                                SysCommonDAO<DeviceParam>.Instance.UpdateColumns(deviceParam, it => new { it.ExpandJson });
                                if (dev.DeviceId > 0) SysCommonDAO<DeviceInfo>.Instance.UpdateColumns(dev, it => new { it.DeviceAlarm });
                            });
                        }
                    }
                    Message = "告警处理完成";
                }
            }

            return Message;
        }

        /// <summary>
        /// 告警页面曲线
        /// </summary>
        /// <param name="alarmselecttype">1：未报警 2：全部报警</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public AlarmChart GetAlarmAllDataChart(int alarmselecttype = 1)
        {
            AlarmChart chart = new AlarmChart();
            TotalCount = 0;
            var optmdl = Request.GetToken();
            if (optmdl == null) return chart;

            var alarmconfiglist = AlarmConfigDAO.Instance.GetList();
            if (!alarmconfiglist.IsZxxAny()) return chart;
            List<string> AlarmTypeList = new List<string>();
            List<string> AlarmGradeList = new List<string>();
            AlarmTypeList.AddRange(alarmconfiglist.Select(t => t.AlarmType).Distinct());
            AlarmGradeList.AddRange(alarmconfiglist.Select(t => t.AlarmGrade).Distinct());

            Expression<Func<EventAlarm, bool>> expression = t => t.TenantId == optmdl.UnitId;
            if (alarmselecttype == 1) expression = t => t.TenantId == optmdl.UnitId && t.CheckResult == "未处理";
            var datavalue = SysCommonDAO<EventAlarm>.Instance.GetListCount(expression);
            if (datavalue > 0)
            {
                chart.alarmcount = datavalue;
            }
            foreach (string key in AlarmTypeList)
            {
                DataChartChild chartChild = new DataChartChild()
                {
                    ChartTuLi = key,
                    ChartTuLiId = key.ToString(),
                };
                Expression<Func<EventAlarm, bool>> typeExpression = t => t.TenantId == optmdl.UnitId && t.AlarmType == key;
                if (alarmselecttype == 1) expression = t => t.TenantId == optmdl.UnitId && t.AlarmType == key && t.CheckResult == "未处理";
                var datavaluetype = SysCommonDAO<EventAlarm>.Instance.GetListCount(typeExpression);
                if (datavaluetype > 0)
                {
                    chartChild.ChartY.Add(datavaluetype.ToString());
                }
                else
                {
                    chartChild.ChartY.Add("0");
                }
                chart.lxchart.ChartTuY.Add(chartChild);
            }
            foreach (string key in AlarmGradeList)
            {
                DataChartChild chartChild = new DataChartChild()
                {
                    ChartTuLi = key,
                    ChartTuLiId = key.ToString(),
                };
                Expression<Func<EventAlarm, bool>> typeExpression = t => t.TenantId == optmdl.UnitId && t.AlarmGrade == key;
                if (alarmselecttype == 1) expression = t => t.TenantId == optmdl.UnitId && t.AlarmGrade == key && t.CheckResult == "未处理";
                var datavaluetype = SysCommonDAO<EventAlarm>.Instance.GetListCount(typeExpression);
                if (datavaluetype > 0)
                {
                    chartChild.ChartY.Add(datavaluetype.ToString());
                }
                else
                {
                    chartChild.ChartY.Add("0");
                }
                chart.djchart.ChartTuY.Add(chartChild);
            }
            TotalCount = 1;

            return chart;
        }

        /// <summary>
        /// 告警分析页面顶部
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public string GetAlarmAnalysisTop()
        {
            JObject json = new JObject();
            json["今日报警数"] = 0;
            json["昨日报警数"] = 0;
            json["日环比"] = "0%";
            json["本月报警数"] = 0;
            json["上月报警数"] = 0;
            json["月环比"] = "0%";
            json["今年报警数"] = 0;
            json["去年报警数"] = 0;
            json["年环比"] = "0%";

            var optmdl = Request.GetToken();
            long dayva = 0;
            long daysnowid = SnowModel.Instance.GetId(DateTime.Now.Date);
            var dayvalue = SysCommonDAO<EventAlarm>.Instance.GetListCount(t => t.SnowId >= daysnowid && t.TenantId == optmdl.UnitId && t.EventType != "离线");
            if (dayvalue > 0)
            {
                dayva = dayvalue;
                json["今日报警数"] = dayva;
            }

            long yesva = 0;
            long yessnowid = SnowModel.Instance.GetId(DateTime.Now.AddDays(-1).Date);
            var yesvalue = SysCommonDAO<EventAlarm>.Instance.GetListCount(t => t.SnowId >= yessnowid && t.SnowId < daysnowid && t.TenantId == optmdl.UnitId && t.EventType != "离线");
            if (yesvalue > 0)
            {
                yesva = yesvalue;
                json["昨日报警数"] = yesva;
            }
            if (yesva > 0) json["日环比"] = ((double)(dayva - yesva) * 100 / yesva).ToString("f2") + "%";

            long daymonth = 0;
            long daymonthsnowid = SnowModel.Instance.GetId(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
            var daymonthvalue = SysCommonDAO<EventAlarm>.Instance.GetListCount(t => t.SnowId >= daymonthsnowid && t.TenantId == optmdl.UnitId && t.EventType != "离线");
            if (daymonthvalue > 0)
            {
                daymonth = daymonthvalue;
                json["本月报警数"] = daymonth;
            }

            long yesmonthva = 0;
            long yesmonthsnowid = SnowModel.Instance.GetId(new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1));
            var yesmonthvalue = SysCommonDAO<EventAlarm>.Instance.GetListCount(t => t.SnowId >= yesmonthsnowid && t.SnowId < daymonthsnowid && t.TenantId == optmdl.UnitId && t.EventType != "离线");
            if (yesmonthvalue > 0)
            {
                yesmonthva = yesmonthvalue;
                json["上月报警数"] = yesmonthva;
            }
            if (yesmonthva > 0) json["月环比"] = ((double)(daymonth - yesmonthva) * 100 / yesmonthva).ToString("f2") + "%";

            long dayyearva = 0;
            long dayyearsnowid = SnowModel.Instance.GetId(new DateTime(DateTime.Now.Year, 1, 1));
            var dayyearvalue = SysCommonDAO<EventAlarm>.Instance.GetListCount(t => t.SnowId >= dayyearsnowid && t.TenantId == optmdl.UnitId && t.EventType != "离线");
            if (dayyearvalue > 0)
            {
                dayyearva = dayyearvalue;
                json["今年报警数"] = dayyearva;
            }

            long yesyearva = 0;
            long yesyearsnowid = SnowModel.Instance.GetId(new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1));
            var yesyearvalue = SysCommonDAO<EventAlarm>.Instance.GetListCount(t => t.SnowId >= yesyearsnowid && t.SnowId < dayyearsnowid && t.TenantId == optmdl.UnitId && t.EventType != "离线");
            if (yesyearvalue > 0)
            {
                yesyearva = yesyearvalue;
                json["去年报警数"] = yesyearva;
            }
            if (yesyearva > 0) json["年环比"] = ((double)(dayyearva - yesyearva) * 100 / yesyearva).ToString("f2") + "%";

            return json.ToJson();
        }

        /// <summary>
        /// 告警分析页面配电房报警类型/等级排名
        /// </summary>
        /// <param name="starttime">开始时间</param>
        /// <param name="endtime">结束时间</param>
        /// <param name="alarmselecttype">1：报警类型 2：报警等级</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public List<RestfulAlarmAnalysisTwo> GetAlarmAnalysisTwo(DateTime starttime, DateTime endtime, int alarmselecttype)
        {
            List<RestfulAlarmAnalysisTwo> list = new List<RestfulAlarmAnalysisTwo>();
            var optmdl = Request.GetToken();
            if (optmdl == null) return list;

            long min = SnowModel.Instance.GetId(starttime);
            long max = SnowModel.Instance.GetId(endtime.AddDays(1));

            var alarmconfiglist = AlarmConfigDAO.Instance.GetList();
            if (!alarmconfiglist.IsZxxAny()) return list;
            List<string> AlarmTypeList = new List<string>();
            List<string> AlarmGradeList = new List<string>();
            if (alarmselecttype == 1)
            {
                AlarmTypeList.AddRange(alarmconfiglist.Select(t => t.AlarmType).Distinct());
                int index = 0;
                foreach (string key in AlarmTypeList)
                {
                    index++;
                    RestfulAlarmAnalysisTwo one = new RestfulAlarmAnalysisTwo
                    {
                        TypeId = index,
                        TypeName = key,
                    };
                    var dayvalue = SysCommonDAO<EventAlarm>.Instance.GetListCount(t => t.SnowId >= min && t.SnowId <= max && t.TenantId == optmdl.UnitId && t.AlarmType == key && t.EventType != "离线");
                    if (dayvalue > 0)
                    {
                        one.AlarmAllCount = dayvalue;
                    }
                    list.Add(one);
                }
            }
            else if (alarmselecttype == 2)
            {
                AlarmGradeList.AddRange(alarmconfiglist.Select(t => t.AlarmGrade).Distinct());
                int index = 0;
                foreach (string key in AlarmGradeList)
                {
                    index++;
                    RestfulAlarmAnalysisTwo one = new RestfulAlarmAnalysisTwo
                    {
                        TypeId = index,
                        TypeName = key,
                    };
                    var dayvalue = SysCommonDAO<EventAlarm>.Instance.GetListCount(t => t.SnowId >= min && t.SnowId <= max && t.TenantId == optmdl.UnitId && t.AlarmGrade == key && t.EventType != "离线");
                    if (dayvalue > 0)
                    {
                        one.AlarmAllCount = dayvalue;
                    }
                    list.Add(one);
                }
            }

            return list;
        }

        /// <summary>
        /// 告警分析页面配电房月报警统计
        /// </summary>
        /// <param name="starttime">开始时间</param>
        /// <param name="endtime">结束时间</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public DataChart GetAlarmAnalysisThree(DateTime starttime, DateTime endtime)
        {
            DataChart chart = new DataChart();
            var optmdl = Request.GetToken();

            int days = starttime.DiffDays(endtime);
            DataChartChild chartChild = new DataChartChild()
            {
                ChartTuLi = "告警数目",
                ChartTuLiId = "1",
            };
            chart.ChartTuY.Add(chartChild);
            for (int i = 0; i < days; i++)
            {
                DateTime start = starttime.Date.AddDays(i);
                DateTime end = starttime.Date.AddDays(i + 1);
                long min = SnowModel.Instance.GetId(start);
                long max = SnowModel.Instance.GetId(end);
                chart.ChartX.Add(start.ToString("yyyy-MM-dd"));

                var dayvalue = SysCommonDAO<EventAlarm>.Instance.GetListCount(t => t.SnowId >= min && t.SnowId <= max && t.TenantId == optmdl.UnitId && t.EventType != "离线");
                if (dayvalue > 0)
                {
                    chartChild.ChartY.Add(dayvalue.ToString());
                }
                else
                {
                    chartChild.ChartY.Add("-");
                }
                TotalCount = 1;
            }
            return chart;
        }

        /// <summary>
        /// 根据告警ID删除告警信息
        /// </summary>
        /// <param name="snowid">告警ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string Delete(string snowid)
        {
            Status = false;
            Message = "告警信息删除失败。";
            Status = SysCommonDAO<EventAlarm>.Instance.DeleteBy(t => t.SnowId.ToString() == snowid);
            if (Status) Message = "告警信息删除成功。";
            return Message;
        }
    }
}
