using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Areas.Basic.Models;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 建筑信息
    /// </summary>
    [ApiController]
    [ControllSort("5-13")]
    public class BuildinfoController : ControllerBaseApi
    {
        /// <summary>
        /// 建筑新增
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string Insert(BuildInfo info)
        {
            Status = false;
            Message = "建筑表信息保存失败。";
            var optmdl = Request.GetToken();
            info.CreateId = optmdl.UserID;
            info.CreateTime = DateTime.Now.ToDateTimeString();
            info.CreateName = optmdl.UserName;
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            info.UnitId = optmdl.UnitId;
            Status = BuildInfoDAO.Instance.Insert(info);
            if (Status) Message = "建筑信息新增成功。";
            return Message;
        }

        /// <summary>
        /// 建筑修改
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string Update(BuildInfo info)
        {
            Status = false;
            Message = "建筑信息更新失败。";
            var optmdl = Request.GetToken();
            var temp = BuildInfoDAO.Instance.GetOneBy(t => t.BuildId == info.BuildId);
            if (temp == null)
            {
                Message = $"建筑[{info.BuildName}]不存在";
                return Message;
            }
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            info.UnitId = optmdl.UnitId;
            Status = BuildInfoDAO.Instance.Update(info);
            if (Status) Message = "建筑信息更新成功。";
            return Message;
        }

        /// <summary>
        /// 根据建筑ID删除建筑信息(包含子建筑)
        /// </summary>
        /// <param name="id">建筑ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string Delete(int id)
        {
            Status = false;
            Message = "建筑信息删除失败。";
            Status = BuildInfoDAO.Instance.DeleteBy(t => t.FullCode.Contains($"|{id}|"));
            if (Status) Message = "建筑信息删除成功。";
            return Message;
        }

        /// <summary>
        /// 根据建筑ID查询单条数据
        /// </summary>
        /// <param name="buildid">建筑ID</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public BuildInfoEntity GetInfoByPk(int buildid)
        {
            var entity = BuildInfoDAO.Instance.GetEntityOneBy(t => t.BuildId == buildid);
            return entity ?? new BuildInfoEntity();
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
        public List<BuildInfoEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            if (model != null && !model.sconlist.Any(t => t.ParamSort > 0))
            {
                model.sconlist.Add(new SelectCondition { ParamName = "SortBorder", ParamSort = 1 });
            }
            var list = BuildInfoDAO.Instance.GetEntityListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
            //List<BuildInfo> list = new List<BuildInfo>();
            //var optmdl = Request.GetToken();
            //if (!optmdl._BuildAllList.IsZxxAny()) return list;
            //var dataitem = model.GetListByPage(optmdl._BuildAllList);
            //if (dataitem.Item1.IsZxxAny()) list.AddRange(dataitem.Item1);
            //TotalCount = dataitem.Item2;
            //return list;
        }

        #region 告警推送配置

        /// <summary>
        /// 告警推送配置批量保存
        /// </summary>
        /// <param name="model">数据</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string NoteSaveBatch(BuildNoteModel model)
        {
            Status = false;
            Message = "告警推送配置保存失败。";
            if (model == null || model.BuildId == 0)
            {
                Message = "建筑ID传递失败。";
                return "";
            }
            var temp = BuildInfoDAO.Instance.GetOneBy(t => t.BuildId == model.BuildId);
            if (temp == null)
            {
                Message = $"建筑Id[{model.BuildId}]不正确";
                return Message;
            }
            BuildInfo info = new BuildInfo
            {
                BuildId = model.BuildId,
                ExpandJson = model.ExpandObjects.ToJson(),
            };

            Status = BuildInfoDAO.Instance.UpdateColumns(info, it => new { it.ExpandJson });
            if (Status) Message = "告警推送配置保存成功。";
            return Message;
        }

        #endregion

    }
}