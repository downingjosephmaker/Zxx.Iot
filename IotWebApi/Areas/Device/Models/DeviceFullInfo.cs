using System.ComponentModel;
using IotModel;

namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 设备信息
    /// </summary>
    public class DeviceFullInfo : DeviceInfoEntity
    {
        /// <summary>
        /// 租户名称
        ///</summary>
        [DisplayName("租户名称")]
        public string TenantName { get; set; }
        /// <summary>
        /// 设备类型名称
        ///</summary>
        [DisplayName("设备类型名称")]
        public string DeviceTypeName { get; set; }
    }

    /// <summary>
    /// 网络拓扑图
    /// </summary>
    public class TuopuAutoInfo
    {
        /// <summary>
        /// 设备Id
        ///</summary>
        [DisplayName("设备Id")]
        public int DeviceId { get; set; }
        /// <summary>
        /// 设备名称
        ///</summary>
        [DisplayName("设备名称")]
        public string DeviceName { get; set; }
        /// <summary>
        /// 设备父Id
        ///</summary>
        [DisplayName("设备父Id")]
        public int ParentId { get; set; }
        /// <summary>
        /// 设备状态(3:告警;2:在线;1:掉电;0:离线)
        ///</summary>
        [DisplayName("设备状态(3:告警;2:在线;1:掉电;0:离线)")]
        public int DeviceState { get; set; }
    }

}
