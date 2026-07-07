using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Services;

namespace IotWebApi
{
    /// <summary>
    /// 推送策略管理(§7.3:产品/设备/点位三级挂靠,保存后合并引擎热重载)
    /// </summary>
    [ApiController]
    [ControllSort("5-14")]
    public class PushStrategyController : ControllerBaseApi
    {
        /// <summary>
        /// 策略合并引擎(三级合并快照热重载)
        /// </summary>
        private readonly StrategyMergeService _strategyMergeService;

        public PushStrategyController(StrategyMergeService strategyMergeService)
        {
            _strategyMergeService = strategyMergeService;
        }

        /// <summary>
        /// 批量保存(保存后合并引擎热重载)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string SaveBatch(List<PushStrategy> list)
        {
            Message = "推送策略保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<PushStrategy> insertlist = new List<PushStrategy>();
                List<PushStrategy> updatelist = new List<PushStrategy>();
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
                        insertlist.Add(item);
                    }
                    else
                    {
                        updatelist.Add(item);
                    }
                }
                Status = PushStrategyDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) PushStrategyDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) PushStrategyDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    _strategyMergeService.Reload();
                    Message = "推送策略保存成功。";
                }
            }

            return Message;
        }

        /// <summary>
        /// 根据主键删除(删除后合并引擎热重载)
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string DeleteByPk(long _SnowId)
        {
            Message = "推送策略删除失败。";
            Status = PushStrategyDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                _strategyMergeService.Reload();
                Message = "推送策略删除成功。";
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
        [ApiGroup(ApiGroupNames.Basic)]
        public PushStrategy GetInfoByPk(long _SnowId)
        {
            var entity = PushStrategyDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        [ApiGroup(ApiGroupNames.Basic)]
        public List<PushStrategy> GetListByPage(ActionPara model)
        {
            var list = PushStrategyDAO.Instance.GetListByPage(model, ref TotalCount);
            return list;
        }
    }
}
