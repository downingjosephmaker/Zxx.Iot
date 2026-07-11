using IotDriverCore;
using NewLife.Configuration;
using System.ComponentModel;

namespace IotPlugin.Cjt188
{
    /// <summary>
    /// CJ/T 188水表采集插件配置。
    /// </summary>
    [Description("CJ/T 188水表采集插件配置")]
    [Config("Config/Cjt188PluginConfig.config")]
    public class Cjt188PluginConfig : Config<Cjt188PluginConfig>
    {
        /// <summary>
        /// 归属本插件的设备类型编码清单(逗号分隔,空=插件不启用)
        /// </summary>
        [DisplayName("设备类型编码清单(逗号分隔,空=不启用)")]
        [ConfigParameter("设备类型编码清单", "逗号分隔,空=插件不启用", true)]
        public string DeviceTypeCodes { get; set; } = "";

        /// <summary>
        /// 类型编码→表型T字节映射(格式"类型编码:表型十进制"逗号分隔,如"water_cold:16,heat_meter:32";
        /// 未配置的类型默认16=0x10冷水水表)
        /// </summary>
        [DisplayName("类型编码→表型T映射(未配置默认16)")]
        [ConfigParameter("类型编码→表型T映射", "格式\"类型编码:表型十进制\"逗号分隔,如water_cold:16,heat_meter:32;未配置默认16=冷水水表", false)]
        public string MeterTypeMap { get; set; } = "";

        /// <summary>
        /// 阀控白名单开关(false=拒绝netcjt188valve阀控指令,方案§6.3要求默认关闭)
        /// </summary>
        [DisplayName("阀控白名单开关(默认关闭)")]
        [ConfigParameter("阀控白名单开关", "false=拒绝netcjt188valve阀控指令,默认关闭", false)]
        public bool EnableValveControl { get; set; } = false;

        /// <summary>
        /// DTU透传服务端监听端口(设备DevicePort=0时按DeviceIp匹配拨入连接,0=不启用)
        /// </summary>
        [DisplayName("DTU透传服务端监听端口(0=不启用)")]
        [ConfigParameter("DTU透传服务端监听端口", "设备DevicePort=0时按DeviceIp匹配拨入连接,0=不启用", false)]
        public int NetPort { get; set; } = 20012;

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
        /// 默认采集周期(毫秒,水表抄读低频,默认1小时)
        /// </summary>
        [DisplayName("默认采集周期(毫秒)")]
        [ConfigParameter("默认采集周期(毫秒)", "水表抄读低频,默认1小时", false)]
        public int CollectCycleMs { get; set; } = 3_600_000;

        /// <summary>
        /// 插件心跳间隔(秒)
        /// </summary>
        [DisplayName("插件心跳间隔(秒)")]
        [ConfigParameter("插件心跳间隔(秒)", "", false)]
        public int HeartSecond { get; set; } = 120;
    }
}
