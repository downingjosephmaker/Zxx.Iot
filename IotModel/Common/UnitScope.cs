using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IotModel
{
    /// <summary>
    /// 租户级数据隔离标记接口：实现后 DbContext 自动追加 TenantId 查询过滤，并在插入时回填 TenantId
    /// </summary>
    public interface ITenantEntity
    {
        /// <summary>
        /// 租户主键
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
        /// 当前登录者是否系统管理员（超管）。超管全局豁免租户隔离：DbContext 对其不追加 TenantId 过滤，看全部。
        /// 语义沿用旧机制（CustomActionFilter/BasicunitinfoController 的 IsSystem→不过滤）。
        /// 存量租户树平级（full_code 互不含），超管可见集会退化为只见自身，故超管必须靠此开关豁免而非可见集。
        /// </summary>
        public static bool CurrentIsSystem
        {
            get => _isSystem.Value;
            set => _isSystem.Value = value;
        }

        /// <summary>
        /// 装配当前请求的租户上下文：写入当前租户ID + 是否超管，并按 full_code 祖先链算出可见子孙集。
        /// 在认证注入点（TokenAuthorizationFilter）调用一次。子孙集不进 Token（会过期），每请求现算。
        /// 单位表带 [EntityCache] 走缓存，且自身不实现 ITenantEntity，查它不触发租户过滤，无循环依赖。
        /// </summary>
        /// <param name="tenantId">当前租户ID（来自 Token 的 TenantId）</param>
        /// <param name="isSystem">是否系统管理员（超管全局豁免隔离）</param>
        public static void Assign(int tenantId, bool isSystem = false)
        {
            _current.Value = tenantId;
            _isSystem.Value = isSystem;

            // 父见子孙：可见集 = 祖先链 full_code 含 |tenantId| 的所有租户（含自身）。
            // 与旧 BuildInfo 的 FullCode.Contains($"|{id}|") 级联逻辑一致。
            var self = "|" + tenantId + "|";
            var visible = BasicunitInfoDAO.Instance.GetList()
                .Where(u => u.FullCode != null && u.FullCode.Contains(self))
                .Select(u => u.TenantId)
                .ToList();
            // 兜底：若树未装配（full_code 空）或查不到自己，至少可见自身，绝不越权。
            if (!visible.Contains(tenantId)) visible.Add(tenantId);
            _visible.Value = visible;
        }
    }
}
