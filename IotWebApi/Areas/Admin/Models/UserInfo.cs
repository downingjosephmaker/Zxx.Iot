using System.ComponentModel;

namespace IotWebApi.Areas.Admin.Models
{
    public class UserInfo
    {
        /// <summary>
        /// 用户ID
        ///</summary>
        [DisplayName("用户ID")]
        public int UserId { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        public int UnitId { get; set; }
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    public class ChangePasswordDto
    {
        /// <summary>
        /// 旧密码
        ///</summary>
        [DisplayName("旧密码")]
        public string OldPwd { get; set; }

        /// <summary>
        /// 新密码
        ///</summary>
        [DisplayName("新密码")]
        public string NewPwd { get; set; }
    }

}
