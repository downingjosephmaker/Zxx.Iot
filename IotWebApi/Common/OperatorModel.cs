using System.ComponentModel;
using IotModel;

namespace IotWebApi
{
    /// <summary>
    /// Token转换后对象
    /// </summary>
    public class OperatorModel : OperatorModelLogin
    {
        /// <summary>
        /// 用户信息
        /// </summary>
        public SysUserEntity _Sysuser { get; set; }

        /// <summary>
        /// 角色信息
        /// </summary>
        public SysRole _Sysrole { get; set; }
    }

    /// <summary>
    /// 登录后返回
    /// </summary>
    public class OperatorModelLogin
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        /// 用户姓名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 是否系统管理员
        /// </summary>
        public bool IsSystem { get; set; }

        /// <summary>
        /// 当前租户ID（Token 载荷字段，整库重建后旧 Token 全部失效，无兼容垫片）
        ///</summary>
        public int TenantId { get; set; }

        /// <summary>
        /// 当前租户名称
        ///</summary>
        public string TenantName { get; set; }

        /// <summary>
        /// 请求来源：1:Web 2:Android 3:APP 
        /// </summary>
        public string SourceType { get; set; }

        /// <summary>
        /// 登录时间
        /// </summary>
        public DateTime LoginTime { get; set; }

        /// <summary>
        /// 用户验证TOKEN
        /// </summary>
        public string LoginToken { get; set; }

        /// <summary>
        /// 令牌软过期时间(前端到点走GetRefreshToken无感换签;服务端硬窗口仍由tokentimeouthour裁决)
        /// </summary>
        public DateTime TokenExpireTime { get; set; }
    }
}
