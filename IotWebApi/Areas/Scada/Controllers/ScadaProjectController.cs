using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Areas.Scada.Models;

namespace IotWebApi.Areas.Scada.Controllers
{
    /// <summary>
    /// 组态项目管理控制器
    /// </summary>
    [ApiController]
    [ControllSort("26-10")]
    public class ScadaProjectController : ControllerBaseApi
    {
        /// <summary> 
        /// 批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public string SaveBatch(List<ScadaProject> list)
        {
            Status = false;
            Message = "组态项目表信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<ScadaProject> insertlist = new List<ScadaProject>();
                List<ScadaProjectData> insertdatalist = new List<ScadaProjectData>();
                List<ScadaProject> updatelist = new List<ScadaProject>();
                DateTime time = DateTime.Now;
                foreach (var item in list)
                {
                    item.UpdateId = optmdl.UserID;
                    item.UpdateTime = time.ToDateTimeString();
                    item.UpdateName = optmdl.UserName;
                    item.TenantId = optmdl.TenantId;
                    if (item.SnowId == 0)
                    {
                        item.SnowId = SnowModel.Instance.NewId();
                        item.CreateId = optmdl.UserID;
                        item.CreateTime = time.ToDateTimeString();
                        item.CreateName = optmdl.UserName;
                        insertlist.Add(item);
                        ScadaProjectData data = new ScadaProjectData
                        {
                            SnowId = SnowModel.Instance.NewId(),
                            ProjectId = item.SnowId,
                            CreateId = item.CreateId,
                            CreateTime = item.CreateTime,
                            CreateName = item.CreateName,
                            UpdateId = item.UpdateId,
                            UpdateTime = item.CreateTime,
                            UpdateName = item.UpdateName,
                        };
                        insertdatalist.Add(data);
                    }
                    else
                    {
                        updatelist.Add(item);
                    }
                }
                Status = ScadaProjectDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) ScadaProjectDAO.Instance.InsertRange(insertlist);
                    if (insertdatalist.Count > 0) ScadaProjectDataDAO.Instance.InsertRange(insertdatalist);
                    if (updatelist.Count > 0) ScadaProjectDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    Message = "组态项目表信息保存成功。";
                }
            }
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
            Message = $"组态项目信息{res}失败。";
            var entity = ScadaProjectDAO.Instance.GetOneBy(t => t.SnowId == projectId);
            if (entity != null)
            {
                entity.ProjectStatus = status;
                entity.RuntimeUrl = status == 1 ? runtimeUrl : "";
                Status = ScadaProjectDAO.Instance.UpdateColumns(entity, it => new
                {
                    it.ProjectStatus,
                    it.RuntimeUrl
                });
                if (Status) Message = $"组态项目信息{res}成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据项目ID删除项目信息
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public string DeleteById(long projectId)
        {
            Status = false;
            Message = "组态项目信息删除失败。";
            Status = ScadaProjectDAO.Instance.TranAction(() =>
            {
                ScadaProjectDAO.Instance.DeleteBy(t => t.SnowId == projectId);
                ScadaProjectDataDAO.Instance.DeleteBy(t => t.ProjectId == projectId);
            });
            if (Status) Message = "组态项目信息删除成功。";

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
        public List<ScadaProject> GetListByPage(ActionPara model)
        {
            var optmdl = Request.GetToken();
            if (model.sconlist == null) model.sconlist = new List<SelectCondition>();
            model.sconlist.Add(new SelectCondition { ParamName = "UnitId", ParamType = "=", ParamValue = optmdl.TenantId.ToString() });
            int totalNumber = 0;
            var list = ScadaProjectDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 根据项目主键查询组态数据信息
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
            var project = ScadaProjectDAO.Instance.GetOneBy(t => t.SnowId == projectId);
            var data = ScadaProjectDataDAO.Instance.GetOneBy(t => t.ProjectId == projectId);
            if (project != null && data != null)
            {
                project.CopyTypeValue(info);
                info.ContentData = data.ContentData;
            }
            return info;
        }

        /// <summary>
        /// 组态数据保存
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
            Message = "组态数据保存失败。";

            var optmdl = Request.GetToken();
            DateTime time = DateTime.Now;
            var project = ScadaProjectDAO.Instance.GetOneBy(t => t.SnowId == data.ProjectId);
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
            var datainfo = ScadaProjectDataDAO.Instance.GetOneBy(t => t.ProjectId == data.ProjectId);
            if (datainfo != null && !data.ContentData.IsZxxNullOrEmpty())
            {
                datainfo.ContentData = data.ContentData;
                datainfo.UpdateId = optmdl.UserID;
                datainfo.UpdateTime = time.ToDateTimeString();
                datainfo.UpdateName = optmdl.UserName;
            }

            Status = ScadaProjectDAO.Instance.TranAction(() =>
            {
                if (!data.Thumbnail.IsZxxNullOrEmpty()) ScadaProjectDAO.Instance.UpdateColumns(project, it => new
                {
                    it.Thumbnail,
                    it.UpdateId,
                    it.UpdateTime,
                    it.UpdateName
                });
                if (datainfo != null && !data.ContentData.IsZxxNullOrEmpty()) ScadaProjectDataDAO.Instance.UpdateColumns(datainfo, it => new
                {
                    it.ContentData,
                    it.UpdateId,
                    it.UpdateTime,
                    it.UpdateName
                });
            });
            if (Status)
            {
                Message = "组态数据保存成功。";
            }

            return Message;
        }

        /// <summary>
        /// 根据项目ID设置为默认组态项目
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
            Message = $"组态项目信息设置默认失败。";
            var list = ScadaProjectDAO.Instance.GetList();
            if (list.IsZxxAny())
            {
                var entity = list.Find(t => t.SnowId == projectId);
                if (entity == null) return Message;
                list.ForEach(t =>
                {
                    t.ProjectDefault = 0;
                    if (t.SnowId == projectId) t.ProjectDefault = 1;
                });
                Status = ScadaProjectDAO.Instance.UpdateColumns(list, it => new
                {
                    it.ProjectDefault
                });
                if (Status) Message = $"组态项目信息设置默认成功。";
            }

            return Message;
        }

    }
}