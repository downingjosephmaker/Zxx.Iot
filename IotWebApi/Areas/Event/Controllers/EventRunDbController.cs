using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary>
    /// 运行日志（关系型分表版本，按周分表）
    /// </summary>
    [ApiController]
    [ControllSort("25-16")]
    public class EventRunDbController : ControllerBaseApi
    {

        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public EventRun GetInfoByPk(long _SnowId)
        {
            var entity = SysCommonDAO<EventRun>.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        [ApiGroup(ApiGroupNames.Event)]
        public List<EventRun> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = SysCommonDAO<EventRun>.Instance.GetListByPage(model, ref totalNumber);
            if (list.Count > 0)
            {
                list.ForEach(t =>
                {
                    t.DeviceName = t.DeviceName.BeautifyFullName();
                });
            }
            TotalCount = totalNumber;
            return list;
        }

    }
}
