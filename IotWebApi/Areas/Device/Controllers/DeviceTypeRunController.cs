using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi
{
    /// <summary> 
    /// 单位设备大类管理
    /// </summary>
    [ApiController]
    [ControllSort("7-20")]
    public class DeviceTypeRunController : ControllerBaseApi
    {
        /// <summary> 
        /// 批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [ApiGroup(ApiGroupNames.Device)]
        public string SaveBatch(List<DeviceTypeRun> list)
        {
            Message = "单位设备大类信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                DateTime time = DateTime.Now;
                List<DeviceTypeRun> insertlist = new List<DeviceTypeRun>();
                List<DeviceTypeRun> updatelist = new List<DeviceTypeRun>();
                foreach (var item in list)
                {
                    item.UnitId = optmdl.UnitId;
                    if(item.SnowId > 0)
                    {
                        updatelist.Add(item);
                    }
                    else
                    {
                        insertlist.Add(item);
                    }
                }
                Status = DeviceTypeRunDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) DeviceTypeRunDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) DeviceTypeRunDAO.Instance.UpdateRange(updatelist);
                });
                if (Status)
                {
                    Message = "单位设备大类信息保存成功。";
                }
            }

            return Message;
        }

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="snowId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string DeleteByPk(long snowId)
        {
            Message = "单位设备大类删除失败。";
            Status = DeviceTypeRunDAO.Instance.DeleteBy(t => t.SnowId == snowId);
            if (Status) Message = "单位设备大类删除成功。";
            return Message;
        }

        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name="_Id">主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public DeviceTypeRun GetInfoByPk(int _Id)
        {
            var entity = DeviceTypeRunDAO.Instance.GetOneBy(t => t.SnowId == _Id);
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
        public List<DeviceTypeRun> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = DeviceTypeRunDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}