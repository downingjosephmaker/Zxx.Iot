using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 月统计表
    /// </summary>
    [ApiController]
    [ControllSort("25-8")]
    public class EventReportMonthController : ControllerBaseApi
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
        public List<EventReportMonthEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = EventReportMonthDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}