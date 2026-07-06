using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 用户表拓展类
    ///</summary>
    [DisplayName("用户表拓展类")]
    [Expand]
    public class Expand_SysUser
    {
        /// <summary>
        /// 人脸照片地址
        ///</summary>
        [DisplayName("人脸照片地址")]
        public string UserPhoto { get; set; }
        /// <summary>
        /// 人脸特征地址
        ///</summary>
        [DisplayName("人脸特征地址")]
        public string FacFeaturePath { get; set; }
    }
}
