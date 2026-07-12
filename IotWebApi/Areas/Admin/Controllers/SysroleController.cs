using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 角色管理
    /// </summary>
    [ApiController]
    [ControllSort("1-15")]
    public class SysroleController : ControllerBaseApi
    {

        /// <summary>
        /// 角色新增
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string Insert(SysRole info)
        {
            Status = false;
            Message = "角色表信息保存失败。";
            var optmdl = Request.GetToken();
            info.CreateId = optmdl.UserID;
            info.CreateTime = DateTime.Now.ToDateTimeString();
            info.CreateName = optmdl.UserName;
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = SysRoleDAO.Instance.Insert(info);
            if (Status) Message = "角色信息新增成功。";
            return Message;
        }

        /// <summary>
        /// 角色修改
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string Update(SysRole info)
        {
            Status = false;
            Message = "角色信息更新失败。";
            var optmdl = Request.GetToken();
            var temp = SysRoleDAO.Instance.GetOneBy(t => t.RoleId == info.RoleId);
            if (temp == null)
            {
                Message = $"角色[{info.RoleName}]不存在";
                return Message;
            }
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = SysRoleDAO.Instance.Update(info);
            if (Status) Message = "角色信息更新成功。";
            return Message;
        }

        /// <summary>
        /// 根据角色ID删除角色信息(包含子角色)
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string Delete(int id)
        {
            Status = false;
            Message = "角色信息删除失败。";
            var self = SysRoleDAO.Instance.GetOneBy(t => t.RoleId == id);
            int parentId = self?.ParentId ?? 0;
            Status = SysRoleDAO.Instance.DeleteBy(t => t.FullCode.Contains($"|{id}|"));
            if (Status)
            {
                // 若父级已无其它子角色，回填 has_child=false(full_code 祖先链子树级联删除的树形范式)
                if (parentId > 0)
                {
                    var parent = SysRoleDAO.Instance.GetOneBy(t => t.RoleId == parentId);
                    if (parent != null)
                    {
                        bool stillHasChild = SysRoleDAO.Instance.GetListBy(t => t.ParentId == parentId).IsZxxAny();
                        if (parent.HasChild != stillHasChild)
                        {
                            parent.HasChild = stillHasChild;
                            SysRoleDAO.Instance.UpdateColumns(parent, it => new { it.HasChild });
                        }
                    }
                }
                Message = "角色信息删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据角色ID查询单条数据
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public SysRole GetInfoByPk(int id)
        {
            var entity = SysRoleDAO.Instance.GetOneBy(t => t.RoleId == id);
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
        [ApiGroup(ApiGroupNames.Admin)]
        public List<SysRole> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = SysRoleDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber.ToZxxInt();
            return list;
        }

    }
}