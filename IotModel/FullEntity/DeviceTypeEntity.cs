using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备类型完整类
    ///</summary>
    [DisplayName("设备类型完整类")]
    [FullEntity]
    public class DeviceTypeEntity : DeviceType
    {
        /// <summary>
        /// 设备类型拓展类
        ///</summary>
        [DisplayName("设备类型拓展类")]
        public Expand_DeviceType ExpandObject { get; set; } = new Expand_DeviceType();
    }
}
