using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 设备关联表
    /// </summary>
    [ApiController]
    [ControllSort("7-8")]
    public class DeviceRelevanceController : ControllerBaseApi
    {

        /// <summary> 
        /// 批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string SaveBatch(List<DeviceRelevance> list)
        {
            Status = false;
            Message = "设备关联表信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                var devids = list.Select(x => x.DeviceId).Distinct().ToList();
                List<DeviceRelevance> insertlist = new List<DeviceRelevance>();
                List<DeviceRelevance> updatelist = new List<DeviceRelevance>();
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
                        item.UnitId = optmdl.UnitId;
                        insertlist.Add(item);
                    }
                    else
                    {
                        updatelist.Add(item);
                    }
                }
                Status = DeviceRelevanceDAO.Instance.TranAction(() =>
                {
                    DeviceRelevanceDAO.Instance.DeleteBy(t => devids.Contains(t.DeviceId));
                    if (insertlist.Count > 0) DeviceRelevanceDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) DeviceRelevanceDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status) Message = "设备关联表信息保存成功。";

                //下发给中台
                var sublist = list.FindAll(t => t.RelevanceTypeCode.Contains("LDRG"));
                if (sublist.Count > 0)
                {
                    var subdevids = sublist.Select(t => t.DeviceId).Distinct().ToList();
                    var hlist = sublist.Select(t => t.RelevanceId).Distinct().ToList();
                    var datajson = new
                    {
                        EquipIds = subdevids,
                        humanlist = hlist,
                    }.ToJson();
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
            Message = "设备关联表删除失败。";
            Status = DeviceRelevanceDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                Message = "设备关联表信息删除成功。";
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
        public DeviceRelevance GetInfoByPk(long _SnowId)
        {
            var entity = DeviceRelevanceDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
            return entity;
        }

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name="model">通用人感关联模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceRelevance> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = DeviceRelevanceDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}