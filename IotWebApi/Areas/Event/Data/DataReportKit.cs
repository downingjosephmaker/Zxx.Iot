using CenBoCommon.Zxx;
using IotModel;
using IotWebApi.Areas.Event.Models;

namespace IotWebApi.Areas.Event.Data
{
    /// <summary>
    /// 能耗报表查询辅助类
    /// </summary>
    public class DataReportKit
    {
        /// <summary>
        /// 根据条件获取日能耗数据列表
        /// </summary>
        /// <param name="model"></param>
        /// <param name="page"></param>
        /// <param name="pagesize"></param>
        /// <returns></returns>
        public static (List<ReportAnalysisInfo>, int) GetDayDataList(DataReportChartSelect model, int page = 1, int pagesize = 10000)
        {
            int totalcount = 0;
            List<ReportAnalysisInfo> infolist = new List<ReportAnalysisInfo>();

            #region 查询条件处理

            ActionPara actionModel = new ActionPara()
            {
                starttime = model.StartTime.ToDateString(),
                endtime = model.EndTime.AddDays(1).ToDateString(),
                page = page,
                pagesize = pagesize,
            };
            actionModel.sconlist.Add(new SelectCondition
            {
                ParamName = "UnitId",
                ParamType = "=",
                ParamValue = model.UnitId.ToString()
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

            #endregion

            var datalist = EventReportDayDAO.Instance.GetListByPage(actionModel, ref totalcount);
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
                        infolist.Add(info);
                    }
                }
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
        public static (List<ReportAnalysisInfo>, int) GetWeekDataList(DataReportChartSelect model, int page = 1, int pagesize = 10000)
        {
            int totalcount = 0;
            List<ReportAnalysisInfo> infolist = new List<ReportAnalysisInfo>();

            #region 查询条件处理

            DateTime starttime = model.StartTime.GetFirstDayOfWeek();
            DateTime endtime = model.EndTime.GetLastDayOfWeek();
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
                ParamValue = model.UnitId.ToString()
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

            #endregion

            var datalist = EventReportWeekDAO.Instance.GetListByPage(actionModel, ref totalcount);
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
                    infolist.Add(info);
                }
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
        public static (List<ReportAnalysisInfo>, int) GetMonthDataList(DataReportChartSelect model, int page = 1, int pagesize = 10000)
        {
            int totalcount = 0;
            List<ReportAnalysisInfo> infolist = new List<ReportAnalysisInfo>();

            #region 查询条件处理

            DateTime starttime = new DateTime(model.StartTime.Year, model.StartTime.Month, 1);
            var _endtime = model.EndTime.AddDays(1);
            DateTime endtime = new DateTime(_endtime.Year, _endtime.Month, 1);
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
                ParamValue = model.UnitId.ToString()
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

            #endregion

            var datalist = EventReportMonthDAO.Instance.GetListByPage(actionModel, ref totalcount);
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
                        infolist.Add(info);
                    }
                }
            }

            return (infolist, totalcount);
        }

    }
}
