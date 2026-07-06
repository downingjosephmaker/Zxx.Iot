using CenBoCommon.Zxx;
using IotModel;
using IotWebApi.Areas.Basic.Models;
using Microsoft.AspNetCore.Mvc;

namespace IotWebApi
{
    /// <summary> 
    /// 组织信息
    /// </summary>
    [ApiController]
    [ControllSort("5-15")]
    public class DeptinfoController : ControllerBaseApi
    {
        /// <summary>
        /// 组织新增
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string Insert(DeptInfo info)
        {
            Status = false;
            Message = "组织表信息保存失败。";
            var optmdl = Request.GetToken();
            info.CreateId = optmdl.UserID;
            info.CreateTime = DateTime.Now.ToDateTimeString();
            info.CreateName = optmdl.UserName;
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            info.UnitId = optmdl.UnitId;
            Status = DeptInfoDAO.Instance.Insert(info);
            if (Status) Message = "组织信息新增成功。";
            return Message;
        }

        /// <summary>
        /// 组织修改
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string Update(DeptInfo info)
        {
            Status = false;
            Message = "组织信息更新失败。";
            var optmdl = Request.GetToken();
            var temp = DeptInfoDAO.Instance.GetOneBy(t => t.DeptId == info.DeptId);
            if (temp == null)
            {
                Message = $"组织[{info.DeptName}]不存在";
                return Message;
            }
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            info.UnitId = optmdl.UnitId;
            Status = DeptInfoDAO.Instance.Update(info);
            if (Status) Message = "组织信息更新成功。";
            return Message;
        }

        /// <summary>
        /// 根据组织ID删除组织信息(包含子组织)
        /// </summary>
        /// <param name="id">组织ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string Delete(int id)
        {
            Status = false;
            Message = "组织信息删除失败。";
            Status = DeptInfoDAO.Instance.DeleteBy(t => t.FullCode.Contains($"|{id}|"));
            if (Status) Message = "组织信息删除成功。";
            return Message;
        }

        /// <summary>
        /// 根据组织ID查询单条数据
        /// </summary>
        /// <param name="deptid">组织ID</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public DeptInfoEntity GetInfoByPk(int deptid)
        {
            var entity = DeptInfoDAO.Instance.GetEntityOneBy(t => t.DeptId == deptid);
            return entity ?? new DeptInfoEntity();
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
        public List<DeptInfoEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            if (model != null && !model.sconlist.Any(t => t.ParamSort > 0))
            {
                model.sconlist.Add(new SelectCondition { ParamName = "SortBorder", ParamSort = 1 });
            }
            var list = DeptInfoDAO.Instance.GetEntityListByPage(model, ref totalNumber);
            TotalCount = totalNumber.ToZxxInt();
            return list;
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
        public string NoteSaveBatch(DeptNoteModel model)
        {
            Status = false;
            Message = "告警推送配置保存失败。";
            if (model == null || model.DeptId == 0)
            {
                Message = "组织ID传递失败。";
                return "";
            }
            var temp = DeptInfoDAO.Instance.GetOneBy(t => t.DeptId == model.DeptId);
            if (temp == null)
            {
                Message = $"组织Id[{model.DeptId}]不正确";
                return Message;
            }
            DeptInfo info = new DeptInfo
            {
                DeptId = model.DeptId,
                ExpandJson = model.ExpandObjects.ToJson(),
            };

            Status = DeptInfoDAO.Instance.UpdateColumns(info, it => new { it.ExpandJson });
            if (Status) Message = "告警推送配置保存成功。";
            return Message;
        }

        #endregion

    }
}