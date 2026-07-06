using CenBoCommon.Zxx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IotModel
{
    public sealed partial class SysAreaDAO : FullEntityContext<SysAreaEntity>
    {
        private static SysAreaDAO instance;
        public static SysAreaDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SysAreaDAO();
                }
                return instance;
            }
        }

        public override bool Update(SysAreaEntity info)
        {
            bool isok = false;
            try
            {
                var entity = GetOneBy(t => t.AreaId == info.AreaId);
                var old_FullCode = entity.FullCode;
                var old_FullName = entity.FullName;
                var old_ParentId = entity.ParentId;
                if (!string.IsNullOrEmpty(info.ParentId) && info.ParentId != "0")
                {
                    var parent = GetOneBy(t => t.AreaId == info.ParentId);
                    if (parent != null)
                    {
                        info.FullCode = parent.FullCode + info.AreaId + "|";
                        info.FullName = parent.FullName + "|" + info.AreaName;
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
                    info.FullCode = "|" + info.AreaId + "|";
                    info.FullName = info.AreaName;
                    info.TreeLevel = 1;
                }

                isok = Update(info);

                if (isok)
                {
                    if (!string.IsNullOrEmpty(old_ParentId) && old_ParentId != "0" && old_ParentId != info.ParentId)
                    {
                        var oldParent = GetOneBy(t => t.AreaId == old_ParentId);
                        if (oldParent != null)
                        {
                            oldParent.HasChild = GetListBy(t => t.ParentId == old_ParentId).IsZxxAny();
                            UpdateColumns(oldParent, it => new { it.HasChild });
                        }
                    }

                    //如果当前节点的FullCode和FullName变更后，批量更新所有子节点的FullCode和FullName
                    if (old_FullCode != info.FullCode || old_FullName != info.FullName)
                    {
                        var childList = GetListBy(t => t.FullCode.StartsWith(old_FullCode) && t.AreaId != info.AreaId);
                        if (childList.IsZxxAny())
                        {
                            var childMap = childList.ToDictionary(t => t.AreaId);
                            foreach (var child in childList.OrderBy(t => t.TreeLevel))
                            {
                                var parentFullCode = info.FullCode;
                                var parentFullName = info.FullName;
                                if (child.ParentId != info.AreaId && childMap.TryGetValue(child.ParentId, out var parentChild))
                                {
                                    parentFullCode = parentChild.FullCode;
                                    parentFullName = parentChild.FullName;
                                }

                                child.FullCode = parentFullCode + child.AreaId + "|";
                                child.FullName = parentFullName + "|" + child.AreaName;
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

        public override bool Insert(SysAreaEntity info)
        {
            bool isok = false;
            try
            {
                Db.BeginTran();
                var upinfo = InsertReturnEntity(info);
                if (!string.IsNullOrEmpty(info.ParentId) && info.ParentId != "0")
                {
                    var parent = GetOneBy(t => t.AreaId == upinfo.ParentId);
                    if (parent != null)
                    {
                        upinfo.FullCode = parent.FullCode + upinfo.AreaId + "|";
                        upinfo.FullName = parent.FullName + "|" + info.AreaName;
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
                    upinfo.FullCode = "|" + upinfo.AreaId + "|";
                    upinfo.FullName = upinfo.AreaName;
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

        public List<SysAreaEntity> GetChildList(List<SysAreaEntity> alllist, string pid)
        {
            var childlist = new List<SysAreaEntity>();

            var sublist = alllist.FindAll(t => t.ParentId == pid);
            if (sublist.Count == 0)
            {
                childlist.Add(alllist.Find(t => t.AreaId == pid));
            }
            else
            {
                foreach (var sub in sublist)
                {
                    childlist.AddRange(GetChildList(alllist, sub.AreaId));
                }
            }

            return childlist;
        }

    }
}