using SqlSugar;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IotModel
{
    /// <summary>
    /// 租户级数据隔离标记接口：实现后 DbContext 自动追加租户查询过滤，并在插入时回填 TenantId
    /// </summary>
    public interface ITenantEntity
    {
        /// <summary>
        /// 租户主键(0=平台共享，决策B)
        /// </summary>
        int TenantId { get; set; }
    }

    /// <summary>
    /// 当前请求的租户上下文。TokenAuthorizationFilter 认证通过后写入，
    /// AsyncLocal 随请求的异步流传递；后台任务/插件无用户上下文时为 null（不过滤）。
    /// </summary>
    public static class TenantScope
    {
        private static readonly AsyncLocal<int?> _current = new AsyncLocal<int?>();
        private static readonly AsyncLocal<List<int>> _visible = new AsyncLocal<List<int>>();
        private static readonly AsyncLocal<bool> _isSystem = new AsyncLocal<bool>();

        /// <summary>
        /// 当前租户ID；null 表示无用户上下文（后台任务/匿名端点），此时数据隔离不生效。
        /// 插入回填以此为准（一条数据只归属单个租户，非子孙集）。
        /// </summary>
        public static int? CurrentTenantId
        {
            get => _current.Value;
            set => _current.Value = value;
        }

        /// <summary>
        /// 当前可见租户集（当前租户 + 所有子孙，决策 B1 父见子孙）。
        /// 登录/上下文装配时按 full_code 祖先链现算并写入；查询过滤器读此集做 IN 过滤。
        /// 未装配时返回 [CurrentTenantId]（退化为只见自己，安全默认，绝不越权）。
        /// </summary>
        public static List<int> CurrentVisibleTenantIds
        {
            get
            {
                var v = _visible.Value;
                if (v != null && v.Count > 0) return v;
                var cur = _current.Value;
                return cur.HasValue ? new List<int> { cur.Value } : new List<int>();
            }
            set => _visible.Value = value;
        }

        /// <summary>
        /// 当前登录者是否系统管理员（超管）。超管全局豁免租户隔离：DbContext 对其不追加租户过滤，看全部。
        /// 存量租户树平级（full_code 互不含）时超管可见集会退化为只见自身，故超管必须靠此开关豁免而非可见集。
        /// </summary>
        public static bool CurrentIsSystem
        {
            get => _isSystem.Value;
            set => _isSystem.Value = value;
        }

        /// <summary>
        /// 装配当前请求的租户上下文：写入当前租户ID + 是否超管，并按 full_code 祖先链算出可见子孙集。
        /// 在认证注入点（TokenAuthorizationFilter）调用一次。子孙集不进 Token（会过期），每请求现算。
        /// 租户表带 [EntityCache] 走缓存，且自身不实现 ITenantEntity，查它不触发租户过滤，无循环依赖。
        /// </summary>
        /// <param name="tenantId">当前租户ID（来自 Token 的 TenantId）</param>
        /// <param name="isSystem">是否系统管理员（超管全局豁免隔离）</param>
        public static void Assign(int tenantId, bool isSystem = false)
        {
            _current.Value = tenantId;
            _isSystem.Value = isSystem;

            // 父见子孙：可见集 = 祖先链 full_code 含 |tenantId| 的所有租户（含自身）
            var self = "|" + tenantId + "|";
            var visible = TenantInfoDAO.Instance.GetList()
                .Where(u => u.FullCode != null && u.FullCode.Contains(self))
                .Select(u => u.TenantId)
                .ToList();
            // 兜底：若树未装配（full_code 空）或查不到自己，至少可见自身，绝不越权。
            if (!visible.Contains(tenantId)) visible.Add(tenantId);
            _visible.Value = visible;
        }
    }

    /// <summary>
    /// 租户隔离接线器：给 SqlSugar 客户端挂查询过滤 + 插入回填。
    /// 挂载点：DbContext&lt;T&gt;.GetOperDb / TranAction 的 CopyNew 实例（CopyNew 不继承 AOP 与过滤器，须逐个挂），
    /// 以及 SqlSugar_Db 创建 Scope 时挂一份，覆盖直接使用 Db 做联表等自定义查询的路径。
    /// Fastest/BulkCopy 与 Ado 原生 SQL 不经过 AOP 与过滤器，调用方须显式赋 TenantId。
    /// </summary>
    public static class TenantIsolation
    {
        /// <summary>
        /// 当前请求是否豁免隔离：超管看全部；无用户上下文（后台任务/种子期/匿名端点）不过滤。
        /// 过滤表达式内引用本属性，SqlSugar 每次生成 SQL 时按当次请求现值求值。
        /// </summary>
        public static bool FilterOff => TenantScope.CurrentIsSystem || TenantScope.CurrentTenantId == null;

        /// <summary>
        /// 挂载租户隔离。查询过滤：tenant_id=0（平台共享，决策B）或落在可见集（父见子孙，决策C）；
        /// 插入回填：非超管且 TenantId==0 时盖当前租户；超管保留显式值（0=平台级记录）。
        /// </summary>
        public static void Attach(ISqlSugarClient db)
        {
            db.QueryFilter.AddTableFilter<ITenantEntity>(it =>
                FilterOff || it.TenantId == 0 || TenantScope.CurrentVisibleTenantIds.Contains(it.TenantId));

            db.Aop.DataExecuting = (oldValue, info) =>
            {
                if (info.OperationType == DataFilterType.InsertByObject
                    && info.PropertyName == nameof(ITenantEntity.TenantId)
                    && info.EntityValue is ITenantEntity te
                    && te.TenantId == 0
                    && !TenantScope.CurrentIsSystem
                    && TenantScope.CurrentTenantId is int cur && cur > 0)
                {
                    info.SetValue(cur);
                }
            };
        }
    }
}
