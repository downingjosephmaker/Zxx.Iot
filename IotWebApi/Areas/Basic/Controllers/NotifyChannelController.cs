using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Services;

namespace IotWebApi
{
    /// <summary>
    /// 通知渠道管理(§9.5:邮件/Webhook/钉钉/企微/短信预留+升级梯队,保存后通知服务热重载)
    /// </summary>
    [ApiController]
    [ControllSort("5-16")]
    public class NotifyChannelController : ControllerBaseApi
    {
        /// <summary>
        /// 告警通知服务(渠道快照热重载)
        /// </summary>
        private readonly AlarmNotifyService _alarmNotifyService;

        public NotifyChannelController(AlarmNotifyService alarmNotifyService)
        {
            _alarmNotifyService = alarmNotifyService;
        }

        /// <summary>
        /// 批量保存(保存后通知服务热重载)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string SaveBatch(List<NotifyChannel> list)
        {
            Message = "通知渠道保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<NotifyChannel> insertlist = new List<NotifyChannel>();
                List<NotifyChannel> updatelist = new List<NotifyChannel>();
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
                Status = NotifyChannelDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) NotifyChannelDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) NotifyChannelDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    _alarmNotifyService.Reload();
                    Message = "通知渠道保存成功。";
                }
            }

            return Message;
        }

        /// <summary>
        /// 根据主键删除(删除后通知服务热重载)
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string DeleteByPk(long _SnowId)
        {
            Message = "通知渠道删除失败。";
            Status = NotifyChannelDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                _alarmNotifyService.Reload();
                Message = "通知渠道删除成功。";
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
        public NotifyChannel GetInfoByPk(long _SnowId)
        {
            var entity = NotifyChannelDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        public List<NotifyChannel> GetListByPage(ActionPara model)
        {
            var list = NotifyChannelDAO.Instance.GetListByPage(model, ref TotalCount);
            return list;
        }
    }
}
