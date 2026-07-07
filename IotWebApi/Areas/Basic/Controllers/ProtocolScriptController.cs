using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotDriverCore;
using IotModel;
using IotWebApi.Services;

namespace IotWebApi
{
    /// <summary>
    /// JS协议脚本管理(§6.4:CRUD+版本历史+试运行干跑;脚本默认禁用需显式启用)
    /// </summary>
    [ApiController]
    [ControllSort("5-17")]
    public class ProtocolScriptController : ControllerBaseApi
    {
        /// <summary>
        /// 脚本管理服务(沙箱缓存热切换/试运行)
        /// </summary>
        private readonly ProtocolScriptService _protocolScriptService;

        public ProtocolScriptController(ProtocolScriptService protocolScriptService)
        {
            _protocolScriptService = protocolScriptService;
        }

        /// <summary>
        /// 批量保存(版本号自增+写历史快照,保存后沙箱缓存热重载)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string SaveBatch(List<ProtocolScript> list)
        {
            Message = "协议脚本保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<ProtocolScript> insertlist = new List<ProtocolScript>();
                List<ProtocolScript> updatelist = new List<ProtocolScript>();
                List<ProtocolScriptHistory> historylist = new List<ProtocolScriptHistory>();
                DateTime time = DateTime.Now;
                foreach (var item in list)
                {
                    item.UpdateId = optmdl.UserID;
                    item.UpdateTime = time.ToDateTimeString();
                    item.UpdateName = optmdl.UserName;
                    if (item.SnowId == 0)
                    {
                        item.SnowId = SnowModel.Instance.NewId();
                        item.Version = 1;
                        item.CreateId = optmdl.UserID;
                        item.CreateTime = time.ToDateTimeString();
                        item.CreateName = optmdl.UserName;
                        insertlist.Add(item);
                    }
                    else
                    {
                        // 版本号以库内为准自增,防客户端回传陈旧版本号
                        var exist = ProtocolScriptDAO.Instance.GetOneBy(t => t.SnowId == item.SnowId);
                        item.Version = (exist?.Version ?? 0) + 1;
                        updatelist.Add(item);
                    }
                    historylist.Add(new ProtocolScriptHistory
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        ScriptId = item.SnowId,
                        Version = item.Version,
                        ScriptContent = item.ScriptContent,
                        CreateId = optmdl.UserID,
                        CreateTime = time.ToDateTimeString(),
                        CreateName = optmdl.UserName,
                        UpdateId = optmdl.UserID,
                        UpdateTime = time.ToDateTimeString(),
                        UpdateName = optmdl.UserName
                    });
                }
                Status = ProtocolScriptDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) ProtocolScriptDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) ProtocolScriptDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    if (historylist.Count > 0) ProtocolScriptHistoryDAO.Instance.InsertRange(historylist);
                    _protocolScriptService.Reload();
                    Message = "协议脚本保存成功。";
                }
            }

            return Message;
        }

        /// <summary>
        /// 根据主键删除(连带删除版本历史,删除后沙箱缓存热重载)
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string DeleteByPk(long _SnowId)
        {
            Message = "协议脚本删除失败。";
            Status = ProtocolScriptDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                ProtocolScriptHistoryDAO.Instance.DeleteBy(t => t.ScriptId == _SnowId);
                _protocolScriptService.Reload();
                Message = "协议脚本删除成功。";
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
        public ProtocolScript GetInfoByPk(long _SnowId)
        {
            var entity = ProtocolScriptDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        public List<ProtocolScript> GetListByPage(ActionPara model)
        {
            var list = ProtocolScriptDAO.Instance.GetListByPage(model, ref TotalCount);
            return list;
        }

        /// <summary>
        /// 查询脚本版本历史(版本倒序,供diff对比与回滚取内容)
        /// </summary>
        /// <param name="_SnowId">脚本主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public List<ProtocolScriptHistory> GetHistoryList(long _SnowId)
        {
            var list = ProtocolScriptHistoryDAO.Instance.GetListBy(t => t.ScriptId == _SnowId);
            return list?.OrderByDescending(t => t.Version).ToList() ?? new List<ProtocolScriptHistory>();
        }

        /// <summary>
        /// 脚本试运行(§6.4调试设计:输入hex帧+模拟context,干跑无副作用,
        /// 返回结果JSON+console日志+耗时;草稿内容优先,为空按脚本ID取库内内容)
        /// </summary>
        /// <param name="model">试运行参数</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public ScriptRunResult PostDryRun(ProtocolScriptDryRunPara model)
        {
            if (model == null) return new ScriptRunResult { Error = "参数为空" };
            return _protocolScriptService.DryRun(model.ScriptId, model.ScriptContent, model.FuncName,
                model.InputHex, model.InputJson, model.ContextJson);
        }
    }

    /// <summary>
    /// 脚本试运行参数
    /// </summary>
    public class ProtocolScriptDryRunPara
    {
        /// <summary>
        /// 脚本主键(0=直接用草稿内容)
        /// </summary>
        public long ScriptId { get; set; }
        /// <summary>
        /// 草稿脚本内容(非空时优先于库内内容,支持编辑器未保存调试)
        /// </summary>
        public string ScriptContent { get; set; } = "";
        /// <summary>
        /// 函数名(decode/encode/splitFrames,默认decode)
        /// </summary>
        public string FuncName { get; set; } = "decode";
        /// <summary>
        /// 输入帧hex(decode/splitFrames用)
        /// </summary>
        public string InputHex { get; set; } = "";
        /// <summary>
        /// 输入命令JSON(encode用)
        /// </summary>
        public string InputJson { get; set; } = "";
        /// <summary>
        /// 模拟上下文JSON(deviceKey/variables等)
        /// </summary>
        public string ContextJson { get; set; } = "";
    }
}
