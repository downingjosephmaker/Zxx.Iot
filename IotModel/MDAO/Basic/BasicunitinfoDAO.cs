using CenBoCommon.Zxx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IotModel
{
    public sealed partial class BasicunitInfoDAO : FullEntityContext<BasicunitInfoEntity>
    {
        private static BasicunitInfoDAO instance;
        public static BasicunitInfoDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BasicunitInfoDAO();
                }
                return instance;
            }
        }

        public override void Init(object[] objs)
        {
            try
            {
                if (_dbContext == null) _dbContext = objs[0];
                string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                // 树形字段预计算后基类直插:种子期在DbContext静态锁内,
                // 不可走重写Insert的BeginTran事务树形逻辑;建表后序列必从1起步,unit_id=1有保证
                var list = new List<BasicunitInfoEntity>
                {
                    new BasicunitInfoEntity
                    {
                        ParentId = 0,
                        TreeLevel = 1,
                        FullCode = "|1|",
                        FullName = "开发单位",
                        UnitName = "开发单位",
                        AreaId = "|330000|330100|330106|",
                        AreaName = "浙江省|杭州市|西湖区",
                        CreateId = 1,
                        CreateTime = time,
                        CreateName = "开发管理员",
                        UpdateId = 1,
                        UpdateTime = time,
                        UpdateName = "开发管理员"
                    }
                };
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

        /// <summary>
        /// 新增租户：插入后按 parent_id 算 full_code/full_name/tree_level 并回填，维护父级 has_child。
        /// 树形逻辑照搬旧 BuildInfoDAO 范式（决策 B1 父见子孙的祖先链基础）。
        /// </summary>
        public override bool Insert(BasicunitInfoEntity info)
        {
            bool isok = false;
            try
            {
                Db.BeginTran();

                var upinfo = InsertReturnEntity(info);
                if (upinfo.ParentId > 0)
                {
                    var parent = GetOneBy(t => t.TenantId == upinfo.ParentId);
                    if (parent != null)
                    {
                        upinfo.FullCode = parent.FullCode + upinfo.TenantId + "|";
                        upinfo.FullName = parent.FullName + "|" + upinfo.UnitName;
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
                    upinfo.FullCode = "|" + upinfo.TenantId + "|";
                    upinfo.FullName = upinfo.UnitName;
                    upinfo.TreeLevel = 1;
                }

                isok = UpdateColumns(upinfo, it => new
                {
                    it.FullCode,
                    it.FullName,
                    it.TreeLevel,
                });

                Db.CommitTran();
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

        /// <summary>
        /// 修改租户：父级变化时重算 full_code/full_name/tree_level，并级联更新所有子孙节点。
        /// </summary>
        public override bool Update(BasicunitInfoEntity info)
        {
            bool isok = false;
            try
            {
                var entity = GetOneBy(t => t.TenantId == info.TenantId);
                var old_FullCode = entity.FullCode;
                var old_FullName = entity.FullName;
                var old_ParentId = entity.ParentId;
                if (info.ParentId > 0)
                {
                    var parent = GetOneBy(t => t.TenantId == info.ParentId);
                    if (parent != null)
                    {
                        info.FullCode = parent.FullCode + info.TenantId + "|";
                        info.FullName = parent.FullName + "|" + info.UnitName;
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
                    info.FullCode = "|" + info.TenantId + "|";
                    info.FullName = info.UnitName;
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
                        var oldParent = GetOneBy(t => t.TenantId == old_ParentId);
                        if (oldParent != null)
                        {
                            oldParent.HasChild = GetListBy(t => t.ParentId == old_ParentId).IsZxxAny();
                            UpdateColumns(oldParent, it => new { it.HasChild });
                        }
                    }

                    // 当前节点 FullCode/FullName 变更后，级联重算所有子孙节点
                    if (old_FullCode != info.FullCode || old_FullName != info.FullName)
                    {
                        var childList = GetListBy(t => t.FullCode.StartsWith(old_FullCode) && t.TenantId != info.TenantId);
                        if (childList.IsZxxAny())
                        {
                            var childMap = childList.ToDictionary(t => t.TenantId);
                            foreach (var child in childList.OrderBy(t => t.TreeLevel))
                            {
                                var parentFullCode = info.FullCode;
                                var parentFullName = info.FullName;
                                if (child.ParentId != info.TenantId && childMap.TryGetValue(child.ParentId, out var parentChild))
                                {
                                    parentFullCode = parentChild.FullCode;
                                    parentFullName = parentChild.FullName;
                                }

                                child.FullCode = parentFullCode + child.TenantId + "|";
                                child.FullName = parentFullName + "|" + child.UnitName;
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

    }
}