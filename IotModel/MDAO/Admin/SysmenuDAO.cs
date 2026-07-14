using CenBoCommon.Zxx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IotModel
{
    public sealed partial class SysMenuDAO : DbContext<SysMenu>
    {
        private static SysMenuDAO instance;
        public static SysMenuDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SysMenuDAO();
                }
                return instance;
            }
        }

        public override bool Update(SysMenu info)
        {
            bool isok = false;
            try
            {
                var entity = GetOneBy(t => t.MenuId == info.MenuId);
                var old_FullCode = entity.FullCode;
                var old_FullName = entity.FullName;
                var old_ParentId = string.IsNullOrEmpty(entity.ParentId) ? "0" : entity.ParentId;
                info.ParentId = string.IsNullOrEmpty(info.ParentId) ? "0" : info.ParentId;
                var isParentChanged = old_ParentId != info.ParentId;
                if (!string.IsNullOrEmpty(info.ParentId) && info.ParentId != "0")
                {
                    var parent = GetOneBy(t => t.MenuId == info.ParentId);
                    if (parent != null)
                    {
                        info.FullCode = parent.FullCode + info.MenuId + "|";
                        info.FullName = parent.FullName + "|" + info.MenuName;
                        info.TreeLevel = info.FullCode.Count(c => c == '|') - 1;
                        if (!parent.HasChild)
                        {
                            parent.HasChild = true;
                            UpdateColumns(parent, it => new { it.HasChild });
                        }
                    }
                }
                else
                {
                    info.FullCode = "|" + info.MenuId + "|";
                    info.FullName = info.MenuName;
                    info.TreeLevel = 1;
                }

                isok = UpdateIgnoreColumns(info, it => new
                {
                    it.CreateId,
                    it.CreateTime,
                    it.CreateName,
                });

                if (isok)
                {
                    if (isParentChanged && old_ParentId != "0")
                    {
                        var oldParent = GetOneBy(t => t.MenuId == old_ParentId);
                        if (oldParent != null)
                        {
                            oldParent.HasChild = GetListBy(t => t.ParentId == old_ParentId && t.MenuId != info.MenuId).IsZxxAny();
                            UpdateColumns(oldParent, it => new { it.HasChild });
                        }
                    }

                    //如果当前节点的FullCode和FullName变更后，批量更新所有子节点的FullCode和FullName
                    if (old_FullCode != info.FullCode || old_FullName != info.FullName)
                    {
                        var childList = GetListBy(t => t.FullCode.StartsWith(old_FullCode) && t.MenuId != info.MenuId);
                        if (childList.IsZxxAny())
                        {
                            var childMap = childList.ToDictionary(t => t.MenuId);
                            foreach (var child in childList.OrderBy(t => t.TreeLevel))
                            {
                                var parentFullCode = info.FullCode;
                                var parentFullName = info.FullName;
                                if (child.ParentId != info.MenuId && childMap.TryGetValue(child.ParentId, out var parentChild))
                                {
                                    parentFullCode = parentChild.FullCode;
                                    parentFullName = parentChild.FullName;
                                }

                                child.FullCode = parentFullCode + child.MenuId + "|";
                                child.FullName = parentFullName + "|" + child.MenuName;
                                child.TreeLevel = child.FullCode.Count(c => c == '|') - 1;
                            }
                            UpdateColumns(childList, it => new
                            {
                                it.FullCode,
                                it.FullName,
                                it.TreeLevel
                            });
                        }
                    }
                }

                var aa = sqlSugar;
                return isok;
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
            }
        }

        public override bool Insert(SysMenu info)
        {
            bool isok = false;
            try
            {
                Db.BeginTran();
                var upinfo = InsertReturnEntity(info);
                if (!string.IsNullOrEmpty(info.ParentId) && info.ParentId != "0")
                {
                    var parent = GetOneBy(t => t.MenuId == upinfo.ParentId);
                    if (parent != null)
                    {
                        upinfo.FullCode = parent.FullCode + upinfo.MenuId + "|";
                        upinfo.FullName = parent.FullName + "|" + info.MenuName;
                        upinfo.TreeLevel = upinfo.FullCode.Count(c => c == '|') - 1;
                        if (!parent.HasChild)
                        {
                            parent.HasChild = true;
                            UpdateColumns(parent, it => new { it.HasChild });
                        }
                    }
                }
                else
                {
                    upinfo.FullCode = "|" + upinfo.MenuId + "|";
                    upinfo.FullName = upinfo.MenuName;
                    upinfo.TreeLevel = 1;
                }
                if (upinfo.SortBorder.IsZxxNullOrEmpty())
                {
                    var list = GetList();
                    if (list.IsZxxAny())
                    {
                        string first = ObjLevel[upinfo.TreeLevel];
                        var levellist = list.FindAll(t => t.TreeLevel == upinfo.TreeLevel && t.ParentId == upinfo.ParentId);
                        if (levellist.IsZxxAny())
                        {
                            int max = levellist.Select(t => t.SortBorder.Replace(first, "").ToZxxInt()).Max();
                            if (max > 0) upinfo.SortBorder = $"{first}{(max + 3).ToString().PadLeft(3, '0')}";
                        }
                        else
                        {
                            upinfo.SortBorder = $"{first}001";
                        }
                    }
                    else
                    {
                        upinfo.SortBorder = "A001";
                    }
                }
                isok = UpdateColumns(upinfo, it => new
                {
                    it.FullCode,
                    it.FullName,
                    it.TreeLevel,
                    it.SortBorder,
                });
                Db.CommitTran();

                var aa = sqlSugar;
                return isok;
            }
            catch (Exception ex)
            {
                Db.RollbackTran();
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
            }
        }

        public bool DeleteById(string menuid)
        {
            bool isok = false;
            try
            {
                Db.BeginTran();
                var info = GetOneBy(t => t.MenuId == menuid);
                if (info != null && !string.IsNullOrWhiteSpace(info.FullCode))
                {
                    DeleteBy(t => t.FullCode.Contains(info.FullCode));
                    SysMenuBtnDAO.Instance.DeleteBy(t => t.MenuId == info.MenuId);
                    isok = true;
                }
                Db.CommitTran();
                return isok;
            }
            catch (Exception ex)
            {
                isok = false;
                Db.RollbackTran();
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
            }
        }

        public List<SysMenu> GetChildList(List<SysMenu> alllist, string pid)
        {
            var childlist = new List<SysMenu>();

            var sublist = alllist.FindAll(t => t.ParentId == pid);
            if (sublist.Count == 0)
            {
                childlist.Add(alllist.Find(t => t.MenuId == pid));
            }
            else
            {
                foreach (var sub in sublist)
                {
                    childlist.AddRange(GetChildList(alllist, sub.MenuId));
                }
            }

            return childlist;
        }

        public override void Init()
        {
            try
            {
                //菜单表是侧边栏的唯一真源:MenuCode 必须严格等于前端路由 name,MenuUrl 等于路由 path,
                //Component 是相对 src/views 的组件路径(目录节点留空——前端把动态路由拍平后统一挂到根 Layout 下,目录只用于渲染菜单树)。
                //首页(/ 与 /welcome)不进此表:它由 router/modules/home.ts 静态提供,是所有动态路由的挂载容器,删不得。
                string time = DateTime.Now.ToDateTimeString();
                var seed = new[]
                {
                    (Id: "2000", Code: "ProjectScada",  Name: "组态管理",     Pid: "0",    Url: "/projectscada",  Comp: "",                          Icon: "ri:computer-line",   Sort: "A002", Meta: ""),
                    //组态与报表共用 views/project 同一个页面,靠 meta.projectKind 区分读写哪套数据;缺了它两个入口会塌成同一个
                    (Id: "2001", Code: "Project",       Name: "项目管理",     Pid: "2000", Url: "/project",       Comp: "project/index.vue",         Icon: "",                   Sort: "B001", Meta: "{\"projectKind\":\"scada\"}"),

                    (Id: "3000", Code: "IotManage",     Name: "物联管理",     Pid: "0",    Url: "/iot",           Comp: "",                          Icon: "ri:cpu-line",        Sort: "A003", Meta: ""),
                    (Id: "3001", Code: "IotCenter",     Name: "设备中心",     Pid: "3000", Url: "/iot/center",    Comp: "iot/center/index.vue",      Icon: "",                   Sort: "B001", Meta: ""),
                    (Id: "3002", Code: "IotProduct",    Name: "产品类型",     Pid: "3000", Url: "/iot/product",   Comp: "iot/product/index.vue",     Icon: "",                   Sort: "B002", Meta: ""),
                    (Id: "3003", Code: "IotTypeParam",  Name: "点表配置",     Pid: "3000", Url: "/iot/typeparam", Comp: "iot/typeparam/index.vue",   Icon: "",                   Sort: "B003", Meta: ""),
                    (Id: "3004", Code: "IotDevice",     Name: "设备管理",     Pid: "3000", Url: "/iot/device",    Comp: "iot/device/index.vue",      Icon: "",                   Sort: "B004", Meta: ""),
                    (Id: "3005", Code: "IotMonitor",    Name: "实时监控",     Pid: "3000", Url: "/iot/monitor",   Comp: "iot/monitor/index.vue",     Icon: "",                   Sort: "B005", Meta: ""),
                    (Id: "3006", Code: "IotStrategy",   Name: "采集推送策略", Pid: "3000", Url: "/iot/strategy",  Comp: "iot/strategy/index.vue",    Icon: "",                   Sort: "B006", Meta: ""),
                    (Id: "3007", Code: "IotAlarm",      Name: "告警中心",     Pid: "3000", Url: "/iot/alarm",     Comp: "iot/alarm/index.vue",       Icon: "",                   Sort: "B007", Meta: ""),
                    (Id: "3008", Code: "IotAlarmMask",  Name: "告警屏蔽",     Pid: "3000", Url: "/iot/alarmmask", Comp: "iot/alarmmask/index.vue",   Icon: "",                   Sort: "B008", Meta: ""),
                    (Id: "3009", Code: "IotNotify",     Name: "通知渠道",     Pid: "3000", Url: "/iot/notify",    Comp: "iot/notify/index.vue",      Icon: "",                   Sort: "B009", Meta: ""),
                    (Id: "3010", Code: "IotLinkage",    Name: "规则联动",     Pid: "3000", Url: "/iot/linkage",   Comp: "iot/linkage/index.vue",     Icon: "",                   Sort: "B010", Meta: ""),
                    (Id: "3011", Code: "IotNorthbound", Name: "北向转发",     Pid: "3000", Url: "/iot/northbound",Comp: "iot/northbound/index.vue",  Icon: "",                   Sort: "B011", Meta: ""),
                    (Id: "3012", Code: "IotScript",     Name: "协议脚本",     Pid: "3000", Url: "/iot/script",    Comp: "iot/script/index.vue",      Icon: "",                   Sort: "B012", Meta: ""),
                    (Id: "3013", Code: "IotCommand",    Name: "产品命令",     Pid: "3000", Url: "/iot/command",   Comp: "iot/command/index.vue",     Icon: "",                   Sort: "B013", Meta: ""),
                    (Id: "3014", Code: "IotPlugin",     Name: "插件管理",     Pid: "3000", Url: "/iot/plugin",    Comp: "iot/plugin/index.vue",      Icon: "",                   Sort: "B014", Meta: ""),

                    (Id: "4000", Code: "ReportCenter",  Name: "报表中心",     Pid: "0",    Url: "/report",        Comp: "",                          Icon: "ri:file-chart-line", Sort: "A004", Meta: ""),
                    (Id: "4001", Code: "ReportProject", Name: "报表项目",     Pid: "4000", Url: "/report/project",Comp: "project/index.vue",         Icon: "",                   Sort: "B001", Meta: "{\"projectKind\":\"dash\"}"),

                    (Id: "9000", Code: "SystemManage",  Name: "系统管理",     Pid: "0",    Url: "/system",        Comp: "",                          Icon: "ri:settings-3-line", Sort: "A999", Meta: ""),
                    (Id: "9001", Code: "SystemUnit",    Name: "租户管理",     Pid: "9000", Url: "/system/unit",   Comp: "system/unit/index.vue",     Icon: "",                   Sort: "B001", Meta: ""),
                    (Id: "9002", Code: "SystemUser",    Name: "用户管理",     Pid: "9000", Url: "/system/user",   Comp: "system/user/index.vue",     Icon: "",                   Sort: "B002", Meta: ""),
                    (Id: "9003", Code: "SystemRole",    Name: "角色授权",     Pid: "9000", Url: "/system/role",   Comp: "system/role/index.vue",     Icon: "",                   Sort: "B003", Meta: ""),
                    (Id: "9004", Code: "SystemMenu",    Name: "菜单管理",     Pid: "9000", Url: "/system/menu",   Comp: "system/menu/index.vue",     Icon: "",                   Sort: "B004", Meta: ""),
                    (Id: "9005", Code: "SystemButton",  Name: "按钮管理",     Pid: "9000", Url: "/system/button", Comp: "system/button/index.vue",   Icon: "",                   Sort: "B005", Meta: ""),
                };

                List<SysMenu> list = new List<SysMenu>();
                var map = new Dictionary<string, SysMenu>();
                foreach (var s in seed)
                {
                    var menu = new SysMenu
                    {
                        MenuId = s.Id,
                        MenuCode = s.Code,
                        MenuName = s.Name,
                        ParentId = s.Pid,
                        MenuUrl = s.Url,
                        Component = s.Comp,
                        MenuIcon = s.Icon,
                        MetaJson = s.Meta,
                        SortBorder = s.Sort,
                        IsShowLink = 1,
                        HasChild = false,
                        CreateId = 1,
                        CreateName = "系统管理员",
                        CreateTime = time,
                        UpdateId = 1,
                        UpdateName = "系统管理员",
                        UpdateTime = time,
                    };
                    if (s.Pid == "0")
                    {
                        menu.FullCode = $"|{menu.MenuId}|";
                        menu.FullName = menu.MenuName;
                        menu.TreeLevel = 1;
                    }
                    else
                    {
                        var parent = map[s.Pid];
                        menu.FullCode = parent.FullCode + menu.MenuId + "|";
                        menu.FullName = parent.FullName + "|" + menu.MenuName;
                        menu.TreeLevel = parent.TreeLevel + 1;
                        parent.HasChild = true;
                    }
                    map[menu.MenuId] = menu;
                    list.Add(menu);
                }

                //Init 跑在 CodeFirst 的静态锁内,不能走本类重写的 Insert(内含 BeginTran,嵌套事务会炸)。
                //上面已把 FullCode/FullName/TreeLevel/HasChild 预先算好,这里一次性 InsertRange 落库。
                InsertRange(list);
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
            }
        }

    }
}