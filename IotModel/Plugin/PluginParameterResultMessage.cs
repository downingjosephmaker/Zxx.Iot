using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 参数下发结果回执，继承自 <see cref="PluginResultMessageBase"/>。
    /// 在通用回执字段基础上扩展参数编码、参数值、备注和设备结果列表。
    /// </summary>
    public class PluginParameterResultMessage : PluginResultMessageBase
    {
        /// <summary>
        /// 参数编码
        /// </summary>
        [DisplayName("参数编码")]
        public string ParamCode { get; set; } = "";

        /// <summary>
        /// 参数值
        /// </summary>
        [DisplayName("参数值")]
        public string ParamValue { get; set; } = "";

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        public string Remark { get; set; } = "";

        /// <summary>
        /// 设备结果集合
        /// </summary>
        [DisplayName("设备结果集合")]
        public List<PluginParameterDeviceResult> DeviceResults { get; set; } = new();
    }

    /// <summary>
    /// 单设备参数下发结果，继承自 <see cref="PluginDeviceResultBase"/>。
    /// </summary>
    public class PluginParameterDeviceResult : PluginDeviceResultBase
    {
    }
}
