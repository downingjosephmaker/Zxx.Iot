using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Services;

namespace IotWebApi
{
    /// <summary>
    /// 告警屏蔽规则管理(§9.4:六种scope×三模式×三动作,保存后屏蔽引擎热重载)
    /// </summary>
    [ApiController]
    [ControllSort("5-15")]
    public class AlarmMaskController : ControllerBaseApi
    {
        /// <summary>
        /// 告警屏蔽引擎(规则快照热重载)
        /// </summary>
        private readonly AlarmMaskService _alarmMaskService;

        public AlarmMaskController(AlarmMaskService alarmMaskService)
        {
            _alarmMaskService = alarmMaskService;
        }

        /// <summary>
        /// 批量保存(保存后屏蔽引擎热重载)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string SaveBatch(List<AlarmMask> list)
        {
            Message = "告警屏蔽规则保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<AlarmMask> insertlist = new List<AlarmMask>();
                List<AlarmMask> updatelist = new List<AlarmMask>();
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
                Status = AlarmMaskDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) AlarmMaskDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) AlarmMaskDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    _alarmMaskService.Reload();
                    Message = "告警屏蔽规则保存成功。";
                }
            }

            return Message;
        }

        /// <summary>
        /// 根据主键删除(删除后屏蔽引擎热重载)
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string DeleteByPk(long _SnowId)
        {
            Message = "告警屏蔽规则删除失败。";
            Status = AlarmMaskDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                _alarmMaskService.Reload();
                Message = "告警屏蔽规则删除成功。";
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
        public AlarmMask GetInfoByPk(long _SnowId)
        {
            var entity = AlarmMaskDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        public List<AlarmMask> GetListByPage(ActionPara model)
        {
            var list = AlarmMaskDAO.Instance.GetListByPage(model, ref TotalCount);
            return list;
        }
    }
}
