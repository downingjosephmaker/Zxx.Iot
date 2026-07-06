using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Areas.Device.Models;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 设备类型
    /// </summary>
    [ApiController]
    [ControllSort("7-1")]
    public class DeviceTypeController : ControllerBaseApi
    {
        /// <summary>
        /// 设备类型新增
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string Insert(DeviceTypeEntity info)
        {
            Status = false;
            Message = "设备类型表信息保存失败。";
            var optmdl = Request.GetToken();
            info.CreateId = optmdl.UserID;
            info.CreateTime = DateTime.Now.ToDateTimeString();
            info.CreateName = optmdl.UserName;
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = DeviceTypeDAO.Instance.Insert(info);
            if (Status) Message = "设备类型信息新增成功。";
            return Message;
        }

        /// <summary>
        /// 设备类型修改
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string Update(DeviceTypeEntity info)
        {
            Status = false;
            Message = "设备类型信息更新失败。";
            var optmdl = Request.GetToken();
            var temp = DeviceTypeDAO.Instance.GetOneBy(t => t.TypeCode == info.TypeCode);
            if (temp == null)
            {
                Message = $"设备类型[{info.TypeName}]不存在";
                return Message;
            }
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = DeviceTypeDAO.Instance.Update(info);
            if (Status) Message = "设备类型信息更新成功。";
            return Message;
        }

        /// <summary>
        /// 根据设备类型ID删除
        /// </summary>
        /// <param name="id">设备类型ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string Delete(string id)
        {
            Message = "设备类型删除失败。";
            var info = DeviceTypeDAO.Instance.GetOneBy(t => t.TypeCode == id);
            if (info != null)
            {
                Status = DeviceTypeDAO.Instance.DeleteById(id);
                if (Status) Message = "设备类型删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据设备类型ID查询单条数据
        /// </summary>
        /// <param name="typecode">设备类型ID</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public DeviceTypeEntity GetInfoByPk(string typecode)
        {
            var entity = DeviceTypeDAO.Instance.GetOneBy(t => t.TypeCode == typecode);
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
        public List<DeviceTypeEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = DeviceTypeDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 获取当前设备大类列表
        /// </summary>
        /// <param name="menucode">菜单编码</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceTypeRun> GetMasterTypeList(string menucode)
        {
            var optmdl = Request.GetToken();
            var dtrlist = DeviceTypeRunDAO.Instance.GetListBy(t => t.UnitId == optmdl.UnitId && t.MenuCode.Contains(menucode));
            //if (dtrlist.Count == 0)
            //{
            //    var devlist = DeviceInfoDAO.Instance.GetListBy(t => t.UnitId == optmdl.UnitId);
            //    var tpclist = devlist.Select(t => t.DeviceTypeCode).Distinct().ToList();
            //    var tpalist = DeviceTypeDAO.Instance.GetListBy(t => t.IsEnable);
            //    var tplist = tpalist.FindAll(t => tpclist.Contains(t.TypeCode));
            //    var mtpclist = tplist.Select(t => t.ParentId).Distinct().ToList();
            //    var mtplist = tpalist.FindAll(t => mtpclist.Contains(t.TypeCode));
            //    dtrlist = mtplist.Select(t => new DeviceTypeRun()
            //    {
            //        UnitId = optmdl.UnitId,
            //        DeviceTypeCode = t.TypeCode,
            //        DeviceTypeName = t.TypeName,
            //        MenuCode = menucode
            //    }).ToList();
            //    DeviceTypeRunDAO.Instance.InsertRange(dtrlist);
            //}

            return dtrlist;
        }

        /// <summary>
        /// 获取历史数据界面设备类型列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceTypeSelect> GetHistoryTypes()
        {
            List<DeviceTypeSelect> list = new List<DeviceTypeSelect>();
            var optmdl = Request.GetToken();
            var devices = SysCommonDAO<DeviceInfo>.Instance.GetListBy(t => t.UnitId == optmdl.UnitId);
            var realtypes = devices.Select(t => t.DeviceTypeCode).Distinct().ToList();
            if (!realtypes.IsZxxAny()) return list;
            var typelist = DeviceTypeDAO.Instance.GetListBy(t => t.IsEnable);
            var _typelist = typelist.FindAll(t => realtypes.Contains(t.TypeCode));
            var _typelistfulls = _typelist.Select(t => t.FullCode).Distinct().ToList();
            var onelevels = typelist.FindAll(t => _typelistfulls.Any(k => k.Contains(t.FullCode)) && t.TreeLevel == 1);
            foreach (var type in onelevels.OrderBy(t => t.SortBorder))
            {
                if (list.Any(t => t.DeviceTypeCode == type.TypeCode)) continue;
                list.Add(new DeviceTypeSelect()
                {
                    DeviceTypeCode = type.TypeCode,
                    DeviceTypeName = type.TypeName,
                });
            }

            return list;
        }

        /// <summary>
        /// 获取设备能耗分析界面设备类型列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceTypeSelect> GetReportTypes()
        {
            List<DeviceTypeSelect> list = new List<DeviceTypeSelect>();
            var optmdl = Request.GetToken();
            var devices = SysCommonDAO<DeviceInfo>.Instance.GetListBy(t => t.UnitId == optmdl.UnitId);
            var realtypes = devices.Select(t => t.DeviceTypeCode).Distinct().ToList();
            if (!realtypes.IsZxxAny()) return list;
            var typelist = DeviceTypeDAO.Instance.GetListBy(t => t.IsEnable);
            var realtypelist = typelist.FindAll(t => realtypes.Contains(t.TypeCode));
            var realtypefulls = realtypelist.Select(t => t.FullCode).Distinct().ToList();
            var realonelevels = typelist.FindAll(t => realtypefulls.Any(k => k.Contains(t.FullCode)) && t.TreeLevel == 1).Select(t => t.TypeCode).Distinct().ToList();
            var typeparamlist = DeviceTypeParamDAO.Instance.GetListBy(t => t.IsReport);
            if (typeparamlist.IsZxxAny()) typeparamlist.RemoveAll(t => t.ParamCode != "energy");
            var typeparams = typeparamlist?.Select(t => t.DeviceTypeCode).Distinct().ToList();
            if (!typeparams.IsZxxAny()) return list;
            var _typelist = typelist.FindAll(t => typeparams.Contains(t.TypeCode));
            var _typelistfulls = _typelist.Select(t => t.FullCode).Distinct().ToList();
            var onelevels = typelist.FindAll(t => _typelistfulls.Any(k => k.Contains(t.FullCode)) && realonelevels.Contains(t.TypeCode) && t.TreeLevel == 1);
            foreach (var type in onelevels.OrderBy(t => t.SortBorder))
            {
                if (list.Any(t => t.DeviceTypeCode == type.TypeCode)) continue;
                list.Add(new DeviceTypeSelect()
                {
                    DeviceTypeCode = type.TypeCode,
                    DeviceTypeName = type.TypeName,
                });
            }

            return list;
        }

        /// <summary>
        /// 获取设备能耗分析界面设备类型列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceTypeSelect> GetPeakTypes()
        {
            List<DeviceTypeSelect> list = new List<DeviceTypeSelect>();
            var optmdl = Request.GetToken();
            var mtplist = DeviceTypeRunDAO.Instance.GetListBy(t => t.UnitId == optmdl.UnitId && !t.MenuCode.Contains("otherCollect"));
            var typelist = DeviceTypeDAO.Instance.GetListBy(t => t.IsEnable);
            var typeparamlist = DeviceTypeParamDAO.Instance.GetListBy(t => t.IsPeak);
            var typeparams = typeparamlist.Select(t => t.DeviceTypeCode).Distinct().ToList();
            var _typelist = typelist.FindAll(t => typeparams.Contains(t.TypeCode));
            foreach (var type in _typelist)
            {
                var arry = type.FullCode.ToStringList('|');
                var first = typelist.Find(t => t.TypeCode == arry[1]);
                if (first != null)
                {
                    var runtype = mtplist.Find(t => t.DeviceTypeCode == first.TypeCode);
                    if (runtype != null && !list.Any(t => t.DeviceTypeCode == first.TypeCode))
                    {
                        list.Add(new DeviceTypeSelect()
                        {
                            DeviceTypeCode = runtype.DeviceTypeCode,
                            DeviceTypeName = runtype.DeviceTypeName,
                        });
                    }
                }
            }

            return list;
        }

    }
}