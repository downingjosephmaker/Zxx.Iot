using System;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 单设备操作结果基类。
    /// 所有设备级的操作结果（参数下发结果、控制结果等）均应继承此类，
    /// 统一提供设备标识、成功标志、结果描述和时间戳。
    /// </summary>
    public abstract class PluginDeviceResultBase
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        [DisplayName("设备ID")]
        public int DeviceId { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        [DisplayName("设备名称")]
        public string DeviceName { get; set; } = "";

        /// <summary>
        /// 是否成功
        /// </summary>
        [DisplayName("是否成功")]
        public bool Success { get; set; }

        /// <summary>
        /// 结果描述
        /// </summary>
        [DisplayName("结果描述")]
        public string Message { get; set; } = "";

        /// <summary>
        /// 结果时间
        /// </summary>
        [DisplayName("结果时间")]
        public string ResultTime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
