using CenBoCommon.Zxx;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IotModel
{
    public sealed partial class DeviceTypeDAO : FullEntityContext<DeviceTypeEntity>
    {
        private static DeviceTypeDAO instance;
        public static DeviceTypeDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeviceTypeDAO();
                }
                return instance;
            }
        }

        public override bool Update(DeviceTypeEntity info)
        {
            bool isok = false;
            try
            {
                var entity = GetOneBy(t => t.TypeCode == info.TypeCode);
                var old_FullCode = entity.FullCode;
                var old_FullName = entity.FullName;
                var old_ParentId = entity.ParentId;
                if (!info.ParentId.IsZxxNullOrEmpty())
                {
                    var parent = GetOneBy(t => t.TypeCode == info.ParentId);
                    if (parent != null)
                    {
                        info.FullCode = parent.FullCode + info.TypeCode + "|";
                        info.FullName = parent.FullName + "|" + info.TypeName;
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
                    info.FullCode = "|" + info.TypeCode + "|";
                    info.FullName = info.TypeName;
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
                    if (!old_ParentId.IsZxxNullOrEmpty() && old_ParentId != info.ParentId)
                    {
                        var oldParent = GetOneBy(t => t.TypeCode == old_ParentId);
                        if (oldParent != null)
                        {
                            oldParent.HasChild = GetListBy(t => t.ParentId == old_ParentId).IsZxxAny();
                            UpdateColumns(oldParent, it => new { it.HasChild });
                        }
                    }

                    //如果当前节点的FullCode和FullName变更后，批量更新所有子节点的FullCode和FullName
                    if (old_FullCode != info.FullCode || old_FullName != info.FullName)
                    {
                        var childList = GetListBy(t => t.FullCode.StartsWith(old_FullCode) && t.TypeCode != info.TypeCode);
                        if (childList.IsZxxAny())
                        {
                            var childMap = childList.ToDictionary(t => t.TypeCode);
                            foreach (var child in childList.OrderBy(t => t.TreeLevel))
                            {
                                var parentFullCode = info.FullCode;
                                var parentFullName = info.FullName;
                                if (child.ParentId != info.TypeCode && childMap.TryGetValue(child.ParentId, out var parentChild))
                                {
                                    parentFullCode = parentChild.FullCode;
                                    parentFullName = parentChild.FullName;
                                }

                                child.FullCode = parentFullCode + child.TypeCode + "|";
                                child.FullName = parentFullName + "|" + child.TypeName;
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

        public override bool Insert(DeviceTypeEntity info)
        {
            bool isok = false;
            try
            {
                Db.BeginTran();

                var upinfo = InsertReturnEntity(info);
                if (!info.ParentId.IsZxxNullOrEmpty())
                {
                    var parent = GetOneBy(t => t.TypeCode == upinfo.ParentId);
                    if (parent != null)
                    {
                        upinfo.FullCode = parent.FullCode + upinfo.TypeCode + "|";
                        upinfo.FullName = parent.FullName + "|" + upinfo.TypeName;
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
                    upinfo.FullCode = "|" + upinfo.TypeCode + "|";
                    upinfo.FullName = upinfo.TypeName;
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

        public List<DeviceTypeEntity> GetChildList(List<DeviceTypeEntity> alllist, string pid)
        {
            var childlist = new List<DeviceTypeEntity>();

            var sublist = alllist.FindAll(t => t.ParentId == pid);
            if (sublist.Count == 0)
            {
                childlist.Add(alllist.Find(t => t.TypeCode == pid));
            }
            else
            {
                foreach (var sub in sublist)
                {
                    childlist.AddRange(GetChildList(alllist, sub.TypeCode));
                }
            }

            return childlist;
        }

        public bool DeleteById(string typecode)
        {
            bool isok = false;
            try
            {
                var list = GetListBy(t => t.FullCode.Contains($"|{typecode}|"));
                if (list.IsZxxAny())
                {
                    Db.BeginTran();

                    var ids = list.Select(t => t.TypeCode).Distinct().ToList();
                    DeleteBy(t => ids.Contains(t.TypeCode));
                    DeviceTypeAlarmConfigDAO.Instance.DeleteBy(t => ids.Contains(t.DeviceTypeCode));
                    isok = true;

                    Db.CommitTran();
                }
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

    }
}