using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 用户日志
    /// </summary>
    [ApiController]
    [ControllSort("25-23")]
    public class SysuserLogController : ControllerBaseApi
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
        public SysuserLog GetInfoByPk(long _SnowId)
        {
            var entity = SysuserLogDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        public List<SysuserLog> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = SysuserLogDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}