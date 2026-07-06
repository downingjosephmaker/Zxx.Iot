using System.Threading;

namespace IotModel
{
    /// <summary>
    /// 单位级数据隔离标记接口：实现后 DbContext 自动追加 UnitId 查询过滤，并在插入时回填 UnitId
    /// </summary>
    public interface IUnitEntity
    {
        /// <summary>
        /// 单位主键
        /// </summary>
        int UnitId { get; set; }
    }

    /// <summary>
    /// 当前请求的单位上下文。TokenAuthorizationFilter 认证通过后写入，
    /// AsyncLocal 随请求的异步流传递；后台任务/插件无用户上下文时为 null（不过滤）。
    /// </summary>
    public static class UnitScope
    {
        private static readonly AsyncLocal<int?> _current = new AsyncLocal<int?>();

        /// <summary>
        /// 当前单位ID；null 表示无用户上下文（后台任务/匿名端点），此时数据隔离不生效
        /// </summary>
        public static int? CurrentUnitId
        {
            get => _current.Value;
            set => _current.Value = value;
        }
    }
}
