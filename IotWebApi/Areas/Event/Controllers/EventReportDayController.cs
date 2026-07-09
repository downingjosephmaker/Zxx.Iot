using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using IotModel;
using IotWebApi.Areas.Event.Data;
using IotWebApi.Areas.Event.Models;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 统计日报
    /// </summary>
    [ApiController]
    [ControllSort("25-6")]
    public class EventReportDayController : ControllerBaseApi
    {

        #region 趋势分析

        /// <summary>
        /// 趋势分析(曲线)
        /// </summary>
        /// <param name="model">趋势分析</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public DataChart GetDataChartBy(DataReportChartSelect model)
        {
            TotalCount = 1;
            DataChart chart = new DataChart();
            if (model.DataType == 1 && model.EndTime != model.StartTime)
            {
                Message = "选中时类型时，开始时间和结束时间必须相同";
                return chart;
            }
            int timemax = 0;
            List<ReportAnalysisInfo> datalist = new List<ReportAnalysisInfo>();
            if (model.DataType >= 1 && model.DataType <= 2)
            {
                //本期日能耗表查询
                var datavalue = GetDayDataList(model);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffDays(model.EndTime);
            }
            else if (model.DataType == 3)
            {
                //本期周能耗表查询
                var datavalue = GetWeekDataList(model);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffWeeks(model.EndTime);
            }
            else if (model.DataType >= 4 && model.DataType <= 5)
            {
                //本期月能耗表查询
                var datavalue = GetMonthDataList(model);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffMonths(model.EndTime) + 1;
                if (model.DataType == 5) timemax = model.StartTime.DiffYears(model.EndTime) + 1;
            }
            if (datalist.Count == 0) return chart;

            var devdata = datalist.MaxBy(t => t.ExpandObjects.Count);
            if (devdata != null)
            {
                int chartid = devdata.DeviceId;
                string chartname = devdata.DeviceName;
                if (devdata.DeviceId > 0)
                {
                    var arry = chartname.Split('|').ToList();
                    chartname = arry.Last();
                }
                if (chartname.Contains("|")) chartname = chartname.Split('|').ToList()[0];
                foreach (var item in devdata.ExpandObjects)
                {
                    DataChartChild chartChild = new DataChartChild()
                    {
                        ChartTuLi = $"{chartname}-{item.ParamName}",
                        ChartTuLiId = $"{chartid}-{item.ParamCode}",
                    };
                    chart.ChartTuY.Add(chartChild);
                }
            }

            if (model.DataType == 1)
            {
                var reporttype = typeof(Expand_EventReportWeek);
                var fields = typeof(Expand_EventReportWeek).GetProperties();
                var len = model.StartTime.Date == DateTime.Today ? DateTime.Now.Hour : 24;
                for (int i = 0; i < len; i++)
                {
                    chart.ChartX.Add($"{i}时");
                    string fieldname = $"HourValue{i}";
                    foreach (var item in chart.ChartTuY)
                    {
                        var tulilsit = item.ChartTuLiId.Split("-").ToList();
                        string paramcode = tulilsit[1];
                        string value = "-";
                        var field = fields.FirstOrDefault(t => t.Name == fieldname);
                        if (field != null)
                        {
                            double totalbq = 0;
                            foreach (var _data in datalist)
                            {
                                var param = _data.ExpandObjects.FirstOrDefault(t => t.ParamCode == paramcode);
                                if (param != null)
                                {
                                    totalbq += field.GetValue(param).ToZxxDouble();
                                }
                            }
                            value = totalbq.ToString("f1");
                        }
                        item.ChartY.Add(value);
                    }
                }
            }
            else
            {
                long minbq = 0, maxbq = 0;
                for (int i = 0; i < timemax; i++)
                {
                    string DateStr = "";
                    if (model.DataType == 2)
                    {
                        DateTime rtime = model.StartTime.AddDays(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(1));
                        DateStr = $"{rtime.Day}日";
                    }
                    else if (model.DataType == 3)
                    {
                        DateTime starttime = model.StartTime.GetFirstDayOfWeek();
                        DateTime rtime = starttime.AddDays(i * 7);
                        int rweek = rtime.GetWeekOfYear();
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(7));
                        DateStr = $"{rweek}周";
                    }
                    else if (model.DataType == 4)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, model.StartTime.Month, 1);
                        DateTime rtime = starttime.AddMonths(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddMonths(1));
                        DateStr = $"{rtime.Month}月";
                    }
                    else if (model.DataType == 5)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, 1, 1);
                        DateTime rtime = starttime.AddYears(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddYears(1));
                        DateStr = $"{rtime.Year}年";
                    }

                    chart.ChartX.Add(DateStr);
                    foreach (var item in chart.ChartTuY)
                    {
                        var tulilsit = item.ChartTuLiId.Split("-").ToList();
                        string paramcode = tulilsit[1];
                        string value = "-";
                        double totalbq = 0;
                        bool isvalue = false;
                        var _datalist = datalist.FindAll(t => t.SnowId >= minbq && t.SnowId < maxbq);
                        if (_datalist.IsZxxAny())
                        {
                            foreach (var _data in _datalist)
                            {
                                var param = _data.ExpandObjects.FirstOrDefault(t => t.ParamCode == paramcode);
                                if (param != null)
                                {
                                    totalbq += param.TotalValue.ToZxxDouble();
                                    isvalue = true;
                                }
                            }
                            if (isvalue) value = totalbq.ToString("f1");
                        }
                        item.ChartY.Add(value);
                    }
                }
            }

            return chart;
        }

        /// <summary>
        /// 根据条件获取日能耗数据列表
        /// </summary>
        /// <param name="model"></param>
        /// <param name="page"></param>
        /// <param name="pagesize"></param>
        /// <returns></returns>
        private (List<ReportAnalysisInfo>, int) GetDayDataList(DataReportChartSelect model, int page = 1, int pagesize = 10000)
        {
            int totalcount = 0;
            List<ReportAnalysisInfo> infolist = new List<ReportAnalysisInfo>();

            #region 查询条件处理

            var optmdl = Request.GetToken();
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

            if (!model.DataTypeDL.IsZxxNullOrEmpty())
            {
                var dtypes = SysCommonDAO<DeviceType>.Instance.GetListBy(t => t.FullCode.Contains($"|{model.DataTypeDL}|"));
                if (dtypes.IsZxxAny())
                {
                    int maxlevel = dtypes.Max(t => t.TreeLevel);
                    var dtypecodes = dtypes.FindAll(t => t.TreeLevel == maxlevel).Select(t => t.TypeCode).Distinct().ToList();
                    if (dtypecodes.IsZxxAny())
                    {
                        actionModel.sconlist.Add(new SelectCondition
                        {
                            ParamName = "DeviceTypeCode",
                            ParamType = "in",
                            ParamValue = string.Join(",", dtypecodes)
                        });
                    }
                }
            }

            #endregion

            //康慈单位在合计(IsTotal=1)时排除总表/热水回水/热水进水设备(避免与分表重复累加)；非合计时保留
            if (IsKangciUnit() && model.IsTotal == 1)
            {
                foreach (var kw in KangciExcludedDeviceNames)
                {
                    actionModel.sconlist.Add(new SelectCondition
                    {
                        ParamName = "DeviceName",
                        ParamType = "nolike",
                        ParamValue = kw
                    });
                }
            }

            // pagesize=0 时不分页(全量查询)，用于图表/导出；否则分页
            List<EventReportDayEntity> datalist;
            if (actionModel.pagesize == 0)
            {
                datalist = EventReportDayDAO.Instance.GetListBy(actionModel);
            }
            else
            {
                datalist = EventReportDayDAO.Instance.GetListByPage(actionModel, ref totalcount);
            }
            if (!datalist.IsZxxAny()) return (new List<ReportAnalysisInfo>(), 0);

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

            foreach (var entity in datalist)
            {
                ReportAnalysisInfo info = new ReportAnalysisInfo();
                entity.CopyTypeValue(info);
                info.ExpandObjects = new List<Expand_EventReportWeek>();
                if (entity.ExpandObjects.IsZxxAny())
                {
                    foreach (var item in entity.ExpandObjects)
                    {
                        Expand_EventReportWeek week = new Expand_EventReportWeek();
                        item.CopyTypeValue(week);
                        info.ExpandObjects.Add(week);
                    }
                }
                infolist.Add(info);
            }

            return (infolist, totalcount);
        }

        /// <summary>
        /// 根据条件获取周能耗数据列表
        /// </summary>
        /// <param name="model"></param>
        /// <param name="page"></param>
        /// <param name="pagesize"></param>
        /// <returns></returns>
        private (List<ReportAnalysisInfo>, int) GetWeekDataList(DataReportChartSelect model, int page = 1, int pagesize = 10000)
        {
            int totalcount = 0;
            List<ReportAnalysisInfo> infolist = new List<ReportAnalysisInfo>();

            #region 查询条件处理

            DateTime starttime = model.StartTime.GetFirstDayOfWeek();
            DateTime endtime = model.EndTime.GetLastDayOfWeek();
            var optmdl = Request.GetToken();
            ActionPara actionModel = new ActionPara()
            {
                starttime = starttime.ToDateString(),
                endtime = endtime.AddDays(1).AddMilliseconds(-1).ToDateString(),
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

            if (!model.DataTypeDL.IsZxxNullOrEmpty())
            {
                var dtypes = SysCommonDAO<DeviceType>.Instance.GetListBy(t => t.FullCode.Contains($"|{model.DataTypeDL}|"));
                if (dtypes.IsZxxAny())
                {
                    int maxlevel = dtypes.Max(t => t.TreeLevel);
                    var dtypecodes = dtypes.FindAll(t => t.TreeLevel == maxlevel).Select(t => t.TypeCode).Distinct().ToList();
                    if (dtypecodes.IsZxxAny())
                    {
                        actionModel.sconlist.Add(new SelectCondition
                        {
                            ParamName = "DeviceTypeCode",
                            ParamType = "in",
                            ParamValue = string.Join(",", dtypecodes)
                        });
                    }
                }
            }

            #endregion

            //康慈单位在合计(IsTotal=1)时排除总表/热水回水/热水进水设备(避免与分表重复累加)；非合计时保留
            if (IsKangciUnit() && model.IsTotal == 1)
            {
                foreach (var kw in KangciExcludedDeviceNames)
                {
                    actionModel.sconlist.Add(new SelectCondition
                    {
                        ParamName = "DeviceName",
                        ParamType = "nolike",
                        ParamValue = kw
                    });
                }
            }

            // pagesize=0 时不分页(全量查询)，用于图表/导出；否则分页
            List<EventReportWeekEntity> datalist;
            if (actionModel.pagesize == 0)
            {
                datalist = EventReportWeekDAO.Instance.GetListBy(actionModel);
            }
            else
            {
                datalist = EventReportWeekDAO.Instance.GetListByPage(actionModel, ref totalcount);
            }
            if (!datalist.IsZxxAny()) return (new List<ReportAnalysisInfo>(), 0);

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

            foreach (var entity in datalist)
            {
                ReportAnalysisInfo info = new ReportAnalysisInfo();
                entity.CopyTypeValue(info);
                info.ExpandObjects = new List<Expand_EventReportWeek>();
                if (entity.ExpandObjects.IsZxxAny())
                {
                    info.ExpandObjects.AddRange(entity.ExpandObjects);
                }
                infolist.Add(info);
            }

            return (infolist, totalcount);
        }

        /// <summary>
        /// 根据条件获取月能耗数据列表
        /// </summary>
        /// <param name="model"></param>
        /// <param name="page"></param>
        /// <param name="pagesize"></param>
        /// <returns></returns>
        private (List<ReportAnalysisInfo>, int) GetMonthDataList(DataReportChartSelect model, int page = 1, int pagesize = 10000)
        {
            int totalcount = 0;
            List<ReportAnalysisInfo> infolist = new List<ReportAnalysisInfo>();

            #region 查询条件处理

            DateTime starttime = new DateTime(model.StartTime.Year, model.StartTime.Month, 1);
            var _endtime = model.EndTime.AddDays(1);
            DateTime endtime = new DateTime(_endtime.Year, _endtime.Month, 1);
            var optmdl = Request.GetToken();
            ActionPara actionModel = new ActionPara()
            {
                starttime = starttime.ToDateString(),
                endtime = endtime.AddMilliseconds(-1).ToDateString(),
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
            if (!model.DataTypeDL.IsZxxNullOrEmpty())
            {
                var dtypes = SysCommonDAO<DeviceType>.Instance.GetListBy(t => t.FullCode.Contains($"|{model.DataTypeDL}|"));
                if (dtypes.IsZxxAny())
                {
                    int maxlevel = dtypes.Max(t => t.TreeLevel);
                    var dtypecodes = dtypes.FindAll(t => t.TreeLevel == maxlevel).Select(t => t.TypeCode).Distinct().ToList();
                    if (dtypecodes.IsZxxAny())
                    {
                        actionModel.sconlist.Add(new SelectCondition
                        {
                            ParamName = "DeviceTypeCode",
                            ParamType = "in",
                            ParamValue = string.Join(",", dtypecodes)
                        });
                    }
                }
            }

            #endregion


            //康慈单位在合计(IsTotal=1)时排除总表/热水回水/热水进水设备(避免与分表重复累加)；非合计时保留
            if (IsKangciUnit() && model.IsTotal == 1)
            {
                foreach (var kw in KangciExcludedDeviceNames)
                {
                    actionModel.sconlist.Add(new SelectCondition
                    {
                        ParamName = "DeviceName",
                        ParamType = "nolike",
                        ParamValue = kw
                    });
                }
            }

            // pagesize=0 时不分页(全量查询)，用于图表/导出；否则分页
            List<EventReportMonthEntity> datalist;
            if (actionModel.pagesize == 0)
            {
                datalist = EventReportMonthDAO.Instance.GetListBy(actionModel);
            }
            else
            {
                datalist = EventReportMonthDAO.Instance.GetListByPage(actionModel, ref totalcount);
            }
            if (!datalist.IsZxxAny()) return (new List<ReportAnalysisInfo>(), 0);

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

            foreach (var entity in datalist)
            {
                ReportAnalysisInfo info = new ReportAnalysisInfo();
                entity.CopyTypeValue(info);
                info.ExpandObjects = new List<Expand_EventReportWeek>();
                if (entity.ExpandObjects.IsZxxAny())
                {
                    foreach (var item in entity.ExpandObjects)
                    {
                        Expand_EventReportWeek week = new Expand_EventReportWeek();
                        item.CopyTypeValue(week);
                        info.ExpandObjects.Add(week);
                    }
                }
                infolist.Add(info);
            }

            return (infolist, totalcount);
        }

        /// <summary>
        /// 趋势分析(表格)
        /// </summary>
        /// <param name="model">趋势分析</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public DataReport GetDataTableBy(DataReportTableSelect model)
        {
            DataReport reportTable = new DataReport();
            if (model.DataType == 1 && model.EndTime != model.StartTime)
            {
                Message = "选中时类型时，开始时间和结束时间必须相同";
                return reportTable;
            }
            //Table 报表强制按时间升序输出(旧→新)，忽略前端 DataSort
            model.DataSort = 1;

            // 添加固定列的表头
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "RowNo", ColumnCn = "序号" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "ComName", ColumnCn = "名称" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "DateStr", ColumnCn = "时间" });

            int timemax = 0;
            List<ReportAnalysisInfo> datalist = new List<ReportAnalysisInfo>();
            if (model.DataType >= 1 && model.DataType <= 2)
            {
                //本期日能耗表查询
                var datavalue = GetDayDataList(model, model.page, model.pagesize);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffDays(model.EndTime);
            }
            else if (model.DataType == 3)
            {
                //本期周能耗表查询
                var datavalue = GetWeekDataList(model, model.page, model.pagesize);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffWeeks(model.EndTime);
            }
            else if (model.DataType >= 4 && model.DataType <= 5)
            {
                //本期月能耗表查询
                var datavalue = GetMonthDataList(model, model.page, model.pagesize);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffMonths(model.EndTime) + 1;
                if (model.DataType == 5) timemax = model.StartTime.DiffYears(model.EndTime) + 1;
            }
            if (datalist.Count == 0) return reportTable;

            var devdata = datalist.MaxBy(t => t.ExpandObjects.Count);
            if (devdata == null) return reportTable;

            int chartid = devdata.DeviceId;
            string chartname = devdata.DeviceName;
            if (devdata.DeviceId > 0)
            {
                var arry = chartname.Split('|').ToList();
                chartname = arry.Last();
            }

            if (chartname.Contains("|")) chartname = chartname.Split('|').ToList()[0];

            // 添加动态列的表头
            foreach (var item in devdata.ExpandObjects)
            {
                reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = item.ParamCode, ColumnCn = item.ParamName });
            }

            if (model.DataType == 1)
            {
                var reporttype = typeof(Expand_EventReportWeek);
                var fields = typeof(Expand_EventReportWeek).GetProperties();
                for (int i = 0; i < 24; i++)
                {
                    var row = new Dictionary<string, object>();
                    row["RowNo"] = i + 1;
                    row["DateStr"] = $"{i}时";
                    row["ComName"] = chartname;

                    string fieldname = $"HourValue{i}";
                    var field = fields.FirstOrDefault(t => t.Name == fieldname);
                    if (field != null)
                    {
                        foreach (var _data in datalist)
                        {
                            foreach (var item in _data.ExpandObjects)
                            {
                                if (reportTable.ReportColumns.Any(t => t.ColumnEn == item.ParamCode))
                                {
                                    var value = field.GetValue(item);
                                    row[item.ParamCode] = value + item.ValueUnit;
                                }
                            }
                        }
                    }
                    reportTable.ReportDatas.Add(row);
                }
            }
            else
            {
                long minbq = 0, maxbq = 0;
                for (int i = 0; i < timemax; i++)
                {
                    var row = new Dictionary<string, object>();
                    row["RowNo"] = i + 1;
                    string DateStr = "";
                    if (model.DataType == 2)
                    {
                        DateTime rtime = model.StartTime.AddDays(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(1));
                        DateStr = $"{rtime.Day}日";
                    }
                    else if (model.DataType == 3)
                    {
                        DateTime starttime = model.StartTime.GetFirstDayOfWeek();
                        DateTime rtime = starttime.AddDays(i * 7);
                        int rweek = rtime.GetWeekOfYear();
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(7));
                        DateStr = $"{rweek}周";
                    }
                    else if (model.DataType == 4)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, model.StartTime.Month, 1);
                        DateTime rtime = starttime.AddMonths(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddMonths(1));
                        DateStr = $"{rtime.Month}月";
                    }
                    else if (model.DataType == 5)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, 1, 1);
                        DateTime rtime = starttime.AddYears(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddYears(1));
                        DateStr = $"{rtime.Year}年";
                    }
                    row["DateStr"] = DateStr;
                    row["ComName"] = chartname;

                    var _datalist = datalist.FindAll(t => t.SnowId >= minbq && t.SnowId < maxbq);
                    if (_datalist.IsZxxAny())
                    {
                        foreach (var item in devdata.ExpandObjects)
                        {
                            string value = "-";
                            double totalbq = 0;
                            bool isvalue = false;
                            foreach (var _data in _datalist)
                            {
                                foreach (var item1 in _data.ExpandObjects)
                                {
                                    if (reportTable.ReportColumns.Any(t => t.ColumnEn == item.ParamCode) && item.ParamCode == item1.ParamCode)
                                    {
                                        totalbq += item1.TotalValue.ToZxxDouble();
                                        isvalue = true;
                                    }
                                }
                            }
                            if (isvalue) value = totalbq.ToString("f1");

                            row[item.ParamCode] = $"{value}{item.ValueUnit}";
                            reportTable.ReportDatas.Add(row);
                        }
                    }
                }
            }

            return reportTable;
        }

        /// <summary>
        /// 趋势分析(导出)
        /// </summary>
        /// <param name="model">趋势分析</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public MetaData GetDataTableExcelBy(DataReportChartSelect model)
        {
            TotalCount = 1;
            MetaData data = new MetaData
            {
                Status = false,
                Message = "趋势分析数据导出失败"
            };
            DataReportTableSelect tableSelect = new DataReportTableSelect();
            model.CopyTypeValue(tableSelect);
            tableSelect.page = 1;
            // pagesize=0 走全量查询(GetListBy)，保证导出数据完整
            tableSelect.pagesize = 0;
            var reportTable = GetDataTableBy(tableSelect);
            if (reportTable == null || reportTable.ReportDatas.Count == 0)
            {
                data.Message = "趋势分析无数据可导出";
                return data;
            }

            // 生成文件名
            string fileName = $"趋势分析数据-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
            string filepath = Path.Combine(OperatorCommon.NetLocalfile, "export", fileName);
            string serverparh = Path.Combine(OperatorCommon.NetYingShefile, "export", fileName);
            filepath.EnsureDirectory(true);

            if (reportTable.ExportExcelCom(filepath))
            {
                // 返回文件信息
                data.Status = true;
                data.Message = "趋势分析数据导出成功";
                data.Result = serverparh;
            }
            return data;
        }

        #endregion

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name="model">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public List<EventReportDayEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = EventReportDayDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber.ToZxxInt();
            return list;
        }

        #region 碳排分析

        /// <summary>
        /// (日=》时/月=》日)碳排分析(曲线)
        /// </summary>
        /// <param name="model">曲线</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public DataChart GetSubEneryDataChartBy(DataReportChartSelect model)
        {
            TotalCount = 1;
            DataChart chart = new DataChart();
            if (model.DataType == 1 && model.EndTime != model.StartTime)
            {
                Message = "选中时类型时，开始时间和结束时间必须相同";
                return chart;
            }
            var optmdl = Request.GetToken();

            Dictionary<string, List<DeviceInfoEntity>> dicEnergyType = new Dictionary<string, List<DeviceInfoEntity>>();

            #region 分项设备特殊处理

            //分项能耗（电/水/氢能/光伏/气/热）
            //电
            var elecDeviceList = GetDevicesBy(optmdl.UnitId, "|zndb|");
            if (elecDeviceList.IsZxxAny())
            {
                dicEnergyType.Add("电", elecDeviceList);
            }
            //水
            var waterDeviceList = GetDevicesBy(optmdl.UnitId, "|znsb|");
            if (waterDeviceList.IsZxxAny())
            {
                dicEnergyType.Add("水", waterDeviceList);
            }
            //dicEnergyType.Add("气", null);
            //dicEnergyType.Add("热", null);
            ////光伏
            //var gqitem = GetGQDevicesBy(optmdl.UnitId);
            //if (gqitem.Item1.Count > 0)
            //{
            //    dicEnergyType.Add("减排", gqitem.Item1);
            //}
            model.DeviceIds.Clear();
            foreach (var item in dicEnergyType)
            {
                if (item.Value != null) model.DeviceIds.AddRange(item.Value.Select(t => t.DeviceId));
            }
            model.ParamCodes = new List<string> { "energy" };
            model.ParamTypeName = "";

            #endregion

            int timemax = 0;

            #region 数据获取时间段整理

            List<ReportAnalysisInfo> datalist = new List<ReportAnalysisInfo>();
            if (model.DataType >= 1 && model.DataType <= 2)
            {
                //本期日能耗表查询
                var datavalue = GetDayDataList(model);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffDays(model.EndTime);
            }
            else if (model.DataType == 3)
            {
                //本期周能耗表查询
                var datavalue = GetWeekDataList(model);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffWeeks(model.EndTime);
            }
            else if (model.DataType >= 4 && model.DataType <= 5)
            {
                //本期月能耗表查询
                var datavalue = GetMonthDataList(model);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffMonths(model.EndTime) + 1;
                if (model.DataType == 5) timemax = model.StartTime.DiffYears(model.EndTime) + 1;
            }
            if (datalist.Count == 0) return chart;

            #endregion

            int keyindex = 1;
            foreach (var key in dicEnergyType.Keys)
            {
                DataChartChild chartChild = new DataChartChild()
                {
                    ChartTuLiId = $"{keyindex}-{key}",
                };
                chartChild.ChartTuLi = key;
                chart.ChartTuY.Add(chartChild);
                keyindex++;
            }

            #region 数据整理

            var unit = BasicunitInfoDAO.Instance.GetOneBy(t => t.UnitId == optmdl.UnitId);
            Dictionary<string, double> dicFactors = new Dictionary<string, double>();
            if (unit != null)
            {
                var area = SysAreaDAO.Instance.GetOneBy(x => x.FullCode == unit.AreaId);
                if (area != null)
                {
                    dicFactors.Add("电", area.ExpandObject.ElecFactors.ToZxxDouble());
                    dicFactors.Add("水", area.ExpandObject.WaterFactors.ToZxxDouble());
                }
            }
            if (model.DataType == 1)
            {
                var reporttype = typeof(Expand_EventReportWeek);
                var fields = typeof(Expand_EventReportWeek).GetProperties();
                for (int i = 0; i < 24; i++)
                {
                    chart.ChartX.Add($"{i}时");
                    string fieldname = $"HourValue{i}";
                    double wetotal = 0;
                    foreach (var item in chart.ChartTuY)
                    {
                        var tulilsit = item.ChartTuLiId.Split("-").ToList();
                        string key = tulilsit[1];
                        string value = "-";
                        if (dicEnergyType[key].IsZxxAny())
                        {
                            var devids = dicEnergyType[key].Select(t => t.DeviceId).Distinct();
                            var _datalist = datalist.FindAll(t => devids.Contains(t.DeviceId));
                            if (_datalist.IsZxxAny())
                            {
                                var newkey = key;
                                if (key == "减排") newkey = "电";
                                var field = fields.FirstOrDefault(t => t.Name == fieldname);
                                if (field != null)
                                {
                                    double totalbq = 0;
                                    bool isvalue = false;
                                    foreach (var _data in _datalist)
                                    {
                                        var param = _data.ExpandObjects.FirstOrDefault(t => t.ParamCode == "energy");
                                        if (param != null)
                                        {
                                            var CarbonValue = field.GetValue(param).ToZxxDouble() * dicFactors[newkey];
                                            totalbq += CarbonValue;
                                            isvalue = true;
                                        }
                                    }
                                    if (key == "减排")
                                    {
                                        if (isvalue)
                                        {
                                            var CarbonValue = totalbq * dicFactors[newkey];
                                            value = (CarbonValue - wetotal).ToString("f1");
                                        }
                                    }
                                    else
                                    {
                                        if (isvalue) value = totalbq.ToString("f1");
                                        wetotal += totalbq;
                                    }
                                }
                            }
                        }
                        item.ChartY.Add(value);
                    }
                }
            }
            else
            {
                long minbq = 0, maxbq = 0;
                for (int i = 0; i <= timemax; i++)
                {
                    string DateStr = "";
                    if (model.DataType == 2)
                    {
                        DateTime rtime = model.StartTime.AddDays(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(1));
                        DateStr = $"{rtime.Month.ToString().PadLeft(2, '0')}/{rtime.Day.ToString().PadLeft(2, '0')}";
                    }
                    else if (model.DataType == 3)
                    {
                        DateTime starttime = model.StartTime.GetFirstDayOfWeek();
                        DateTime rtime = starttime.AddDays(i * 7);
                        int rweek = rtime.GetWeekOfYear();
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(7));
                        DateStr = $"{rweek}周";
                    }
                    else if (model.DataType == 4)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, model.StartTime.Month, 1);
                        DateTime rtime = starttime.AddMonths(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddMonths(1));
                        DateStr = $"{rtime.Month}月";
                    }
                    else if (model.DataType == 5)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, 1, 1);
                        DateTime rtime = starttime.AddYears(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddYears(1));
                        DateStr = $"{rtime.Year}年";
                    }

                    chart.ChartX.Add(DateStr);
                    double wetotal = 0;
                    foreach (var item in chart.ChartTuY)
                    {
                        var tulilsit = item.ChartTuLiId.Split("-").ToList();
                        string key = tulilsit[1];
                        string value = "-";
                        if (dicEnergyType[key].IsZxxAny())
                        {
                            var devids = dicEnergyType[key].Select(t => t.DeviceId).Distinct();
                            var _datalist = datalist.FindAll(t => t.SnowId >= minbq && t.SnowId < maxbq && devids.Contains(t.DeviceId));
                            if (_datalist.IsZxxAny())
                            {
                                double totalbq = 0;
                                bool isvalue = false;
                                foreach (var _data in _datalist)
                                {
                                    var param = _data.ExpandObjects.FirstOrDefault(t => t.ParamCode == "energy");
                                    if (param != null)
                                    {
                                        totalbq += param.CarbonValue.ToZxxDouble();
                                        isvalue = true;
                                    }
                                }
                                if (key == "减排")
                                {
                                    if (isvalue)
                                    {
                                        value = (totalbq - wetotal).ToString("f1");
                                    }
                                }
                                else
                                {
                                    if (isvalue)
                                    {
                                        value = totalbq.ToString("f1");
                                        wetotal += totalbq;
                                    }
                                }
                            }
                        }
                        item.ChartY.Add(value);
                    }
                }
            }

            #endregion

            return chart;
        }

        /// <summary>
        /// 根据设备类型获取设备列表
        /// </summary>
        /// <param name="unitid">单位ID</param>
        /// <param name="devicetype">设备类型</param>
        /// <returns></returns>
        private List<DeviceInfoEntity> GetDevicesBy(int unitid, string devicetype)
        {
            List<DeviceInfoEntity> list = new List<DeviceInfoEntity>();
            var devicecList = DeviceInfoDAO.Instance.GetListBy(t => t.UnitId == unitid && t.DeviceTypeFullCode.Contains(devicetype));
            if (devicecList.IsZxxAny())
            {
                var minTreeLevel = devicecList.Min(t => t.TreeLevel);
                if (minTreeLevel > 0)
                {
                    var elecDeviceList = devicecList.FindAll(t => t.TreeLevel == minTreeLevel);
                    if (elecDeviceList.IsZxxAny()) list.AddRange(elecDeviceList);
                }
            }
            var optmdl = Request.GetToken();
            if (IsKangciUnit()) list.RemoveAll(t => IsKangciExcludedDeviceName(t.DeviceName));
            return list;
        }

        /// <summary>
        /// (日=》时/月=》日)碳排分析(表格)
        /// </summary>
        /// <param name="model">曲线</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public DataReport GetSubEneryDataTableBy(DataReportTableSelect model)
        {
            TotalCount = 1;
            DataReport reportTable = new DataReport();
            if (model.DataType == 1 && model.EndTime != model.StartTime)
            {
                Message = "选中时类型时，开始时间和结束时间必须相同";
                return reportTable;
            }
            //Table 报表强制按时间升序输出(旧→新)，忽略前端 DataSort
            model.DataSort = 1;
            // 添加固定列的表头
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "RowNo", ColumnCn = "序号" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "DateStr", ColumnCn = "时间" });

            Dictionary<string, List<DeviceInfoEntity>> dicEnergyType = new Dictionary<string, List<DeviceInfoEntity>>();

            #region 分项设备特殊处理

            var optmdl = Request.GetToken();
            //分项能耗（电/水/气/热） 
            //电
            var elecDeviceList = GetDevicesBy(optmdl.UnitId, "|zndb|");
            if (elecDeviceList.IsZxxAny())
            {
                dicEnergyType.Add("电", elecDeviceList);
            }
            //水
            var waterDeviceList = GetDevicesBy(optmdl.UnitId, "|znsb|");
            if (waterDeviceList.IsZxxAny())
            {
                dicEnergyType.Add("水", waterDeviceList);
            }
            //dicEnergyType.Add("气", null);
            //dicEnergyType.Add("热", null);
            ////光伏
            //var gqitem = GetGQDevicesBy(optmdl.UnitId);
            //if (gqitem.Item1.Count > 0)
            //{
            //    dicEnergyType.Add("减排", gqitem.Item1);
            //}
            model.DeviceIds.Clear();
            foreach (var item in dicEnergyType)
            {
                if (item.Value != null) model.DeviceIds.AddRange(item.Value.Select(t => t.DeviceId));
            }
            model.ParamCodes = new List<string> { "energy" };
            model.ParamTypeName = "";

            #endregion

            int timemax = 0;

            #region 数据获取时间段整理

            List<ReportAnalysisInfo> datalist = new List<ReportAnalysisInfo>();
            if (model.DataType >= 1 && model.DataType <= 2)
            {
                //本期日能耗表查询
                var datavalue = GetDayDataList(model);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffDays(model.EndTime);
            }
            else if (model.DataType == 3)
            {
                //本期周能耗表查询
                var datavalue = GetWeekDataList(model);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffWeeks(model.EndTime);
            }
            else if (model.DataType >= 4 && model.DataType <= 5)
            {
                //本期月能耗表查询
                var datavalue = GetMonthDataList(model);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffMonths(model.EndTime) + 1;
                if (model.DataType == 5) timemax = model.StartTime.DiffYears(model.EndTime) + 1;
            }
            if (datalist.Count == 0) return reportTable;

            #endregion

            // 添加动态列的表头
            foreach (var key in dicEnergyType.Keys)
            {
                reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = key, ColumnCn = key });
            }

            #region 数据整理

            var unit = BasicunitInfoDAO.Instance.GetOneBy(t => t.UnitId == optmdl.UnitId);
            Dictionary<string, double> dicFactors = new Dictionary<string, double>();
            if (unit != null)
            {
                var area = SysAreaDAO.Instance.GetOneBy(x => x.FullCode == unit.AreaId);
                if (area != null)
                {
                    dicFactors.Add("电", area.ExpandObject.ElecFactors.ToZxxDouble());
                    dicFactors.Add("水", area.ExpandObject.WaterFactors.ToZxxDouble());
                }
            }
            if (model.DataType == 1)
            {
                var reporttype = typeof(Expand_EventReportWeek);
                var fields = typeof(Expand_EventReportWeek).GetProperties();
                for (int i = 0; i < 24; i++)
                {
                    var row = new Dictionary<string, object>();
                    row["RowNo"] = i + 1;
                    row["DateStr"] = $"{model.StartTime.ToDateString()} {i}时";
                    string fieldname = $"HourValue{i}";
                    double wetotal = 0;
                    foreach (var key in dicEnergyType.Keys)
                    {
                        string value = "-";
                        if (dicEnergyType[key].IsZxxAny())
                        {
                            var devids = dicEnergyType[key].Select(t => t.DeviceId).Distinct();
                            var _datalist = datalist.FindAll(t => devids.Contains(t.DeviceId));
                            if (_datalist.IsZxxAny())
                            {
                                var newkey = key;
                                if (key == "减排") newkey = "电";
                                var field = fields.FirstOrDefault(t => t.Name == fieldname);
                                if (field != null)
                                {
                                    bool isvalue = false;
                                    double totalbq = 0;
                                    foreach (var _data in _datalist)
                                    {
                                        var param = _data.ExpandObjects.FirstOrDefault(t => t.ParamCode == "energy");
                                        if (param != null)
                                        {
                                            var CarbonValue = field.GetValue(param).ToZxxDouble() * dicFactors[newkey];
                                            totalbq += CarbonValue;
                                            isvalue = true;
                                        }
                                    }
                                    if (key == "减排")
                                    {
                                        if (isvalue)
                                        {
                                            var CarbonValue = totalbq * dicFactors[newkey];
                                            value = (CarbonValue - wetotal).ToString("f1");
                                        }
                                    }
                                    else
                                    {
                                        if (isvalue) value = totalbq.ToString("f1");
                                        wetotal += totalbq;
                                    }
                                }
                            }
                        }
                        row[key] = value;
                    }
                    reportTable.ReportDatas.Add(row);
                }
            }
            else
            {
                long minbq = 0, maxbq = 0;
                for (int i = 0; i <= timemax; i++)
                {
                    var row = new Dictionary<string, object>();
                    row["RowNo"] = i + 1;
                    string DateStr = "";
                    if (model.DataType == 2)
                    {
                        DateTime rtime = model.StartTime.AddDays(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(1));
                        DateStr = $"{rtime.Month.ToString().PadLeft(2, '0')}/{rtime.Day.ToString().PadLeft(2, '0')}";
                    }
                    else if (model.DataType == 3)
                    {
                        DateTime starttime = model.StartTime.GetFirstDayOfWeek();
                        DateTime rtime = starttime.AddDays(i * 7);
                        int rweek = rtime.GetWeekOfYear();
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(7));
                        DateStr = $"{rweek}周";
                    }
                    else if (model.DataType == 4)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, model.StartTime.Month, 1);
                        DateTime rtime = starttime.AddMonths(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddMonths(1));
                        DateStr = $"{rtime.Month}月";
                    }
                    else if (model.DataType == 5)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, 1, 1);
                        DateTime rtime = starttime.AddYears(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddYears(1));
                        DateStr = $"{rtime.Year}年";
                    }

                    row["DateStr"] = DateStr;
                    double wetotal = 0;
                    foreach (var key in dicEnergyType.Keys)
                    {
                        string value = "-";
                        if (dicEnergyType[key].IsZxxAny())
                        {
                            var devids = dicEnergyType[key].Select(t => t.DeviceId).Distinct();
                            var _datalist = datalist.FindAll(t => t.SnowId >= minbq && t.SnowId < maxbq && devids.Contains(t.DeviceId));
                            if (_datalist.IsZxxAny())
                            {
                                double totalbq = 0;
                                bool isvalue = false;
                                foreach (var _data in _datalist)
                                {
                                    var param = _data.ExpandObjects.FirstOrDefault(t => t.ParamCode == "energy");
                                    if (param != null)
                                    {
                                        totalbq += param.CarbonValue.ToZxxDouble();
                                        isvalue = true;
                                    }
                                }
                                if (key == "减排")
                                {
                                    if (isvalue)
                                    {
                                        value = (totalbq - wetotal).ToString("f1");
                                    }
                                }
                                else
                                {
                                    if (isvalue)
                                    {
                                        value = totalbq.ToString("f1");
                                        wetotal += totalbq;
                                    }
                                }
                            }
                        }
                        row[key] = value;
                    }
                    reportTable.ReportDatas.Add(row);
                }
            }

            #endregion
            return reportTable;
        }

        /// <summary>
        /// 碳排分析(导出)
        /// </summary>
        /// <param name="model">导出</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public MetaData GetSubEneryDataTableExcelBy(DataReportChartSelect model)
        {
            TotalCount = 1;
            MetaData data = new MetaData
            {
                Status = false,
                Message = "碳排分析数据导出失败"
            };
            DataReportTableSelect tableSelect = new DataReportTableSelect();
            model.CopyTypeValue(tableSelect);
            tableSelect.page = 1;
            // pagesize=0 走全量查询(GetListBy)，保证导出数据完整
            tableSelect.pagesize = 0;
            var reportTable = GetSubEneryDataTableBy(tableSelect);
            if (reportTable == null || reportTable.ReportDatas.Count == 0)
            {
                data.Message = "碳排分析无数据可导出";
                return data;
            }
            // 生成文件名
            string fileName = $"碳排分析数据-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
            string filepath = Path.Combine(OperatorCommon.NetLocalfile, "export", fileName);
            string serverparh = Path.Combine(OperatorCommon.NetYingShefile, "export", fileName);
            filepath.EnsureDirectory(true);

            if (reportTable.ExportExcelCom(filepath))
            {
                // 返回文件信息
                data.Status = true;
                data.Message = "碳排分析数据导出成功";
                data.Result = serverparh;
            }
            return data;
        }

        #endregion

        #region 能耗报表

        /// <summary>
        /// 日报表(表格)
        /// </summary>
        /// <param name="model">日报表查询条件</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public DataReport GetReoprtDayTableBy(DataReportTableSelect model)
        {
            DataReport reportTable = new DataReport();
            if (model.DataType == 1 && model.EndTime != model.StartTime)
            {
                Message = "选中时类型时，开始时间和结束时间必须相同";
                return reportTable;
            }
            //Table 报表强制按时间升序输出(旧→新)，忽略前端 DataSort
            model.DataSort = 1;
            model.ParamTypeName = "";
            model.ParamCodes.Clear();
            model.ParamCodes.Add("energy");
            // 添加固定列的表头
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "RowNo", ColumnCn = "序号" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "DeviceId", ColumnCn = "设备编号" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "DeviceName", ColumnCn = "设备名称" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "DateStr", ColumnCn = "日期" });

            List<ReportAnalysisInfo> datalist = new List<ReportAnalysisInfo>();
            //本期日能耗表查询
            var datavalue = GetDayDataList(model, model.page, model.pagesize);
            if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
            var timemax = model.StartTime.DiffDays(model.EndTime);
            if (datalist.Count == 0) return reportTable;

            var devdata = datalist.MaxBy(t => t.ExpandObjects.Count);
            if (devdata == null) return reportTable;

            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "energy", ColumnCn = "天能耗" });
            // 添加动态列的表头
            var reporttype = typeof(Expand_EventReportWeek);
            var hourFields = reporttype.GetProperties()
                .Where(p => p.Name.StartsWith("HourValue"))
                .OrderBy(p => int.Parse(p.Name.Replace("HourValue", "")))
                .ToList();
            foreach (var hf in hourFields)
            {
                var attr = hf.GetCustomAttributes(typeof(System.ComponentModel.DisplayNameAttribute), false)
                    .FirstOrDefault() as System.ComponentModel.DisplayNameAttribute;
                string hourCn = attr?.DisplayName ?? hf.Name;
                reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = $"energy_{hf.Name}", ColumnCn = $"{hourCn}" });
            }

            long minbq = 0, maxbq = 0;
            int rowNo = 1;
            for (int i = 0; i < timemax; i++)
            {
                DateTime rtime = model.StartTime.AddDays(i);
                minbq = SnowModel.Instance.GetId(rtime);
                maxbq = SnowModel.Instance.GetId(rtime.AddDays(1));

                var _datalist = datalist.FindAll(t => t.SnowId >= minbq && t.SnowId < maxbq);
                if (!_datalist.IsZxxAny()) continue;

                // 按设备分组，每台设备生成一行
                var deviceGroups = _datalist.GroupBy(t => t.DeviceId);
                foreach (var devGroup in deviceGroups)
                {
                    var row = new Dictionary<string, object>();
                    row["RowNo"] = rowNo++;
                    row["DateStr"] = rtime.ToDateString();

                    // 取当前设备名称
                    var firstDev = devGroup.First();
                    string devName = firstDev.DeviceName ?? "";
                    if (devName.Contains("|"))
                        devName = devName.Split('|').Last();
                    row["DeviceId"] = firstDev.DeviceId;
                    row["DeviceName"] = devName;

                    // 同一设备同一天可能有多条记录，合并累加小时数据
                    foreach (var _data in devGroup)
                    {
                        foreach (var item in _data.ExpandObjects)
                        {
                            if (!reportTable.ReportColumns.Any(t => t.ColumnEn == item.ParamCode)) continue;

                            // 天能耗（总量）
                            row[item.ParamCode] = $"{item.TotalValue}{item.ValueUnit}";

                            // 24小时逐时数据
                            foreach (var hf in hourFields)
                            {
                                string colKey = $"{item.ParamCode}_{hf.Name}";
                                if (!reportTable.ReportColumns.Any(t => t.ColumnEn == colKey)) continue;
                                var hval = hf.GetValue(item).ToZxxDecimal();
                                row[colKey] = hval != null ? $"{hval}{item.ValueUnit}" : "";
                            }
                        }
                    }
                    reportTable.ReportDatas.Add(row);
                }
            }

            return reportTable;
        }

        /// <summary>
        /// 日报表(导出)
        /// </summary>
        /// <param name="model">日报表查询条件</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public MetaData GetReoprtDayExcelBy(DataReportChartSelect model)
        {
            TotalCount = 1;
            MetaData data = new MetaData
            {
                Status = false,
                Message = "日报表数据导出失败"
            };
            DataReportTableSelect tableSelect = new DataReportTableSelect();
            model.CopyTypeValue(tableSelect);
            tableSelect.page = 1;
            // pagesize=0 走全量查询(GetListBy)，保证导出数据完整
            tableSelect.pagesize = 0;
            var reportTable = GetReoprtDayTableBy(tableSelect);
            if (reportTable == null || reportTable.ReportDatas.Count == 0)
            {
                data.Message = "日报表无数据可导出";
                return data;
            }

            // 生成文件名
            string fileName = $"日报表数据-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
            string filepath = Path.Combine(OperatorCommon.NetLocalfile, "export", fileName);
            string serverparh = Path.Combine(OperatorCommon.NetYingShefile, "export", fileName);
            filepath.EnsureDirectory(true);

            if (reportTable.ExportExcelCom(filepath))
            {
                // 返回文件信息
                data.Status = true;
                data.Message = "日报表数据导出成功";
                data.Result = serverparh;
            }
            return data;
        }

        /// <summary>
        /// 所有设备能耗(日/月/年)(表格)
        /// </summary>
        /// <remarks>
        /// 数据来源参考 GetDataTableExcelBy(日/周/月能耗表)，导出格式参考 GetReoprtDayExcelBy。
        /// 通过 DataType 切换统计粒度：2=日(走日能耗表)、4=月(走月能耗表)、5=年(走月能耗表按年聚合)。
        /// 仅返回能耗参数(energy)。IsTotal=1 时多设备能耗按时间点累加成一个合计值(每个时间一行)；IsTotal=0 时每台设备每个时间点各一行。
        /// DeviceIds 传值即可。
        /// </remarks>
        /// <param name="model">查询条件(DeviceIds/DataType/IsTotal)</param>
        /// <returns>DataReport(设备能耗表格)</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public DataReport GetDeviceEnergyTableBy(DataReportTableSelect model)
        {
            DataReport reportTable = new DataReport();
            // 入口参数限制为能耗：ParamCodes 仅保留 energy，或 ParamTypeName='能耗'，二选一
            // 其它非能耗参数(温度/功率等)一律忽略，确保 IsTotal 累加只对能耗生效
            if (model.ParamTypeName == "能耗")
            {
                model.ParamCodes.Clear();
            }
            else
            {
                model.ParamTypeName = "";
                model.ParamCodes.Clear();
                model.ParamCodes.Add("energy");
            }
            //Table 报表强制按时间升序输出(旧→新)，忽略前端 DataSort
            model.DataSort = 1;

            // 固定列：序号、时间
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "RowNo", ColumnCn = "序号" });
            bool isTotal = model.IsTotal == 1;
            if (!isTotal)
            {
                // 非累加：按设备逐行
                reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "DeviceId", ColumnCn = "设备编号" });
                reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "DeviceName", ColumnCn = "设备名称" });
            }
            else
            {
                // 累加 + 设备维度(DeviceIds)：追加一列"合计"标识
                reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "TotalName", ColumnCn = "合计" });
            }
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "DateStr", ColumnCn = "日期" });
            // 能耗动态列(实际只有 energy 一列，按数据返回的单位呈现)
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "energy", ColumnCn = "能耗" });

            // 按粒度取数：2=日表，4/5=月表(年=月表按年聚合)
            List<ReportAnalysisInfo> datalist = new List<ReportAnalysisInfo>();
            int timemax = 0;
            if (model.DataType == 2)
            {
                var datavalue = GetDayDataList(model, model.page, model.pagesize);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                timemax = model.StartTime.DiffDays(model.EndTime);
            }
            else if (model.DataType == 4 || model.DataType == 5)
            {
                var datavalue = GetMonthDataList(model, model.page, model.pagesize);
                if (datavalue.Item1.IsZxxAny()) datalist.AddRange(datavalue.Item1);
                if (model.DataType == 4)
                {
                    timemax = model.StartTime.DiffMonths(model.EndTime) + 1;
                }
                else
                {
                    timemax = model.StartTime.DiffYears(model.EndTime) + 1;
                }
            }
            else
            {
                Message = "数据类型仅支持 2=日、4=月、5=年";
                return reportTable;
            }
            if (datalist.Count == 0) return reportTable;

            int rowNo = 1;
            if (model.DataType == 2)
            {
                // 日：逐日遍历
                for (int i = 0; i < timemax; i++)
                {
                    DateTime rtime = model.StartTime.AddDays(i);
                    long minbq = SnowModel.Instance.GetId(rtime);
                    long maxbq = SnowModel.Instance.GetId(rtime.AddDays(1));
                    string dateStr = rtime.ToDateString();
                    var _datalist = datalist.FindAll(t => t.SnowId >= minbq && t.SnowId < maxbq);
                    if (!_datalist.IsZxxAny()) continue;

                    if (isTotal)
                    {
                        AppendEnergyTotalRow(reportTable, model, ref rowNo, dateStr, _datalist);
                    }
                    else
                    {
                        AppendEnergyDeviceRows(reportTable, ref rowNo, dateStr, _datalist);
                    }
                }
            }
            else
            {
                // 月/年：月表数据按月或按年聚合
                for (int i = 0; i < timemax; i++)
                {
                    DateTime rtime;
                    long minbq, maxbq;
                    string dateStr;
                    if (model.DataType == 4)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, model.StartTime.Month, 1);
                        rtime = starttime.AddMonths(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddMonths(1));
                        dateStr = $"{rtime.Year}-{rtime.Month.ToString().PadLeft(2, '0')}";
                    }
                    else
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, 1, 1);
                        rtime = starttime.AddYears(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddYears(1));
                        dateStr = $"{rtime.Year}";
                    }
                    var _datalist = datalist.FindAll(t => t.SnowId >= minbq && t.SnowId < maxbq);
                    if (!_datalist.IsZxxAny()) continue;

                    if (isTotal)
                    {
                        AppendEnergyTotalRow(reportTable, model, ref rowNo, dateStr, _datalist);
                    }
                    else
                    {
                        AppendEnergyDeviceRows(reportTable, ref rowNo, dateStr, _datalist);
                    }
                }
            }

            return reportTable;
        }

        /// <summary>
        /// 所有设备能耗(日/月/年)(导出)
        /// </summary>
        /// <remarks>
        /// 数据来源参考 GetDataTableExcelBy，导出格式参考 GetReoprtDayExcelBy。
        /// 将 GetDeviceEnergyTableBy 的结果导出为 xlsx，返回文件下载路径。
        /// </remarks>
        /// <param name="model">查询条件(DeviceIds/DataType/IsTotal)</param>
        /// <returns>MetaData(Result 为导出文件路径)</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public MetaData GetDeviceEnergyExcelBy(DataReportChartSelect model)
        {
            TotalCount = 1;
            MetaData data = new MetaData
            {
                Status = false,
                Message = "设备能耗数据导出失败"
            };
            DataReportTableSelect tableSelect = new DataReportTableSelect();
            model.CopyTypeValue(tableSelect);
            tableSelect.page = 1;
            // pagesize=0 走全量查询(GetListBy)，保证导出数据完整
            tableSelect.pagesize = 0;
            // 入口参数限制为能耗：忽略外部传入的非能耗参数，强制收敛为 energy 或 ParamTypeName='能耗'
            if (tableSelect.ParamTypeName == "能耗")
            {
                tableSelect.ParamCodes.Clear();
            }
            else
            {
                tableSelect.ParamTypeName = "";
                tableSelect.ParamCodes.Clear();
                tableSelect.ParamCodes.Add("energy");
            }
            var reportTable = GetDeviceEnergyTableBy(tableSelect);
            if (reportTable == null || reportTable.ReportDatas.Count == 0)
            {
                data.Message = "设备能耗无数据可导出";
                return data;
            }

            // 生成文件名
            string typeName = model.DataType == 5 ? "年" : (model.DataType == 4 ? "月" : "日");
            string fileName = $"所有设备({typeName})能耗-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
            string filepath = Path.Combine(OperatorCommon.NetLocalfile, "export", fileName);
            string serverparh = Path.Combine(OperatorCommon.NetYingShefile, "export", fileName);
            filepath.EnsureDirectory(true);

            if (reportTable.ExportExcelCom(filepath))
            {
                // 返回文件信息
                data.Status = true;
                data.Message = "设备能耗数据导出成功";
                data.Result = serverparh;
            }
            return data;
        }

        /// <summary>
        /// 能耗合计行：同一时间点(日/月/年)所有设备的 energy 累加成一个值
        /// </summary>
        /// <remarks>
        /// 维度列填"合计"标识列(DeviceIds)。
        /// </remarks>
        /// <param name="reportTable">表格输出</param>
        /// <param name="model">查询条件(用于判断维度)</param>
        /// <param name="rowNo">当前序号(引用递增)</param>
        /// <param name="dateStr">时间显示文本</param>
        /// <param name="datalist">该时间点范围内的数据</param>
        private void AppendEnergyTotalRow(DataReport reportTable, DataReportTableSelect model, ref int rowNo, string dateStr, List<ReportAnalysisInfo> datalist)
        {
            double totalbq = 0;
            bool isvalue = false;
            string unit = "";
            foreach (var _data in datalist)
            {
                var param = _data.ExpandObjects.FirstOrDefault(t => t.ParamCode == "energy");
                if (param != null)
                {
                    totalbq += param.TotalValue.ToZxxDouble();
                    unit = param.ValueUnit;
                    isvalue = true;
                }
            }
            var row = new Dictionary<string, object>();
            row["RowNo"] = rowNo++;
            row["DateStr"] = dateStr;

            // 维度列：合计(DeviceIds)
            row["TotalName"] = "合计";

            row["energy"] = isvalue ? $"{totalbq.ToString("f1")}{unit}" : "-";
            reportTable.ReportDatas.Add(row);
        }

        /// <summary>
        /// 能耗设备行：同一时间点(日/月/年)每台设备各占一行
        /// </summary>
        /// <param name="reportTable">表格输出</param>
        /// <param name="rowNo">当前序号(引用递增)</param>
        /// <param name="dateStr">时间显示文本</param>
        /// <param name="datalist">该时间点范围内的数据</param>
        private void AppendEnergyDeviceRows(DataReport reportTable, ref int rowNo, string dateStr, List<ReportAnalysisInfo> datalist)
        {
            // 同一设备在区间内可能有多条记录，按设备分组累加
            var deviceGroups = datalist.GroupBy(t => t.DeviceId);
            foreach (var devGroup in deviceGroups)
            {
                double totalbq = 0;
                bool isvalue = false;
                string unit = "";
                foreach (var _data in devGroup)
                {
                    var param = _data.ExpandObjects.FirstOrDefault(t => t.ParamCode == "energy");
                    if (param != null)
                    {
                        totalbq += param.TotalValue.ToZxxDouble();
                        unit = param.ValueUnit;
                        isvalue = true;
                    }
                }
                var firstDev = devGroup.First();
                string devName = firstDev.DeviceName ?? "";
                if (devName.Contains("|")) devName = devName.Split('|').Last();

                var row = new Dictionary<string, object>();
                row["RowNo"] = rowNo++;
                row["DateStr"] = dateStr;
                row["DeviceId"] = firstDev.DeviceId;
                row["DeviceName"] = devName;
                row["energy"] = isvalue ? $"{totalbq.ToString("f1")}{unit}" : "-";
                reportTable.ReportDatas.Add(row);
            }
        }

        #endregion

        #region 24小时用电能耗曲线(今日 vs 昨日)

        /// <summary>
        /// 24小时能耗曲线(今日 vs 昨日)
        /// </summary>
        /// <remarks>
        /// 返回格式与 GetDataChartBy(DataType=1, 时类型) 一致：
        /// ChartX 为 "0时"~"23时"；ChartTuY 中每个参数拆分为两条曲线，
        /// 图例名称带 "今日"/"昨日" 前缀，图例ID 为 "today/yesterday-{paramcode}"。
        /// 今日曲线只填充到当前小时(实时)，昨日为完整24小时。
        /// </remarks>
        /// <param name="model">查询条件(StartTime/EndTime/DataType 自动忽略，强制使用今日与昨日)</param>
        /// <returns>DataChart(包含今日、昨日两条曲线)</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public DataChart GetTodayVsYesterdayChart()
        {
            TotalCount = 1;
            DataChart chart = new DataChart();
            DataReportChartSelect model = new DataReportChartSelect
            {
                DataTypeDL = "zndb",
            };
            if (!model.ParamCodes.IsZxxAny()) model.ParamCodes.Add("energy");
            DateTime today = DateTime.Today;
            DateTime yesterday = today.AddDays(-1);
            int curHour = DateTime.Now.Hour;

            // 计算今日、昨日的 SnowId 边界(左闭右开)
            long todayMin = SnowModel.Instance.GetId(today);
            long todayMax = SnowModel.Instance.GetId(today.AddDays(1));
            long yestMin = SnowModel.Instance.GetId(yesterday);
            long yestMax = SnowModel.Instance.GetId(today);

            // 强制按"时类型"查询区间(昨日 ~ 今日，覆盖这两天)
            model.DataType = 1;
            model.StartTime = yesterday;
            model.EndTime = today.AddDays(1);

            var datavalue = GetDayDataList(model);
            if (!datavalue.Item1.IsZxxAny()) return chart;
            var datalist = datavalue.Item1;

            // 今日曲线
            chart.ChartTuY.Add(new DataChartChild()
            {
                ChartTuLi = $"今日",
                ChartTuLiId = $"today",
            });
            // 昨日曲线
            chart.ChartTuY.Add(new DataChartChild()
            {
                ChartTuLi = $"昨日",
                ChartTuLiId = $"yesterday",
            });

            // 反射获取 HourValue0~HourValue23 字段
            var fields = typeof(Expand_EventReportWeek).GetProperties();

            // 按 SnowId 拆分今日/昨日数据
            var todayList = datalist.FindAll(t => t.SnowId >= todayMin && t.SnowId < todayMax).SelectMany(t => t.ExpandObjects);
            var yestList = datalist.FindAll(t => t.SnowId >= yestMin && t.SnowId < yestMax).SelectMany(t => t.ExpandObjects);

            // X 轴固定 24 小时；今日按 ChartY 索引推算当前小时
            for (int i = 0; i < 24; i++)
            {
                chart.ChartX.Add($"{i}时");

                for (int j = 0; j < chart.ChartTuY.Count; j++)
                {
                    var tuli = chart.ChartTuY[j];
                    var tulilsit = tuli.ChartTuLiId.Split("-").ToList();
                    string prefix = tulilsit[0];           // today / yesterday
                    string fieldname = $"HourValue{i}";

                    string value = "-";
                    // 今日：当前小时之后显示 "-"；昨日：完整 24 小时
                    bool needFill = prefix == "yesterday" || (prefix == "today" && i <= curHour);
                    if (needFill)
                    {
                        var field = fields.FirstOrDefault(t => t.Name == fieldname);
                        if (field != null)
                        {
                            var srcList = prefix == "today" ? todayList : yestList;
                            double totalbq = 0;
                            bool isvalue = false;
                            if (srcList.IsZxxAny())
                            {
                                foreach (var _data in srcList)
                                {
                                    totalbq += field.GetValue(_data).ToZxxDouble();
                                    isvalue = true;
                                }
                            }
                            // 今日当前小时数据为0时显示"-"
                            if (prefix == "today" && i == curHour && totalbq == 0)
                                value = "-";
                            else if (isvalue)
                                value = totalbq.ToString("f1");
                        }
                    }
                    tuli.ChartY.Add(value);
                }
            }

            return chart;
        }

        #endregion

    }
}