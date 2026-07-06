using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using IotModel;
using IotWebApi.Areas.Event.Data;
using IotWebApi.Areas.Event.Models;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 极值数据
    /// </summary>
    [ApiController]
    [ControllSort("25-5")]
    public class EventPeakDayController : ControllerBaseApi
    {

        /// <summary>
        /// 根据条件查询极值曲线(日)
        /// </summary>
        /// <param name="model">查询模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public DataChart GetDataChartBy(DataChartSelect model)
        {
            DataChart chart = new DataChart();
            TotalCount = 0;
            var datavalue = GetDataList(model);
            if (!datavalue.Item1.IsZxxAny()) return chart;
            Dictionary<string, string> devdic = new Dictionary<string, string>()
            {
                { "最大值", "MaxValue"},
                { "最小值", "MinValue"},
                { "累加值", "SumValue"},
                { "平均值", "AvgValue"},
            };
            var deviceids = datavalue.Item1.Select(t => t.DeviceId).Distinct().ToList();
            foreach (var devid in deviceids)
            {
                var devdataList = datavalue.Item1.FindAll(t => t.DeviceId == devid);
                if (devdataList != null)
                {
                    var devdata = devdataList.MaxBy(t => t.ExpandObjects.Count);
                    if (devdata != null)
                    {
                        var arry = devdata.DeviceName.Split('|').ToList();
                        string devname = arry.Last();
                        foreach (var item in devdata.ExpandObjects)
                        {
                            foreach (var dic in devdic)
                            {
                                DataChartChild chartChild = new DataChartChild()
                                {
                                    ChartTuLi = $"{devname}{item.ParamName}{dic.Key}",
                                    ChartTuLiId = $"{devid}-{item.ParamCode}-{dic.Value}",
                                };
                                chart.ChartTuY.Add(chartChild);
                            }
                        }
                    }
                }
            }
            if (chart.ChartTuY.Count == 0) return chart;

            var timelist = datavalue.Item1.Select(t => t.EventTime).Distinct().ToList();
            if (timelist.IsZxxAny())
            {
                var reporttype = typeof(Expand_EventPeakDay);
                var fields = typeof(Expand_EventPeakDay).GetProperties();
                foreach (string ctime in timelist)
                {
                    chart.ChartX.Add(ctime);
                    foreach (var item in chart.ChartTuY)
                    {
                        var tulilsit = item.ChartTuLiId.Split("-").ToList();
                        string paramcode = tulilsit[1];
                        int devid = tulilsit[0].ToZxxInt();
                        string value = "-";
                        var dataone = datavalue.Item1.FirstOrDefault(t => t.EventTime == ctime && t.DeviceId == devid);
                        if (dataone != null)
                        {
                            var param = dataone.ExpandObjects.FirstOrDefault(t => t.ParamCode == paramcode);
                            if (param != null)
                            {
                                var field = fields.FirstOrDefault(t => t.Name == tulilsit[2]);
                                if (field != null)
                                {
                                    value = field.GetValue(param)?.ToString();
                                }
                            }
                        }
                        item.ChartY.Add(value);
                    }
                }
            }
            TotalCount = 1;

            return chart;
        }

        /// <summary>
        /// 根据条件查询极值表格(日)
        /// </summary>
        /// <param name="model">查询模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public DataReport GetDataTableBy(DataTableSelect model)
        {
            DataReport reportTable = new DataReport();

            // 添加固定列的表头
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "DeviceName", ColumnCn = "设备名称" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "EventTime", ColumnCn = "时间" });

            var datavalue = GetDataList(model, model.page, model.pagesize);
            if (!datavalue.Item1.IsZxxAny()) return reportTable;

            Dictionary<string, string> dicname = new Dictionary<string, string>();
            dicname.Add("最大值", "MaxValue");
            dicname.Add("最大值时间", "MaxTime");
            dicname.Add("最小值", "MinValue");
            dicname.Add("最小值时间", "MinTime");
            dicname.Add("累加值", "SumValue");
            dicname.Add("累加值时间", "SumTime");
            dicname.Add("平均值", "AvgValue");

            TotalCount = datavalue.Item2;
            var devdata = datavalue.Item1.MaxBy(t => t.ExpandObjects.Count);
            if (devdata != null)
            {
                foreach (var param in devdata.ExpandObjects)
                {
                    foreach (var dic in dicname)
                    {
                        reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = $"{param.ParamCode}_{dic.Value}", ColumnCn = $"{param.ParamName}_{dic.Key}" });
                    }
                }
            }

            var timelist = datavalue.Item1.Select(t => t.EventTime).Distinct().ToList();
            if (timelist.IsZxxAny())
            {
                var reporttype = typeof(Expand_EventPeakDay);
                var fields = typeof(Expand_EventPeakDay).GetProperties();
                foreach (string ctime in timelist)
                {
                    var datalist = datavalue.Item1.FindAll(t => t.EventTime == ctime);
                    if (datalist.IsZxxAny())
                    {
                        foreach (var dataone in datalist)
                        {
                            var row = new Dictionary<string, object>();
                            row["DeviceName"] = dataone.DeviceName;
                            row["EventTime"] = dataone.EventTime;
                            foreach (var param in dataone.ExpandObjects)
                            {
                                foreach (var dic in dicname)
                                {
                                    string _ColumnEn = $"{param.ParamCode}_{dic.Value}";
                                    var field = fields.FirstOrDefault(t => t.Name == dic.Value);
                                    if (field != null)
                                    {
                                        row[_ColumnEn] = field.GetValue(param);
                                    }
                                }
                            }
                            reportTable.ReportDatas.Add(row);
                        }
                    }
                }
            }

            return reportTable;
        }

        /// <summary>
        /// 根据条件查询极值表格(导出)
        /// </summary>
        /// <param name="model">查询模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public MetaData GetDataTableExcelBy(DataChartSelect model)
        {
            TotalCount = 1;
            MetaData data = new MetaData
            {
                Status = false,
                Message = "极值分析数据导出失败"
            };
            DataTableSelect tableSelect = new DataTableSelect();
            model.CopyTypeValue(tableSelect);
            tableSelect.page = 1;
            tableSelect.pagesize = 9998;
            var reportTable = GetDataTableBy(tableSelect);
            if (reportTable == null || reportTable.ReportDatas.Count == 0)
            {
                data.Message = "极值分析无数据可导出";
                return data;
            }
            // 生成文件名
            string fileName = $"极值分析数据-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
            string filepath = Path.Combine(OperatorCommon.NetLocalfile, "export", fileName);
            string serverparh = Path.Combine(OperatorCommon.NetYingShefile, "export", fileName);
            filepath.EnsureDirectory(true);

            if (reportTable.ExportExcelCom(filepath))
            {
                // 返回文件信息
                data.Status = true;
                data.Message = "极值分析数据导出成功";
                data.Result = serverparh;
            }
            return data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="page"></param>
        /// <param name="pagesize"></param>
        /// <returns></returns>
        private (List<EventPeakDayEntity>, int) GetDataList(DataChartSelect model, int page = 1, int pagesize = 10000)
        {
            var optmdl = Request.GetToken();
            int totalcount = 0;
            if (model == null) return (new List<EventPeakDayEntity>(), 0);

            #region 查询条件处理

            ActionPara actionModel = new ActionPara()
            {
                starttime = model.StartTime.ToDateString(),
                endtime = model.EndTime.AddDays(1).AddMilliseconds(-1).ToDateTimeString(),
                page = page,
                pagesize = pagesize,
            };
            actionModel.sconlist.Add(new SelectCondition
            {
                ParamName = "UnitId",
                ParamType = "=",
                ParamValue = model.UnitId == 0 ? optmdl.UnitId.ToString() : model.UnitId.ToString()
            });
            if (model.DataSort == 0)
            {
                actionModel.sconlist.Add(new SelectCondition
                {
                    ParamName = "SnowId",
                    ParamSort = 2,
                });
            }
            else if (model.DataSort == 1)
            {
                actionModel.sconlist.Add(new SelectCondition
                {
                    ParamName = "SnowId",
                    ParamSort = 1,
                });
            }
            if (model.DeviceIds.Count > 0)
            {
                actionModel.sconlist.Add(new SelectCondition
                {
                    ParamName = "DeviceId",
                    ParamType = "in",
                    ParamValue = model.DeviceIds.ListIntZdToString()
                });
            }
            else if (model.BuildId > 0)
            {
                actionModel.sconlist.Add(new SelectCondition
                {
                    ParamName = "BuildId",
                    ParamType = "=",
                    ParamValue = model.BuildId.ToString()
                });
                //var builds = SysCommonDAO<BuildInfo>.Instance.GetListBy(t => t.FullCode.Contains($"|{model.BuildId}|"));
                //if (builds.IsZxxAny())
                //{
                //    var buildids = builds.Select(t => t.BuildId).Distinct().ToList();
                //    actionModel.sconlist.Add(new SelectCondition
                //    {
                //        ParamName = "BuildId",
                //        ParamType = "in",
                //        ParamValue = buildids.ListIntZdToString()
                //    });
                //}
            }
            else if (model.DeptId > 0)
            {
                actionModel.sconlist.Add(new SelectCondition
                {
                    ParamName = "DeptId",
                    ParamType = "=",
                    ParamValue = model.DeptId.ToString()
                });
                //var depts = SysCommonDAO<DeptInfo>.Instance.GetListBy(t => t.FullCode.Contains($"|{model.DeptId}|"));
                //if (depts.IsZxxAny())
                //{
                //    var deptsids = depts.Select(t => t.DeptId).Distinct().ToList();
                //    actionModel.sconlist.Add(new SelectCondition
                //    {
                //        ParamName = "DeptId",
                //        ParamType = "in",
                //        ParamValue = deptsids.ListIntZdToString()
                //    });
                //}
            }

            #endregion

            var datalist = EventPeakDayDAO.Instance.GetListByPage(actionModel, ref totalcount);
            if (!datalist.IsZxxAny()) return (new List<EventPeakDayEntity>(), 0);

            if (model.ParamCodes.IsZxxAny())
            {
                datalist.ForEach(t =>
                {
                    t.ExpandObjects.RemoveAll(t => !model.ParamCodes.Contains(t.ParamCode));
                });
            }
            else if (!model.ParamTypeName.IsZxxNullOrEmpty())
            {
                datalist.ForEach(t =>
                {
                    t.ExpandObjects.RemoveAll(t => !t.ParamName.Contains(model.ParamTypeName));
                });
            }
            datalist.RemoveAll(t => t.ExpandObjects.Count == 0);

            return (datalist, totalcount);
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
        public List<EventPeakDayEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = EventPeakDayDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}