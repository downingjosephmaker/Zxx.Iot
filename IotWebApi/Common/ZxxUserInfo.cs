namespace IotWebApi
{
    /// <summary>
    /// 用户登录会话信息（在线用户列表元素）。
    /// 原 ZhjngkModelOS.ZxxUserInfo 移除 OS 依赖后在主项目本地定义。
    /// </summary>
    public class ZxxUserInfo
    {
        /// <summary>用户ID</summary>
        public int UserId { get; set; }

        /// <summary>登录令牌</summary>
        public string Token { get; set; } = "";

        /// <summary>用户名</summary>
        public string UserName { get; set; } = "";

        /// <summary>客户端IP</summary>
        public string ClientIp { get; set; } = "";

        /// <summary>登录令牌对应的操作模型</summary>
        public OperatorModelLogin? OperatorModel { get; set; }
    }
}
