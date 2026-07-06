using CenBoCommon.Zxx;
using IotModel;
using IotWebApi.Areas.Device.Models;
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
            info.UnitId = optmdl.UnitId;
            Status = DeviceInfoDAO.Instance.Insert(info);
            if (Status) Message = "设备信息新增成功。";

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
                var wjlist = DeviceInfoDAO.Instance.GetListBy(it => it.UnitId == 4 && it.DeviceTypeCode == "vrvwj");
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
                                    BuildId = wj.BuildId,
                                    DeptId = wj.DeptId,
                                    UnitId = wj.UnitId,
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

                if (Status) Message = "VRV设备信息新增成功。" + resultstr;
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
            info.UnitId = optmdl.UnitId;
            Status = DeviceInfoDAO.Instance.Update(info);
            if (Status) Message = "设备信息更新成功。";

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
                if (Status) Message = "设备信息删除成功。";
            }
            return Message;
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
                var unitlist = BasicunitInfoDAO.Instance.GetList();
                var buildlist = BuildInfoDAO.Instance.GetList();
                var deptlist = DeptInfoDAO.Instance.GetList();
                var typelist = DeviceTypeDAO.Instance.GetList();
                foreach (var dev in list)
                {
                    DeviceFullInfo info = new DeviceFullInfo();
                    dev.CopyTypeValue(info);
                    info.ExpandObject = dev.ExpandObject;
                    var unit = unitlist.FirstOrDefault(t => t.UnitId == info.UnitId);
                    if (unit != null) info.UnitName = unit.UnitName;
                    var dept = deptlist.FirstOrDefault(t => t.DeptId == info.DeptId);
                    if (dept != null) info.DeptName = dept.FullName.BeautifyFullName();
                    var build = buildlist.FirstOrDefault(t => t.BuildId == info.BuildId);
                    if (build != null) info.BuildName = build.FullName.BeautifyFullName();
                    var devtype = typelist.FirstOrDefault(t => t.TypeCode == info.DeviceTypeCode);
                    if (devtype != null) info.DeviceTypeName = devtype.TypeName;
                    alllist.Add(info);
                }
            }
            TotalCount = totalNumber;
            return alllist;
        }

        /// <summary>
        /// 根据建筑ID获取网络拓扑图数据
        /// </summary>
        /// <param name="buildid">建筑ID</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<TuopuAutoInfo> GetAutoMapDataByBuild(int buildid)
        {
            List<TuopuAutoInfo> list = new List<TuopuAutoInfo>();
            var optmdl = Request.GetToken();
            var buildlist = BuildInfoDAO.Instance.GetListBy(t => t.UnitId == optmdl.UnitId);
            if (!buildlist.IsZxxAny()) return list;
            var _buildlist = buildlist.FindAll(t => t.FullCode.Contains($"|{buildid}|"));
            if (!_buildlist.IsZxxAny()) return list;
            var bids = _buildlist.Select(t => t.BuildId).Distinct().ToList();
            var devicelist = DeviceInfoDAO.Instance.GetListBy(t => bids.Contains(t.BuildId));
            if (!devicelist.IsZxxAny()) return list;
            //var paramlist = DeviceParamDAO.Instance.GetListBy(t => bids.Contains(t.BuildId));
            //if (!paramlist.IsZxxAny()) return list;

            foreach (var device in devicelist)
            {
                TuopuAutoInfo info = new TuopuAutoInfo
                {
                    DeviceId = device.DeviceId,
                    DeviceName = device.DeviceName,
                    ParentId = device.ParentId,
                    DeviceState = device.DeviceState,
                };
                if (device.DeviceAlarm == 1) info.DeviceState = 3;
                //if (info.DeviceState == 2)
                //{
                //    var _paramlist = paramlist.FindAll(t => t.DeviceId == info.DeviceId);
                //    if (_paramlist.IsZxxAny() && _paramlist.Any(t => t.ExpandObjects.Any(k => k.IsAlarm == 1))) info.DeviceState = 2;
                //}
                list.Add(info);
            }

            return list;
        }

        /// <summary>
        /// 根据建筑ID查询统计分析设备列表
        /// </summary>
        /// <param name="buildid">建筑ID</param>
        /// <param name="devicetype">设备类型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceInfo> GetReprotList(int buildid, string devicetype)
        {
            var typeList = SysCommonDAO<DeviceType>.Instance.GetListBy(t => t.FullCode.Contains($"|{devicetype}|"));
            if (!typeList.IsZxxAny()) return new List<DeviceInfo>();
            var _typecodes = typeList.Select(t => t.TypeCode).Distinct().ToList();
            //var typeparamlist = DeviceTypeParamDAO.Instance.GetListBy(t => t.IsReport && _typecodes.Contains(t.DeviceTypeCode));
            //if (!typeparamlist.IsZxxAny()) return new List<DeviceInfo>();
            //var typecodes = typeparamlist.Select(t => t.DeviceTypeCode).Distinct().ToList();
            var buildlist = BuildInfoDAO.Instance.GetListBy(t => t.FullCode.Contains($"|{buildid}|"));
            if (!buildlist.IsZxxAny()) return new List<DeviceInfo>();
            var buildids = buildlist.Select(t => t.BuildId).Distinct().ToList();
            var list = SysCommonDAO<DeviceInfo>.Instance.GetListBy(t => buildids.Contains(t.BuildId) && _typecodes.Contains(t.DeviceTypeCode));
            return list;
        }

        /// <summary>
        /// 根据建筑ID查询极值分析设备列表
        /// </summary>
        /// <param name="buildid">建筑ID</param>
        /// <param name="devicetype">设备类型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceInfo> GetPeakList(int buildid, string devicetype)
        {
            var typeList = SysCommonDAO<DeviceType>.Instance.GetListBy(t => t.FullCode.Contains($"|{devicetype}|"));
            if (!typeList.IsZxxAny()) return new List<DeviceInfo>();
            var _typecodes = typeList.Select(t => t.TypeCode).Distinct().ToList();
            var typeparamlist = DeviceTypeParamDAO.Instance.GetListBy(t => t.IsPeak && _typecodes.Contains(t.DeviceTypeCode));
            if (!typeparamlist.IsZxxAny()) return new List<DeviceInfo>();
            var typecodes = typeparamlist.Select(t => t.DeviceTypeCode).Distinct().ToList();
            var buildlist = BuildInfoDAO.Instance.GetListBy(t => t.FullCode.Contains($"|{buildid}|"));
            if (!buildlist.IsZxxAny()) return new List<DeviceInfo>();
            var buildids = buildlist.Select(t => t.BuildId).Distinct().ToList();
            var list = SysCommonDAO<DeviceInfo>.Instance.GetListBy(t => buildids.Contains(t.BuildId) && typecodes.Contains(t.DeviceTypeCode));
            return list;
        }

        /// <summary>
        /// 根据建筑ID查询运行数据设备列表
        /// </summary>
        /// <param name="buildid">建筑ID</param>
        /// <param name="devicetype">设备类型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceInfo> GetRunListByBuild(int buildid, string devicetype)
        {
            var typeList = SysCommonDAO<DeviceType>.Instance.GetListBy(t => t.FullCode.Contains($"|{devicetype}|"));
            if (!typeList.IsZxxAny()) return new List<DeviceInfo>();
            typeList.RemoveAll(t => t.FullCode.Contains($"|zhwg|"));
            var _typecodes = typeList.Select(t => t.TypeCode).Distinct().ToList();
            //var typeparamlist = DeviceTypeParamDAO.Instance.GetListBy(t => t.IsReport && _typecodes.Contains(t.DeviceTypeCode));
            //if (!typeparamlist.IsZxxAny()) return new List<DeviceInfo>();
            //var typecodes = typeparamlist.Select(t => t.DeviceTypeCode).Distinct().ToList();
            var buildlist = BuildInfoDAO.Instance.GetListBy(t => t.FullCode.Contains($"|{buildid}|"));
            if (!buildlist.IsZxxAny()) return new List<DeviceInfo>();
            var buildids = buildlist.Select(t => t.BuildId).Distinct().ToList();
            var list = SysCommonDAO<DeviceInfo>.Instance.GetListBy(t => buildids.Contains(t.BuildId) && _typecodes.Contains(t.DeviceTypeCode));
            return list;
        }

        /// <summary>
        /// 设备信息导入模板下载
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public MetaData DownloadDeviceTemplate()
        {
            MetaData data = new()
            {
                Status = false,
                Message = "设备信息导入模板不存在"
            };
            string serverparh = Path.Combine(OperatorCommon.NetYingShefile, "Templates", "DeviceInTemplate.xlsx");
            string templatePath = Path.Combine(OperatorCommon.NetLocalfile, serverparh);
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

            #region 单位

            BasicunitInfo unit = null;
            string unitname = "";
            foreach (var row in importRes.Data)
            {
                if (!row.UnitName.IsZxxNullOrEmpty())
                {
                    unitname = row.UnitName;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(unitname))
            {
                var _Basicunitinfo = BasicunitInfoDAO.Instance.GetOneBy(it => it.UnitName == unitname);
                if (_Basicunitinfo != null)
                {
                    unit = _Basicunitinfo;
                }
            }
            if (unit == null)
            {
                data.Message = "请先创建单位信息";
                return data;
            }

            #endregion

            #region 建筑

            int index = 1;
            List<BuildInfo> buildlist = new List<BuildInfo>();
            var oldbuildlist = BuildInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();
            List<BuildInfo> _buildlist = new List<BuildInfo>();
            foreach (var row in importRes.Data)
            {
                bool iscontue = false;
                if (!string.IsNullOrEmpty(row.BuildId1))
                {
                    if (oldbuildlist.Count > 0)
                    {
                        if (oldbuildlist.Find(t => t.BuildName == row.BuildId1) == null)
                        {
                            iscontue = true;
                        }
                    }
                    else
                    {
                        iscontue = true;
                    }
                    if (!iscontue) continue;

                    if (_buildlist.Find(t => t.BuildName == row.BuildId1) == null)
                    {
                        BuildInfo buildinginfo = new BuildInfo();
                        buildinginfo.BuildName = row.BuildId1;
                        buildinginfo.ParentId = 0;
                        buildinginfo.UnitId = unit.UnitId;
                        buildinginfo.CreateId = optmdl.UserID;
                        buildinginfo.CreateTime = DateTime.Now.ToDateTimeString();
                        buildinginfo.CreateName = optmdl.UserName;
                        buildinginfo.UpdateId = optmdl.UserID;
                        buildinginfo.UpdateTime = DateTime.Now.ToDateTimeString();
                        buildinginfo.UpdateName = optmdl.UserName;
                        Status = BuildInfoDAO.Instance.Insert(buildinginfo);
                        _buildlist = BuildInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();
                        index++;
                    }
                }
            }

            buildlist = BuildInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();
            index = 1;
            foreach (var row in importRes.Data)
            {
                var pbuild = buildlist.Find(t => t.BuildName == row.BuildId1);
                if (pbuild != null)
                {
                    if (!string.IsNullOrEmpty(row.BuildId2))
                    {
                        if (buildlist.Count > 0)
                        {
                            string str = $"{row.BuildId1}|{row.BuildId2}";
                            if (buildlist.Find(t => t.FullName == str) == null)
                            {
                                BuildInfo buildinginfo = new BuildInfo();
                                buildinginfo.BuildName = row.BuildId2;
                                buildinginfo.ParentId = pbuild.BuildId;
                                buildinginfo.UnitId = unit.UnitId;
                                buildinginfo.CreateId = optmdl.UserID;
                                buildinginfo.CreateTime = DateTime.Now.ToDateTimeString();
                                buildinginfo.CreateName = optmdl.UserName;
                                buildinginfo.UpdateId = optmdl.UserID;
                                buildinginfo.UpdateTime = DateTime.Now.ToDateTimeString();
                                buildinginfo.UpdateName = optmdl.UserName;
                                Status = BuildInfoDAO.Instance.Insert(buildinginfo);
                                buildlist = BuildInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();
                                index++;
                            }
                        }
                    }
                }
            }

            buildlist = null;
            buildlist = BuildInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();

            index = 1;
            foreach (var row in importRes.Data)
            {
                string str = $"{row.BuildId1}|{row.BuildId2}";
                var pbuild = buildlist.Find(t => t.FullName == str);
                if (pbuild != null)
                {
                    if (!string.IsNullOrEmpty(row.BuildId3))
                    {
                        str = $"{row.BuildId1}|{row.BuildId2}|{row.BuildId3}";
                        if (buildlist.Find(t => t.FullName == str) == null)
                        {
                            BuildInfo buildinginfo = new BuildInfo();
                            buildinginfo.BuildName = row.BuildId3;
                            buildinginfo.ParentId = pbuild.BuildId;
                            buildinginfo.UnitId = unit.UnitId;
                            buildinginfo.CreateId = optmdl.UserID;
                            buildinginfo.CreateTime = DateTime.Now.ToDateTimeString();
                            buildinginfo.CreateName = optmdl.UserName;
                            buildinginfo.UpdateId = optmdl.UserID;
                            buildinginfo.UpdateTime = DateTime.Now.ToDateTimeString();
                            buildinginfo.UpdateName = optmdl.UserName;
                            Status = BuildInfoDAO.Instance.Insert(buildinginfo);
                            buildlist = BuildInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();
                            index++;
                        }
                    }
                }
            }

            buildlist = null;
            buildlist = BuildInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();

            index = 1;
            foreach (var row in importRes.Data)
            {
                string str = $"{row.BuildId1}|{row.BuildId2}|{row.BuildId3}";
                var pbuild = buildlist.Find(t => t.FullName == str);
                if (pbuild != null)
                {
                    if (!string.IsNullOrEmpty(row.BuildId4))
                    {
                        str = $"{row.BuildId1}|{row.BuildId2}|{row.BuildId3}|{row.BuildId4}";
                        if (buildlist.Find(t => t.FullName == str) == null)
                        {
                            BuildInfo buildinginfo = new BuildInfo();
                            buildinginfo.BuildName = row.BuildId4;
                            buildinginfo.ParentId = pbuild.BuildId;
                            buildinginfo.UnitId = unit.UnitId;
                            buildinginfo.CreateId = optmdl.UserID;
                            buildinginfo.CreateTime = DateTime.Now.ToDateTimeString();
                            buildinginfo.CreateName = optmdl.UserName;
                            buildinginfo.UpdateId = optmdl.UserID;
                            buildinginfo.UpdateTime = DateTime.Now.ToDateTimeString();
                            buildinginfo.UpdateName = optmdl.UserName;
                            Status = BuildInfoDAO.Instance.Insert(buildinginfo);
                            buildlist = BuildInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();
                            index++;
                        }
                    }
                }
            }

            buildlist = null;
            buildlist = BuildInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();

            #endregion

            #region 部门

            index = 1;
            List<DeptInfo> departlist = new List<DeptInfo>();
            var olddepartlist = DeptInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();
            List<DeptInfo> _departlist = new List<DeptInfo>();
            foreach (var row in importRes.Data)
            {
                bool iscontue = false;
                if (!string.IsNullOrEmpty(row.DeptId1))
                {
                    if (olddepartlist.Count > 0)
                    {
                        if (olddepartlist.Find(t => t.DeptName == row.DeptId1) == null)
                        {
                            iscontue = true;
                        }
                    }
                    else
                    {
                        iscontue = true;
                    }
                    if (!iscontue) continue;

                    if (_departlist.Find(t => t.DeptName == row.DeptId1) == null)
                    {
                        DeptInfo depart = new DeptInfo();
                        depart.DeptName = row.DeptId1;
                        depart.ParentId = 0;
                        depart.UnitId = unit.UnitId;
                        depart.CreateId = optmdl.UserID;
                        depart.CreateTime = DateTime.Now.ToDateTimeString();
                        depart.CreateName = optmdl.UserName;
                        depart.UpdateId = optmdl.UserID;
                        depart.UpdateTime = DateTime.Now.ToDateTimeString();
                        depart.UpdateName = optmdl.UserName;
                        Status = DeptInfoDAO.Instance.Insert(depart);
                        _departlist = DeptInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();
                        index++;
                    }
                }
            }

            departlist = DeptInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();
            index = 1;
            foreach (var row in importRes.Data)
            {
                var pbuild = departlist.Find(t => t.DeptName == row.DeptId1);
                if (pbuild != null)
                {
                    if (!string.IsNullOrEmpty(row.DeptId2))
                    {
                        if (departlist.Count > 0)
                        {
                            string str = $"{row.DeptId1}|{row.DeptId2}";
                            if (departlist.Find(t => t.FullName == str) == null)
                            {
                                DeptInfo depart = new DeptInfo();
                                depart.DeptName = row.DeptId2;
                                depart.ParentId = pbuild.DeptId;
                                depart.UnitId = unit.UnitId;
                                depart.CreateId = optmdl.UserID;
                                depart.CreateTime = DateTime.Now.ToDateTimeString();
                                depart.CreateName = optmdl.UserName;
                                depart.UpdateId = optmdl.UserID;
                                depart.UpdateTime = DateTime.Now.ToDateTimeString();
                                depart.UpdateName = optmdl.UserName;
                                Status = DeptInfoDAO.Instance.Insert(depart);
                                departlist = DeptInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();
                                index++;
                            }
                        }
                    }
                }
            }

            departlist = null;
            departlist = DeptInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();

            index = 1;
            foreach (var row in importRes.Data)
            {
                string str = $"{row.DeptId1}|{row.DeptId2}";
                var pbuild = departlist.Find(t => t.FullName == str);
                if (pbuild != null)
                {
                    if (!string.IsNullOrEmpty(row.DeptId3))
                    {
                        str = $"{row.DeptId1}|{row.DeptId2}|{row.DeptId3}";
                        if (departlist.Find(t => t.FullName == str) == null)
                        {
                            DeptInfo depart = new DeptInfo();
                            depart.DeptName = row.DeptId3;
                            depart.ParentId = pbuild.DeptId;
                            depart.UnitId = unit.UnitId;
                            depart.CreateId = optmdl.UserID;
                            depart.CreateTime = DateTime.Now.ToDateTimeString();
                            depart.CreateName = optmdl.UserName;
                            depart.UpdateId = optmdl.UserID;
                            depart.UpdateTime = DateTime.Now.ToDateTimeString();
                            depart.UpdateName = optmdl.UserName;
                            Status = DeptInfoDAO.Instance.Insert(depart);
                            departlist = DeptInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();
                            index++;
                        }
                    }
                }
            }

            departlist = null;
            departlist = DeptInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId).ToList();

            #endregion

            #region 设备

            Dictionary<string, string> typelist = new();
            DateTime time = DateTime.Now;

            var oldequiplist = DeviceInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId);
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
                    var build = GetBuild(row.BuildId1, row.BuildId2, row.BuildId3, row.BuildId4, buildlist);
                    var depart = GetDepart(row.DeptId1, row.DeptId2, null, departlist);
                    int buildID = 0;
                    int departID = 0;
                    if (build == null || build.BuildId <= 0)
                    {
                        errorstr += $"{row.DeviceName}建筑不存在{Environment.NewLine}";
                    }
                    else
                    {
                        buildID = build.BuildId;
                    }
                    if (depart == null || depart.DeptId <= 0)
                    {
                        errorstr += $"{row.DeviceName}部门不存在{Environment.NewLine}";
                    }
                    else
                    {
                        departID = depart.DeptId;
                    }
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
                        var pdev = oldequiplist.Find(t => t.DeviceName == row.ParentName && t.BuildId == buildID);
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
                        if (oldequiplist.Find(t => t.DeviceName == row.DeviceName && t.BuildId == buildID) == null)
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
                                BuildId = buildID,
                                DeptId = departID,
                                UnitId = unit.UnitId,
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
                                oldequiplist = DeviceInfoDAO.Instance.GetListBy(it => it.UnitId == unit.UnitId);
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
                data.Message = "设备导入成功";

            #endregion

            return data;
        }

        private BuildInfo GetBuild(string a1, string a2, string a3, string a4, List<BuildInfo> list)
        {
            BuildInfo result = null;

            string buildname = "";
            if (!string.IsNullOrEmpty(a1))
            {
                buildname = a1;
                if (!string.IsNullOrEmpty(a2))
                {
                    buildname += $"|{a2}";
                    if (!string.IsNullOrEmpty(a3))
                    {
                        buildname += $"|{a3}";
                        if (!string.IsNullOrEmpty(a4))
                        {
                            buildname += $"|{a4}";
                        }
                    }
                }
            }
            var build = list.Find(t => t.FullName == buildname);
            if (build != null)
            {
                result = build;
            }

            return result;
        }

        private DeptInfo GetDepart(string a1, string a2, string a3, List<DeptInfo> list)
        {
            DeptInfo depart = null;

            var parent = list.Find(t => t.DeptName == a1);
            if (parent != null)
            {
                if (!string.IsNullOrEmpty(a2))
                {
                    var child = list.Find(t => t.DeptName == a2 && t.ParentId == parent.DeptId);
                    if (child != null)
                    {
                        if (!string.IsNullOrEmpty(a3))
                        {
                            depart = list.Find(t => t.DeptName == a3 && t.ParentId == child.DeptId);
                            if (depart == null)
                            {
                                depart = child;
                            }
                        }
                        else
                        {
                            depart = child;
                        }
                    }
                }
                else
                {
                    depart = parent;
                }
            }

            return depart;
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
            var devlist = DeviceInfoDAO.Instance.GetListBy(t => t.UnitId == optmdl.UnitId && t.DeviceTypeFullCode.Contains(mastertypecode));
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
            var devlist = DeviceInfoDAO.Instance.GetListBy(t => t.UnitId == optmdl.UnitId && t.DeviceTypeCode == typecode);
            if (!devlist.IsZxxAny()) return Message;
            var devparamlist = DeviceParamDAO.Instance.GetListBy(t => t.UnitId == optmdl.UnitId && t.DeviceTypeCode == typecode);
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
                if (Status) Message = $"参数公式刷新成功！";
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