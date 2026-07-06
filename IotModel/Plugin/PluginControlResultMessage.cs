using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 烟感控制结果回执，继承自 <see cref="PluginResultMessageBase"/>。
    /// 在通用回执字段基础上扩展烟感设备控制结果列表。
    /// </summary>
    public class PluginControlResultMessage : PluginResultMessageBase
    {
        /// <summary>
        /// 设备结果集合
        /// </summary>
        [DisplayName("设备结果集合")]
        public List<ControlDeviceResult> DeviceResults { get; set; } = new();
    }

    /// <summary>
    /// 单设备烟感控制结果，继承自 <see cref="PluginDeviceResultBase"/>。
    /// </summary>
    public class ControlDeviceResult : PluginDeviceResultBase
    {
    }
}
