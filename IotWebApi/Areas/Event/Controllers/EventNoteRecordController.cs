using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi
{
    /// <summary> 
    /// 通知日志
    /// </summary>
    [ApiController]
    [ControllSort("25-20")]
    public class EventNoteRecordController : ControllerBaseApi
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
        public EventNoteRecord GetInfoByPk(long _SnowId)
        {
            var entity = EventNoteRecordDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
            return entity;
        }

        /// <summary>
        /// 分页查询短信记录
        /// </summary>
        /// <param name="model">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public List<EventNoteRecord> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = EventNoteRecordDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}