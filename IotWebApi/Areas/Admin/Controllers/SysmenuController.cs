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
            // MenuId 为 varchar(10) 主键(非自增),UI 新增未带时取现有最大数字ID+1(顺序短号,符合字段长度约束,稳定被 menu_btn/role_menu_btn 引用)
            if (string.IsNullOrEmpty(info.MenuId))
            {
                var maxId = SysMenuDAO.Instance.GetList()
                    .Select(m => long.TryParse(m.MenuId, out var v) ? v : 0L)
                    .DefaultIfEmpty(0L).Max();
                info.MenuId = (maxId + 1).ToString();
            }
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

                var _MenuIds = rolemenulist.Select(t => t.MenuId).Distinct().ToList();
                var _menulist = SysMenuDAO.Instance.GetListBy(t => _MenuIds.Contains(t.MenuId));
                if (_menulist.IsZxxAny())
                {
                    //补全祖先:授权只勾了子菜单而没勾父目录时,GetMenuInfoList 从"0"递归找不到父节点,
                    //会把整棵子树静默丢掉。沿 ParentId 上溯把缺失的祖先补进来,保证树可达。
                    var allmenu = SysMenuDAO.Instance.GetList();
                    var picked = _menulist.ToDictionary(t => t.MenuId);
                    foreach (var m in _menulist)
                    {
                        var pid = m.ParentId;
                        while (!pid.IsZxxNullOrEmpty() && pid != "0" && !picked.ContainsKey(pid))
                        {
                            var parent = allmenu.Find(t => t.MenuId == pid);
                            if (parent == null) break;
                            picked[parent.MenuId] = parent;
                            pid = parent.ParentId;
                        }
                    }
                    menulist.AddRange(picked.Values);
                }
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
                        rank = ParseRank(item.SortBorder),
                        showLink = item.IsShowLink == 1,
                    };
                    //meta_json 的键平铺进 meta(如 projectKind:组态与报表共用同一个 project 页面,靠它区分读写哪套数据)
                    if (!item.MetaJson.IsZxxNullOrEmpty())
                    {
                        try
                        {
                            var extra = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(item.MetaJson);
                            if (extra != null)
                            {
                                foreach (var kv in extra) meta.extra[kv.Key] = kv.Value;
                            }
                        }
                        catch
                        {
                            //meta_json 由人在菜单管理里手填,格式写错不该让整棵菜单树崩掉
                        }
                    }
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
                        //目录节点不配组件:前端会把动态路由拍平后统一挂到根路由(Layout)下
                        component = item.Component.IsZxxNullOrEmpty() ? null : item.Component,
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
        /// 把排序码解析成 pure-admin 需要的数字 rank(如 "A002"→2、"B011"→11)。
        /// SortBorder 兼具树内排序码与前端 rank 两个用途,前端只认数字,故在此剥离字母前缀。
        /// </summary>
        private static int? ParseRank(string sortBorder)
        {
            if (sortBorder.IsZxxNullOrEmpty()) return null;
            var digits = new string(sortBorder.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var rank) ? rank : (int?)null;
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