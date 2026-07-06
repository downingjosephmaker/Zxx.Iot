using System.ComponentModel;

namespace IotWebApi.Areas.Admin.Models
{
    /// <summary>
    /// 查询通用条件
    /// </summary>
    public class SelectParam
    {
        /// <summary>
        /// 页码
        /// </summary>
        [DisplayName("页码")]
        public int page { get; set; }
        /// <summary>
        /// 行数
        /// </summary>
        [DisplayName("行数")]
        public int pagesize { get; set; }
    }

    /// <summary>
    /// 用户查询条件
    /// </summary>
    public class UserParam : SelectParam
    {
        /// <summary>
        /// 角色ID
        ///</summary>
        [DisplayName("角色ID")]
        public int RoleId { get; set; }
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

    }

    /// <summary>
    /// 菜单查询条件
    /// </summary>
    public class MenuParam : SelectParam
    {
        /// <summary>
        /// 菜单编码
        ///</summary>
        [DisplayName("菜单编码")]
        public string MenuCode { get; set; }
        /// <summary>
        /// 菜单名称
        ///</summary>
        [DisplayName("菜单名称")]
        public string MenuName { get; set; }
    }

    /// <summary>
    /// 按钮查询条件
    /// </summary>
    public class ButtonParam : SelectParam
    {
        /// <summary>
        /// 按钮编码
        ///</summary>
        [DisplayName("按钮编码")]
        public string ButtonCode { get; set; }
        /// <summary>
        /// 按钮名称
        ///</summary>
        [DisplayName("按钮名称")]
        public string ButtonName { get; set; }
    }

}
