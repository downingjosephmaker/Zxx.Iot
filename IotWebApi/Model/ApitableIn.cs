using System.ComponentModel;

namespace IotWebApi
{
    /// <summary>
    /// API信息
    /// </summary>
    [DisplayName("API信息")]
    public class ApitableIn
    {
        /// <summary>
        /// 主键(不显示)
        ///</summary>
        [DisplayName("主键")]
        public int Id { get; set; }
        /// <summary>
        /// 数据库名称
        ///</summary>
        [DisplayName("数据库名称")]
        public string DbNameXh { get; set; }
        /// <summary>
        /// API名称
        ///</summary>
        [DisplayName("API名称")]
        public string TbDescribe { get; set; }
        /// <summary>
        /// API地址
        ///</summary>
        [DisplayName("API地址")]
        public string ApiUrl { get; set; }
        /// <summary>
        /// 表所有字段josn
        ///</summary>
        [DisplayName("表所有字段josn")]
        public string TbFieldJosn { get; set; }
        /// <summary>
        /// 是否已申请
        /// </summary>
        [DisplayName("是否已申请")]
        public bool IsApply { get; set; }
    }
}
