using IotModel;

namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 物联网设备信息
    /// </summary>
    public class IotDeviceInfo : DeviceInfoEntity
    {
        /// <summary>
        /// 临时设备编号
        /// </summary>
        public string DeviceCode { get; set; }
        /// <summary>
        /// 临时父设备编号
        /// </summary>
        public string ParentDeviceCode { get; set; }
    }
}
