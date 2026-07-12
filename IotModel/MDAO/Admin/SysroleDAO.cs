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
                var list = new List<SysRole>
                {
                    new SysRole { RoleId = 1, RoleName = "开发管理员", SortBorder = "A001", TreeLevel = 1, ParentId = 0, FullName = "开发管理员", FullCode = "|1|", RoleDescribe = "最高级权限", HasChild = false, CreateId = 1, CreateTime = "2024-02-22 14:53:06", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-02-22 14:53:03", UpdateName = "开发管理员" },
                    new SysRole { RoleId = 2, RoleName = "代理商", SortBorder = "B001", TreeLevel = 2, ParentId = 1, FullName = "代理商", FullCode = "|1|2|", RoleDescribe = "前台和部分后台权限", HasChild = false, CreateId = 1, CreateTime = "2024-02-22 14:53:08", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-02-22 14:53:03", UpdateName = "开发管理员" },
                    new SysRole { RoleId = 3, RoleName = "租户管理员", SortBorder = "C001", TreeLevel = 3, ParentId = 2, FullName = "租户管理员", FullCode = "|1|2|3|", RoleDescribe = "前台和部分后台权限", HasChild = true, CreateId = 1, CreateTime = "2024-02-22 14:53:08", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-04-03 15:17:57", UpdateName = "开发管理员" },
                    new SysRole { RoleId = 4, RoleName = "租户普通", SortBorder = "D001", TreeLevel = 4, ParentId = 3, FullName = "租户普通", FullCode = "|1|2|3|4|", RoleDescribe = "前台和部分后台权限", HasChild = false, CreateId = 1, CreateTime = "2024-02-22 14:53:09", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-02-22 14:53:03", UpdateName = "开发管理员" },
                };
                // RoleId 是 IsIdentity 自增列。superadmin.RoleId=1 依赖此处 RoleId=1 精确落地,
                // 而普通 InsertRange 会剔除自增列、由 DB 生成(显式值被忽略)。
                // SeedOffIdentity 强制写入 1..4 并同步 PG 序列,避免运行期新增角色撞种子主键。
                SeedOffIdentity(list);
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