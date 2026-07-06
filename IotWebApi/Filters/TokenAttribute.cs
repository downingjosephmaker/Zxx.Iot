namespace IotWebApi
{
    /// <summary>
    /// 令牌验证
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TokenAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public TokenAttribute()
        {
        }
    }

    /// <summary>
    /// 不需要令牌验证
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class UnTokenAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public UnTokenAttribute()
        {
        }
    }

    /// <summary>
    /// 罪犯认证
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class StaffTokenAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public StaffTokenAttribute()
        {
        }
    }

    /// <summary>
    /// 三方物联网认证
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class IotTripartiteAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public IotTripartiteAttribute()
        {
        }
    }

    /// <summary>
    /// 不记录操作日志
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class NoOptLogAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public NoOptLogAttribute()
        {
        }
    }

}
