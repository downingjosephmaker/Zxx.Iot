using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备参数表完整类
    ///</summary>
    [DisplayName("设备参数表完整类")]
    [FullEntity]
    public class DeviceParamEntity : DeviceParam
    {
        /// <summary>
        /// 设备参数表拓展属性
        ///</summary>
        [DisplayName("设备参数表拓展属性")]
        public List<Expand_DeviceParam> ExpandObjects { get; set; } = new();
    }
}
