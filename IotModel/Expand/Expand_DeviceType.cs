using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备类型拓展类
    ///</summary>
    [DisplayName("设备类型拓展类")]
    [Expand]
    public class Expand_DeviceType
    {
        /// <summary>
        /// 离线判断间隔(分钟)
        ///</summary>
        [DisplayName("离线判断间隔(分钟)")]
        public int OfflineMinute { get; set; } = 0;

        /// <summary>
        /// 支路数量
        ///</summary>
        [DisplayName("支路数量")]
        public int SubChannels { get; set; } = 0;

        /// <summary>
        /// 是否采集
        ///</summary>
        [DisplayName("是否采集")]
        public bool SbjgType { get; set; } = false;

        /// <summary>
        /// Mqtt通讯Key
        ///</summary>
        [DisplayName("Mqtt通讯Key")]
        public string MqttKey { get; set; } = "";

    }
}