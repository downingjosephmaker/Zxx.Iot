using System.ComponentModel;

namespace IotWebApi
{
    /// <summary>
    /// 密码加密后信息
    /// </summary>
    public class PwdInfo
    {
        /// <summary>
        /// MD5密码
        /// </summary>
        [DisplayName("MD5密码")]
        public string PwdMd5 { get; set; }
        /// <summary>
        /// RSA密码
        /// </summary>
        [DisplayName("RSA密码")]
        public string PwdRsa { get; set; }

    }
    /// <summary>
    /// 三方IOT账号密码
    /// </summary>
    public class IotUserInfo
    {
        /// <summary>
        /// IOT账号
        /// </summary>
        [DisplayName("IOT账号")]
        public string IOT_ID { get; set; }
        /// <summary>
        /// IOT密码
        /// </summary>
        [DisplayName("IOT密码")]
        public string IOT_KEY { get; set; }
    }
}
