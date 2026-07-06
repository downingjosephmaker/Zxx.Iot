using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备表拓展属性
    ///</summary>
    [DisplayName("设备表拓展属性")]
    [Expand]
    public class Expand_DeviceInfo
    {
        /// <summary>
        /// 硬件设备类型
        ///</summary>
        [DisplayName("硬件设备类型")]
        public int DeviceType { get; set; } = 0;
        /// <summary>
        /// 能耗类型(照明与插座用电,空调用电,动力用电,特殊用电,其他)
        ///</summary>
        [DisplayName("能耗类型(照明与插座用电,空调用电,动力用电,特殊用电,其他)")]
        public string EnergyType { get; set; } = "其他";
        /// <summary>
        /// 线路名称
        ///</summary>
        [DisplayName("线路名称")]
        public string LineNum { get; set; }
        /// <summary>
        /// 设备标识(IMEI)
        ///</summary>
        [DisplayName("设备标识(IMEI)")]
        public string DeviceIMEI { get; set; }
        /// <summary>
        /// SIM标识(ICCID)
        ///</summary>
        [DisplayName("SIM标识(ICCID)")]
        public string DeviceSim { get; set; }
        /// <summary>
        /// 关联视频id集合
        ///</summary>
        [DisplayName("关联视频id集合")]
        public string VideoIds { get; set; }
        /// <summary>
        /// 策略下发状态 0:未下发 1:下发中 2:下发成功 3:下发失败  4:服务下发中
        ///</summary>
        [DisplayName("策略下发状态 0:未下发 1:下发中 2:下发成功 3:下发失败  4:服务下发中")]
        public string StrategySendStatus { get; set; } = "未下发";
        /// <summary>
        /// 策略下发时间
        ///</summary>
        [DisplayName("策略下发时间")]
        public string StrategySendTime { get; set; }

        /// <summary>
        /// 电流互感器变比
        ///</summary>
        [DisplayName("电流互感器变比")]
        public int CurrentTransformer { get; set; } = 1;
        /// <summary>
        /// 电压互感器变比
        ///</summary>
        [DisplayName("电压互感器变比")]
        public int VoltageTransformer { get; set; } = 1;
    }
}
