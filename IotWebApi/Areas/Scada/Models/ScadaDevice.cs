using System.ComponentModel;

namespace IotWebApi.Areas.Scada.Models
{
    /// <summary>
    /// 大屏设备信息模型
    /// </summary>
    public class ScadaDevice
    {
        /// <summary>
        /// 设备ID
        ///</summary>
        [DisplayName("设备ID")]
        public string DeviceId { get; set; }
        /// <summary>
        /// 设备名称
        ///</summary>
        [DisplayName("设备名称")]
        public string DeviceName { get; set; }
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备类型名称
        ///</summary>
        [DisplayName("设备类型名称")]
        public string DeviceTypeName { get; set; }
        /// <summary>
        /// 设备全类型编码
        ///</summary>
        [DisplayName("设备全类型编码")]
        public string DeviceTypeFullCode { get; set; }
        /// <summary>
        /// 设备编号
        ///</summary>
        [DisplayName("设备编号")]
        public string DeviceGuid { get; set; }
        /// <summary>
        /// 最后在线时间
        ///</summary>
        [DisplayName("最后在线时间")]
        public string LastOnlineTime { get; set; }
        /// <summary>
        /// 设备状态(2:在线;1:掉电;0:离线)
        ///</summary>
        [DisplayName("设备状态(2:在线;1:掉电;0:离线)")]
        public int DeviceState { get; set; }
        /// <summary>
        /// 设备告警状态(1:告警;0:正常)
        ///</summary>
        [DisplayName("设备告警状态(1:告警;0:正常)")]
        public int DeviceAlarm { get; set; }
        /// <summary>
        /// 开关状态(0:关1:开)
        ///</summary>
        [DisplayName("开关状态(0:关1:开)")]
        public int DeviceSwitch { get; set; }
        /// <summary>
        /// 设备ID(全)
        ///</summary>
        [DisplayName("设备ID(全)")]
        public string DeviceFullCode { get; set; }
        /// <summary>
        /// 设备参数信息(集合)
        /// </summary>
        public List<ScadaDeviceParam> DeviceParams { get; set; } = new List<ScadaDeviceParam>();
    }

    /// <summary>
    /// 大屏设备参数信息模型
    /// </summary>
    public class ScadaDeviceParam
    {
        /// <summary>
        /// 参数编码
        ///</summary>
        [DisplayName("参数编码")]
        public string ParamCode { get; set; }
        /// <summary>
        /// 参数名称
        ///</summary>
        [DisplayName("参数名称")]
        public string ParamName { get; set; }
        /// <summary>
        /// 值单位
        ///</summary>
        [DisplayName("值单位")]
        public string ValueUnit { get; set; }
        /// <summary>
        /// 采集时间
        ///</summary>
        [DisplayName("采集时间")]
        public string CollectTime { get; set; }
        /// <summary>
        /// 上次参数值
        ///</summary>
        [DisplayName("上次参数值")]
        public string ParamLastValue { get; set; }
        /// <summary>
        /// 参数值
        ///</summary>
        [DisplayName("参数值")]
        public string ParamValue { get; set; }
        /// <summary>
        /// 是否告警(0:正常 1:告警)
        ///</summary>
        [DisplayName("是否告警(0:正常 1:告警)")]
        public int IsAlarm { get; set; }
    }
}
