namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 第三方设备数据
    /// </summary>
    public class ThirdDaviceData
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }
        /// <summary>
        /// 开关状态(0:关1:开)
        ///</summary>
        public int DeviceSwitch { get; set; }
        /// <summary>
        /// 设备参数集合
        /// </summary>
        public List<ThirdDeviceParam> DeviceParam { get; set; } = new();
    }

    /// <summary>
    /// 设备参数属性
    ///</summary>
    public class ThirdDeviceParam
    {
        /// <summary>
        /// 参数编码
        ///</summary>
        public string ParamCode { get; set; }
        /// <summary>
        /// 参数值
        ///</summary>
        public string ParamValue { get; set; }
        /// <summary>
        /// 设备告警状态(1:告警;0:正常)
        ///</summary>
        public int IsAlarm { get; set; }
        /// <summary>
        /// 告警内容
        ///</summary>
        public string AlarmContent { get; set; }
    }

}
