using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Areas.Device.Models;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 设备告警配置
    /// </summary>
    [ApiController]
    [ControllSort("7-11")]
    public class DeviceAlarmConfigController : ControllerBaseApi
    {
        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string DeleteByPk(long _SnowId)
        {
            Status = false;
            Message = "设备告警配置删除失败。";
            Status = DeviceAlarmConfigDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                Message = "设备告警配置信息删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public DeviceAlarmConfig GetInfoByPk(long _SnowId)
        {
            var entity = DeviceAlarmConfigDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        public List<DeviceAlarmConfig> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = DeviceAlarmConfigDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

        #region 自定义报警

        /// <summary>
        /// 阈值管理：根据条件获取设备告警统计列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceAlarmInfo> GetAlarmConfigDeviceList(AlarmConfigSelect model)
        {
            List<DeviceAlarmInfo> entitylist = new List<DeviceAlarmInfo>();
            var optmdl = Request.GetToken();
            List<DeviceInfo> devlist = new List<DeviceInfo>();
            List<DeviceAlarmConfig> alarmconfiglist = new List<DeviceAlarmConfig>();
            if (model.buildid > 0)
            {
                var buildlist = BuildInfoDAO.Instance.GetListBy(t => t.UnitId == optmdl.UnitId);
                if (!buildlist.IsZxxAny()) return entitylist;
                var _buildlist = buildlist.FindAll(t => t.FullCode.Contains($"|{model.buildid}|"));
                if (!_buildlist.IsZxxAny()) return entitylist;
                var buildids = _buildlist.Select(t => t.BuildId).ToList();
                var _devlist = DeviceInfoDAO.Instance.GetListBy(t => t.DeviceTypeCode == model.typecode && buildids.Contains(t.BuildId));
                if (_devlist.IsZxxAny()) devlist.AddRange(_devlist);
                var _alarmconfiglist = DeviceAlarmConfigDAO.Instance.GetListBy(t => t.DeviceTypeCode == model.typecode && buildids.Contains(t.BuildId));
                if (_alarmconfiglist.IsZxxAny()) alarmconfiglist.AddRange(_alarmconfiglist);
            }
            else
            {
                var _devlist = DeviceInfoDAO.Instance.GetListBy(t => t.DeviceTypeCode == model.typecode && t.UnitId == optmdl.UnitId);
                if (_devlist.IsZxxAny()) devlist.AddRange(_devlist);
                var _alarmconfiglist = DeviceAlarmConfigDAO.Instance.GetListBy(t => t.DeviceTypeCode == model.typecode && t.UnitId == optmdl.UnitId);
                if (_alarmconfiglist.IsZxxAny()) alarmconfiglist.AddRange(_alarmconfiglist);
            }
            if (devlist.Count > 0)
            {
                foreach (var dev in devlist.OrderBy(t => t.DeviceId))
                {
                    DeviceAlarmInfo devdp = new DeviceAlarmInfo
                    {
                        DeviceId = dev.DeviceId,
                        DeviceName = dev.FullName,
                    };

                    if (alarmconfiglist.Count > 0)
                    {
                        var _parconfiglist = alarmconfiglist.FindAll(t => t.DeviceId == devdp.DeviceId);
                        if (_parconfiglist.IsZxxAny())
                        {
                            devdp.OneAlarmNum = _parconfiglist.Count(t => t.ConfigType == 0);
                            devdp.MoreAlarmNum = _parconfiglist.Count(t => t.ConfigType == 1);
                        }
                    }
                    entitylist.Add(devdp);
                }
            }
            TotalCount = entitylist.Count;
            int startindex = (model.page - 1) * model.pagesize;
            if (TotalCount > startindex)
            {
                var _list = entitylist.Skip(startindex).Take(model.pagesize).ToList();
                return _list;
            }
            return new List<DeviceAlarmInfo>();
        }

        /// <summary>
        /// 阈值管理：根据设备ID获取组合参数报警信息
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceParamDb> GetMoreAlarmConfigList(int deviceId)
        {
            List<DeviceParamDb> entitylist = new List<DeviceParamDb>();
            var parconfiglist = DeviceAlarmConfigDAO.Instance.GetListBy(t => t.DeviceId == deviceId && t.ConfigType == 1);
            if (parconfiglist.IsZxxAny())
            {
                var alarmConfigList = AlarmConfigDAO.Instance.GetList();
                foreach (var config in parconfiglist.OrderBy(t => t.SnowId))
                {
                    DeviceParamDb devdp = new DeviceParamDb();
                    config.CopyTypeValue(devdp);
                    if (alarmConfigList.IsZxxAny())
                    {
                        var alarmconfig = alarmConfigList.Find(t => t.Id == devdp.AlarmConfigId);
                        if (alarmconfig != null)
                        {
                            devdp.AlarmConfigEventType = alarmconfig.EventType;
                        }
                    }
                    entitylist.Add(devdp);
                }
            }
            TotalCount = entitylist.Count;
            return entitylist;
        }

        /// <summary>
        /// 阈值管理：根据设备ID获取单个告警统计列表(所有参数)
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<ParamAlarmInfo> GetAlarmConfigParamList(int deviceId)
        {
            List<ParamAlarmInfo> entitylist = new List<ParamAlarmInfo>();
            var deviceparam = DeviceParamDAO.Instance.GetOneBy(t => t.DeviceId == deviceId);
            if (deviceparam != null)
            {
                //自定义报警：仅展示类型参数中标记为 IsCustomAlarm 的参数，避免前端展示全部参数
                var customCodes = DeviceTypeParamDAO.Instance
                    .GetListBy(t => t.DeviceTypeCode == deviceparam.DeviceTypeCode && t.IsCustomAlarm)
                    .Select(t => t.ParamCode).Distinct().ToList();
                var parconfiglist = DeviceAlarmConfigDAO.Instance.GetListBy(t => t.DeviceId == deviceId && t.ConfigType == 0);
                foreach (var param in deviceparam.ExpandObjects)
                {
                    if (customCodes.IsZxxAny() && !customCodes.Contains(param.ParamCode)) continue;
                    ParamAlarmInfo devdp = new ParamAlarmInfo();
                    param.CopyTypeValue(devdp);
                    int num = 0; int qy = 0; int unqy = 0;
                    if (parconfiglist.IsZxxAny())
                    {
                        num = parconfiglist.Count(t => t.ParamCode == devdp.ParamCode);
                        qy = parconfiglist.Count(t => t.ParamCode == devdp.ParamCode && t.IsFormulaEnable == 1);
                        unqy = parconfiglist.Count(t => t.ParamCode == devdp.ParamCode && t.IsFormulaEnable == 0);
                    }
                    devdp.AlarmText = $"无告警配置";
                    if (num > 0) devdp.AlarmText = $"共{num}个告警配置；启用{qy}个，未启用{unqy}个";
                    entitylist.Add(devdp);
                }
            }
            TotalCount = entitylist.Count;
            return entitylist;
        }

        /// <summary>
        /// 阈值管理：根据设备ID和参数编码获取参数报警信息
        /// </summary>
        /// <param name="deviceid">设备ID</param>
        /// <param name="paramcode">参数编码</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceParamDb> GetParamAlarmConfigList(int deviceid, string paramcode)
        {
            List<DeviceParamDb> entitylist = new List<DeviceParamDb>();
            var parconfiglist = DeviceAlarmConfigDAO.Instance.GetListBy(t => t.DeviceId == deviceid && t.ParamCode == paramcode);
            if (parconfiglist.IsZxxAny())
            {
                var alarmConfigList = AlarmConfigDAO.Instance.GetList();
                foreach (var config in parconfiglist.OrderBy(t => t.SnowId))
                {
                    DeviceParamDb devdp = new DeviceParamDb();
                    config.CopyTypeValue(devdp);
                    if (alarmConfigList.IsZxxAny())
                    {
                        var alarmconfig = alarmConfigList.Find(t => t.Id == devdp.AlarmConfigId);
                        if (alarmconfig != null)
                        {
                            devdp.AlarmConfigEventType = alarmconfig.EventType;
                        }
                    }
                    entitylist.Add(devdp);
                }
            }
            TotalCount = entitylist.Count;
            return entitylist;
        }

        /// <summary>
        /// 阈值管理：根据告警配置ID集合批量更新参数报警信息的启用状态
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string UpdateBatchEnableByDevice(ParamAlarmConfigEnable model)
        {
            Status = false;
            Message = "参数报警信息的启用状态更新失败。";
            if (model.snowIds.IsZxxAny())
            {
                List<DeviceAlarmConfig> parconfiglist = new List<DeviceAlarmConfig>();
                model.snowIds.ForEach(sid =>
                {
                    DeviceAlarmConfig config = new DeviceAlarmConfig
                    {
                        SnowId = sid,
                        IsFormulaEnable = model.isEnable
                    };
                    parconfiglist.Add(config);

                });
                Status = DeviceAlarmConfigDAO.Instance.UpdateColumns(parconfiglist, it => new { it.IsFormulaEnable });
            }
            if (Status) Message = "参数报警信息的启用状态更新成功。";
            return Message;
        }

        /// <summary>
        /// 阈值管理：参数报警配置信息保存
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string Save(DeviceAlarmConfig model)
        {
            Status = false;
            Message = "参数报警设置信息保存失败。";
            var optmdl = Request.GetToken();
            DateTime time = DateTime.Now;
            if (model.SnowId == 0)
            {
                var paclist = DeviceAlarmConfigDAO.Instance.GetListBy(t => t.DeviceId == model.DeviceId);
                if (paclist.IsZxxAny() && paclist.Any(t => t.JisuanFormula == model.JisuanFormula))
                {
                    Message = "参数报警设置公式出现重复，请重新操作。";
                    return Message;
                }
                var device = SysCommonDAO<DeviceInfo>.Instance.GetOneBy(t => t.DeviceId == model.DeviceId);
                if (device == null) return Message;
                DeviceAlarmConfig config = new DeviceAlarmConfig();
                model.CopyTypeValue(config);
                device.CopyTypeValue(config);
                config.CreateId = optmdl.UserID;
                config.CreateTime = time.ToDateTimeString();
                config.CreateName = optmdl.UserName;
                config.UnitId = optmdl.UnitId;
                config.SnowId = SnowModel.Instance.NewId();
                Status = DeviceAlarmConfigDAO.Instance.Insert(config);
            }
            else
            {
                DeviceAlarmConfig config = new DeviceAlarmConfig
                {
                    SnowId = model.SnowId,
                    FormulaName = model.FormulaName,
                    IsFormulaEnable = model.IsFormulaEnable,
                    JisuanFormula = model.JisuanFormula,
                    AlarmConfigId = model.AlarmConfigId,
                    UpdateId = optmdl.UserID,
                    UpdateTime = time.ToDateTimeString(),
                    UpdateName = optmdl.UserName,
                };
                Status = DeviceAlarmConfigDAO.Instance.UpdateColumns(config, it => new
                {
                    it.FormulaName,
                    it.IsFormulaEnable,
                    it.JisuanFormula,
                    it.AlarmConfigId,
                    it.UpdateId,
                    it.UpdateTime,
                    it.UpdateName,
                });
            }
            if (Status)
            {
                Message = "设备参数阈值信息保存成功。";
            }

            return Message;
        }

        #endregion
    }
}