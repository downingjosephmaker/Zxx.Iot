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