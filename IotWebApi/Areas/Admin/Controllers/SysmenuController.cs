using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using IotModel;
using IotWebApi.Areas.Admin.Models;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 导航菜单
    /// </summary>
    [ApiController]
    [ControllSort("1-18")]
    public class SysmenuController : ControllerBaseApi
    {

        /// <summary>
        /// 菜单新增
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string Insert(SysMenu info)
        {
            Message = "菜单表信息保存失败。";
            var optmdl = Request.GetToken();
            info.CreateId = optmdl.UserID;
            info.CreateTime = DateTime.Now.ToDateTimeString();
            info.CreateName = optmdl.UserName;
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = SysMenuDAO.Instance.Insert(info);
            if (Status) Message = "菜单信息新增成功。";
            return Message;
        }

        /// <summary>
        /// 菜单修改
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string Update(SysMenu info)
        {
            Message = "菜单信息更新失败。";
            var optmdl = Request.GetToken();
            var temp = SysMenuDAO.Instance.GetOneBy(t => t.MenuId == info.MenuId);
            if (temp == null)
            {
                Message = $"菜单[{info.MenuName}]不存在";
                return Message;
            }
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = SysMenuDAO.Instance.Update(info);
            if (Status) Message = "菜单信息更新成功。";
            return Message;
        }

        /// <summary>
        /// 根据菜单ID删除菜单信息(包含子菜单)
        /// </summary>
        /// <param name="id">菜单ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string Delete(string id)
        {
            Status = false;
            Message = "菜单信息删除失败。";
            Status = SysMenuDAO.Instance.DeleteById(id);
            if (Status) Message = "菜单信息删除成功。";
            return Message;
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
        public List<SysMenu> GetListByParam(MenuParam model)
        {
            List<SysMenu> list = new List<SysMenu>();
            var SysmenuAllList = SysMenuDAO.Instance.GetList();
            if (SysmenuAllList.IsZxxAny()) list.AddRange(SysmenuAllList);
            if (!string.IsNullOrEmpty(model.MenuCode))
            {
                var _list = list.FindAll(t => t.MenuCode.Contains(model.MenuCode));
                if (_list.IsZxxAny())
                {
                    list.Clear();
                    list.AddRange(_list);
                }
            }
            if (!string.IsNullOrEmpty(model.MenuName))
            {
                var _list = list.FindAll(t => t.MenuName.Contains(model.MenuName));
                if (_list.IsZxxAny())
                {
                    list.Clear();
                    list.AddRange(_list);
                }
            }
            TotalCount = list.Count;
            int startindex = (model.page - 1) * model.pagesize;
            if (TotalCount > startindex)
            {
                return list.Skip(startindex).Take(model.pagesize).ToList();
            }
            else
            {
                return new List<SysMenu>();
            }
        }

        /// <summary> 
        /// 批量保存菜单和按钮
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string SaveMenuBtnBatch(List<SysMenuBtn> list)
        {
            Message = "菜单和按钮关系保存失败。";
            if (list.IsZxxAny())
            {
                string _MenuId = "";
                foreach (SysMenuBtn item in list)
                {
                    item.SnowId = SnowModel.Instance.NewId();
                    if (!string.IsNullOrEmpty(item.MenuId)) _MenuId = item.MenuId;
                }
                Status = SysMenuBtnDAO.Instance.TranAction(() =>
                {
                    SysMenuBtnDAO.Instance.DeleteBy(t => t.MenuId == _MenuId);
                    SysMenuBtnDAO.Instance.InsertRange(list);
                });
                if (Status)
                {
                    Message = "菜单和按钮关系保存成功。";
                }
            }
            return Message;
        }

        /// <summary>
        /// 根据菜单ID获取页面按钮
        /// </summary>
        /// <param name="_MenuId">菜单ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public List<MenuBtnInfo> GetButtonListByMenuId(string _MenuId)
        {
            List<MenuBtnInfo> list = new List<MenuBtnInfo>();

            var _mblist = SysMenuBtnDAO.Instance.GetListBy(t => t.MenuId == _MenuId);
            if (_mblist.IsZxxAny())
            {
                var sysbtnlist = SysButtonDAO.Instance.GetList();
                foreach (var item in _mblist)
                {
                    var btn = sysbtnlist.Find(t => t.ButtonId == item.ButtonId);
                    if (btn != null)
                    {
                        MenuBtnInfo mb = new MenuBtnInfo()
                        {
                            MenuId = item.MenuId,
                            MbSort = item.MbSort,
                            ButtonId = btn.ButtonId,
                            ButtonCode = btn.ButtonCode,
                            ButtonName = btn.ButtonName,
                            ButtonType = btn.ButtonType,
                            ButtonHtml = btn.ButtonHtml,
                        };
                        list.Add(mb);
                    }
                }
            }

            TotalCount = list.Count;
            return list;
        }

        /// <summary>
        /// 获取类左侧菜单树结构
        /// </summary>
        /// <param name="islimit">1：限制 2：全部</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public List<MenuInfo> GetMenuTree(int islimit = 2)
        {
            List<MenuInfo> list = new List<MenuInfo>();

            List<SysMenu> menulist = new List<SysMenu>();
            List<MenuBtnInfo> menubtnlist = new List<MenuBtnInfo>();
            var optmdl = Request.GetToken();
            if (optmdl.IsSystem || islimit == 2)
            {
                var _menulist = SysMenuDAO.Instance.GetList();
                if (_menulist.IsZxxAny()) menulist.AddRange(_menulist);
                var _menubtnlist = SysMenuBtnDAO.Instance.GetList();
                if (_menubtnlist.IsZxxAny())
                {
                    var sysbtnlist = SysButtonDAO.Instance.GetList();
                    foreach (var item in _menubtnlist)
                    {
                        var btn = sysbtnlist.Find(t => t.ButtonId == item.ButtonId);
                        if (btn != null)
                        {
                            MenuBtnInfo mb = new MenuBtnInfo()
                            {
                                MenuId = item.MenuId,
                                ButtonId = btn.ButtonId,
                                ButtonCode = btn.ButtonCode,
                                ButtonName = btn.ButtonName,
                                ButtonType = btn.ButtonType,
                                ButtonHtml = btn.ButtonHtml,
                            };
                            menubtnlist.Add(mb);
                        }
                    }
                }
            }
            else if (islimit == 1)
            {
                var rolemenulist = SysRoleMenuBtnDAO.Instance.GetListBy(t => t.RoleId == optmdl._Sysuser.RoleId);
                if (!rolemenulist.IsZxxAny()) return list;

                var _MenuIds = rolemenulist.Select(t => t.MenuId);
                var _menulist = SysMenuDAO.Instance.GetListBy(t => _MenuIds.Contains(t.MenuId));
                if (_menulist.IsZxxAny()) menulist.AddRange(_menulist);
                var sysbtnlist = SysButtonDAO.Instance.GetList();
                foreach (var item in rolemenulist)
                {
                    var btn = sysbtnlist.Find(t => t.ButtonId == item.ButtonId);
                    if (btn != null)
                    {
                        MenuBtnInfo mb = new MenuBtnInfo()
                        {
                            MenuId = item.MenuId,
                            ButtonId = btn.ButtonId,
                            ButtonCode = btn.ButtonCode,
                            ButtonName = btn.ButtonName,
                            ButtonType = btn.ButtonType,
                            ButtonHtml = btn.ButtonHtml,
                        };
                        menubtnlist.Add(mb);
                    }
                }
            }

            if (menulist.IsZxxAny())
            {
                list.AddRange(GetMenuInfoList(menulist, menubtnlist, "0"));
            }

            TotalCount = list.Count;
            return list;
        }

        /// <summary>
        /// 递归获取菜单树
        /// </summary>
        /// <param name="menulist">所有菜单</param>
        /// <param name="menubtnlist">菜单和按钮关系</param>
        /// <param name="pmenuid">父菜单ID</param>
        /// <returns></returns>
        private List<MenuInfo> GetMenuInfoList(List<SysMenu> menulist, List<MenuBtnInfo> menubtnlist, string pmenuid)
        {
            List<MenuInfo> list = new List<MenuInfo>();
            var _menulist = menulist.FindAll(t => t.ParentId == pmenuid).OrderBy(t => t.SortBorder).ToList();
            if (_menulist.IsZxxAny())
            {
                foreach (var item in _menulist)
                {
                    MetaInfo meta = new MetaInfo()
                    {
                        title = item.MenuName,
                        icon = item.MenuIcon,
                        rank = item.SortBorder,
                        showLink = item.IsShowLink == 1,
                    };
                    var btnlist = menubtnlist.FindAll(t => t.MenuId == item.MenuId);
                    if (btnlist.IsZxxAny())
                    {
                        meta.auths.AddRange(btnlist.Select(t => t.ButtonCode));
                        foreach (var btn in btnlist)
                        {
                            BtnInfo btnInfo = new BtnInfo
                            {
                                ButtonId = btn.ButtonId,
                                ButtonName = btn.ButtonName,
                            };
                            meta.btns.Add(btnInfo);
                        }
                    }

                    MenuInfo menu = new MenuInfo()
                    {
                        menuid = item.MenuId,
                        name = item.MenuCode,
                        path = item.MenuUrl,
                        meta = meta,
                    };

                    var childrenList = GetMenuInfoList(menulist, menubtnlist, item.MenuId);
                    if (childrenList.IsZxxAny())
                    {
                        menu.children = new List<MenuInfo>();
                        menu.children.AddRange(childrenList);
                    }
                    list.Add(menu);
                }
            }

            return list;
        }

        /// <summary>
        /// 根据角色ID保存菜单按钮权限
        /// </summary>
        /// <param name="models">权限菜单模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string SaveRoleMenuBtns(List<RoleMenuBtn> models)
        {
            Message = "权限菜单和按钮保存失败。";
            if (models.IsZxxAny())
            {
                List<SysRoleMenuBtn> list = new List<SysRoleMenuBtn>();
                foreach (var item in models)
                {
                    SysRoleMenuBtn rolemenu = new SysRoleMenuBtn()
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        RoleId = item.RoleId,
                        MenuId = item.MenuId,
                        ButtonId = item.ButtonId
                    };
                    if (rolemenu.RoleId > 0 && !string.IsNullOrEmpty(rolemenu.MenuId))
                        list.Add(rolemenu);
                }
                if (list.IsZxxAny())
                {
                    Status = SysRoleMenuBtnDAO.Instance.TranAction(() =>
                    {
                        SysRoleMenuBtnDAO.Instance.DeleteBy(t => t.RoleId == list[0].RoleId);
                        SysRoleMenuBtnDAO.Instance.InsertRange(list);
                    });
                    if (Status)
                    {
                        Message = "权限菜单和按钮保存成功。";
                    }
                }
            }
            return Message;
        }

        /// <summary>
        /// 获取角色菜单权限数据(只能是管理员)
        /// </summary>
        /// <param name="roleid">角色ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public List<SysRoleMenuBtn> Get(int roleid)
        {
            List<SysRoleMenuBtn> list = new List<SysRoleMenuBtn>();

            var optmdl = Request.GetToken();
            if (!optmdl.IsSystem)
            {
                Message = "用户不是管理员，不能进行此操作。";
                Status = false;
                return list;
            }
            var rolemenulist = SysRoleMenuBtnDAO.Instance.GetListBy(t => t.RoleId == roleid);
            if (!rolemenulist.IsZxxAny()) return list;
            list.AddRange(rolemenulist);

            TotalCount = list.Count;
            return list;
        }

    }
}