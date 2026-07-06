using System.ComponentModel;

namespace IotWebApi.Areas.Admin.Models
{
    public class SysuserOutput
    {
        /// <summary>
		/// 用户ID
		///</summary>
		[DisplayName("用户ID")]
        public int UserId { get; set; }
        /// <summary>
        /// 角色ID
        ///</summary>
        [DisplayName("角色ID")]
        public int RoleId { get; set; }
        /// <summary>
        /// 角色名称
        ///</summary>
        [DisplayName("角色名称")]
        public string RoleName { get; set; }
        /// <summary>
        /// 账号
        ///</summary>
        [DisplayName("账号")]
        public string UserUid { get; set; }
        /// <summary>
        /// 昵称
        ///</summary>
        [DisplayName("昵称")]
        public string TrueName { get; set; }
        /// <summary>
        /// 性别
        ///</summary>
        [DisplayName("性别")]
        public string UserXb { get; set; }
        /// <summary>
        /// 手机号
        ///</summary>
        [DisplayName("手机号")]
        public string UserPhone { get; set; }
        /// <summary>
        /// 微信OpenId
        ///</summary>
        [DisplayName("微信OpenId")]
        public string WxOpenId { get; set; }
        /// <summary>
        /// 登录次数
        ///</summary>
        [DisplayName("登录次数")]
        public int LoginCount { get; set; }
        /// <summary>
        /// 上次登录时间
        ///</summary>
        [DisplayName("上次登录时间")]
        public string LastLoginTime { get; set; }
        /// <summary>
        /// 是否启用(1:启用 0:禁用)
        ///</summary>
        [DisplayName("是否启用(1:启用 0:禁用)")]
        public int IsEnable { get; set; }
        /// <summary>
        /// 是否在线(1:在线 0:不在线)
        ///</summary>
        [DisplayName("是否在线(1:在线 0:不在线)")]
        public int OnlineState { get; set; }
        /// <summary>
        /// 上次退出时间
        ///</summary>
        [DisplayName("上次退出时间")]
        public string LastOutTime { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        public int UnitId { get; set; }
        /// <summary>
        /// 单位名称
        ///</summary>
        [DisplayName("单位名称")]
        public string UnitName { get; set; }
        /// <summary>
        /// 备注
        ///</summary>
        [DisplayName("备注")]
        public string UserRemark { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        public string ExpandJson { get; set; }
    }
}
