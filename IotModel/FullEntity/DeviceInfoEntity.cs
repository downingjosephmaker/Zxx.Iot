using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotModel
{
    /// <summary>
    /// 设备表完整类
    ///</summary>
    [DisplayName("设备表完整类")]
    [FullEntity]
    public class DeviceInfoEntity : DeviceInfo
    {
        /// <summary>
        /// 设备表拓展类
        ///</summary>
        [DisplayName("设备表拓展类")]
        public Expand_DeviceInfo ExpandObject { get; set; } = new();
    }
}
