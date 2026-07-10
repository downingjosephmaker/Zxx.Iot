using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 用户信息表
    ///</summary>
    [DisplayName("用户信息表")]
    [EntityCache]
    [SugarTable(TableName = "sys_user", TableDescription = "用户信息表", IsDisabledUpdateAll = true)]
    public class SysUser : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// 用户ID
        ///</summary>
        [DisplayName("用户ID")]
        [SugarColumn(ColumnName = "user_id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "用户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int UserId { get; set; }
        /// <summary>
        /// 角色ID
        ///</summary>
        [DisplayName("角色ID")]
        [SugarColumn(ColumnName = "role_id", ColumnDescription = "角色ID", DefaultValue = "0", ColumnDataType = "int")]
        public int RoleId { get; set; }
        /// <summary>
        /// 账号
        ///</summary>
        [DisplayName("账号")]
        [SugarColumn(ColumnName = "user_uid", Length = 50, ColumnDescription = "账号", DefaultValue = "", ColumnDataType = "varchar")]
        public string UserUid { get; set; }
        /// <summary>
        /// 登录密码
        ///</summary>
        [DisplayName("登录密码")]
        [SugarColumn(ColumnName = "password", Length = 128, ColumnDescription = "登录密码", DefaultValue = "", ColumnDataType = "varchar")]
        public string Password { get; set; }
        /// <summary>
        /// 密码盐
        ///</summary>
        [DisplayName("密码盐")]
        [SugarColumn(ColumnName = "password_salt", Length = 32, ColumnDescription = "密码盐", DefaultValue = "", ColumnDataType = "varchar")]
        public string PasswordSalt { get; set; }
        /// <summary>
        /// 昵称
        ///</summary>
        [DisplayName("昵称")]
        [SugarColumn(ColumnName = "true_name", IsNullable = true, Length = 50, ColumnDescription = "昵称", DefaultValue = "", ColumnDataType = "varchar")]
        public string TrueName { get; set; }
        /// <summary>
        /// 性别
        ///</summary>
        [DisplayName("性别")]
        [SugarColumn(ColumnName = "user_xb", IsNullable = true, Length = 10, ColumnDescription = "性别", DefaultValue = "", ColumnDataType = "varchar")]
        public string UserXb { get; set; }
        /// <summary>
        /// 手机号
        ///</summary>
        [DisplayName("手机号")]
        [SugarColumn(ColumnName = "user_phone", IsNullable = true, Length = 11, ColumnDescription = "手机号", DefaultValue = "", ColumnDataType = "varchar")]
        public string UserPhone { get; set; }
        /// <summary>
        /// 微信OpenId
        ///</summary>
        [DisplayName("微信OpenId")]
        [SugarColumn(ColumnName = "wx_open_id", IsNullable = true, Length = 50, ColumnDescription = "微信OpenId", DefaultValue = "", ColumnDataType = "varchar")]
        public string WxOpenId { get; set; }
        /// <summary>
        /// 登录次数
        ///</summary>
        [DisplayName("登录次数")]
        [SugarColumn(ColumnName = "login_count", ColumnDescription = "登录次数", DefaultValue = "0", ColumnDataType = "int")]
        public int LoginCount { get; set; }
        /// <summary>
        /// 上次登录时间
        ///</summary>
        [DisplayName("上次登录时间")]
        [SugarColumn(ColumnName = "last_login_time", IsNullable = true, Length = 20, ColumnDescription = "上次登录时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string LastLoginTime { get; set; }
        /// <summary>
        /// 是否启用(1:启用 0:禁用)
        ///</summary>
        [DisplayName("是否启用(1:启用 0:禁用)")]
        [IntRange(0, 1, ErrorMessage = "属性值只能为0或1")]
        [SugarColumn(ColumnName = "is_enable", ColumnDescription = "是否启用(1:启用 0:禁用)", DefaultValue = "1", ColumnDataType = "int")]
        public int IsEnable { get; set; }
        /// <summary>
        /// 是否在线(1:在线 0:不在线)
        ///</summary>
        [DisplayName("是否在线(1:在线 0:不在线)")]
        [IntRange(0, 1, ErrorMessage = "属性值只能为0或1")]
        [SugarColumn(ColumnName = "online_state", ColumnDescription = "是否在线(1:在线 0:不在线)", DefaultValue = "0", ColumnDataType = "int")]
        public int OnlineState { get; set; }
        /// <summary>
        /// 上次退出时间
        ///</summary>
        [DisplayName("上次退出时间")]
        [SugarColumn(ColumnName = "last_out_time", IsNullable = true, Length = 20, ColumnDescription = "上次退出时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string LastOutTime { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }
        /// <summary>
        /// 单位名称
        ///</summary>
        [DisplayName("单位名称")]
        [SugarColumn(ColumnName = "unit_name", IsNullable = true, Length = 100, ColumnDescription = "单位名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string UnitName { get; set; }
        /// <summary>
        /// 备注
        ///</summary>
        [DisplayName("备注")]
        [SugarColumn(ColumnName = "user_remark", IsNullable = true, Length = 300, ColumnDescription = "备注", DefaultValue = "", ColumnDataType = "varchar")]
        public string UserRemark { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [JsonField(typeof(Expand_SysUser))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}