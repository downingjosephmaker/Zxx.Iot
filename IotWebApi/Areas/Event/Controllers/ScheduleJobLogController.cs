using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi.Areas.Admin.Controllers
{
    /// <summary>
    /// 任务调度日志
    /// </summary>
    [ApiController]
    [ControllSort("25-20")]
    public class ScheduleJobLogController : ControllerBaseApi
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
        public ScheduleJobLog GetInfoByPk(long _SnowId)
        {
            var entity = ScheduleJobLogDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
            return entity;
        }

        /// <summary>
        /// 获取任务日志列表
        /// </summary>
        /// <param name="model">参数模型</param>
        /// <returns>任务日志列表</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public List<ScheduleJobLog> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = ScheduleJobLogDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}
