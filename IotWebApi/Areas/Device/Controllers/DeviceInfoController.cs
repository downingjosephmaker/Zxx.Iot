using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using IotWebApi.Areas.Device.Models;
using IotWebApi.Services;
using Magicodes.ExporterAndImporter.Excel;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using OfficeOpenXml;
using System.Data;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 设备基本信息
    /// </summary>
    [ApiController]
    [ControllSort("7-5")]
    public class DeviceInfoController : ControllerBaseApi
    {
        private readonly ConfigReloadNotifier _configReload;

        /// <summary>
        /// 构造函数-获取依赖注入
        /// </summary>
        /// <param name="configReload">配置热刷新通知器(设备变更后去抖广播插件重建采集拓扑,C-4)</param>
        public DeviceInfoController(ConfigReloadNotifier configReload)
        {
            _configReload = configReload;
        }

        /// <summary>
        /// 设备新增
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string Insert(DeviceInfoEntity info)
        {
            Status = false;
            Message = "设备表信息保存失败。";
            if (!string.IsNullOrEmpty(info.DeviceGuid))
            {
                var temp = DeviceInfoDAO.Instance.GetOneBy(t => t.DeviceGuid == info.DeviceGuid);
                if (temp != null)
                {
                    Message = $"设备[{info.DeviceGuid}]已存在";
                    return Message;
                }
            }

            var optmdl = Request.GetToken();
            info.CreateId = optmdl.UserID;
            info.CreateTime = DateTime.Now.ToDateTimeString();
            info.CreateName = optmdl.UserName;
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            info.TenantId = optmdl.TenantId;
            Status = DeviceInfoDAO.Instance.Insert(info);
            if (Status)
            {
                Message = "设备信息新增成功。";
                _configReload.Notify("设备新增");
            }

            return Message;
        }

        /// <summary>
        /// VRV内机设备新增
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string VRVNjImport(string filename)
        {
            Status = false;
            Message = "VRV设备信息保存失败。";
            string resultstr = "";
            if (!filename.IsNullOrEmpty())
            {
                var optmdl = Request.GetToken();
                List<DeviceInfoEntity> insertlist = new();
                var wjlist = DeviceInfoDAO.Instance.GetListBy(it => it.TenantId == 4 && it.DeviceTypeCode == "vrvwj");
                if (wjlist.IsZxxAny())
                {
                    foreach (var wj in wjlist)
                    {
                        for (var i = 0; i < 12; i++)
                        {
                            var temp = DeviceInfoDAO.Instance.GetOneBy(t => t.ParentId == wj.DeviceId && t.DeviceAdr == i && t.DeviceTypeCode == "vrvpt");
                            if (temp != null)
                            {
                                resultstr += $"{wj.DeviceName}-{i},";
                            }
                            else
                            {
                                DeviceInfoEntity newnj = new DeviceInfoEntity()
                                {
                                    IsCollection = 1,
                                    IsVirtual = 0,
                                    ParentId = wj.DeviceId,
                                    DeviceName = $"{i}号内机",
                                    DeviceAdr = i,
                                    TenantId = wj.TenantId,
                                    DeviceTypeCode = "vrvpt",
                                    DeviceTypeFullCode = "|zhkt|vrvpt|",
                                    CreateId = optmdl.UserID,
                                    CreateTime = DateTime.Now.ToDateTimeString(),
                                    CreateName = optmdl.UserName,
                                    UpdateId = optmdl.UserID,
                                    UpdateTime = DateTime.Now.ToDateTimeString(),
                                    UpdateName = optmdl.UserName,
                                };
                                Status = DeviceInfoDAO.Instance.Insert(newnj);
                            }

                        }
                    }
                }

                if (Status)
                {
                    Message = "VRV设备信息新增成功。" + resultstr;
                    _configReload.Notify("VRV设备导入");
                }
            }

            return Message;
        }

        /// <summary>
        /// 设备修改
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string Update(DeviceInfoEntity info)
        {
            Status = false;
            Message = "设备信息更新失败。";
            var optmdl = Request.GetToken();
            var temp = DeviceInfoDAO.Instance.GetOneBy(t => t.DeviceId == info.DeviceId);
            if (temp == null)
            {
                Message = $"设备[{info.DeviceName}]不存在";
                return Message;
            }
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            info.TenantId = optmdl.TenantId;
            Status = DeviceInfoDAO.Instance.Update(info);
            if (Status)
            {
                Message = "设备信息更新成功。";
                _configReload.Notify("设备修改");
            }

            return Message;
        }

        /// <summary>
        /// 根据设备ID删除设备信息(包含子设备)
        /// </summary>
        /// <param name="id">设备ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string Delete(int id)
        {
            Status = false;
            Message = "设备信息删除失败。";
            var info = DeviceInfoDAO.Instance.GetOneBy(t => t.DeviceId == id);
            if (info != null)
            {
                Status = DeviceInfoDAO.Instance.DeleteById(id);
                if (Status)
                {
                    Message = "设备信息删除成功。";
                    _configReload.Notify("设备删除");
                }
            }
            return Message;
        }

        /// <summary>
        /// 启用/停用单设备采集(切IsCollection单字段+通知所属插件重建拓扑,即时生效)
        /// </summary>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        public string ToggleCollection(int deviceId, int isCollection)
        {
            try
            {
                var dev = DeviceInfoDAO.Instance.GetOneBy(t => t.DeviceId == deviceId);
                if (dev == null) return "设备不存在";
                dev.IsCollection = isCollection == 1 ? 1 : 0;
                DeviceInfoDAO.Instance.UpdateColumns(dev, it => new { it.IsCollection });
                _configReload.Notify(dev.IsCollection == 1 ? "启用设备采集" : "停用设备采集");
                return "success";
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("DeviceInfoController", "ToggleCollection", ex.ToString(), "设备管理");
                return "操作失败";
            }
        }

        /// <summary>
        /// 根据设备ID查询单条数据
        /// </summary>
        /// <param name="deviceid">设备ID</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public DeviceInfoEntity GetInfoByPk(int deviceid)
        {
            var optmdl = Request.GetToken();
            var entity = DeviceInfoDAO.Instance.GetOneBy(t => t.DeviceId == deviceid);
            return entity ?? new DeviceInfoEntity();
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
        public List<DeviceFullInfo> GetListByPage(ActionPara model)
        {
            List<DeviceFullInfo> alllist = new List<DeviceFullInfo>();
            int totalNumber = 0;
            var list = DeviceInfoDAO.Instance.GetListByPage(model, ref totalNumber);
            if (list.IsZxxAny())
            {
                var tenantlist = TenantInfoDAO.Instance.GetList();
                var typelist = DeviceTypeDAO.Instance.GetList();
                foreach (var dev in list)
                {
                    DeviceFullInfo info = new DeviceFullInfo();
                    dev.CopyTypeValue(info);
                    info.ExpandObject = dev.ExpandObject;
                    var tenant = tenantlist.FirstOrDefault(t => t.TenantId == info.TenantId);
                    if (tenant != null) info.TenantName = tenant.TenantName;
                    var devtype = typelist.FirstOrDefault(t => t.TypeCode == info.DeviceTypeCode);
                    if (devtype != null) info.DeviceTypeName = devtype.TypeName;
                    alllist.Add(info);
                }
            }
            TotalCount = totalNumber;
            return alllist;
        }

        /// <summary>
        /// 设备信息导入模板下载
        /// </summary>
        /// <param name="env">Web宿主环境(定位wwwroot下模板实体)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public MetaData DownloadDeviceTemplate([FromServices] IWebHostEnvironment env)
        {
            MetaData data = new()
            {
                Status = false,
                Message = "设备信息导入模板不存在"
            };
            // 模板实体位于wwwroot下由静态文件中间件对外服务，返回正斜杠相对路径供前端拼下载URL
            string serverparh = "Templates/DeviceInTemplate.xlsx";
            string templatePath = Path.Combine(env.WebRootPath, "Templates", "DeviceInTemplate.xlsx");
            if (System.IO.File.Exists(templatePath))
            {
                // 读取设备类型数据
                var typelist = DeviceTypeDAO.Instance.GetListBy(t => t.IsEnable);
                if (!typelist.IsZxxAny()) return data;
                using (var pck = new ExcelPackage(new FileInfo(templatePath)))
                {
                    foreach (var sheet in pck.Workbook.Worksheets)
                    {
                        if (sheet.Name.Equals("设备类型(请勿更改)"))
                        {
                            sheet.Cells.Clear();
                            for (int i = 0; i < typelist.Count; i++)
                            {
                                sheet.Cells[$"A{i + 1}"].Value = $"{typelist[i].TypeName}({typelist[i].TypeCode})";
                            }
                        }
                    }
                    // 保存更改
                    pck.Save();
                }
                data.Status = true;
                data.Message = "设备信息导入模板存在";
                data.Result = serverparh;
            }

            return data;
        }

        /// <summary>
        /// 设备信息导入
        /// </summary>
        /// <param name="file">Excel附件</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public MetaData DeviceImport(IFormFile file)
        {
            MetaData data = new()
            {
                Status = false,
                Message = "设备信息导入失败"
            };
            if (file.Length == 0) return data;
            var optmdl = Request.GetToken();
            var fileStream = file.OpenReadStream();
            IExcelImporter import = new ExcelImporter();
            var importRes = import.Import<DeviceImportDto>(fileStream).Result;
            if (importRes == null)
            {
                //数据为空处理
                return data;
            }
            if (importRes.RowErrors.Any())
            {
                //参数验证错误处理
                data.Message = "参数验证错误";
                return data;
            }
            if (importRes.Exception != null)
            {
                //发生异常处理
                data.Message = $"数据信息:{importRes.Exception.ToString()}";
                return data;
            }
            if (!importRes.Data.Any())
            {
                //正常导入数据为空处理
                data.Message = "数据不能为空";
                return data;
            }
            if (importRes.Data.Count > 100000)
            {
                //数据量过大处理
                data.Message = "数据量超过100000,请分批导入";
                return data;
            }

            #region 租户

            TenantInfo unit = null;
            string tenantname = "";
            foreach (var row in importRes.Data)
            {
                if (!row.TenantName.IsZxxNullOrEmpty())
                {
                    tenantname = row.TenantName;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(tenantname))
            {
                var _Tenantinfo = TenantInfoDAO.Instance.GetOneBy(it => it.TenantName == tenantname);
                if (_Tenantinfo != null)
                {
                    unit = _Tenantinfo;
                }
            }
            if (unit == null)
            {
                data.Message = "请先创建租户信息";
                return data;
            }

            #endregion

            #region 设备

            Dictionary<string, string> typelist = new();
            DateTime time = DateTime.Now;

            var oldequiplist = DeviceInfoDAO.Instance.GetListBy(it => it.TenantId == unit.TenantId);
            if (oldequiplist != null && oldequiplist.Count > 0)
                oldequiplist = oldequiplist.OrderBy(it => it.DeviceId).ToList();
            List<DeviceInfoEntity> equipist = new List<DeviceInfoEntity>();

            string errorstr = "";
            int idx = 1;
            if (oldequiplist != null && oldequiplist.Count > 0)
                idx = oldequiplist.Last().DeviceId + 1;
            foreach (var row in importRes.Data)
            {
                if (!string.IsNullOrEmpty(row.DeviceName))
                {
                    var port = 0;
                    if (!int.TryParse(row.DevicePort, out port))
                        port = 0;
                    var comm = 0;
                    if (!int.TryParse(row.DeviceCom, out comm))
                        comm = 0;
                    var switchadr = 0;
                    if (!int.TryParse(row.DeviceAdr, out switchadr))
                        switchadr = 0;
                    var parentid = 0;
                    if (!string.IsNullOrEmpty(row.ParentName))
                    {
                        var pdev = oldequiplist.Find(t => t.DeviceName == row.ParentName);
                        if (pdev != null)
                            parentid = pdev.DeviceId;
                    }
                    if (!string.IsNullOrEmpty(row.DeviceTypeCode))
                    {
                        DeviceTypeEntity? devtype = null;
                        string actualTypeCode = ExtractTypeCode(row.DeviceTypeCode);
                        devtype = DeviceTypeDAO.Instance.GetOneBy(it => it.TypeCode == actualTypeCode);
                        if (devtype == null)
                        {
                            errorstr += $"{row.DeviceName}设备类型不存在{Environment.NewLine}";
                            continue;
                        }
                        if (oldequiplist.Find(t => t.DeviceName == row.DeviceName) == null)
                        {
                            string _DeviceGuid = row.DeviceGuid;
                            if (_DeviceGuid.IsZxxNullOrEmpty() && !row.DeviceIp.IsZxxNullOrEmpty())
                            {
                                _DeviceGuid = $"{row.DeviceIp}_{port}_{switchadr}";
                            }
                            DeviceInfoEntity newnj = new DeviceInfoEntity()
                            {
                                IsCollection = 1,
                                IsVirtual = 0,
                                ParentId = parentid,
                                DeviceName = $"{row.DeviceName}",
                                DeviceGuid = _DeviceGuid,
                                DeviceGateway = row.DeviceGateway,
                                DeviceIp = row.DeviceIp,
                                DevicePort = port,
                                DeviceCom = comm,
                                DeviceAdr = switchadr,
                                TenantId = unit.TenantId,
                                DeviceTypeCode = actualTypeCode,
                                DeviceTypeFullCode = devtype.FullCode,
                                CreateId = optmdl.UserID,
                                CreateTime = DateTime.Now.ToDateTimeString(),
                                CreateName = optmdl.UserName,
                                UpdateId = optmdl.UserID,
                                UpdateTime = DateTime.Now.ToDateTimeString(),
                                UpdateName = optmdl.UserName,
                            };
                            data.Status = DeviceInfoDAO.Instance.Insert(newnj);
                            if (data.Status)
                            {
                                if (!typelist.ContainsKey(actualTypeCode))
                                {
                                    typelist.Add(actualTypeCode, actualTypeCode);
                                }
                                oldequiplist = DeviceInfoDAO.Instance.GetListBy(it => it.TenantId == unit.TenantId);
                            }
                            idx++;
                        }
                    }
                    else
                    {
                        errorstr += $"{row.DeviceName}设备类型字段不符合要求{Environment.NewLine}";
                    }
                }
            }
            if (data.Status)
            {
                data.Message = "设备导入成功";
                _configReload.Notify("设备批量导入");
            }

            #endregion

            return data;
        }

        private string ExtractTypeCode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var match = System.Text.RegularExpressions.Regex.Match(input, @"\(([^)]+)\)");
            return match.Success ? match.Groups[1].Value.Trim() : input.Trim();
        }

        /// <summary>
        /// 根据大类获取各个小类设备数量统计
        /// </summary>
        /// <param name="mastertypecode">设备大类编号</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<dynamic> GetDevCountByMasterType(string mastertypecode)
        {
            var result = new List<dynamic>();
            var optmdl = Request.GetToken();
            var devlist = DeviceInfoDAO.Instance.GetListBy(t => t.TenantId == optmdl.TenantId && t.DeviceTypeFullCode.Contains(mastertypecode));
            var devtypelist = DeviceTypeDAO.Instance.GetListBy(t => t.ParentId == mastertypecode);
            var grps = devlist.GroupBy(t => t.DeviceTypeCode);
            foreach (var grp in grps)
            {
                var devtype = devtypelist.Find(t => t.TypeCode == grp.Key);
                result.Add(new
                {
                    DeviceTypeCode = grp.Key,
                    DeviceTypeName = devtype?.TypeName ?? "",
                    DevCount = grp.Count(),
                });
            }

            return result;
        }

        /// <summary>
        /// 根据设备类型和电流/电压互感器变比刷公式
        /// </summary>
        /// <param name="typecode">类型编码</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string FormulaByCtPtType(string typecode)
        {
            Status = false;
            Message = "参数公式刷新失败。";
            var result = new List<dynamic>();
            var optmdl = Request.GetToken();
            var devlist = DeviceInfoDAO.Instance.GetListBy(t => t.TenantId == optmdl.TenantId && t.DeviceTypeCode == typecode);
            if (!devlist.IsZxxAny()) return Message;
            var devparamlist = DeviceParamDAO.Instance.GetListBy(t => t.TenantId == optmdl.TenantId && t.DeviceTypeCode == typecode);
            if (!devlist.IsZxxAny()) return Message;
            List<DeviceParamEntity> updatelist = new List<DeviceParamEntity>();
            foreach (var dev in devlist)
            {
                // 获取设备的互感器变比
                int currentTransformer = dev.ExpandObject?.CurrentTransformer ?? 1;
                int voltageTransformer = dev.ExpandObject?.VoltageTransformer ?? 1;
                // 如果变比都为1，跳过该设备
                if (currentTransformer == 1 && voltageTransformer == 1) continue;
                var devparam = devparamlist.Find(t => t.DeviceId == dev.DeviceId);
                if (devparam != null && devparam.ExpandObjects != null)
                {
                    bool deviceUpdated = false;
                    foreach (var dp in devparam.ExpandObjects)
                    {
                        // 跳过空公式
                        if (string.IsNullOrWhiteSpace(dp.ParamFormula)) continue;

                        string newFormula = UpdateParamFormula(dp.ParamFormula, dp.ParamName, dp.ParamTypeName, currentTransformer, voltageTransformer);
                        if (!string.IsNullOrEmpty(newFormula) && newFormula != dp.ParamFormula)
                        {
                            dp.ParamFormula = newFormula;
                            deviceUpdated = true;
                        }
                    }
                    // 如果该设备有参数被更新，保存到数据库
                    if (deviceUpdated) updatelist.Add(devparam);
                }
            }
            // 批量保存更新
            if (updatelist.Count > 0)
            {
                Status = DeviceParamDAO.Instance.UpdateColumns(updatelist, it => new { it.ExpandJson });
                if (Status)
                {
                    Message = $"参数公式刷新成功！";
                    _configReload.Notify("设备参数公式刷新");
                }
            }

            return Message;
        }

        /// <summary>
        /// 更新参数公式（根据互感器变比）
        /// </summary>
        /// <param name="currentFormula">当前公式</param>
        /// <param name="paramName">参数名称</param>
        /// <param name="paramTypeName">参数类型名称</param>
        /// <param name="ct">电流互感器变比</param>
        /// <param name="pt">电压互感器变比</param>
        /// <returns>新公式，如果无需更新则返回null</returns>
        private string UpdateParamFormula(string currentFormula, string paramName, string paramTypeName, int ct, int pt)
        {
            try
            {
                // 判断参数类型需要应用的变比类型
                bool needCT = false;  // 需要电流变比
                bool needPT = false;  // 需要电压变比

                // 根据参数名称和类型判断
                if (!string.IsNullOrEmpty(paramName) && paramName.Contains("电流"))
                {
                    needCT = true;
                }
                else if (!string.IsNullOrEmpty(paramName) && paramName.Contains("电压"))
                {
                    needPT = true;
                }
                else if (!string.IsNullOrEmpty(paramTypeName))
                {
                    // 功率和能耗类参数需要双变比
                    if (paramTypeName.Contains("功率") || paramTypeName.Contains("能耗") ||
                        paramTypeName.Contains("电能") || paramTypeName.Contains("电量"))
                    {
                        needCT = true;
                        needPT = true;
                    }
                }

                // 如果不需要任何变比，返回null
                if (!needCT && !needPT)
                    return null;

                string originalFormula = currentFormula;
                int existingCT = 1;
                int existingPT = 1;

                // 检查公式格式并提取原始公式
                if (needCT && needPT)
                {
                    // 双变比格式: CT*PT*(公式)
                    var pattern = @"^(\d+)\*(\d+)\*\((.+)\)$";
                    var match = System.Text.RegularExpressions.Regex.Match(currentFormula, pattern);

                    if (match.Success)
                    {
                        existingCT = int.Parse(match.Groups[1].Value);
                        existingPT = int.Parse(match.Groups[2].Value);
                        originalFormula = match.Groups[3].Value;

                        // 如果变比一致，无需更新
                        if (existingCT == ct && existingPT == pt)
                            return null;
                    }

                    // 返回新公式
                    return $"{ct}*{pt}*({originalFormula})";
                }
                else if (needCT)
                {
                    // 单电流变比格式: CT*(公式)
                    var pattern = @"^(\d+)\*\((.+)\)$";
                    var match = System.Text.RegularExpressions.Regex.Match(currentFormula, pattern);

                    if (match.Success)
                    {
                        existingCT = int.Parse(match.Groups[1].Value);
                        originalFormula = match.Groups[2].Value;

                        // 如果变比一致，无需更新
                        if (existingCT == ct)
                            return null;
                    }

                    // 返回新公式
                    return $"{ct}*({originalFormula})";
                }
                else if (needPT)
                {
                    // 单电压变比格式: PT*(公式)
                    var pattern = @"^(\d+)\*\((.+)\)$";
                    var match = System.Text.RegularExpressions.Regex.Match(currentFormula, pattern);

                    if (match.Success)
                    {
                        existingPT = int.Parse(match.Groups[1].Value);
                        originalFormula = match.Groups[2].Value;

                        // 如果变比一致，无需更新
                        if (existingPT == pt)
                            return null;
                    }

                    // 返回新公式
                    return $"{pt}*({originalFormula})";
                }

                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}