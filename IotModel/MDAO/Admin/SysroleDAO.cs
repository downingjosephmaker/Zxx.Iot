using CenBoCommon.Zxx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IotModel
{
    public sealed partial class SysRoleDAO : DbContext<SysRole>
    {
        private static SysRoleDAO instance;
        public static SysRoleDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SysRoleDAO();
                }
                return instance;
            }
        }

        public override bool Update(SysRole info)
        {
            bool isok = false;
            try
            {
                var entity = GetOneBy(t => t.RoleId == info.RoleId);
                var old_FullCode = entity.FullCode;
                var old_FullName = entity.FullName;
                var old_ParentId = entity.ParentId;
                if (info.ParentId > 0)
                {
                    var parent = GetOneBy(t => t.RoleId == info.ParentId);
                    if (parent != null)
                    {
                        info.FullCode = parent.FullCode + info.RoleId + "|";
                        info.FullName = parent.FullName + "|" + info.RoleName;
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
                    info.FullCode = "|" + info.RoleId + "|";
                    info.FullName = info.RoleName;
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
                    if (old_ParentId > 0 && old_ParentId != info.ParentId)
                    {
                        var oldParent = GetOneBy(t => t.RoleId == old_ParentId);
                        if (oldParent != null)
                        {
                            oldParent.HasChild = GetListBy(t => t.ParentId == old_ParentId).IsZxxAny();
                            UpdateColumns(oldParent, it => new { it.HasChild });
                        }
                    }

                    //如果当前节点的FullCode和FullName变更后，批量更新所有子节点的FullCode和FullName
                    if (old_FullCode != info.FullCode || old_FullName != info.FullName)
                    {
                        var childList = GetListBy(t => t.FullCode.StartsWith(old_FullCode) && t.RoleId != info.RoleId);
                        if (childList.IsZxxAny())
                        {
                            var childMap = childList.ToDictionary(t => t.RoleId);
                            foreach (var child in childList.OrderBy(t => t.TreeLevel))
                            {
                                var parentFullCode = info.FullCode;
                                var parentFullName = info.FullName;
                                if (child.ParentId != info.RoleId && childMap.TryGetValue(child.ParentId, out var parentChild))
                                {
                                    parentFullCode = parentChild.FullCode;
                                    parentFullName = parentChild.FullName;
                                }

                                child.FullCode = parentFullCode + child.RoleId + "|";
                                child.FullName = parentFullName + "|" + child.RoleName;
                                child.TreeLevel = child.FullCode.Count(c => c == '|') - 1;
                            }
                            UpdateColumns(childList, it => new
                            {
                                it.FullCode,
                                it.FullName,
                                it.TreeLevel,
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

        public override bool Insert(SysRole info)
        {
            bool isok = false;
            try
            {
                Db.BeginTran();

                var upinfo = InsertReturnEntity(info);
                if (upinfo.ParentId > 0)
                {
                    var parent = GetOneBy(t => t.RoleId == upinfo.ParentId);
                    if (parent != null)
                    {
                        upinfo.FullCode = parent.FullCode + upinfo.RoleId + "|";
                        upinfo.FullName = parent.FullName + "|" + upinfo.RoleName;
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
                    upinfo.FullCode = "|" + upinfo.RoleId + "|";
                    upinfo.FullName = upinfo.RoleName;
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

        public List<SysRole> GetChildList(List<SysRole> alllist, int pid)
        {
            var childlist = new List<SysRole>();

            var sublist = alllist.FindAll(t => t.ParentId == pid);
            if (sublist.Count == 0)
            {
                childlist.Add(alllist.Find(t => t.RoleId == pid));
            }
            else
            {
                foreach (var sub in sublist)
                {
                    childlist.AddRange(GetChildList(alllist, sub.RoleId));
                }
            }

            return childlist;
        }

        public override void Init()
        {
            try
            {
                string sql = @"
INSERT INTO `sys_role` VALUES (1, '开发管理员', 'A001', 1, 0, '开发管理员','|1|','最高级权限', 0, 1, '2024-02-22 14:53:06', '开发管理员', 1, '2024-02-22 14:53:03', '开发管理员');
INSERT INTO `sys_role` VALUES (2, '代理商', 'B001', 2, 1,'代理商','|1|2|','前台和部分后台权限', 0, 1, '2024-02-22 14:53:08', '开发管理员', 1, '2024-02-22 14:53:03', '开发管理员');
INSERT INTO `sys_role` VALUES (3, '单位管理员', 'C001', 3, 2,'单位管理员','|1|2|3|','前台和部分后台权限', 1, 1, '2024-02-22 14:53:08', '开发管理员', 1, '2024-04-03 15:17:57', '开发管理员');
INSERT INTO `sys_role` VALUES (4, '单位普通', 'D001', 4, 3,'单位普通','|1|2|3|4|','前台和部分后台权限', 0, 1, '2024-02-22 14:53:09', '开发管理员', 1, '2024-02-22 14:53:03', '开发管理员');
                ";
                Db.Ado.ExecuteCommand(sql);
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