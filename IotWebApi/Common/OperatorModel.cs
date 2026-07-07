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

        /// <summary>
        /// 权限单位
        /// </summary>
        public List<BasicunitInfo> _UnitAllList = new List<BasicunitInfo>();

        /// <summary>
        /// 权限单位ID
        /// </summary>
        public List<int> _UnitIdList = new List<int>();

        /// <summary>
        /// 权限建筑
        /// </summary>
        public List<BuildInfo> _BuildAllList = new List<BuildInfo>();

        /// <summary>
        /// 单位ID和建筑信息集合
        /// </summary>
        public Dictionary<int, List<BuildInfo>> _BuildInfoDic { get; set; } = new Dictionary<int, List<BuildInfo>>();

        /// <summary>
        /// 权限建筑ID
        /// </summary>
        public List<int> _BuildIdList = new List<int>();

        /// <summary>
        /// 权限部门
        /// </summary>
        public List<DeptInfo> _DeptAllList = new List<DeptInfo>();

        /// <summary>
        /// 单位ID和部门信息集合
        /// </summary>
        public Dictionary<int, List<DeptInfo>> _DeptInfoDic { get; set; } = new Dictionary<int, List<DeptInfo>>();

        /// <summary>
        /// 权限部门ID
        /// </summary>
        public List<int> _DeptIdList = new List<int>();

        /// <summary>
        /// 组织查询级别(1:多公司 2：公司 3：部门)
        /// </summary>
        public int DepartSelectLevel { get; set; } = 3;
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
        /// 当前单位ID
        ///</summary>
        public int UnitId { get; set; }

        /// <summary>
        /// 当前单位名称
        ///</summary>
        public string UnitName { get; set; }

        /// <summary>
        /// 请求来源：1:Web 2:Android 3:APP 
        /// </summary>
        public string SourceType { get; set; }

        /// <summary>
        /// 登录时间
        /// </summary>
        public DateTime LoginTime { get; set; }

        /// <summary>
        /// 单位总数量(不显示:1  弹框:>1)
        ///</summary>
        public int UnitAllCount { get; set; }

        /// <summary>
        /// 用户验证TOKEN
        /// </summary>
        public string LoginToken { get; set; }

        /// <summary>
        /// 令牌软过期时间(前端到点走GetRefreshToken无感换签;服务端硬窗口仍由tokentimeouthour裁决)
        /// </summary>
        public DateTime TokenExpireTime { get; set; }
        /// <summary>
        /// 大屏跳转路径
        ///</summary>
        [DisplayName("大屏跳转路径")]
        public string RouterPath { get; set; }
    }

    /// <summary>
    /// 罪犯登录
    /// </summary>
    public class OperatorStaffModel
    {
        /// <summary>
        /// 罪犯ID
        /// </summary>
        [DisplayName("罪犯ID")]
        public string StaffUid { get; set; }

        /// <summary>
        /// 罪犯编号
        /// </summary>
        [DisplayName("罪犯编号")]
        public string StaffCode { get; set; }

        /// <summary>
        /// 罪犯姓名
        /// </summary>
        [DisplayName("罪犯姓名")]
        public string StaffName { get; set; }

        /// <summary>
        /// 部门编码
        ///</summary>
        [DisplayName("部门编码")]
        public string DeptCode { get; set; }

        /// <summary>
        /// 登录时间
        /// </summary>
        public DateTime LoginTime { get; set; }

        /// <summary>
        /// StaffToken(传入Headers)
        /// </summary>
        public string StaffToken { get; set; }

    }

    /// <summary>
    /// StaffToken转换后对象
    /// </summary>
    public class OperatorStaff : OperatorStaffModel
    {
        /// <summary>
        /// 权限单位
        /// </summary>
        public BasicunitInfo unit = null;

        /// <summary>
        /// 权限部门
        /// </summary>
        public DeptInfo dept = null;
    }

}
