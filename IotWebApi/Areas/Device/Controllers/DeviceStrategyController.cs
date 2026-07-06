using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text;
using IotModel;
using IotWebApi.Areas.Device.Models;
using IotWebApi.Areas.Event.Data;
using IotWebApi.Areas.Event.Models;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 设备策略表
    /// </summary>
    [ApiController]
    [ControllSort("7-7")]
    public class DeviceStrategyController : ControllerBaseApi
    {
        /// <summary> 
        /// 批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string SaveBatch(List<DeviceStrategy> list)
        {
            Status = false;
            Message = "设备策略表信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<DeviceStrategy> insertlist = new List<DeviceStrategy>();
                List<DeviceStrategy> updatelist = new List<DeviceStrategy>();
                DateTime time = DateTime.Now;
                foreach (var item in list)
                {
                    item.UpdateId = optmdl.UserID;
                    item.UpdateTime = time.ToDateTimeString();
                    item.UpdateName = optmdl.UserName;
                    if (item.DeviceId == 0)
                    {
                        item.CreateId = optmdl.UserID;
                        item.CreateTime = time.ToDateTimeString();
                        item.CreateName = optmdl.UserName;
                        insertlist.Add(item);
                    }
                    else
                    {
                        updatelist.Add(item);
                    }
                }
                Status = DeviceStrategyDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) DeviceStrategyDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) DeviceStrategyDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    Message = "设备策略表信息保存成功。";
                }
            }
            return Message;
        }

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="_DeviceId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string DeleteByPk(int _DeviceId)
        {
            Status = false;
            Message = "设备策略表删除失败。";
            Status = DeviceStrategyDAO.Instance.DeleteBy(t => t.DeviceId == _DeviceId);
            if (Status)
            {
                Message = "设备策略表信息删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name="_DeviceId">主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public DeviceStrategy GetInfoByPk(int _DeviceId)
        {
            var entity = DeviceStrategyDAO.Instance.GetOneBy(t => t.DeviceId == _DeviceId);
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
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceStrategy> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = DeviceStrategyDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 导出策略详情Excel
        /// </summary>
        /// <param name="model">通用参数模型（支持条件筛选）</param>
        /// <returns>导出结果，Result 为文件URL</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public MetaData ExportStrategyDetail(ActionPara model)
        {
            TotalCount = 1;
            MetaData data = new MetaData
            {
                Status = false,
                Message = "策略详情导出失败"
            };

            var devices = SysCommonDAO<DeviceInfo>.Instance.GetListBy(model);
            if (!devices.IsZxxAny())
            {
                data.Message = "无策略数据可导出";
                return data;
            }

            // 关联设备名称
            var deviceIds = devices.Select(d => d.DeviceId).Distinct().ToList();
            var list = DeviceStrategyDAO.Instance.GetListBy(t => deviceIds.Contains(t.DeviceId));
            TotalCount = list.Count;
            var devDict = devices.ToDictionary(d => d.DeviceId, d => d);

            // 组装导出数据
            var reportTable = new DataReport();
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "RowNo", ColumnCn = "序号" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "DeviceName", ColumnCn = "设备名称" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "TypeName", ColumnCn = "类型名称" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "BuildName", ColumnCn = "建筑名称" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "DeptName", ColumnCn = "部门名称" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "GeneralDesc", ColumnCn = "常规策略参数" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "TimingDesc", ColumnCn = "时间策略" });
            reportTable.ReportColumns.Add(new ReportColumn { ColumnEn = "TaskDesc", ColumnCn = "定时任务" });

            var depts = SysCommonDAO<DeptInfo>.Instance.GetList();
            var builds = SysCommonDAO<BuildInfo>.Instance.GetList();
            var types = SysCommonDAO<DeviceType>.Instance.GetList();

            int rowNo = 1;
            foreach (var strategy in list)
            {
                string deviceName = "";
                string typeCode = strategy.DeviceTypeCode ?? "";
                if (devDict.TryGetValue(strategy.DeviceId, out var dev))
                {
                    deviceName = dev.DeviceName ?? "";
                }
                var type = types.FirstOrDefault(t => t.TypeCode == typeCode);
                var build = builds.FirstOrDefault(b => b.BuildId == strategy.BuildId);
                var dept = depts.FirstOrDefault(d => d.DeptId == strategy.DeptId);
                var rowData = new Dictionary<string, object>
                {
                    ["RowNo"] = rowNo++,
                    ["DeviceName"] = deviceName,
                    ["TypeName"] = type?.TypeName ?? "",
                    ["BuildName"] = build?.BuildName ?? "",
                    ["DeptName"] = dept?.DeptName ?? "",
                    ["GeneralDesc"] = InterpretGeneralJson(strategy.GeneralJson, typeCode),
                    ["TimingDesc"] = InterpretTimingJson(strategy.TimingJson, typeCode),
                    ["TaskDesc"] = InterpretTaskJson(strategy.TaskJson, typeCode),
                };
                reportTable.ReportDatas.Add(rowData);
            }

            // 导出 Excel
            string fileName = $"设备策略详情-{DateTime.Now:yyyyMMddHHmmssfff}.xlsx";
            string filepath = Path.Combine(OperatorCommon.NetLocalfile, "export", fileName);
            string serverparh = Path.Combine(OperatorCommon.NetYingShefile, "export", fileName);
            filepath.EnsureDirectory(true);

            if (reportTable.ExportExcelCom(filepath))
            {
                data.Status = true;
                data.Message = $"设备策略详情导出成功，共{list.Count}条";
                data.Result = serverparh;
            }
            return data;
        }

        /// <summary>
        /// 根据条件查询设备信息及策略内容(策略内容保留分块字段)
        /// 必传：单位ID(UnitId)、设备类型编码(TypeCode)。
        /// 常规设备条件(设备名称、建筑、部门)参与数据库过滤；
        /// 策略内容(工作模式)在内存中过滤后分页。
        /// </summary>
        /// <param name="model">查询参数</param>
        /// <returns>设备策略视图列表</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceStrategyView> GetStrategyDeviceList(StrategyQueryPara model)
        {
            List<DeviceStrategyView> result = new List<DeviceStrategyView>();
            var optmdl = Request.GetToken();
            // 必传参数校验
            if (model == null || model.TypeCode.IsZxxNullOrEmpty())
            {
                Message = "参数不完整";
                TotalCount = 0;
                return result;
            }

            // Step1: 构造 ActionPara，把必传条件(单位、类型)注入 sconlist，常规条件一并下推到数据库
            var sconlist = new List<SelectCondition>
            {
                new SelectCondition { ParamName = "UnitId", ParamType = "=", ParamValue = $"{optmdl.UnitId}", ParamSort = 0 },
                new SelectCondition { ParamName = "DeviceTypeCode", ParamType = "like", ParamValue = model.TypeCode, ParamSort = 0 },
            };
            if (!model.DevName.IsZxxNullOrEmpty())
            {
                sconlist.Add(new SelectCondition { ParamName = "DeviceName", ParamType = "like", ParamValue = model.DevName, ParamSort = 0 });
            }
            if (model.DeviceState >= 0)
            {
                sconlist.Add(new SelectCondition { ParamName = "DeviceState", ParamType = "=", ParamValue = $"{model.DeviceState}", ParamSort = 0 });
            }

            // 建筑ID/部门ID 支持查询包含子集(按 FullCode 树形展开，注入 in 条件)
            if (model.BuildId > 0)
            {
                var buildIds = GetChildIds(SysCommonDAO<BuildInfo>.Instance.GetList(), b => b.BuildId, b => b.FullCode, model.BuildId);
                if (buildIds.IsZxxAny())
                {
                    sconlist.Add(new SelectCondition { ParamName = "BuildId", ParamType = "in", ParamValue = string.Join(",", buildIds), ParamSort = 0 });
                }
                else
                {
                    sconlist.Add(new SelectCondition { ParamName = "BuildId", ParamType = "=", ParamValue = $"{model.BuildId}", ParamSort = 0 });
                }
            }
            if (model.DeptId > 0)
            {
                var deptIds = GetChildIds(SysCommonDAO<DeptInfo>.Instance.GetList(), d => d.DeptId, d => d.FullCode, model.DeptId);
                if (deptIds.IsZxxAny())
                {
                    sconlist.Add(new SelectCondition { ParamName = "DeptId", ParamType = "in", ParamValue = string.Join(",", deptIds), ParamSort = 0 });
                }
                else
                {
                    sconlist.Add(new SelectCondition { ParamName = "DeptId", ParamType = "=", ParamValue = $"{model.DeptId}", ParamSort = 0 });
                }
            }

            // 不分页：先全量查出单位+类型+常规条件下的设备(单位+类型范围内数据量可控)
            var ap = new ActionPara()
            {
                page = 1,
                pagesize = 0,
                sconlist = sconlist,
            };
            var devices = SysCommonDAO<DeviceInfo>.Instance.GetListBy(ap);
            if (!devices.IsZxxAny())
            {
                TotalCount = 0;
                return result;
            }

            // Step2: 批量查询策略
            var deviceIds = devices.Select(d => d.DeviceId).Distinct().ToList();
            var strategyList = DeviceStrategyDAO.Instance.GetListBy(t => deviceIds.Contains(t.DeviceId));
            var strategyDict = strategyList.ToDictionary(s => s.DeviceId, s => s);

            // Step3: 关联名称(均带 [EntityCache]，走 Redis 全表缓存)
            var depts = SysCommonDAO<DeptInfo>.Instance.GetList();
            var builds = SysCommonDAO<BuildInfo>.Instance.GetList();
            var types = SysCommonDAO<DeviceType>.Instance.GetList();

            // Step4: 组装视图 + 解析策略(保留分块字段)
            foreach (var dev in devices)
            {
                var view = new DeviceStrategyView
                {
                    DeviceId = dev.DeviceId,
                    DeviceName = dev.DeviceName ?? "",
                    DeviceTypeCode = dev.DeviceTypeCode ?? "",
                    BuildId = dev.BuildId,
                    DeptId = dev.DeptId,
                    UnitId = dev.UnitId,
                    DeviceState = dev.DeviceState,
                    DeviceAlarm = dev.DeviceAlarm,
                    DeviceSwitch = dev.DeviceSwitch,
                };

                // 关联名称
                var type = types.FirstOrDefault(t => t.TypeCode == dev.DeviceTypeCode);
                view.DeviceTypeName = type?.TypeName ?? "";
                var build = builds.FirstOrDefault(b => b.BuildId == dev.BuildId);
                view.BuildName = build?.FullName.BeautifyFullName() ?? "";
                var dept = depts.FirstOrDefault(d => d.DeptId == dev.DeptId);
                view.DeptName = dept?.FullName.BeautifyFullName() ?? "";

                // 策略内容(左连接语义：未配置策略的设备策略字段为空)
                if (strategyDict.TryGetValue(dev.DeviceId, out var strategy))
                {
                    view.HasStrategy = 1;
                    view.GeneralJson = strategy.GeneralJson ?? "";
                    view.TimingJson = strategy.TimingJson ?? "";
                    view.TaskJson = strategy.TaskJson ?? "";
                    string typeCode = strategy.DeviceTypeCode ?? dev.DeviceTypeCode ?? "";

                    // 关键拆分：工作模式单独提取(可查询、可显示)
                    view.WorkModelCode = ExtractWorkModelCode(strategy.GeneralJson);
                    view.WorkModel = InterpretWorkModel(view.WorkModelCode, typeCode);

                    // 其他关键枚举字段单独提取(可查询、可显示)
                    view.AirSeason = ExtractMappedField(strategy.GeneralJson, "AirSeason", AirSeasonMap);

                    // 时间策略/定时任务是否存在
                    view.HasTiming = HasStrategyBlock(strategy.TimingJson);
                    view.HasTask = HasStrategyBlock(strategy.TaskJson);

                    // 整体描述(直接显示)
                    view.GeneralDesc = InterpretGeneralJson(strategy.GeneralJson, typeCode);
                    view.TimingDesc = InterpretTimingJson(strategy.TimingJson, typeCode);
                    view.TaskDesc = InterpretTaskJson(strategy.TaskJson, typeCode);
                }
                else
                {
                    view.HasStrategy = 0;
                    view.HasTiming = 0;
                    view.HasTask = 0;
                    view.WorkModel = "无";
                    view.WorkModelCode = "";
                    view.GeneralDesc = "无";
                    view.TimingDesc = "无";
                    view.TaskDesc = "无";
                }

                result.Add(view);
            }

            // Step5: 内存过滤 - 策略内容条件(统一按中文匹配)
            result = FilterByStrategy(result, model);

            // Step6: 内存分页(先记录总数，再取当前页)
            TotalCount = result.Count;
            int pageSize = model.pagesize > 0 ? model.pagesize : 20;
            int pageIndex = model.page > 0 ? model.page : 1;
            result = result.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

            return result;
        }

        /// <summary>
        /// 按策略内容条件过滤(统一按中文匹配，字段值在 JSON 内部 DB 无法精确 where)。
        /// 多值条件用逗号分隔，匹配任一即为命中。
        /// </summary>
        private static List<DeviceStrategyView> FilterByStrategy(List<DeviceStrategyView> source, StrategyQueryPara model)
        {
            var result = source;

            // 通用模糊查询关键字(命中 常规/时间/定时 三个描述中的任一)
            if (!model.Keyword.IsZxxNullOrEmpty())
            {
                var kw = model.Keyword.Trim();
                result = result.FindAll(v =>
                    (v.GeneralDesc?.Contains(kw) == true) ||
                    (v.TimingDesc?.Contains(kw) == true) ||
                    (v.TaskDesc?.Contains(kw) == true));
            }

            // 工作模式(中文，多选逗号分隔，匹配任一)
            if (!model.WorkModel.IsZxxNullOrEmpty())
            {
                var keys = model.WorkModel.Split('、', ',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(c => c.Trim()).ToList();
                result = result.FindAll(v =>
                    !v.WorkModel.IsZxxNullOrEmpty() &&
                    v.WorkModel.Split('、', ',', StringSplitOptions.RemoveEmptyEntries)
                               .Any(x => keys.Contains(x.Trim())));
            }

            // 季节选择(中文精确匹配)
            if (!model.AirSeason.IsZxxNullOrEmpty())
            {
                result = result.FindAll(v => v.AirSeason == model.AirSeason.Trim());
            }

            // 是否有时间策略
            if (model.HasTiming == 1) result = result.FindAll(v => v.HasTiming == 1);
            else if (model.HasTiming == 0) result = result.FindAll(v => v.HasTiming == 0);

            // 是否有定时任务
            if (model.HasTask == 1) result = result.FindAll(v => v.HasTask == 1);
            else if (model.HasTask == 0) result = result.FindAll(v => v.HasTask == 0);

            // 是否已配置策略
            if (model.HasStrategy == 0) result = result.FindAll(v => v.HasStrategy == 0);
            else if (model.HasStrategy == 1) result = result.FindAll(v => v.HasStrategy == 1);

            return result;
        }

        /// <summary>
        /// 按树形 FullCode 展开指定节点及其所有子节点的 ID 集合(含自身)。
        /// FullCode 形如 "|1|12|123|"，子节点的 FullCode 必然包含父节点的 "|{id}|" 段。
        /// 与 DeviceInfoController.GetChildBuildIds 同款逻辑，抽成通用方法供建筑/部门复用。
        /// </summary>
        /// <typeparam name="T">树形实体类型</typeparam>
        /// <param name="allList">全量列表</param>
        /// <param name="idSelector">主键取值</param>
        /// <param name="fullCodeSelector">FullCode 取值</param>
        /// <param name="parentId">要展开的节点 ID</param>
        /// <returns>包含自身及所有子节点的 ID 列表；无匹配时返回空列表</returns>
        private static List<int> GetChildIds<T>(List<T> allList, Func<T, int> idSelector, Func<T, string> fullCodeSelector, int parentId)
        {
            var result = new List<int>();
            if (allList == null || allList.Count == 0 || parentId <= 0) return result;

            // 先找到自身，拿到它的 FullCode 段
            var self = allList.Find(x => idSelector(x) == parentId);
            if (self == null) return result;

            string segment = $"|{parentId}|";
            foreach (var item in allList)
            {
                var fc = fullCodeSelector(item) ?? "";
                // 自身或其子孙：FullCode 含 "|{parentId}|" 段
                if (idSelector(item) == parentId || fc.Contains(segment))
                {
                    result.Add(idSelector(item));
                }
            }
            return result;
        }

        #region 策略JSON解读（参考 AirControlController 控制指令解读）

        // 工作模式映射表(普通空调)：0:调温 1:人感 2:温度 3:时间 4:手动 5:计量 7:断电 8:机房省电 9:临时
        private static readonly Dictionary<int, string> WorkModelAirMap = new Dictionary<int, string>
        {
            [0] = "调温",
            [1] = "人感",
            [2] = "温度",
            [3] = "时间",
            [4] = "手动",
            [5] = "计量",
            [7] = "断电",
            [8] = "机房省电",
            [9] = "临时"
        };

        // 工作模式映射表(VRF)：0:调温 1:温度 2:时间 3:定时
        private static readonly Dictionary<int, string> WorkModelVrfMap = new Dictionary<int, string>
        {
            [0] = "调温",
            [1] = "温度",
            [2] = "时间",
            [3] = "定时"
        };

        /// <summary>
        /// 从常规策略 JSON 中提取工作模式原始编码串(如 "0,1")。
        /// 供 DeviceStrategyView.WorkModelCode 使用，便于查询和显示。
        /// </summary>
        private static string ExtractWorkModelCode(string generalJson)
        {
            if (generalJson.IsZxxNullOrEmpty()) return "";
            try
            {
                var obj = JObject.Parse(generalJson);
                var wmToken = obj["WorkModel"];
                if (wmToken != null && !wmToken.ToString().IsZxxNullOrEmpty())
                    return wmToken.ToString().Trim();
            }
            catch
            {
                // 解析失败返回空
            }
            return "";
        }

        /// <summary>
        /// 将工作模式编码串(如 "0,1")转换为中文描述(如 "调温、人感")。
        /// </summary>
        /// <param name="workModelCode">工作模式编码串</param>
        /// <param name="typeCode">设备类型编码(VRF 用 VRF 映射)</param>
        /// <returns>中文描述，无则返回空串</returns>
        private static string InterpretWorkModel(string workModelCode, string typeCode)
        {
            if (workModelCode.IsZxxNullOrEmpty()) return "";
            var mapping = (typeCode?.Contains("vrf") == true) ? WorkModelVrfMap : WorkModelAirMap;
            var parts = workModelCode.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var descs = new List<string>();
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out int code) && mapping.TryGetValue(code, out var desc))
                    descs.Add(desc);
                else
                    descs.Add(p.Trim());
            }
            return string.Join("、", descs);
        }

        /// <summary>
        /// 季节选择映射：0=夏季 1=冬季
        /// </summary>
        private static readonly Dictionary<int, string> AirSeasonMap = new Dictionary<int, string>
        {
            [0] = "夏季", [1] = "冬季"
        };

        /// <summary>
        /// 从常规策略 JSON 中提取数字字段并转中文(用于查询和独立显示)。
        /// </summary>
        /// <param name="generalJson">常规策略 JSON</param>
        /// <param name="fieldName">JSON 字段名(如 AirSeason)</param>
        /// <param name="mapping">编码→中文映射表</param>
        /// <returns>中文描述，无则返回空串</returns>
        private static string ExtractMappedField(string generalJson, string fieldName, Dictionary<int, string> mapping)
        {
            if (generalJson.IsZxxNullOrEmpty() || mapping == null) return "";
            try
            {
                var obj = JObject.Parse(generalJson);
                if (obj.ContainsKey(fieldName) && obj[fieldName] != null)
                {
                    int val = obj[fieldName].Value<int>();
                    if (mapping.TryGetValue(val, out var desc))
                        return desc;
                }
            }
            catch
            {
                // 解析失败返回空
            }
            return "";
        }

        /// <summary>
        /// 判断策略 JSON(数组型)是否有有效数据。
        /// </summary>
        private static int HasStrategyBlock(string json)
        {
            if (json.IsZxxNullOrEmpty()) return 0;
            try
            {
                var arr = JArray.Parse(json);
                return arr.Count > 0 ? 1 : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 解读常规策略 JSON（GeneralJson）
        /// 用 JObject 通用解析，兼容 AirGeneral/NetAirStrategy/VRFV4Strategy/NetFJPGGeneral 等不同结构。
        /// </summary>
        private static string InterpretGeneralJson(string json, string typeCode)
        {
            if (json.IsZxxNullOrEmpty()) return "无";
            try
            {
                var obj = JObject.Parse(json);
                var sb = new StringBuilder();

                // 工作模式（逗号分隔的数字编码，需逐个转中文）
                // 普通空调：0:调温 1:人感 2:温度 3:时间 4:手动 5:计量 7:断电 8:机房省电 9:临时
                // VRF：    0:调温 1:温度 2:时间 3:定时
                var wmToken = obj["WorkModel"];
                if (wmToken != null && !wmToken.ToString().IsZxxNullOrEmpty())
                {
                    // VRF 类型用 VRF 映射，其他用普通空调映射
                    var wmDesc = InterpretWorkModel(wmToken.ToString(), typeCode);
                    if (!wmDesc.IsZxxNullOrEmpty())
                        sb.Append($" 工作模式:{wmDesc} ");
                }

                // 温度参数（各类型字段名一致）
                AppendTempField(sb, obj, "RefrigStartTemp", "制冷开启温度");
                AppendTempField(sb, obj, "RefrigOpenTemp", "制冷开机温度");
                AppendTempField(sb, obj, "RefrigCloseTemp", "制冷关机温度");
                AppendTempField(sb, obj, "HotStartTemp", "制热开启温度");
                AppendTempField(sb, obj, "HotOpenTemp", "制热开机温度");
                AppendTempField(sb, obj, "HotCloseTemp", "制热关机温度");
                AppendTempField(sb, obj, "SummerTemp", "夏季判断温度");
                AppendTempField(sb, obj, "WinterTemp", "冬季判断温度");
                AppendTempField(sb, obj, "SummerOpenTemp", "夏季开机温度");
                AppendTempField(sb, obj, "WinterOpenTemp", "冬季开机温度");

                // 温度模式使能（0=关闭 1=开启）
                AppendMappedField(sb, obj, "OpenTempEnable", "温度模式使能", new Dictionary<int, string> { [0] = "关闭", [1] = "开启" });

                // 温度使能运行方式（0=按天 1=按时段）
                AppendMappedField(sb, obj, "OperatModeTemp", "温度使能运行方式", new Dictionary<int, string> { [0] = "按天", [1] = "按时段" });

                // 温度模式开关机控制（1/2/3）
                AppendMappedField(sb, obj, "OpenCloseTemp", "温度模式开关机控制", new Dictionary<int, string>
                {
                    [1] = "制冷/制热开机控制",
                    [2] = "制冷/制热关机控制",
                    [3] = "制冷/制热开关机控制"
                });

                // 进入时间段自动开机（VRF，0=关闭 1=开启）
                AppendMappedField(sb, obj, "AutoOpen", "进入时间段自动开机", new Dictionary<int, string> { [0] = "关闭", [1] = "开启" });

                // 季节选择（0=夏季 1=冬季）
                AppendMappedField(sb, obj, "AirSeason", "季节选择", new Dictionary<int, string> { [0] = "夏季", [1] = "冬季" });

                return sb.Length > 0 ? sb.ToString().Trim() : json;
            }
            catch
            {
                return json; // 解析失败返回原始JSON
            }
        }

        /// <summary>
        /// 解读时间策略 JSON（TimingJson，数组）
        /// 兼容 NetAirTimeInfo（DayType 1~7）/ VRFV4TimeInfo（weeks 位掩码）/ NetFJPGSeasonTimeInfo。
        /// </summary>
        private static string InterpretTimingJson(string json, string typeCode)
        {
            if (json.IsZxxNullOrEmpty()) return "无";
            try
            {
                var arr = JArray.Parse(json);
                if (arr.Count == 0) return "无时间策略";

                var weekNames = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
                var dayDict = new Dictionary<int, string> { [1] = "周一", [2] = "周二", [3] = "周三", [4] = "周四", [5] = "周五", [6] = "周六", [7] = "周日" };
                var lines = new List<string>();

                foreach (var item in arr)
                {
                    var sb = new StringBuilder();
                    int idx = arr.IndexOf(item) + 1;

                    // 星期：优先 DayType（1~7），其次 weeks 位掩码
                    string weekDesc = "";
                    if (item["DayType"] != null)
                    {
                        int day = (int)item["DayType"];
                        weekDesc = dayDict.TryGetValue(day, out var dn) ? dn : $"Day{day}";
                    }
                    else if (item["weeks"] != null)
                    {
                        int weeks = (int)item["weeks"];
                        var days = new List<string>();
                        for (int d = 0; d < 7; d++)
                        {
                            if ((weeks & (1 << d)) != 0) days.Add(weekNames[d]);
                        }
                        weekDesc = days.Count == 7 ? "每天" : (days.Count > 0 ? string.Join("、", days) : "未设置");
                    }

                    // 时段编号
                    if (item["TimeNum"] != null)
                    {
                        int tn = (int)item["TimeNum"];
                        sb.Append($"时段{tn}");
                    }

                    // 启用/人感
                    if (item["enable"] != null)
                    {
                        string enDesc = (int)item["enable"] == 1 ? "启用" : "未启用";
                        sb.Append($" {enDesc} ");
                    }
                    else if (item["IsHuman"] != null)
                    {
                        string huDesc = (int)item["IsHuman"] == 1 ? "启用" : "不启用";
                        sb.Append($" 人感:{huDesc} ");
                    }

                    // 时间范围
                    int sh = item["StartHour"]?.Value<int>() ?? item["startHour"]?.Value<int>() ?? 0;
                    int sm = item["StartMinute"]?.Value<int>() ?? item["startMinute"]?.Value<int>() ?? 0;
                    int eh = item["EndHour"]?.Value<int>() ?? item["endHour"]?.Value<int>() ?? 0;
                    int em = item["EndMinute"]?.Value<int>() ?? item["endMinute"]?.Value<int>() ?? 0;
                    sb.Append($" {sh:00}:{sm:00}-{eh:00}:{em:00}");

                    if (!weekDesc.IsZxxNullOrEmpty()) sb.Append($" [{weekDesc}]");

                    lines.Add(sb.ToString().Trim());
                }
                return string.Join("\n", lines);
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// 解读定时任务 JSON（TaskJson，数组）
        /// 兼容 VRFV4TaskInfo / NetFJPGTaskInfo / ClockSetting。
        /// </summary>
        private static string InterpretTaskJson(string json, string typeCode)
        {
            if (json.IsZxxNullOrEmpty()) return "无";
            try
            {
                var arr = JArray.Parse(json);
                if (arr.Count == 0) return "无定时任务";

                var weekNames = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
                var lines = new List<string>();

                foreach (var item in arr)
                {
                    var sb = new StringBuilder();
                    int idx = arr.IndexOf(item) + 1;

                    // 只处理启用的
                    if (item["enable"] != null && (int)item["enable"] != 1) continue;
                    if (item["ControlType"] != null)
                    {
                        int ct = (int)item["ControlType"];
                        if (ct < 1 || ct > 3) continue;
                    }

                    sb.Append($"任务{idx}: ");

                    // 星期
                    if (item["weeks"] != null)
                    {
                        int weeks = (int)item["weeks"];
                        var days = new List<string>();
                        for (int d = 0; d < 7; d++)
                        {
                            if ((weeks & (1 << d)) != 0) days.Add(weekNames[d]);
                        }
                        sb.Append(days.Count > 0 ? $"生效:{string.Join("、", days)} " : "每天 ");
                    }
                    else if (item["ClockWeek"] != null)
                    {
                        string cw = item["ClockWeek"].ToString();
                        var days = new List<string>();
                        for (int i = 0; i < Math.Min(cw.Length, 7); i++)
                        {
                            if (cw[i] == '1' && i < weekNames.Length) days.Add(weekNames[i]);
                        }
                        sb.Append(days.Count > 0 ? $"生效:{string.Join("、", days)} " : "未设置 ");
                    }

                    // 开关
                    if (item["airSwitch"] != null)
                    {
                        int sw = (int)item["airSwitch"];
                        sb.Append($"开关:{(sw == 1 ? "开" : sw == 0 ? "关" : "未设置")} ");
                    }

                    // 模式（位编码：0自动/1制热/2制冷/4送风/8除湿）
                    if (item["airModel"] != null)
                    {
                        int mode = (int)item["airModel"];
                        string modeDesc = mode switch
                        {
                            0 => "自动",
                            1 => "制热",
                            2 => "制冷",
                            4 => "送风",
                            8 => "除湿",
                            _ => "未设置"
                        };
                        sb.Append($"模式:{modeDesc} ");
                    }

                    // 温度
                    if (item["airTemp"] != null)
                    {
                        int t = (int)item["airTemp"];
                        if (t != 255) sb.Append($"温度:{t}℃ ");
                    }

                    // 风速
                    if (item["airSpeed"] != null)
                    {
                        int sp = (int)item["airSpeed"];
                        string spDesc = sp switch { 0 => "自动", 1 => "低风", 2 => "中风", 3 => "高风", _ => "未设置" };
                        sb.Append($"风速:{spDesc} ");
                    }

                    // 时间（VRF用 startHour/startMin，ClockSetting 用 ClockOpenHour/Minute）
                    if (item["startHour"] != null)
                    {
                        int sh = (int)item["startHour"];
                        int sm = item["startMin"]?.Value<int>() ?? 0;
                        sb.Append($"开始:{sh:00}:{sm:00} ");
                    }
                    if (item["ClockOpenHour"] != null)
                    {
                        int coh = (int)item["ClockOpenHour"];
                        int com = item["ClockOpenMinute"]?.Value<int>() ?? 0;
                        sb.Append($"开启:{coh:00}:{com:00} ");
                    }
                    if (item["ClockCloseHour"] != null)
                    {
                        int cch = (int)item["ClockCloseHour"];
                        int ccm = item["ClockCloseMinute"]?.Value<int>() ?? 0;
                        sb.Append($"关闭:{cch:00}:{ccm:00} ");
                    }

                    lines.Add(sb.ToString().Trim());
                }
                return lines.Count > 0 ? string.Join("\n", lines) : "无启用的定时任务";
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// 追加数值字段（带℃单位）。仅当 JSON 中存在该字段且值为有效温度时才显示。
        /// </summary>
        private static void AppendTempField(StringBuilder sb, JObject obj, string fieldName, string label)
        {
            if (obj.ContainsKey(fieldName) && obj[fieldName] != null)
            {
                var val = obj[fieldName].Value<double>();
                if (val > 0 && val < 255)
                    sb.Append($" {label}:{val}℃ ");
            }
        }

        /// <summary>
        /// 追加映射字段（数字→中文）。仅当 JSON 中存在该字段时才显示（0 也是有效值，如"夏季""关闭"）。
        /// </summary>
        private static void AppendMappedField(StringBuilder sb, JObject obj, string fieldName, string label, Dictionary<int, string> mapping)
        {
            if (obj.ContainsKey(fieldName) && obj[fieldName] != null)
            {
                int val = obj[fieldName].Value<int>();
                if (mapping.TryGetValue(val, out var desc))
                    sb.Append($" {label}:{desc} ");
            }
        }

        #endregion

    }
}