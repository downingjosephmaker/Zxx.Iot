using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotModel
{
    /// <summary>
    /// 设备策略表完整类
    ///</summary>
    [DisplayName("设备策略表完整类")]
    [FullEntity]
    public class DeviceStrategyEntity : DeviceStrategy
    {
        /// <summary>
        /// 设备策略表拓展类(常规)
        ///</summary>
        [DisplayName("设备策略表拓展类(常规)")]
        public Expand_DeviceStrategy_General ExpandGeneral { get; set; }

        /// <summary>
        /// 设备策略表拓展类(时间)
        ///</summary>
        [DisplayName("设备策略表拓展类(时间)")]
        public Expand_DeviceStrategy_Timing ExpandTiming { get; set; }
    }
}
