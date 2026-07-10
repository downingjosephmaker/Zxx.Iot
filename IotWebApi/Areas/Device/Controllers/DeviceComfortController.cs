using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 空调舒适度
    /// </summary>
    [ApiController]
    [ControllSort("7-10")]
    public class DeviceComfortController : ControllerBaseApi
    {
        /// <summary> 
        /// 批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string SaveBatch(List<DeviceComfort> list)
        {
            Status = false;
            Message = "空调舒适度信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<DeviceComfort> insertlist = new List<DeviceComfort>();
                List<DeviceComfort> updatelist = new List<DeviceComfort>();
                DateTime time = DateTime.Now;
                foreach (var item in list)
                {
                    item.UpdateId = optmdl.UserID;
                    item.UpdateTime = time.ToDateTimeString();
                    item.UpdateName = optmdl.UserName;
                    if (item.SnowId == 0)
                    {
                        item.SnowId = SnowModel.Instance.NewId();
                        item.CreateId = optmdl.UserID;
                        item.CreateTime = time.ToDateTimeString();
                        item.CreateName = optmdl.UserName;
                        item.TenantId = optmdl.TenantId;
                        insertlist.Add(item);
                    }
                    else
                    {
                        updatelist.Add(item);
                    }
                }
                Status = DeviceComfortDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) DeviceComfortDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) DeviceComfortDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    Message = "空调舒适度信息保存成功。";
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
            Message = "空调舒适度删除失败。";
            Status = DeviceComfortDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                Message = "空调舒适度信息删除成功。";
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
        public DeviceComfort GetInfoByPk(long _SnowId)
        {
            var entity = DeviceComfortDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        public List<DeviceComfort> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = DeviceComfortDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}