using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Services;

namespace IotWebApi
{
    /// <summary>
    /// 规则联动管理(§10.1:CRUD+漏斗指标+试运行干跑)
    /// </summary>
    [ApiController]
    [ControllSort("5-11")]
    public class LinkageRuleController : ControllerBaseApi
    {
        /// <summary>
        /// 规则联动引擎(指标快照/试运行/热重载)
        /// </summary>
        private readonly RuleLinkageService _ruleLinkageService;

        public LinkageRuleController(RuleLinkageService ruleLinkageService)
        {
            _ruleLinkageService = ruleLinkageService;
        }

        /// <summary>
        /// 批量保存(保存后引擎热重载)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string SaveBatch(List<LinkageRule> list)
        {
            Message = "规则联动配置保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<LinkageRule> insertlist = new List<LinkageRule>();
                List<LinkageRule> updatelist = new List<LinkageRule>();
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
                Status = LinkageRuleDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) LinkageRuleDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) LinkageRuleDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    _ruleLinkageService.Reload();
                    Message = "规则联动配置保存成功。";
                }
            }

            return Message;
        }

        /// <summary>
        /// 根据主键删除(删除后引擎热重载)
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string DeleteByPk(long _SnowId)
        {
            Message = "规则联动配置删除失败。";
            Status = LinkageRuleDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                _ruleLinkageService.Reload();
                Message = "规则联动配置删除成功。";
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
        public LinkageRule GetInfoByPk(long _SnowId)
        {
            var entity = LinkageRuleDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        public List<LinkageRule> GetListByPage(ActionPara model)
        {
            var list = LinkageRuleDAO.Instance.GetListByPage(model, ref TotalCount);
            return list;
        }

        /// <summary>
        /// 漏斗指标快照(§10.1工程化:每规则matched/passed/failed/action计数,进程内累计)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public Dictionary<string, RuleLinkageService.RuleMetrics> GetMetrics()
        {
            return _ruleLinkageService.GetMetrics().ToDictionary(t => t.Key.ToString(), t => t.Value);
        }

        /// <summary>
        /// 规则试运行(干跑无副作用:按当前最新值评估时间窗/条件/冷却,不执行动作)
        /// </summary>
        /// <param name="_SnowId">规则主键</param>
        /// <param name="deviceid">模拟触发设备ID(条件裸参数编码按此设备取最新值,0=不代入)</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public RuleLinkageService.LinkageDryRunResult GetDryRun(long _SnowId, int deviceid = 0)
        {
            return _ruleLinkageService.DryRun(_SnowId, deviceid);
        }
    }
}
