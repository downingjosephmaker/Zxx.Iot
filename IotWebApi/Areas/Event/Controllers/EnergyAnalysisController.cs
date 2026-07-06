using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using IotModel;
using IotWebApi.Areas.Event.Data;
using IotWebApi.Areas.Event.Models;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 统计分析页面(所有统计参数)
    /// </summary>
    [ApiController]
    [ControllSort("25-7")]
    public class EnergyAnalysisController : ControllerBaseApi
    {
        #region 同比分析

        /// <summary>
        /// 同比分析
        /// </summary>
        /// <param name="model">同比分析</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public EnergyAnalysis<EnergyAnalysisTable> GetEnergyTbAnalysis(EnergyAnalysisSelect model)
        {
            EnergyAnalysis<EnergyAnalysisTable> energy = new EnergyAnalysis<EnergyAnalysisTable>();
            if (model.DataType == 1 && model.EndTime != model.StartTime)
            {
                Message = "选中时类型时，开始时间和结束时间必须相同";
                return energy;
            }
            if (model.ParamCodes.IsZxxAny()) model.ParamCodes.RemoveAll(t => t.Contains("_"));
            int timemax = 0;
            //本期能耗表查询
            List<ReportAnalysisInfo> datalist = new List<ReportAnalysisInfo>();
            //同期能耗表查询
            List<ReportAnalysisInfo> datalistyes = new List<ReportAnalysisInfo>();
            if (model.DataType >= 1 && model.DataType <= 2)
            {
                //本期日能耗表查询
                datalist.AddRange(GetDayDataList(model));
                timemax = model.StartTime.DiffDays(model.EndTime);
                //同期日能耗表查询
                EnergyAnalysisSelect modeltq = new EnergyAnalysisSelect();
                model.CopyTypeValue(modeltq);
                modeltq.StartTime = model.StartTime.AddYears(-1).Date;
                modeltq.EndTime = model.EndTime.AddYears(-1).Date;
                datalistyes.AddRange(GetDayDataList(modeltq));
            }
            else if (model.DataType == 3)
            {
                //本期周能耗表查询
                datalist.AddRange(GetWeekDataList(model));
                timemax = model.StartTime.DiffWeeks(model.EndTime);
                //同期周能耗表查询
                int startweek = model.StartTime.GetWeekOfYear();
                int endweek = model.EndTime.GetWeekOfYear();
                EnergyAnalysisSelect modeltq = new EnergyAnalysisSelect();
                model.CopyTypeValue(modeltq);
                modeltq.StartTime = (model.StartTime.Year - 1).GetFirstDayOfWeek(startweek);
                modeltq.EndTime = (model.EndTime.Year - 1).GetEndDateOfWeek(endweek);
                datalistyes.AddRange(GetWeekDataList(modeltq));
            }
            else if (model.DataType >= 4 && model.DataType <= 5)
            {
                //本期月能耗表查询
                datalist.AddRange(GetMonthDataList(model));
                timemax = model.StartTime.DiffMonths(model.EndTime);
                if (model.DataType == 5) timemax = model.StartTime.DiffYears(model.EndTime);
                //同期月能耗表查询
                EnergyAnalysisSelect modeltq = new EnergyAnalysisSelect();
                model.CopyTypeValue(modeltq);
                modeltq.StartTime = model.StartTime.AddYears(-1).Date;
                modeltq.EndTime = model.EndTime.AddYears(-1).Date;
                datalistyes.AddRange(GetMonthDataList(modeltq));
            }
            if (datalist.Count == 0) return energy;

            DataChartChild charttq = new DataChartChild()
            {
                ChartTuLi = "同期",
                ChartTuLiId = "1",
            };
            energy.chart.ChartTuY.Add(charttq);

            DataChartChild chartbq = new DataChartChild()
            {
                ChartTuLi = "本期",
                ChartTuLiId = "2",
            };
            energy.chart.ChartTuY.Add(chartbq);

            DataChartChild charzjl = new DataChartChild()
            {
                ChartTuLi = $"增减率",
                ChartTuLiId = "3",
            };
            energy.chart.ChartTuY.Add(charzjl);

            if (model.DataType == 1)
            {
                var reporttype = typeof(Expand_EventReportWeek);
                var fields = typeof(Expand_EventReportWeek).GetProperties();
                for (int i = 0; i < 24; i++)
                {
                    var item = new EnergyAnalysisTable
                    {
                        DateStr = $"{i}时",
                    };
                    string fieldname = $"HourValue{i}";
                    var field = fields.FirstOrDefault(t => t.Name == fieldname);
                    if (field != null)
                    {
                        //本期
                        {
                            double totalbq = 0;
                            foreach (var _data in datalist)
                            {
                                totalbq += field.GetValue(_data.ExpandObjects[0]).ToZxxDouble();
                            }
                            item.BenQi = totalbq.ToString("f1");
                            chartbq.ChartY.Add(item.BenQi);
                        }
                        //同期
                        if (datalistyes.IsZxxAny())
                        {
                            double totaltq = 0;
                            foreach (var _data in datalistyes)
                            {
                                totaltq += field.GetValue(_data.ExpandObjects[0]).ToZxxDouble();
                            }
                            item.TongQi = totaltq.ToString("f1");
                            charttq.ChartY.Add(item.TongQi);
                        }
                        else
                        {
                            charttq.ChartY.Add("-");
                        }
                    }
                    item.ZengJianZhi = (item.BenQi.ToZxxDecimal() - item.TongQi.ToZxxDecimal()).ToString("f1");
                    if (item.TongQi.ToZxxDecimal() > 0)
                    {
                        item.ZengJianLv = (item.ZengJianZhi.ToZxxDecimal() * 100 / item.TongQi.ToZxxDecimal()).ToString("f2");
                        charzjl.ChartY.Add(item.ZengJianLv);
                    }
                    else
                    {
                        charzjl.ChartY.Add("-");
                    }
                    energy.table.Add(item);
                    energy.chart.ChartX.Add(item.DateStr);
                }
            }
            else
            {
                long minbq = 0, maxbq = 0, mintq = 0, maxtq = 0;
                for (int i = 0; i <= timemax; i++)
                {
                    EnergyAnalysisTable item = new EnergyAnalysisTable();
                    if (model.DataType == 2)
                    {
                        DateTime rtime = model.StartTime.AddDays(i);
                        DateTime yestime = rtime.AddYears(-1);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(1));

                        mintq = SnowModel.Instance.GetId(yestime);
                        maxtq = SnowModel.Instance.GetId(yestime.AddDays(1));
                        item.DateStr = $"{rtime.Day}日";
                    }
                    else if (model.DataType == 3)
                    {
                        DateTime starttime = model.StartTime.GetFirstDayOfWeek();
                        DateTime rtime = starttime.AddDays(i * 7);
                        int rweek = rtime.GetWeekOfYear();
                        DateTime yestime = (rtime.Year - 1).GetFirstDayOfWeek(rweek);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(7));

                        mintq = SnowModel.Instance.GetId(yestime);
                        maxtq = SnowModel.Instance.GetId(yestime.AddDays(7));
                        item.DateStr = $"{rweek}周";
                    }
                    else if (model.DataType == 4)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, model.StartTime.Month, 1);
                        DateTime rtime = starttime.AddMonths(i);
                        DateTime yestime = rtime.AddYears(-1);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddMonths(1));

                        mintq = SnowModel.Instance.GetId(yestime);
                        maxtq = SnowModel.Instance.GetId(yestime.AddMonths(1));
                        item.DateStr = $"{rtime.Month}月";
                    }
                    else if (model.DataType == 5)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, 1, 1);
                        DateTime rtime = starttime.AddYears(i);
                        DateTime yestime = rtime.AddYears(-1);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddYears(1));

                        mintq = SnowModel.Instance.GetId(yestime);
                        maxtq = SnowModel.Instance.GetId(yestime.AddYears(1));
                        item.DateStr = $"{rtime.Year}年";
                    }

                    //本期
                    var _datalistbq = datalist.FindAll(t => t.SnowId >= minbq && t.SnowId < maxbq);
                    if (_datalistbq.IsZxxAny())
                    {
                        double total = 0;
                        foreach (var _data in _datalistbq)
                        {
                            total += _data.ExpandObjects.Sum(t => t.TotalValue.ToZxxDouble());
                        }
                        item.BenQi = total.ToString("f1");
                        chartbq.ChartY.Add(item.BenQi);
                    }
                    else
                    {
                        chartbq.ChartY.Add("-");
                    }
                    //同期
                    if (datalistyes.IsZxxAny())
                    {
                        var _datalisttq = datalistyes.FindAll(t => t.SnowId >= mintq && t.SnowId < maxtq);
                        if (_datalisttq.IsZxxAny())
                        {
                            double total = 0;
                            foreach (var _data in _datalisttq)
                            {
                                total += _data.ExpandObjects.Sum(t => t.TotalValue.ToZxxDouble());
                            }
                            item.TongQi = total.ToString("f1");
                            charttq.ChartY.Add(item.TongQi);
                        }
                        else
                        {
                            charttq.ChartY.Add("-");
                        }
                    }
                    else
                    {
                        charttq.ChartY.Add("-");
                    }
                    item.ZengJianZhi = (item.BenQi.ToZxxDecimal() - item.TongQi.ToZxxDecimal()).ToString("f1");
                    if (item.TongQi.ToZxxDecimal() > 0)
                    {
                        item.ZengJianLv = (item.ZengJianZhi.ToZxxDecimal() * 100 / item.TongQi.ToZxxDecimal()).ToString("f2");
                        charzjl.ChartY.Add(item.ZengJianLv);
                    }
                    else
                    {
                        charzjl.ChartY.Add("-");
                    }

                    energy.table.Add(item);
                    energy.chart.ChartX.Add(item.DateStr);
                }
            }

            return energy;
        }

        /// <summary>
        /// 根据条件获取日能耗数据列表（电表有等级不能查询子设备）
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private List<ReportAnalysisInfo> GetDayDataList(EnergyAnalysisSelect model)
        {
            List<ReportAnalysisInfo> infolist = new List<ReportAnalysisInfo>();
            //BuildId/DeptId 维度必须传设备大类(DataTypeDL)，否则不查询
            if (model.DeviceId == 0 && model.DataTypeDL.IsZxxNullOrEmpty() && (model.BuildId > 0 || model.DeptId > 0))
            {
                return infolist;
            }
            //DataTypeDL 转设备类型码集合
            var dtypecodes = GetDeviceTypeCodes(model.DataTypeDL);
            //IsTotal=1 时康慈单位排除总表/热水回水/热水进水设备
            bool excludeKangci = model.IsTotal == 1 && IsKangciUnit();
            long rmin = SnowModel.Instance.GetId(model.StartTime.Date);
            long rmax = SnowModel.Instance.GetId(model.EndTime.AddDays(1).Date);
            List<EventReportDayEntity> datalist = new List<EventReportDayEntity>();
            if (model.DeviceId > 0)
            {
                var _datalist = EventReportDayDAO.Instance.GetListBy(t => t.SnowId >= rmin && t.SnowId < rmax && t.DeviceId == model.DeviceId
                    && (excludeKangci ? !IsKangciExcludedDeviceName(t.DeviceName) : true));
                if (_datalist.IsZxxAny()) datalist.AddRange(_datalist);
            }
            else if (model.BuildId > 0)
            {
                var buildids = GetTargetBuildIds(model.BuildId, model.QueryMode);
                if (buildids.IsZxxAny())
                {
                    var _datalist = EventReportDayDAO.Instance.GetListBy(t => t.SnowId >= rmin && t.SnowId < rmax && buildids.Contains(t.BuildId)
                        && (excludeKangci ? !IsKangciExcludedDeviceName(t.DeviceName) : true)
                        && (dtypecodes.IsZxxAny() ? dtypecodes.Contains(t.DeviceTypeCode) : true));
                    if (_datalist.IsZxxAny()) datalist.AddRange(_datalist);
                }
            }
            else if (model.DeptId > 0)
            {
                var deptids = GetTargetDeptIds(model.DeptId, model.QueryMode);
                if (deptids.IsZxxAny())
                {
                    var _datalist = EventReportDayDAO.Instance.GetListBy(t => t.SnowId >= rmin && t.SnowId < rmax && deptids.Contains(t.DeptId)
                        && (excludeKangci ? !IsKangciExcludedDeviceName(t.DeviceName) : true)
                        && (dtypecodes.IsZxxAny() ? dtypecodes.Contains(t.DeviceTypeCode) : true));
                    if (_datalist.IsZxxAny()) datalist.AddRange(_datalist);
                }
            }
            if (!datalist.IsZxxAny()) return infolist;

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

            return infolist;
        }

        /// <summary>
        /// 根据条件获取周能耗数据列表（电表有等级不能查询子设备）
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private List<ReportAnalysisInfo> GetWeekDataList(EnergyAnalysisSelect model)
        {
            List<ReportAnalysisInfo> infolist = new List<ReportAnalysisInfo>();
            //BuildId/DeptId 维度必须传设备大类(DataTypeDL)，否则不查询
            if (model.DeviceId == 0 && model.DataTypeDL.IsZxxNullOrEmpty() && (model.BuildId > 0 || model.DeptId > 0))
            {
                return infolist;
            }
            //DataTypeDL 转设备类型码集合
            var dtypecodes = GetDeviceTypeCodes(model.DataTypeDL);
            //IsTotal=1 时康慈单位排除总表/热水回水/热水进水设备
            bool excludeKangci = model.IsTotal == 1 && IsKangciUnit();
            DateTime starttime = model.StartTime.GetFirstDayOfWeek();
            DateTime endtime = model.EndTime.GetLastDayOfWeek();
            long rmin = SnowModel.Instance.GetId(starttime.Date);
            long rmax = SnowModel.Instance.GetId(endtime.AddDays(1).Date);
            List<EventReportWeekEntity> datalist = new List<EventReportWeekEntity>();
            if (model.DeviceId > 0)
            {
                var _datalist = EventReportWeekDAO.Instance.GetListBy(t => t.SnowId >= rmin && t.SnowId < rmax && t.DeviceId == model.DeviceId
                    && (excludeKangci ? !IsKangciExcludedDeviceName(t.DeviceName) : true));
                if (_datalist.IsZxxAny()) datalist.AddRange(_datalist);
            }
            else if (model.BuildId > 0)
            {
                var buildids = GetTargetBuildIds(model.BuildId, model.QueryMode);
                if (buildids.IsZxxAny())
                {
                    var _datalist = EventReportWeekDAO.Instance.GetListBy(t => t.SnowId >= rmin && t.SnowId < rmax && buildids.Contains(t.BuildId)
                        && (excludeKangci ? !IsKangciExcludedDeviceName(t.DeviceName) : true)
                        && (dtypecodes.IsZxxAny() ? dtypecodes.Contains(t.DeviceTypeCode) : true));
                    if (_datalist.IsZxxAny()) datalist.AddRange(_datalist);
                }
            }
            else if (model.DeptId > 0)
            {
                var deptids = GetTargetDeptIds(model.DeptId, model.QueryMode);
                if (deptids.IsZxxAny())
                {
                    var _datalist = EventReportWeekDAO.Instance.GetListBy(t => t.SnowId >= rmin && t.SnowId < rmax && deptids.Contains(t.DeptId)
                        && (excludeKangci ? !IsKangciExcludedDeviceName(t.DeviceName) : true)
                        && (dtypecodes.IsZxxAny() ? dtypecodes.Contains(t.DeviceTypeCode) : true));
                    if (_datalist.IsZxxAny()) datalist.AddRange(_datalist);
                }
            }
            if (!datalist.IsZxxAny()) return infolist;

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

            return infolist;
        }

        /// <summary>
        /// 根据条件获取月能耗数据列表（电表有等级不能查询子设备）
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private List<ReportAnalysisInfo> GetMonthDataList(EnergyAnalysisSelect model)
        {
            List<ReportAnalysisInfo> infolist = new List<ReportAnalysisInfo>();
            //BuildId/DeptId 维度必须传设备大类(DataTypeDL)，否则不查询
            if (model.DeviceId == 0 && model.DataTypeDL.IsZxxNullOrEmpty() && (model.BuildId > 0 || model.DeptId > 0))
            {
                return infolist;
            }
            //DataTypeDL 转设备类型码集合
            var dtypecodes = GetDeviceTypeCodes(model.DataTypeDL);
            //IsTotal=1 时康慈单位排除总表/热水回水/热水进水设备
            bool excludeKangci = model.IsTotal == 1 && IsKangciUnit();
            DateTime starttime = new DateTime(model.StartTime.Year, model.StartTime.Month, 1);
            var _endtime = model.EndTime.AddDays(1);
            DateTime endtime = new DateTime(_endtime.Year, _endtime.Month, 1);
            long rmin = SnowModel.Instance.GetId(starttime);
            long rmax = SnowModel.Instance.GetId(endtime);
            List<EventReportMonthEntity> datalist = new List<EventReportMonthEntity>();
            if (model.DeviceId > 0)
            {
                var _datalist = EventReportMonthDAO.Instance.GetListBy(t => t.SnowId >= rmin && t.SnowId < rmax && t.DeviceId == model.DeviceId
                    && (excludeKangci ? !IsKangciExcludedDeviceName(t.DeviceName) : true));
                if (_datalist.IsZxxAny()) datalist.AddRange(_datalist);
            }
            else if (model.BuildId > 0)
            {
                var buildids = GetTargetBuildIds(model.BuildId, model.QueryMode);
                if (buildids.IsZxxAny())
                {
                    var _datalist = EventReportMonthDAO.Instance.GetListBy(t => t.SnowId >= rmin && t.SnowId < rmax && buildids.Contains(t.BuildId)
                        && (excludeKangci ? !IsKangciExcludedDeviceName(t.DeviceName) : true)
                        && (dtypecodes.IsZxxAny() ? dtypecodes.Contains(t.DeviceTypeCode) : true));
                    if (_datalist.IsZxxAny()) datalist.AddRange(_datalist);
                }
            }
            else if (model.DeptId > 0)
            {
                var deptids = GetTargetDeptIds(model.DeptId, model.QueryMode);
                if (deptids.IsZxxAny())
                {
                    var _datalist = EventReportMonthDAO.Instance.GetListBy(t => t.SnowId >= rmin && t.SnowId < rmax && deptids.Contains(t.DeptId)
                        && (excludeKangci ? !IsKangciExcludedDeviceName(t.DeviceName) : true)
                        && (dtypecodes.IsZxxAny() ? dtypecodes.Contains(t.DeviceTypeCode) : true));
                    if (_datalist.IsZxxAny()) datalist.AddRange(_datalist);
                }
            }
            if (!datalist.IsZxxAny()) return infolist;

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

            return infolist;
        }

        /// <summary>
        /// 设备大类(DataTypeDL)转设备类型码集合(取最大树层级的叶子类型码)
        /// </summary>
        /// <param name="dataTypeDL">设备大类编码</param>
        /// <returns>类型码集合；为空时返回空集合(表示不限类型)</returns>
        private List<string> GetDeviceTypeCodes(string dataTypeDL)
        {
            List<string> codes = new List<string>();
            if (dataTypeDL.IsZxxNullOrEmpty()) return codes;
            var dtypes = SysCommonDAO<DeviceType>.Instance.GetListBy(t => t.FullCode.Contains($"|{dataTypeDL}|"));
            if (dtypes.IsZxxAny())
            {
                int maxlevel = dtypes.Max(t => t.TreeLevel);
                codes = dtypes.FindAll(t => t.TreeLevel == maxlevel).Select(t => t.TypeCode).Distinct().ToList();
            }
            return codes;
        }

        /// <summary>
        /// 根据 BuildId 和 QueryMode 解析目标建筑ID集合
        /// </summary>
        /// <param name="buildId">建筑ID</param>
        /// <param name="queryMode">0:仅当前 1:含子集 2:仅子集</param>
        /// <returns>目标建筑ID集合</returns>
        private List<int> GetTargetBuildIds(int buildId, int queryMode)
        {
            if (queryMode == 0) return new List<int> { buildId };
            //含子集/仅子集：按 FullCode 查询所有下级建筑
            var builds = SysCommonDAO<BuildInfo>.Instance.GetListBy(t => t.FullCode.Contains($"|{buildId}|"));
            var buildids = builds.IsZxxAny() ? builds.Select(t => t.BuildId).Distinct().ToList() : new List<int> { buildId };
            //仅子集时剔除当前建筑本身
            if (queryMode == 2) buildids.RemoveAll(t => t == buildId);
            //含子集但没查到子集时，退回当前建筑
            if (buildids.Count == 0) buildids.Add(buildId);
            return buildids;
        }

        /// <summary>
        /// 根据 DeptId 和 QueryMode 解析目标部门ID集合
        /// </summary>
        /// <param name="deptId">部门ID</param>
        /// <param name="queryMode">0:仅当前 1:含子集 2:仅子集</param>
        /// <returns>目标部门ID集合</returns>
        private List<int> GetTargetDeptIds(int deptId, int queryMode)
        {
            if (queryMode == 0) return new List<int> { deptId };
            //含子集/仅子集：按 FullCode 查询所有下级部门
            var depts = SysCommonDAO<DeptInfo>.Instance.GetListBy(t => t.FullCode.Contains($"|{deptId}|"));
            var deptids = depts.IsZxxAny() ? depts.Select(t => t.DeptId).Distinct().ToList() : new List<int> { deptId };
            //仅子集时剔除当前部门本身
            if (queryMode == 2) deptids.RemoveAll(t => t == deptId);
            //含子集但没查到子集时，退回当前部门
            if (deptids.Count == 0) deptids.Add(deptId);
            return deptids;
        }

        /// <summary>
        /// 同比分析-导出
        /// </summary>
        /// <param name="model">同比分析</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public MetaData GetEnergyTbAnalysisExcel(EnergyAnalysisSelect model)
        {
            TotalCount = 1;
            MetaData data = new MetaData
            {
                Status = false,
                Message = "同比分析数据导出失败"
            };
            if (model.DataType == 1 && model.EndTime != model.StartTime)
            {
                Message = "选中时类型时，开始时间和结束时间必须相同";
                return data;
            }
            if (model.ParamCodes.IsZxxAny()) model.ParamCodes.RemoveAll(t => t.Contains("_"));
            int timemax = 0;
            //本期能耗表查询
            List<ReportAnalysisInfo> datalist = new List<ReportAnalysisInfo>();
            //同期能耗表查询
            List<ReportAnalysisInfo> datalistyes = new List<ReportAnalysisInfo>();
            if (model.DataType >= 1 && model.DataType <= 2)
            {
                //本期日能耗表查询
                datalist.AddRange(GetDayDataList(model));
                timemax = model.StartTime.DiffDays(model.EndTime);
                //同期日能耗表查询
                EnergyAnalysisSelect modeltq = new EnergyAnalysisSelect();
                model.CopyTypeValue(modeltq);
                modeltq.StartTime = model.StartTime.AddYears(-1).Date;
                modeltq.EndTime = model.EndTime.AddYears(-1).Date;
                datalistyes.AddRange(GetDayDataList(modeltq));
            }
            else if (model.DataType == 3)
            {
                //本期周能耗表查询
                datalist.AddRange(GetWeekDataList(model));
                timemax = model.StartTime.DiffWeeks(model.EndTime);
                //同期周能耗表查询
                int startweek = model.StartTime.GetWeekOfYear();
                int endweek = model.EndTime.GetWeekOfYear();
                EnergyAnalysisSelect modeltq = new EnergyAnalysisSelect();
                model.CopyTypeValue(modeltq);
                modeltq.StartTime = (model.StartTime.Year - 1).GetFirstDayOfWeek(startweek);
                modeltq.EndTime = (model.EndTime.Year - 1).GetEndDateOfWeek(endweek);
                datalistyes.AddRange(GetWeekDataList(modeltq));
            }
            else if (model.DataType >= 4 && model.DataType <= 5)
            {
                //本期月能耗表查询
                datalist.AddRange(GetMonthDataList(model));
                timemax = model.StartTime.DiffMonths(model.EndTime);
                if (model.DataType == 5) timemax = model.StartTime.DiffYears(model.EndTime);
                //同期月能耗表查询
                EnergyAnalysisSelect modeltq = new EnergyAnalysisSelect();
                model.CopyTypeValue(modeltq);
                modeltq.StartTime = model.StartTime.AddYears(-1).Date;
                modeltq.EndTime = model.EndTime.AddYears(-1).Date;
                datalistyes.AddRange(GetMonthDataList(modeltq));
            }
            if (datalist.Count == 0) return data;

            List<EnergyAnalysisTbExcel> energys = new List<EnergyAnalysisTbExcel>();
            if (model.DataType == 1)
            {
                var reporttype = typeof(Expand_EventReportWeek);
                var fields = typeof(Expand_EventReportWeek).GetProperties();
                for (int i = 0; i < 24; i++)
                {
                    var item = new EnergyAnalysisTbExcel
                    {
                        RowNo = i + 1,
                        DateStr = $"{i}时",
                    };
                    string fieldname = $"HourValue{i}";
                    var field = fields.FirstOrDefault(t => t.Name == fieldname);
                    if (field != null)
                    {
                        //本期
                        {
                            double totalbq = 0;
                            foreach (var _data in datalist)
                            {
                                totalbq += field.GetValue(_data.ExpandObjects[0]).ToZxxDouble();
                            }
                            item.BenQi = totalbq.ToString("f1");
                        }
                        //同期
                        if (datalistyes.IsZxxAny())
                        {
                            double totaltq = 0;
                            foreach (var _data in datalistyes)
                            {
                                totaltq += field.GetValue(_data.ExpandObjects[0]).ToZxxDouble();
                            }
                            item.TongQi = totaltq.ToString("f1");
                        }
                    }
                    item.ZengJianZhi = (item.BenQi.ToZxxDecimal() - item.TongQi.ToZxxDecimal()).ToString("f1");
                    if (item.TongQi.ToZxxDecimal() > 0)
                    {
                        item.ZengJianLv = (item.ZengJianZhi.ToZxxDecimal() * 100 / item.TongQi.ToZxxDecimal()).ToString("f2");
                    }
                    energys.Add(item);
                }
            }
            else
            {
                long minbq = 0, maxbq = 0, mintq = 0, maxtq = 0;
                for (int i = 0; i <= timemax; i++)
                {
                    EnergyAnalysisTbExcel item = new EnergyAnalysisTbExcel
                    {
                        RowNo = i + 1,
                    };
                    if (model.DataType == 2)
                    {
                        DateTime rtime = model.StartTime.AddDays(i);
                        DateTime yestime = rtime.AddYears(-1);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(1));

                        mintq = SnowModel.Instance.GetId(yestime);
                        maxtq = SnowModel.Instance.GetId(yestime.AddDays(1));
                        item.DateStr = $"{rtime.Day}日";
                    }
                    else if (model.DataType == 3)
                    {
                        DateTime starttime = model.StartTime.GetFirstDayOfWeek();
                        DateTime rtime = starttime.AddDays(i * 7);
                        int rweek = rtime.GetWeekOfYear();
                        DateTime yestime = (rtime.Year - 1).GetFirstDayOfWeek(rweek);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(7));

                        mintq = SnowModel.Instance.GetId(yestime);
                        maxtq = SnowModel.Instance.GetId(yestime.AddDays(7));
                        item.DateStr = $"{rweek}周";
                    }
                    else if (model.DataType == 4)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, model.StartTime.Month, 1);
                        DateTime rtime = starttime.AddMonths(i);
                        DateTime yestime = rtime.AddYears(-1);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddMonths(1));

                        mintq = SnowModel.Instance.GetId(yestime);
                        maxtq = SnowModel.Instance.GetId(yestime.AddMonths(1));
                        item.DateStr = $"{rtime.Month}月";
                    }
                    else if (model.DataType == 5)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, 1, 1);
                        DateTime rtime = starttime.AddYears(i);
                        DateTime yestime = rtime.AddYears(-1);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddYears(1));

                        mintq = SnowModel.Instance.GetId(yestime);
                        maxtq = SnowModel.Instance.GetId(yestime.AddYears(1));
                        item.DateStr = $"{rtime.Year}年";
                    }

                    //本期
                    var _datalistbq = datalist.FindAll(t => t.SnowId >= minbq && t.SnowId < maxbq);
                    if (_datalistbq.IsZxxAny())
                    {
                        double total = 0;
                        foreach (var _data in _datalistbq)
                        {
                            total += _data.ExpandObjects.Sum(t => t.TotalValue.ToZxxDouble());
                        }
                        item.BenQi = total.ToString("f1");
                    }
                    //同期
                    if (datalistyes.IsZxxAny())
                    {
                        var _datalisttq = datalistyes.FindAll(t => t.SnowId >= mintq && t.SnowId < maxtq);
                        if (_datalisttq.IsZxxAny())
                        {
                            double total = 0;
                            foreach (var _data in _datalisttq)
                            {
                                total += _data.ExpandObjects.Sum(t => t.TotalValue.ToZxxDouble());
                            }
                            item.TongQi = total.ToString("f1");
                        }
                    }
                    item.ZengJianZhi = (item.BenQi.ToZxxDecimal() - item.TongQi.ToZxxDecimal()).ToString("f1");
                    if (item.TongQi.ToZxxDecimal() > 0)
                    {
                        item.ZengJianLv = (item.ZengJianZhi.ToZxxDecimal() * 100 / item.TongQi.ToZxxDecimal()).ToString("f2");
                    }
                    energys.Add(item);
                }
            }

            if (energys.Count == 0)
            {
                data.Message = "同比分析无数据可导出";
                return data;
            }

            string fileName = $"同比分析数据-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
            string filepath = Path.Combine(OperatorCommon.NetLocalfile, "export", fileName);
            string serverparh = Path.Combine(OperatorCommon.NetYingShefile, "export", fileName);
            filepath.EnsureDirectory(true);

            if (energys.ExportExcelCom(filepath))
            {
                data.Status = true;
                data.Message = "同比分析数据导出成功";
                data.Result = serverparh;
            }
            return data;
        }

        #endregion

        #region 环比分析

        /// <summary>
        /// 环比分析
        /// </summary>
        /// <param name="model">环比分析</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public EnergyAnalysis<EnergyAnalysisTable> GetEnergyHbAnalysis(EnergyAnalysisSelect model)
        {
            EnergyAnalysis<EnergyAnalysisTable> energy = new EnergyAnalysis<EnergyAnalysisTable>();
            if (model.DataType == 1 && model.EndTime != model.StartTime)
            {
                Message = "选中时类型时，开始时间和结束时间必须相同";
                return energy;
            }
            if (model.ParamCodes.IsZxxAny()) model.ParamCodes.RemoveAll(t => t.Contains("_"));
            int timemax = 0;
            //本期能耗表查询
            List<ReportAnalysisInfo> datalist = new List<ReportAnalysisInfo>();
            if (model.DataType >= 1 && model.DataType <= 2)
            {
                //本期日能耗表查询
                datalist.AddRange(GetDayDataList(model));
                timemax = model.StartTime.DiffDays(model.EndTime);
            }
            else if (model.DataType == 3)
            {
                //本期周能耗表查询
                datalist.AddRange(GetWeekDataList(model));
                timemax = model.StartTime.DiffWeeks(model.EndTime);
            }
            else if (model.DataType >= 4 && model.DataType <= 5)
            {
                //本期月能耗表查询
                datalist.AddRange(GetMonthDataList(model));
                timemax = model.StartTime.DiffMonths(model.EndTime);
                if (model.DataType == 5) timemax = model.StartTime.DiffYears(model.EndTime);
            }
            if (datalist.Count == 0) return energy;

            DataChartChild chartbq = new DataChartChild()
            {
                ChartTuLi = "能耗",
                ChartTuLiId = "1",
            };
            energy.chart.ChartTuY.Add(chartbq);

            DataChartChild charzjl = new DataChartChild()
            {
                ChartTuLi = $"增减率",
                ChartTuLiId = "2",
            };
            energy.chart.ChartTuY.Add(charzjl);

            Dictionary<int, EnergyAnalysisTable> hbDateValues = new Dictionary<int, EnergyAnalysisTable>();
            if (model.DataType == 1)
            {
                var reporttype = typeof(Expand_EventReportWeek);
                var fields = typeof(Expand_EventReportWeek).GetProperties();
                for (int i = 0; i < 24; i++)
                {
                    var item = new EnergyAnalysisTable
                    {
                        DateStr = $"{i}时",
                    };
                    string fieldname = $"HourValue{i}";
                    var field = fields.FirstOrDefault(t => t.Name == fieldname);
                    if (field != null)
                    {
                        //本期
                        double totalbq = 0;
                        foreach (var _data in datalist)
                        {
                            totalbq += field.GetValue(_data.ExpandObjects[0]).ToZxxDouble();
                        }
                        item.BenQi = totalbq.ToString("f1");
                        chartbq.ChartY.Add(item.BenQi);
                    }
                    else
                    {
                        chartbq.ChartY.Add("-");
                    }
                    if (i > 0)
                    {
                        if (hbDateValues.ContainsKey(i - 1))
                        {
                            var yes = hbDateValues[i - 1];
                            item.ZengJianZhi = (item.BenQi.ToZxxDecimal() - yes.BenQi.ToZxxDecimal()).ToString("f1");
                            if (yes.BenQi.ToZxxDecimal() > 0)
                            {
                                item.ZengJianLv = (item.ZengJianZhi.ToZxxDecimal() * 100 / yes.BenQi.ToZxxDecimal()).ToString("f2");
                                charzjl.ChartY.Add(item.ZengJianLv);
                            }
                            else
                            {
                                charzjl.ChartY.Add("-");
                            }
                        }
                        else
                        {
                            charzjl.ChartY.Add("-");
                        }
                    }
                    else
                    {
                        charzjl.ChartY.Add("-");
                    }
                    energy.table.Add(item);
                    hbDateValues.TryAdd(i, item);
                    energy.chart.ChartX.Add(item.DateStr);
                }
            }
            else
            {
                long minbq = 0, maxbq = 0;
                for (int i = 0; i <= timemax; i++)
                {
                    EnergyAnalysisTable item = new EnergyAnalysisTable();
                    if (model.DataType == 2)
                    {
                        DateTime rtime = model.StartTime.AddDays(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(1));
                        item.DateStr = $"{rtime.Day}日";
                    }
                    else if (model.DataType == 3)
                    {
                        DateTime starttime = model.StartTime.GetFirstDayOfWeek();
                        DateTime rtime = starttime.AddDays(i * 7);
                        int rweek = rtime.GetWeekOfYear();
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(7));
                        item.DateStr = $"{rweek}周";
                    }
                    else if (model.DataType == 4)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, model.StartTime.Month, 1);
                        DateTime rtime = starttime.AddMonths(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddMonths(1));
                        item.DateStr = $"{rtime.Month}月";
                    }
                    else if (model.DataType == 5)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, 1, 1);
                        DateTime rtime = starttime.AddYears(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddYears(1));
                        item.DateStr = $"{rtime.Year}年";
                    }

                    //本期
                    var _datalistbq = datalist.FindAll(t => t.SnowId >= minbq && t.SnowId < maxbq);
                    if (_datalistbq.IsZxxAny())
                    {
                        double total = 0;
                        foreach (var _data in _datalistbq)
                        {
                            total += _data.ExpandObjects.Sum(t => t.TotalValue.ToZxxDouble());
                        }
                        item.BenQi = total.ToString("f1");
                        chartbq.ChartY.Add(item.BenQi);
                    }
                    else
                    {
                        chartbq.ChartY.Add("-");
                    }
                    if (i > 0)
                    {
                        if (hbDateValues.ContainsKey(i - 1))
                        {
                            var yes = hbDateValues[i - 1];
                            item.ZengJianZhi = (item.BenQi.ToZxxDecimal() - yes.BenQi.ToZxxDecimal()).ToString("f1");
                            if (yes.BenQi.ToZxxDecimal() > 0)
                            {
                                item.ZengJianLv = (item.ZengJianZhi.ToZxxDecimal() * 100 / yes.BenQi.ToZxxDecimal()).ToString("f2");
                                charzjl.ChartY.Add(item.ZengJianLv);
                            }
                            else
                            {
                                charzjl.ChartY.Add("-");
                            }
                        }
                        else
                        {
                            charzjl.ChartY.Add("-");
                        }
                    }
                    else
                    {
                        charzjl.ChartY.Add("-");
                    }
                    energy.table.Add(item);
                    hbDateValues.TryAdd(i, item);
                    energy.chart.ChartX.Add(item.DateStr);
                }
            }

            return energy;
        }

        /// <summary>
        /// 环比分析(日)-导出
        /// </summary>
        /// <param name="model">环比分析</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public MetaData GetEnergyHbAnalysisExcel(EnergyAnalysisSelect model)
        {
            TotalCount = 1;
            MetaData data = new MetaData
            {
                Status = false,
                Message = "环比分析数据导出失败"
            };
            if (model.DataType == 1 && model.EndTime != model.StartTime)
            {
                Message = "选中时类型时，开始时间和结束时间必须相同";
                return data;
            }
            if (model.ParamCodes.IsZxxAny()) model.ParamCodes.RemoveAll(t => t.Contains("_"));
            int timemax = 0;
            //本期能耗表查询
            List<ReportAnalysisInfo> datalist = new List<ReportAnalysisInfo>();
            if (model.DataType >= 1 && model.DataType <= 2)
            {
                //本期日能耗表查询
                datalist.AddRange(GetDayDataList(model));
                timemax = model.StartTime.DiffDays(model.EndTime);
            }
            else if (model.DataType == 3)
            {
                //本期周能耗表查询
                datalist.AddRange(GetWeekDataList(model));
                timemax = model.StartTime.DiffWeeks(model.EndTime);
            }
            else if (model.DataType >= 4 && model.DataType <= 5)
            {
                //本期月能耗表查询
                datalist.AddRange(GetMonthDataList(model));
                timemax = model.StartTime.DiffMonths(model.EndTime);
                if (model.DataType == 5) timemax = model.StartTime.DiffYears(model.EndTime);
            }
            if (datalist.Count == 0) return data;

            List<EnergyAnalysisHbExcel> energys = new List<EnergyAnalysisHbExcel>();
            Dictionary<int, EnergyAnalysisHbExcel> hbDateValues = new Dictionary<int, EnergyAnalysisHbExcel>();
            if (model.DataType == 1)
            {
                var reporttype = typeof(Expand_EventReportWeek);
                var fields = typeof(Expand_EventReportWeek).GetProperties();
                for (int i = 0; i < 24; i++)
                {
                    var item = new EnergyAnalysisHbExcel
                    {
                        RowNo = i + 1,
                        DateStr = $"{i}时",
                    };
                    string fieldname = $"HourValue{i}";
                    var field = fields.FirstOrDefault(t => t.Name == fieldname);
                    if (field != null)
                    {
                        //本期
                        double totalbq = 0;
                        foreach (var _data in datalist)
                        {
                            totalbq += field.GetValue(_data.ExpandObjects[0]).ToZxxDouble();
                        }
                        item.BenQi = totalbq.ToString("f1");
                    }
                    if (i > 0)
                    {
                        if (hbDateValues.ContainsKey(i - 1))
                        {
                            var yes = hbDateValues[i - 1];
                            item.ZengJianZhi = (item.BenQi.ToZxxDecimal() - yes.BenQi.ToZxxDecimal()).ToString("f1");
                            if (yes.BenQi.ToZxxDecimal() > 0)
                            {
                                item.ZengJianLv = (item.ZengJianZhi.ToZxxDecimal() * 100 / yes.BenQi.ToZxxDecimal()).ToString("f2");
                            }
                        }
                    }
                    energys.Add(item);
                    hbDateValues.TryAdd(i, item);
                }
            }
            else
            {
                long minbq = 0, maxbq = 0;
                for (int i = 0; i <= timemax; i++)
                {
                    EnergyAnalysisHbExcel item = new EnergyAnalysisHbExcel
                    {
                        RowNo = i + 1
                    };
                    if (model.DataType == 2)
                    {
                        DateTime rtime = model.StartTime.AddDays(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(1));
                        item.DateStr = $"{rtime.Day}日";
                    }
                    else if (model.DataType == 3)
                    {
                        DateTime starttime = model.StartTime.GetFirstDayOfWeek();
                        DateTime rtime = starttime.AddDays(i * 7);
                        int rweek = rtime.GetWeekOfYear();
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddDays(7));
                        item.DateStr = $"{rweek}周";
                    }
                    else if (model.DataType == 4)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, model.StartTime.Month, 1);
                        DateTime rtime = starttime.AddMonths(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddMonths(1));
                        item.DateStr = $"{rtime.Month}月";
                    }
                    else if (model.DataType == 5)
                    {
                        DateTime starttime = new DateTime(model.StartTime.Year, 1, 1);
                        DateTime rtime = starttime.AddYears(i);
                        minbq = SnowModel.Instance.GetId(rtime);
                        maxbq = SnowModel.Instance.GetId(rtime.AddYears(1));
                        item.DateStr = $"{rtime.Year}年";
                    }

                    //本期
                    var _datalistbq = datalist.FindAll(t => t.SnowId >= minbq && t.SnowId < maxbq);
                    if (_datalistbq.IsZxxAny())
                    {
                        double total = 0;
                        foreach (var _data in _datalistbq)
                        {
                            total += _data.ExpandObjects.Sum(t => t.TotalValue.ToZxxDouble());
                        }
                        item.BenQi = total.ToString("f1");
                    }
                    if (i > 0)
                    {
                        if (hbDateValues.ContainsKey(i - 1))
                        {
                            var yes = hbDateValues[i - 1];
                            item.ZengJianZhi = (item.BenQi.ToZxxDecimal() - yes.BenQi.ToZxxDecimal()).ToString("f1");
                            if (yes.BenQi.ToZxxDecimal() > 0)
                            {
                                item.ZengJianLv = (item.ZengJianZhi.ToZxxDecimal() * 100 / yes.BenQi.ToZxxDecimal()).ToString("f2");
                            }
                        }
                    }
                    energys.Add(item);
                    hbDateValues.TryAdd(i, item);
                }
            }

            if (energys.Count == 0)
            {
                data.Message = "环比分析无数据可导出";
                return data;
            }
            string fileName = $"环比分析数据-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
            string filepath = Path.Combine(OperatorCommon.NetLocalfile, "export", fileName);
            string serverparh = Path.Combine(OperatorCommon.NetYingShefile, "export", fileName);
            filepath.EnsureDirectory(true);

            if (energys.ExportExcelCom(filepath))
            {
                data.Status = true;
                data.Message = "环比分析数据导出成功";
                data.Result = serverparh;
            }
            return data;
        }

        #endregion

    }
}
