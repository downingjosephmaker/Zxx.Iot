using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 设备参数表
    /// </summary>
    [ApiController]
    [ControllSort("7-6")]
    public class DeviceParamController : ControllerBaseApi
    {
        /// <summary> 
        /// 批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string SaveBatch(List<DeviceParamEntity> list)
        {
            Status = false;
            Message = "设备参数表信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<DeviceParamEntity> insertlist = new List<DeviceParamEntity>();
                List<DeviceParamEntity> updatelist = new List<DeviceParamEntity>();
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
                Status = DeviceParamDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) DeviceParamDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) DeviceParamDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    Message = "设备参数表信息保存成功。";
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
            Message = "设备参数表删除失败。";
            Status = DeviceParamDAO.Instance.DeleteBy(t => t.DeviceId == _DeviceId);
            if (Status)
            {
                Message = "设备参数表信息删除成功。";
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
        public DeviceParamEntity GetInfoByPk(int _DeviceId)
        {
            var entity = DeviceParamDAO.Instance.GetOneBy(t => t.DeviceId == _DeviceId);
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
        public List<DeviceParamEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = DeviceParamDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}