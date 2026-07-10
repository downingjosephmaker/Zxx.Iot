using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Areas.Device.Models;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 设备类型参数报警配置表
    /// </summary>
    [ApiController]
    [ControllSort("7-3")]
    public class DeviceTypeAlarmConfigController : ControllerBaseApi
    {
        /// <summary> 
        /// 设备类型参数报警配置表批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string AddBatch(List<DeviceTypeAlarmConfig> list)
        {
            Status = false;
            Message = "设备类型参数报警配置表信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<DeviceTypeAlarmConfig> insertlist = new List<DeviceTypeAlarmConfig>();
                List<DeviceAlarmConfig> configlist = new List<DeviceAlarmConfig>();
                DateTime time = DateTime.Now;
                var devices = SysCommonDAO<DeviceInfo>.Instance.GetListBy(t => t.DeviceTypeCode == list[0].DeviceTypeCode);
                if (devices == null) return Message;
                foreach (var item in list)
                {
                    if (item.SnowId == 0)
                    {
                        item.SnowId = SnowModel.Instance.NewId();
                        item.CreateId = optmdl.UserID;
                        item.CreateTime = time.ToDateTimeString();
                        item.CreateName = optmdl.UserName;
                        item.TenantId = optmdl.TenantId;
                        item.UpdateId = optmdl.UserID;
                        item.UpdateTime = time.ToDateTimeString();
                        item.UpdateName = optmdl.UserName;
                        insertlist.Add(item);

                        foreach (var device in devices)
                        {
                            DeviceAlarmConfig config = new DeviceAlarmConfig();
                            item.CopyTypeValue(config);
                            device.CopyTypeValue(config);
                            config.SnowId = SnowModel.Instance.NewId();
                            config.TypeSnowId = item.SnowId;
                            configlist.Add(config);
                        }
                    }
                }
                Status = DeviceTypeAlarmConfigDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) DeviceTypeAlarmConfigDAO.Instance.InsertRange(insertlist);
                    if (configlist.Count > 0) DeviceAlarmConfigDAO.Instance.InsertRange(configlist);
                });
                if (Status)
                {
                    Message = "设备类型参数报警配置表信息保存成功。";
                }
            }
            return Message;
        }

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
            Message = "设备类型参数报警配置表删除失败。";
            Status = DeviceTypeAlarmConfigDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                Message = "设备类型参数报警配置表信息删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据设备类型批量单个参数告警信息(删除用)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string DelYuZhiBatch(TypeDelAlarmConfig model)
        {
            Status = false;
            Message = "删除失败。";
            if (model.snowIds.IsZxxAny())
            {
                Status = DeviceTypeAlarmConfigDAO.Instance.TranAction(() =>
                {
                    DeviceTypeAlarmConfigDAO.Instance.DeleteBy(t => model.snowIds.Contains(t.SnowId));
                    DeviceAlarmConfigDAO.Instance.DeleteBy(t => model.snowIds.Contains(t.TypeSnowId));
                });
            }

            if (Status) Message = "删除成功。";

            return Message;
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
        public List<DeviceTypeAlarmConfig> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = DeviceTypeAlarmConfigDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}