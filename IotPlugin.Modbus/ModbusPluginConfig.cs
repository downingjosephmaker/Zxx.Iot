using IotDriverCore;
using NewLife.Configuration;
using System.ComponentModel;

namespace IotPlugin.Modbus
{
    /// <summary>
    /// Modbus通用采集插件配置。
    /// </summary>
    [Description("Modbus通用采集插件配置")]
    [Config("Config/ModbusPluginConfig.config")]
    public class ModbusPluginConfig : Config<ModbusPluginConfig>
    {
        /// <summary>
        /// 归属本插件的设备类型编码清单(逗号分隔,空=插件不启用)
        /// </summary>
        [DisplayName("设备类型编码清单(逗号分隔,空=不启用)")]
        [ConfigParameter("设备类型编码清单", "逗号分隔,空=插件不启用", true)]
        public string DeviceTypeCodes { get; set; } = "";

        /// <summary>
        /// RTU over TCP服务端监听端口(DTU拨入模式,0=不启用;
        /// 设备DevicePort=0时按DeviceIp匹配拨入连接走本端口)
        /// </summary>
        [DisplayName("RTU透传服务端监听端口(0=不启用)")]
        [ConfigParameter("RTU透传服务端监听端口", "DTU拨入模式监听端口,0=不启用;设备DevicePort=0时按DeviceIp匹配拨入连接", false)]
        public int NetPort { get; set; } = 20010;

        /// <summary>
        /// 启用DTU注册包模式(§6.6:启用后拨入连接须先发注册包——可打印ASCII注册ID(可带回车换行),
        /// 匹配设备网关编号DeviceGateway绑定会话,超时未注册踢连接;关闭时按来源IP匹配DeviceIp)
        /// </summary>
        [DisplayName("启用DTU注册包模式(默认关闭)")]
        [ConfigParameter("启用DTU注册包模式", "启用后拨入连接须先发ASCII注册ID匹配设备网关编号DeviceGateway,关闭时按来源IP匹配DeviceIp", false)]
        public bool EnableDtuRegistration { get; set; } = false;

        /// <summary>
        /// 同通道指令发送间隔(毫秒,兼作RS-485从站喘息时间)
        /// </summary>
        [DisplayName("同通道指令发送间隔(毫秒)")]
        [ConfigParameter("同通道指令发送间隔(毫秒)", "兼作RS-485从站喘息时间", false)]
        public int SendIntervalMs { get; set; } = 300;

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
        /// 默认采集周期(毫秒,策略引擎点位级周期接入前的全局默认)
        /// </summary>
        [DisplayName("默认采集周期(毫秒)")]
        [ConfigParameter("默认采集周期(毫秒)", "策略引擎点位级周期接入前的全局默认", false)]
        public int CollectCycleMs { get; set; } = 60_000;

        /// <summary>
        /// 单批次最大寄存器数(Modbus协议上限125/0x7D)
        /// </summary>
        [DisplayName("单批次最大寄存器数")]
        [ConfigParameter("单批次最大寄存器数", "Modbus协议上限125", false)]
        public int MaxBatchLength { get; set; } = 125;

        /// <summary>
        /// 合包空洞容忍寄存器数(0=禁止跨洞,部分设备对未定义地址整段回异常)
        /// </summary>
        [DisplayName("合包空洞容忍寄存器数(0=禁止跨洞)")]
        [ConfigParameter("合包空洞容忍寄存器数", "0=禁止跨洞,部分设备对未定义地址整段回异常", false)]
        public int GapTolerance { get; set; } = 8;

        /// <summary>
        /// 插件心跳间隔(秒)
        /// </summary>
        [DisplayName("插件心跳间隔(秒)")]
        [ConfigParameter("插件心跳间隔(秒)", "", false)]
        public int HeartSecond { get; set; } = 120;
    }
}
