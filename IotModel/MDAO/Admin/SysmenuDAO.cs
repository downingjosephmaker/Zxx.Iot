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
                List<SysMenu> list = new List<SysMenu>();
                string time = DateTime.Now.ToDateTimeString();
                SysMenu menu1 = new SysMenu
                {
                    MenuId = "10000",
                    MenuCode = "System",
                    MenuName = "系统管理",
                    ParentId = "0",
                    MenuUrl = "/system",
                    MenuIcon = "ri/settings-3-line",
                    SortBorder = "A999"
                };
                list.Add(menu1);

                SysMenu menu5 = new SysMenu
                {
                    MenuId = "10015",
                    MenuCode = "equipmentType",
                    MenuName = "设备类型管理",
                    ParentId = "10000",
                    MenuUrl = "/system/equipmentType/index",
                    MenuIcon = ""
                };
                list.Add(menu5);

                SysMenu menu6 = new SysMenu
                {
                    MenuId = "10021",
                    MenuCode = "SystemMenu",
                    MenuName = "菜单管理",
                    ParentId = "10000",
                    MenuUrl = "/system/menu/index",
                    MenuIcon = ""
                };
                list.Add(menu6);

                SysMenu menu7 = new SysMenu
                {
                    MenuId = "10022",
                    MenuCode = "buttonManagement",
                    MenuName = "按钮管理",
                    ParentId = "10000",
                    MenuUrl = "/system/buttonManagement/index",
                    MenuIcon = ""
                };
                list.Add(menu7);

                SysMenu menu8 = new SysMenu
                {
                    MenuId = "10025",
                    MenuCode = "SystemRole",
                    MenuName = "角色管理",
                    ParentId = "10000",
                    MenuUrl = "/system/role/index",
                    MenuIcon = ""
                };
                list.Add(menu8);

                SysMenu menu9 = new SysMenu
                {
                    MenuId = "10026",
                    MenuCode = "SystemUser",
                    MenuName = "用户管理",
                    ParentId = "10000",
                    MenuUrl = "/system/user/index",
                    MenuIcon = ""
                };
                list.Add(menu9);

                SysMenu menu11 = new SysMenu
                {
                    MenuId = "3000",
                    MenuCode = "information",
                    MenuName = "基础信息",
                    ParentId = "0",
                    MenuUrl = "/information",
                    MenuIcon = "ep/document",
                    SortBorder = "A001"
                };
                list.Add(menu11);

                SysMenu menu12 = new SysMenu
                {
                    MenuId = "3004",
                    MenuCode = "environmentalMap",
                    MenuName = "环境平面图",
                    ParentId = "3000",
                    MenuUrl = "/environmentalMonitor/environmentalMap/index",
                    MenuIcon = ""
                };
                list.Add(menu12);

                SysMenu menu13 = new SysMenu
                {
                    MenuId = "5000",
                    MenuCode = "record",
                    MenuName = "记录管理",
                    ParentId = "0",
                    MenuUrl = "/record",
                    MenuIcon = "ep/document-copy",
                    SortBorder = "A011"
                };
                list.Add(menu13);

                SysMenu menu14 = new SysMenu
                {
                    MenuId = "5002",
                    MenuCode = "alarmLog",
                    MenuName = "设备报警记录",
                    ParentId = "5000",
                    MenuUrl = "/record/alarmLog/index",
                    MenuIcon = ""
                };
                list.Add(menu14);

                SysMenu menu15 = new SysMenu
                {
                    MenuId = "5003",
                    MenuCode = "operationRecords",
                    MenuName = "设备运行记录",
                    ParentId = "5000",
                    MenuUrl = "/record/operationRecords/index",
                    MenuIcon = ""
                };
                list.Add(menu15);

                SysMenu menu16 = new SysMenu
                {
                    MenuId = "5004",
                    MenuCode = "controlRecords",
                    MenuName = "设备控制记录",
                    ParentId = "5000",
                    MenuUrl = "/record/controlRecords/index",
                    MenuIcon = ""
                };
                list.Add(menu16);

                SysMenu menu17 = new SysMenu
                {
                    MenuId = "5005",
                    MenuCode = "SMSLogs",
                    MenuName = "短信发送记录",
                    ParentId = "5000",
                    MenuUrl = "/record/SMSLogs/index",
                    MenuIcon = ""
                };
                list.Add(menu17);

                SysMenu menu10 = new SysMenu
                {
                    MenuId = "5015",
                    MenuCode = "sysUserLog",
                    MenuName = "用户登录日志",
                    ParentId = "5000",
                    MenuUrl = "/record/sysUserLog/index",
                    MenuIcon = ""
                };
                list.Add(menu10);

                foreach (var item in list)
                {
                    item.IsShowLink = 1;
                    item.CreateId = 1;
                    item.CreateName = "系统管理员";
                    item.CreateTime = time;
                    item.UpdateId = 1;
                    item.UpdateName = "系统管理员";
                    item.UpdateTime = time;
                    Insert(item);
                }
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