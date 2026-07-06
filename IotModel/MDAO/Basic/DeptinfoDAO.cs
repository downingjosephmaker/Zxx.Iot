using CenBoCommon.Zxx;
using NewLife.Log;
using NewLife.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IotModel
{
    public sealed partial class DeptInfoDAO : DbContext<DeptInfo>
    {
        private static DeptInfoDAO instance;
        public static DeptInfoDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeptInfoDAO();
                }
                return instance;
            }
        }

        public override bool Update(DeptInfo info)
        {
            bool isok = false;
            try
            {
                var entity = GetOneBy(t => t.DeptId == info.DeptId);
                var old_FullCode = entity.FullCode;
                var old_FullName = entity.FullName;
                var old_ParentId = entity.ParentId;
                if (info.ParentId > 0)
                {
                    var parent = GetOneBy(t => t.DeptId == info.ParentId);
                    if (parent != null)
                    {
                        info.FullCode = parent.FullCode + info.DeptId + "|";
                        info.FullName = parent.FullName + "|" + info.DeptName;
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
                    info.FullCode = "|" + info.DeptId + "|";
                    info.FullName = info.DeptName;
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
                        var oldParent = GetOneBy(t => t.DeptId == old_ParentId);
                        if (oldParent != null)
                        {
                            oldParent.HasChild = GetListBy(t => t.ParentId == old_ParentId).IsZxxAny();
                            UpdateColumns(oldParent, it => new { it.HasChild });
                        }
                    }

                    //如果当前节点的FullCode和FullName变更后，批量更新所有子节点的FullCode和FullName
                    if (old_FullCode != info.FullCode || old_FullName != info.FullName)
                    {
                        var childList = GetListBy(t => t.FullCode.StartsWith(old_FullCode) && t.DeptId != info.DeptId);
                        if (childList.IsZxxAny())
                        {
                            var childMap = childList.ToDictionary(t => t.DeptId);
                            foreach (var child in childList.OrderBy(t => t.TreeLevel))
                            {
                                var parentFullCode = info.FullCode;
                                var parentFullName = info.FullName;
                                if (child.ParentId != info.DeptId && childMap.TryGetValue(child.ParentId, out var parentChild))
                                {
                                    parentFullCode = parentChild.FullCode;
                                    parentFullName = parentChild.FullName;
                                }

                                child.FullCode = parentFullCode + child.DeptId + "|";
                                child.FullName = parentFullName + "|" + child.DeptName;
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

        public override bool Insert(DeptInfo info)
        {
            bool isok = false;
            try
            {
                Db.BeginTran();

                var upinfo = InsertReturnEntity(info);
                if (upinfo.ParentId > 0)
                {
                    var parent = GetOneBy(t => t.DeptId == upinfo.ParentId);
                    if (parent != null)
                    {
                        upinfo.FullCode = parent.FullCode + upinfo.DeptId + "|";
                        upinfo.FullName = parent.FullName + "|" + upinfo.DeptName;
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
                    upinfo.FullCode = "|" + upinfo.DeptId + "|";
                    upinfo.FullName = upinfo.DeptName;
                    upinfo.TreeLevel = 1;
                }
                if (upinfo.SortBorder.IsZxxNullOrEmpty())
                {
                    var list = GetListBy(t => t.UnitId == upinfo.UnitId);
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

        public List<DeptInfo> GetChildList(List<DeptInfo> alllist, int pid)
        {
            var childlist = new List<DeptInfo>();

            var sublist = alllist.FindAll(t => t.ParentId == pid);
            if (sublist.Count == 0)
            {
                childlist.Add(alllist.Find(t => t.DeptId == pid));
            }
            else
            {
                foreach (var sub in sublist)
                {
                    childlist.AddRange(GetChildList(alllist, sub.DeptId));
                }
            }

            return childlist;
        }

        private DeptInfoEntity GetEntity(DeptInfo deviceType)
        {
            var entity = new DeptInfoEntity();
            try
            {
                deviceType.CopyTypeValue(entity);
                if (!string.IsNullOrEmpty(deviceType.ExpandJson))
                {
                    entity.ExpandObjects.AddRange(deviceType.ExpandJson.ToObject<List<Expand_DeptBuild>>());
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            return entity;
        }

        /// <summary>
        /// 获取单个设备类型实体
        /// </summary>
        public DeptInfoEntity GetEntityOneBy(Expression<Func<DeptInfo, bool>> wheres)
        {
            var deviceType = GetOneBy(wheres);
            if (deviceType == null) return null;
            return GetEntity(deviceType);
        }

        public List<DeptInfoEntity> GetEntityListByPage(ActionPara model, ref int total)
        {
            var deviceTypes = GetListByPage(model, ref total);
            if (deviceTypes == null) return null;

            return deviceTypes.Select(deviceType =>
            {
                return GetEntity(deviceType);
            }).ToList();
        }

    }
}