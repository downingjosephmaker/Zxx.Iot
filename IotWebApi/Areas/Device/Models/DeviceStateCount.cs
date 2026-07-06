using System.ComponentModel;

namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 设备状态数量统计
    /// </summary>
    [DisplayName("设备状态数量统计")]
    public class DeviceStateCount
    {
        /// <summary>
        /// 在线数量
        ///</summary>
        [DisplayName("在线数量")]
        public int Online { get; set; }
        /// <summary>
        /// 掉电数量
        ///</summary>
        [DisplayName("掉电数量")]
        public int Offpwr { get; set; }
        /// <summary>
        /// 离线数量
        ///</summary>
        [DisplayName("离线数量")]
        public int Offline { get; set; }
        /// <summary>
        /// 异常数量
        ///</summary>
        [DisplayName("异常数量")]
        public int Alarm { get; set; }

    }
}
