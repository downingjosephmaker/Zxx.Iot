using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary>
    /// 通用数据池接口
    /// </summary>
    [ApiController]
    [ControllSort("26-1")]
    public class DashDataPoolController : ControllerBaseApi
    {
        /// <summary>
        /// 批量保存（新增 + 更新）
        /// </summary>
        /// <param name="list">数据池配置列表，SnowId=0 为新增，否则为更新</param>
        /// <returns>操作结果消息</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public string SaveBatch(List<DashDataPoolEntity> list)
        {
            Status = false;
            Message = "数据池信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<DashDataPoolEntity> insertlist = new List<DashDataPoolEntity>();
                List<DashDataPoolEntity> updatelist = new List<DashDataPoolEntity>();
                DateTime time = DateTime.Now;
                foreach (var item in list)
                {
                    item.UpdateId = optmdl.UserID;
                    item.UpdateTime = time.ToDateTimeString();
                    item.UpdateName = optmdl.UserName;
                    if (item.SnowId == 0)
                    {
                        item.SnowId = SnowModel.Instance.NewId();
                        item.TenantId = optmdl.UnitId;
                        item.CreateId = optmdl.UserID;
                        item.CreateTime = time.ToDateTimeString();
                        item.CreateName = optmdl.UserName;
                        insertlist.Add(item);
                    }
                    else
                    {
                        updatelist.Add(item);
                    }
                    if (item.ExpandRequestHeaders.IsZxxAny())
                        item.RequestHeaders = JsonConvert.SerializeObject(item.ExpandRequestHeaders);
                    if (item.ExpandRequestParams.IsZxxAny())
                        item.RequestParams = JsonConvert.SerializeObject(item.ExpandRequestParams);
                    if (item.ExpandResponseMapping != null)
                        item.ResponseMapping = JsonConvert.SerializeObject(item.ExpandResponseMapping);
                }
                Status = DashDataPoolDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) DashDataPoolDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) DashDataPoolDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status) Message = "数据池信息保存成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="_SnowId">主键ID</param>
        /// <returns>操作结果消息</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public string DeleteByPk(long _SnowId)
        {
            Status = false;
            Message = "数据池删除失败。";
            Status = DashDataPoolDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status) Message = "数据池删除成功。";
            return Message;
        }

        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name="_SnowId">主键ID</param>
        /// <returns>数据池配置实体</returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public DashDataPoolEntity GetInfoByPk(long _SnowId)
        {
            var entity = DashDataPoolDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
            return entity;
        }

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name="model">分页查询参数</param>
        /// <returns>数据池配置分页列表</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public List<DashDataPoolEntity> GetListByPage(ActionPara model)
        {
            var optmdl = Request.GetToken();
            if (model.sconlist == null) model.sconlist = new List<SelectCondition>();
            model.sconlist.Add(new SelectCondition { ParamName = "UnitId", ParamType = "=", ParamValue = optmdl.UnitId.ToString() });
            int totalNumber = 0;
            var list = DashDataPoolDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 测试数据源连接
        /// </summary>
        /// <param name="_SnowId">数据池配置主键ID</param>
        /// <returns>连接测试结果消息</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public string TestConnection(long _SnowId)
        {
            Status = false; Message = "连接测试失败。";
            var entity = DashDataPoolDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
            if (entity == null) { Message = "数据源不存在。"; return Message; }
            if (string.IsNullOrEmpty(entity.DataUrl)) { Message = "数据源 URL 未配置。"; return Message; }
            try
            {
                string response = string.Empty;
                if (entity.RequestMethod == HttpMethodEnum.GET) response = HttpHelper.ManGet(entity.DataUrl);
                else if (entity.RequestMethod == HttpMethodEnum.POST)
                {
                    if (entity.RequestBody.IsZxxNullOrEmpty()) response = HttpHelper.ManPost(entity.DataUrl);
                    else response = HttpHelper.ManPostBodyJson(entity.DataUrl, entity.RequestBody);
                }
                Status = !response.IsZxxNullOrEmpty();
                Message = Status ? "连接成功。" : "连接失败。";
            }
            catch (Exception ex) { Message = $"连接异常：{ex.Message}"; }
            return Message;
        }

        /// <summary>
        /// 刷新数据源数据
        /// </summary>
        /// <param name="_SnowId">数据池配置主键ID</param>
        /// <returns>刷新结果消息</returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public string RefreshData(long _SnowId)
        {
            Status = false; Message = "刷新失败。";
            var entity = DashDataPoolDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
            if (entity == null) { Message = "数据源不存在。"; return Message; }
            if (!entity.IsEnabled) { Message = "数据源已禁用。"; return Message; }
            TestConnection(_SnowId);
            Status = true;
            Message = "数据已刷新。";
            return Message;
        }
    }
}
