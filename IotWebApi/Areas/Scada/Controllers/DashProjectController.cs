using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Areas.Scada.Models;

namespace IotWebApi.Areas.Scada.Controllers
{
    /// <summary>
    /// 大屏项目管理控制器
    /// </summary>
    [ApiController]
    [ControllSort("26-2")]
    public class DashProjectController : ControllerBaseApi
    {
        /// <summary>
        /// 大屏项目新增
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public string Insert(DashProject info)
        {
            Status = false;
            Message = "大屏项目信息新增失败。";
            string res = "";

            var optmdl = Request.GetToken();
            info.SnowId = SnowModel.Instance.NewId();
            info.UnitId = optmdl.UnitId;
            info.CreateId = optmdl.UserID;
            info.CreateTime = DateTime.Now.ToDateTimeString();
            info.CreateName = optmdl.UserName;
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;

            DashProjectData data = new DashProjectData
            {
                SnowId = SnowModel.Instance.NewId(),
                ProjectId = info.SnowId,
                CreateId = info.CreateId,
                CreateTime = info.CreateTime,
                CreateName = info.CreateName,
                UpdateId = info.UpdateId,
                UpdateTime = info.CreateTime,
                UpdateName = info.UpdateName,
            };
            Status = DashProjectDAO.Instance.TranAction(() =>
            {
                DashProjectDAO.Instance.Insert(info);
                DashProjectDataDAO.Instance.Insert(data);
            });
            if (Status)
            {
                Message = "大屏项目信息新增成功。";
                res = info.SnowId.ToString();
            }

            return res;
        }

        /// <summary>
        /// 大屏项目更新
        /// </summary>
        /// <param name="info">项目信息</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public string Update(DashProject info)
        {
            Status = false;
            Message = $"大屏项目信息保存失败。";
            var optmdl = Request.GetToken();
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = DashProjectDAO.Instance.UpdateColumns(info, it => new
            {
                it.ProjectName,
                it.ProjectDesc,
                it.ProjectStatus,
                it.UpdateId,
                it.UpdateTime,
                it.UpdateName,
            });
            if (Status) Message = $"大屏项目信息保存成功。";
            return Message;
        }

        /// <summary>
        /// 根据项目ID发布/取消发布
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="status">项目状态(0:草稿 1:发布)</param>
        /// <param name="runtimeUrl">运行态访问地址</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public string DashPublish(long projectId, int status, string runtimeUrl)
        {
            Status = false;
            string res = status == 1 ? "发布" : "取消发布";
            Message = $"大屏项目信息{res}失败。";
            var entity = DashProjectDAO.Instance.GetOneBy(t => t.SnowId == projectId);
            if (entity != null)
            {
                entity.ProjectStatus = status;
                entity.RuntimeUrl = status == 1 ? runtimeUrl : "";
                Status = DashProjectDAO.Instance.UpdateColumns(entity, it => new
                {
                    it.ProjectStatus,
                    it.RuntimeUrl
                });
                if (Status) Message = $"大屏项目信息{res}成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据主键集合批量删除
        /// </summary>
        /// <param name="ids">主键集合</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public string DeleteByIds(List<long> ids)
        {
            Status = false;
            Message = "大屏项目表删除失败。";
            Status = DashProjectDAO.Instance.DeleteBy(t => ids.Contains(t.SnowId));
            if (Status)
            {
                Message = "大屏项目表信息删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据条件查询项目分页数据
        /// </summary>
        /// <param name="model">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public List<DashProject> GetListByPage(ActionPara model)
        {
            var optmdl = Request.GetToken();
            if (model.sconlist == null) model.sconlist = new List<SelectCondition>();
            model.sconlist.Add(new SelectCondition { ParamName = "UnitId", ParamType = "=", ParamValue = optmdl.UnitId.ToString() });
            int totalNumber = 0;
            var list = DashProjectDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 根据项目主键查询大屏数据信息
        /// </summary>
        /// <param name="projectId">项目主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public ProjectInfo GetDataInfo(long projectId)
        {
            ProjectInfo info = new ProjectInfo();
            var project = DashProjectDAO.Instance.GetOneBy(t => t.SnowId == projectId);
            var data = DashProjectDataDAO.Instance.GetOneBy(t => t.ProjectId == projectId);
            if (project != null && data != null)
            {
                project.CopyTypeValue(info);
                info.ContentData = data.ContentData;
            }
            return info;
        }

        /// <summary>
        /// 大屏数据保存
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public string SaveProjectData(ProjectInfoData data)
        {
            Status = false;
            Message = "大屏数据保存失败。";

            var optmdl = Request.GetToken();
            DateTime time = DateTime.Now;
            var project = DashProjectDAO.Instance.GetOneBy(t => t.SnowId == data.ProjectId);
            if (project != null)
            {
                if (!data.Thumbnail.IsZxxNullOrEmpty())
                {
                    project.Thumbnail = data.Thumbnail;
                    project.UpdateId = optmdl.UserID;
                    project.UpdateTime = time.ToDateTimeString();
                    project.UpdateName = optmdl.UserName;
                }
            }
            var datainfo = DashProjectDataDAO.Instance.GetOneBy(t => t.ProjectId == data.ProjectId);
            if (datainfo != null && !data.ContentData.IsZxxNullOrEmpty())
            {
                datainfo.ContentData = data.ContentData;
                datainfo.UpdateId = optmdl.UserID;
                datainfo.UpdateTime = time.ToDateTimeString();
                datainfo.UpdateName = optmdl.UserName;
            }

            Status = DashProjectDAO.Instance.TranAction(() =>
            {
                if (!data.Thumbnail.IsZxxNullOrEmpty()) DashProjectDAO.Instance.UpdateColumns(project, it => new
                {
                    it.Thumbnail,
                    it.UpdateId,
                    it.UpdateTime,
                    it.UpdateName
                });
                if (datainfo != null && !data.ContentData.IsZxxNullOrEmpty()) DashProjectDataDAO.Instance.UpdateColumns(datainfo, it => new
                {
                    it.ContentData,
                    it.UpdateId,
                    it.UpdateTime,
                    it.UpdateName
                });
            });
            if (Status)
            {
                Message = "大屏数据保存成功。";
            }

            return Message;
        }

        /// <summary>
        /// 根据项目ID发布/取消发布
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public string DashSetDefault(long projectId)
        {
            Status = false;
            Message = $"大屏项目信息设置默认失败。";
            var list = DashProjectDAO.Instance.GetList();
            if (list.IsZxxAny())
            {
                var entity = list.Find(t => t.SnowId == projectId);
                if (entity == null) return Message;
                list.ForEach(t =>
                {
                    t.ProjectDefault = 0;
                    if (t.SnowId == projectId) t.ProjectDefault = 1;
                });
                Status = DashProjectDAO.Instance.UpdateColumns(list, it => new
                {
                    it.ProjectDefault
                });
                if (Status) Message = $"大屏项目信息设置默认成功。";
            }

            return Message;
        }

    }
}