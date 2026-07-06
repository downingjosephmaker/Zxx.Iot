using System.ComponentModel;

namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 参数下拉框
    /// </summary>
    public class ParamInfoCode
    {
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 参数编码
        /// </summary>
        [DisplayName("参数编码")]
        public string ParamCode { get; set; }
        /// <summary>
        /// 参数名称
        /// </summary>
        [DisplayName("参数名称")]
        public string ParamName { get; set; }
        /// <summary>
        /// 参数单位
        ///</summary>
        [DisplayName("参数单位")]
        public string ParamUnit { get; set; }
    }

    /// <summary>
    /// 设备参数分类下拉框
    /// </summary>
    public class ParamTypeInfo
    {
        /// <summary>
        /// 参数集合
        /// </summary>
        [DisplayName("参数集合")]
        public List<ParamInfoCode> ParamIds { get; set; } = new List<ParamInfoCode>();
        /// <summary>
        /// 参数分类名称
        /// </summary>
        [DisplayName("参数分类名称")]
        public string ParamTypeName { get; set; }
    }
}
