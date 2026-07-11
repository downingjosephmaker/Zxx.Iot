using IotDriverCore;
using NewLife.Configuration;
using System.ComponentModel;

namespace IotPlugin.OpcUa
{
    /// <summary>
    /// OPC UA采集插件配置。
    /// </summary>
    [Description("OPC UA采集插件配置")]
    [Config("Config/OpcUaPluginConfig.config")]
    public class OpcUaPluginConfig : Config<OpcUaPluginConfig>
    {
        /// <summary>
        /// 归属本插件的设备类型编码清单(逗号分隔,空=插件不启用;
        /// 一台设备=一个OPC UA服务器,端点为opc.tcp://DeviceIp:DevicePort)
        /// </summary>
        [DisplayName("设备类型编码清单(逗号分隔,空=不启用)")]
        [ConfigParameter("设备类型编码清单", "逗号分隔,空=插件不启用;一台设备=一个OPC UA服务器,端点为opc.tcp://DeviceIp:DevicePort", true)]
        public string DeviceTypeCodes { get; set; } = "";

        /// <summary>
        /// 用户名(空=匿名认证;证书认证待后续)
        /// </summary>
        [DisplayName("用户名(空=匿名认证)")]
        [ConfigParameter("用户名", "空=匿名认证;证书认证待后续", false)]
        public string UserName { get; set; } = "";

        /// <summary>
        /// 密码
        /// </summary>
        [DisplayName("密码")]
        [ConfigParameter("密码", "", false)]
        public string Password { get; set; } = "";

        /// <summary>
        /// 自动接受不受信服务器证书(现场自签名服务器妥协项,默认关闭)
        /// </summary>
        [DisplayName("自动接受不受信服务器证书(默认关闭)")]
        [ConfigParameter("自动接受不受信服务器证书", "现场自签名服务器妥协项,默认关闭", false)]
        public bool AutoAcceptUntrustedCertificates { get; set; } = false;

        /// <summary>
        /// 采集模式(1=订阅推送(默认,服务端变化上报);2=批量轮询读)
        /// </summary>
        [DisplayName("采集模式(1=订阅推送,2=批量轮询读)")]
        [ConfigParameter("采集模式", "1=订阅推送(默认,服务端变化上报);2=批量轮询读", false)]
        public int CollectMode { get; set; } = 1;

        /// <summary>
        /// 订阅发布间隔/轮询周期(毫秒)
        /// </summary>
        [DisplayName("订阅发布间隔/轮询周期(毫秒)")]
        [ConfigParameter("订阅发布间隔/轮询周期(毫秒)", "", false)]
        public int CollectCycleMs { get; set; } = 5_000;

        /// <summary>
        /// 会话操作超时(毫秒)
        /// </summary>
        [DisplayName("会话操作超时(毫秒)")]
        [ConfigParameter("会话操作超时(毫秒)", "", false)]
        public int SessionTimeoutMs { get; set; } = 60_000;

        /// <summary>
        /// 插件心跳间隔(秒)
        /// </summary>
        [DisplayName("插件心跳间隔(秒)")]
        [ConfigParameter("插件心跳间隔(秒)", "", false)]
        public int HeartSecond { get; set; } = 120;
    }
}
