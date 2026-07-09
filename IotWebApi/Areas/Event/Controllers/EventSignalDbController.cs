using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary>
    /// 状态变化日志（关系型分表版本，按周分表）
    /// </summary>
    [ApiController]
    [ControllSort("25-19")]
    public class EventSignalDbController : ControllerBaseApi
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
        public List<EventSignal> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = SysCommonDAO<EventSignal>.Instance.GetListByPage(model, ref totalNumber);
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
