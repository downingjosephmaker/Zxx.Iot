using System.ComponentModel;

namespace IotWebApi.Services.Jobs
{
    /// <summary>
    /// 用户状态信息模型
    /// </summary>
    public class ZxxUserInfo
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [DisplayName("用户ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 用户名称
        /// </summary>
        [DisplayName("用户名称")]
        public string UserName { get; set; }

        /// <summary>
        /// 用户SignalR
        /// </summary>
        [DisplayName("用户SignalR")]
        public string UserSignalR { get; set; }

        /// <summary>
        /// 用户Token
        /// </summary>
        [DisplayName("用户Token")]
        public string Token { get; set; }

        /// <summary>
        /// 客户端Ip
        /// </summary>
        [DisplayName("客户端Ip")]
        public string ClientIp { get; set; }

        /// <summary>
        /// 用户登录模型
        /// </summary>
        [DisplayName("用户登录模型")]
        public OperatorModelLogin OperatorModel { get; set; }

    }
}
