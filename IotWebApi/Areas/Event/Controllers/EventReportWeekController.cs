using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using IotModel;
using IotWebApi.Areas.Event.Models;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 周统计表
    /// </summary>
    [ApiController]
    [ControllSort("25-7")]
    public class EventReportWeekController : ControllerBaseApi
    {

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name="model">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public List<EventReportWeekEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = EventReportWeekDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}