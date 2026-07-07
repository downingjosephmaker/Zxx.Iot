using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Services;

namespace IotWebApi
{
    /// <summary>
    /// 北向转发目的地管理(§10.2:CRUD+测试连接+样例报文干跑+队列水位)
    /// </summary>
    [ApiController]
    [ControllSort("5-12")]
    public class NorthboundSinkController : ControllerBaseApi
    {
        /// <summary>
        /// 北向转发服务(水位快照/测试连接/热重载)
        /// </summary>
        private readonly NorthboundForwardService _northboundForwardService;

        public NorthboundSinkController(NorthboundForwardService northboundForwardService)
        {
            _northboundForwardService = northboundForwardService;
        }

        /// <summary>
        /// 批量保存(保存后转发器热重载)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string SaveBatch(List<NorthboundSink> list)
        {
            Message = "北向目的地配置保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<NorthboundSink> insertlist = new List<NorthboundSink>();
                List<NorthboundSink> updatelist = new List<NorthboundSink>();
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
                Status = NorthboundSinkDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) NorthboundSinkDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) NorthboundSinkDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    _northboundForwardService.Reload();
                    Message = "北向目的地配置保存成功。";
                }
            }

            return Message;
        }

        /// <summary>
        /// 根据主键删除(删除后转发器热重载)
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string DeleteByPk(long _SnowId)
        {
            Message = "北向目的地配置删除失败。";
            Status = NorthboundSinkDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                _northboundForwardService.Reload();
                Message = "北向目的地配置删除成功。";
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
        public NorthboundSink GetInfoByPk(long _SnowId)
        {
            var entity = NorthboundSinkDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        public List<NorthboundSink> GetListByPage(ActionPara model)
        {
            var list = NorthboundSinkDAO.Instance.GetListByPage(model, ref TotalCount);
            return list;
        }

        /// <summary>
        /// 队列水位快照(每目的地:在线状态/内存积压/落盘积压/累计计数)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public List<NorthboundForwardService.SinkStatus> GetStatus()
        {
            return _northboundForwardService.GetStatus();
        }

        /// <summary>
        /// 样例报文预览(§10.2向导④:干跑不发送,展示将发出的JSON)
        /// </summary>
        /// <param name="_SnowId">目的地主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public NorthboundForwardService.SinkTestResult GetSample(long _SnowId)
        {
            return _northboundForwardService.BuildSample(_SnowId);
        }

        /// <summary>
        /// 测试连接并实际发送一条样例报文
        /// </summary>
        /// <param name="_SnowId">目的地主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public async Task<NorthboundForwardService.SinkTestResult> PostTestSend(long _SnowId)
        {
            return await _northboundForwardService.TestSendAsync(_SnowId);
        }
    }
}
