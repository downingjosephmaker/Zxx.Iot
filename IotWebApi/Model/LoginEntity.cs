using System.ComponentModel;

namespace IotWebApi
{
    /// <summary>
    /// 登录模型
    /// </summary>
    public class LoginEntity
    {
        /// <summary>
        /// 账号
        /// </summary>
        [DisplayName("账号")]
        [DefaultValue("superadmin")]
        public string UserUid { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [DisplayName("密码")]
        [DefaultValue("V8o2Eo5SFDRM9FbjbpYk7aohGVL+Xgfz5ksdGOjez47t6y2RIJKX/t+Bo8uI+HCWwAblKP12dfxUq1PsIYiDWmtkIxtrQU7/QZcBvFcF1vZqF6cDirmbqKr9eEW3Xiuu0zfZyp4GhBxwsiNag5vqTpaf8xWfKUvtSldHqurx4/k=")]
        public string UserPwd { get; set; }

        /// <summary>
        /// 请求来源
        /// </summary>
        [DisplayName("请求来源")]
        [DefaultValue(SourceType.Web)]
        public SourceType SourceType { get; set; }
    }

    /// <summary>
    /// 请求来源枚举
    /// </summary>
    public enum SourceType
    {
        [Description("网页")]
        Web = 1,
        [Description("一体机")]
        Android = 2,
        [Description("手机APP")]
        APP = 3,
        [Description("微信")]
        Wx = 4,
        [Description("H5页面")]
        H5 = 5,
    }

    /// <summary>
    /// 
    /// </summary>
    public class LoginFace
    {
        /// <summary>
        /// 登录类型(2:蓝牙 1:人脸)
        /// </summary>
        [DisplayName("登录类型(2:蓝牙 1:人脸)")]
        public int LoginType { get; set; }
        /// <summary>
        /// 账号
        /// </summary>
        [DisplayName("账号")]
        public string UserUid { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        [DisplayName("密码")]
        public string UserPwd { get; set; }
        /// <summary>
        /// 蓝牙编号
        /// </summary>
        [DisplayName("蓝牙编号")]
        public string BluetoothCode { get; set; }
        /// <summary>
        /// 请求来源
        /// </summary>
        [DisplayName("请求来源")]
        [DefaultValue(SourceType.Android)]
        public SourceType SourceType { get; set; }
    }

}
