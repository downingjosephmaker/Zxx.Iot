using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Areas.Scada.Models;

namespace IotWebApi.Areas.Scada.Controllers
{
    /// <summary>
    /// 组态项目数据控制器
    /// </summary>
    [ApiController]
    [ControllSort("26-11")]
    public class ScadaProjectDataController : ControllerBaseApi
    {
        /// <summary>
        /// 根据条件查询设备信息分页数据
        /// </summary>
        /// <param name="model">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public List<ScadaDevice> GetListByPage(ActionPara model)
        {
            List<ScadaDevice> alllist = new List<ScadaDevice>();
            int totalNumber = 0;
            var optmdl = Request.GetToken();
            //单位权限
            {
                if (model.sconlist.IsZxxAny() && !model.sconlist.Any(t => t.ParamName.ToLower() == "unitid"))
                {
                    model.sconlist.RemoveAll(t => t.ParamName.ToLower() == "unitid");
                }
                model.sconlist.Add(new SelectCondition()
                {
                    ParamName = "UnitId",
                    ParamType = "=",
                    ParamValue = optmdl.UnitId.ToString(),
                });
            }
            //部门权限
            {
                List<DeptInfo> departInfo = new();
                foreach (var item in optmdl._DeptInfoDic)
                {
                    departInfo.AddRange(item.Value);
                }
                var departCon = model.sconlist.Find(s => s.ParamName.ToLower() == "deptid" && s.ParamType == "=" && !s.ParamValue.IsZxxNullOrEmpty());
                if (departCon != null)
                {
                    var deptid = departCon.ParamValue.ToZxxInt();
                    var tempDepart = DeptInfoDAO.Instance.GetOneBy(s => s.DeptId == deptid);
                    if (tempDepart != null)
                    {
                        if (departInfo.Any(s => s.FullCode.Contains($"|{tempDepart.DeptId}|") && s.TreeLevel > tempDepart.TreeLevel))
                        {
                            model.sconlist.Remove(departCon);
                            var thirdIdList = departInfo.Where(s => s.FullCode.Contains($"|{tempDepart.DeptId}|")).Select(s => s.DeptId);
                            if (thirdIdList.IsZxxAny())
                            {
                                model.sconlist.Add(new SelectCondition()
                                {
                                    ParamName = "DeptId",
                                    ParamType = "in",
                                    ParamValue = thirdIdList.ListIntZdToString(","),
                                });
                            }
                        }
                    }
                }
                else   //部门权限为必需条件
                {
                    model.sconlist.Add(new SelectCondition()
                    {
                        ParamName = "dept_id",
                        ParamValue = optmdl._DeptIdList.ListIntZdToString(","),
                        ParamType = "in",
                    });
                }
            }

            var list = DeviceInfoDAO.Instance.GetListByPage(model, ref totalNumber);
            if (list.IsZxxAny())
            {
                var devids = list.Select(t => t.DeviceId).ToList();
                var devparamlist = DeviceParamDAO.Instance.GetListBy(t => devids.Contains(t.DeviceId));
                if (!devparamlist.IsZxxAny()) return alllist;
                var typelist = DeviceTypeDAO.Instance.GetList();
                foreach (var dev in list)
                {
                    ScadaDevice info = new ScadaDevice
                    {
                        DeviceId = dev.DeviceId.ToString(),
                        DeviceName = dev.DeviceName,
                        DeviceTypeCode = dev.DeviceTypeCode,
                        DeviceTypeFullCode = dev.DeviceTypeFullCode,
                        DeviceGuid = dev.DeviceGuid,
                        LastOnlineTime = dev.LastOnlineTime,
                        DeviceState = dev.DeviceState,
                        DeviceAlarm = dev.DeviceAlarm,
                        DeviceSwitch = dev.DeviceSwitch,
                        DeviceFullCode = dev.FullCode,
                        DeviceParams = new List<ScadaDeviceParam>(),
                    };
                    var devtype = typelist.FirstOrDefault(t => t.TypeCode == info.DeviceTypeCode);
                    if (devtype != null) info.DeviceTypeName = devtype.TypeName;
                    var paraminfo = devparamlist.Find(t => t.DeviceId == dev.DeviceId);
                    if (paraminfo != null)
                    {
                        List<ScadaDeviceParam> DeviceParams = new List<ScadaDeviceParam>();
                        foreach (var param in paraminfo.ExpandObjects)
                        {
                            ScadaDeviceParam _paraminfo = new ScadaDeviceParam
                            {
                                ParamCode = param.ParamCode,
                                ParamName = param.ParamName,
                                ValueUnit = param.ValueUnit,
                                CollectTime = param.CollectTime,
                                ParamLastValue = param.ParamLastValue,
                                ParamValue = param.ParamValue,
                                IsAlarm = param.IsAlarm,

                            };
                            DeviceParams.Add(_paraminfo);
                        }
                        if (DeviceParams.Count > 0)
                        {
                            info.DeviceParams = DeviceParams;
                        }
                    }
                    alllist.Add(info);
                }
            }
            TotalCount = totalNumber;
            return alllist;
        }

    }
}