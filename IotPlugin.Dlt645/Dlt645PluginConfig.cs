using IotDriverCore;
using NewLife.Configuration;
using System.ComponentModel;

namespace IotPlugin.Dlt645
{
    /// <summary>
    /// DL/T 645电表采集插件配置。
    /// </summary>
    [Description("DL/T 645电表采集插件配置")]
    [Config("Config/Dlt645PluginConfig.config")]
    public class Dlt645PluginConfig : Config<Dlt645PluginConfig>
    {
        /// <summary>
        /// 归属本插件的设备类型编码清单(逗号分隔,空=插件不启用,默认2007版)
        /// </summary>
        [DisplayName("设备类型编码清单(逗号分隔,空=不启用)")]
        [ConfigParameter("设备类型编码清单", "逗号分隔,空=插件不启用,默认2007版", true)]
        public string DeviceTypeCodes { get; set; } = "";

        /// <summary>
        /// 按1997版协议通信的设备类型编码清单(逗号分隔,须同时在DeviceTypeCodes中;
        /// 1997版为2字节DI+控制码01/81体系)
        /// </summary>
        [DisplayName("1997版设备类型编码清单(逗号分隔)")]
        [ConfigParameter("1997版设备类型编码清单", "逗号分隔,须同时在设备类型编码清单中;1997版为2字节DI体系", false)]
        public string Dlt1997TypeCodes { get; set; } = "";

        /// <summary>
        /// DTU透传服务端监听端口(设备DevicePort=0时按DeviceIp匹配拨入连接,0=不启用)
        /// </summary>
        [DisplayName("DTU透传服务端监听端口(0=不启用)")]
        [ConfigParameter("DTU透传服务端监听端口", "设备DevicePort=0时按DeviceIp匹配拨入连接,0=不启用", false)]
        public int NetPort { get; set; } = 20011;

        /// <summary>
        /// 启用DTU注册包模式(§6.6:启用后拨入连接须先发注册包——可打印ASCII注册ID(可带回车换行),
        /// 匹配设备网关编号DeviceGateway绑定会话,超时未注册踢连接;关闭时按来源IP匹配DeviceIp)
        /// </summary>
        [DisplayName("启用DTU注册包模式(默认关闭)")]
        [ConfigParameter("启用DTU注册包模式", "启用后拨入连接须先发ASCII注册ID匹配设备网关编号DeviceGateway,关闭时按来源IP匹配DeviceIp", false)]
        public bool EnableDtuRegistration { get; set; } = false;

        /// <summary>
        /// 同总线指令发送间隔(毫秒,RS-485一问一答的从站喘息时间,2400bps建议>=500)
        /// </summary>
        [DisplayName("同总线指令发送间隔(毫秒)")]
        [ConfigParameter("同总线指令发送间隔(毫秒)", "RS-485一问一答的从站喘息时间,2400bps建议>=500", false)]
        public int SendIntervalMs { get; set; } = 500;

        /// <summary>
        /// 指令超时时间(秒)
        /// </summary>
        [DisplayName("指令超时时间(秒)")]
        [ConfigParameter("指令超时时间(秒)", "", false)]
        public int CmdTimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// 控制指令超时重发次数限制
        /// </summary>
        [DisplayName("控制指令超时重发次数限制")]
        [ConfigParameter("控制指令超时重发次数限制", "", false)]
        public int RetryLimit { get; set; } = 2;

        /// <summary>
        /// 默认采集周期(毫秒,电表抄读低频,默认15分钟)
        /// </summary>
        [DisplayName("默认采集周期(毫秒)")]
        [ConfigParameter("默认采集周期(毫秒)", "电表抄读低频,默认15分钟", false)]
        public int CollectCycleMs { get; set; } = 900_000;

        /// <summary>
        /// 插件心跳间隔(秒)
        /// </summary>
        [DisplayName("插件心跳间隔(秒)")]
        [ConfigParameter("插件心跳间隔(秒)", "", false)]
        public int HeartSecond { get; set; } = 120;
    }
}
