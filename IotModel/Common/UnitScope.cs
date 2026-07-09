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

        /// <summary>
        /// 当前租户ID；null 表示无用户上下文（后台任务/匿名端点），此时数据隔离不生效
        /// </summary>
        public static int? CurrentTenantId
        {
            get => _current.Value;
            set => _current.Value = value;
        }
    }
}
