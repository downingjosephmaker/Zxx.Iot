using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 用户表完整类
    ///</summary>
    [DisplayName("用户表完整类")]
    [FullEntity]
    public class SysUserEntity : SysUser
    {
        /// <summary>
        /// 用户表拓展类
        ///</summary>
        [DisplayName("用户表拓展类")]
        public Expand_SysUser ExpandObject { get; set; } = new Expand_SysUser();
    }
}
