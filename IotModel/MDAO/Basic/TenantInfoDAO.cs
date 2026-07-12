using CenBoCommon.Zxx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IotModel
{
    public sealed partial class TenantInfoDAO : DbContext<TenantInfo>
    {
        private static TenantInfoDAO instance;
        public static TenantInfoDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TenantInfoDAO();
                }
                return instance;
            }
        }

        public override void Init()
        {
            try
            {
                string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                // 树形字段预计算后基类直插:种子期在DbContext静态锁内,
                // 不可走重写Insert的事务树形逻辑;建表后序列必从1起步,tenant_id=1有保证
                var list = new List<TenantInfo>
                {
                    new TenantInfo
                    {
                        ParentId = 0,
                        TreeLevel = 1,
                        FullCode = "|1|",
                        FullName = "开发租户",
                        TenantName = "开发租户",
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
        /// TranAction 确保插入与树字段回填同事务（事务连接由 _transactionDb 统一提供）。
        /// </summary>
        public override bool Insert(TenantInfo info)
        {
            bool isok = false;
            try
            {
                TranAction(() =>
                {
                    var upinfo = InsertReturnEntity(info);
                    if (upinfo.ParentId > 0)
                    {
                        var parent = GetOneBy(t => t.TenantId == upinfo.ParentId);
                        if (parent != null)
                        {
                            upinfo.FullCode = parent.FullCode + upinfo.TenantId + "|";
                            upinfo.FullName = parent.FullName + "|" + upinfo.TenantName;
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
                        upinfo.FullName = upinfo.TenantName;
                        upinfo.TreeLevel = 1;
                    }

                    isok = UpdateColumns(upinfo, it => new
                    {
                        it.FullCode,
                        it.FullName,
                        it.TreeLevel,
                    });
                });
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

        /// <summary>
        /// 修改租户：父级变化时重算 full_code/full_name/tree_level，并级联更新所有子孙节点。
        /// </summary>
        public override bool Update(TenantInfo info)
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
                        info.FullName = parent.FullName + "|" + info.TenantName;
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
                    info.FullName = info.TenantName;
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
                                child.FullName = parentFullName + "|" + child.TenantName;
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
