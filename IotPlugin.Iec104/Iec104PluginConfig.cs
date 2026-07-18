using IotDriverCore;
using NewLife.Configuration;
using System.ComponentModel;

namespace IotPlugin.Iec104
{
    /// <summary>
    /// IEC 60870-5-104主站采集插件配置(k/w/t1~t3默认值取自标准——方案§4.4)
    /// </summary>
    [Description("IEC104主站采集插件配置")]
    [Config("Config/Iec104PluginConfig.config")]
    public class Iec104PluginConfig : Config<Iec104PluginConfig>
    {
        /// <summary>
        /// 归属本插件的设备类型编码清单(逗号分隔,空=插件不启用)
        /// </summary>
        [DisplayName("设备类型编码清单(逗号分隔,空=不启用)")]
        [ConfigParameter("设备类型编码清单", "逗号分隔,空=插件不启用", true)]
        public string DeviceTypeCodes { get; set; } = "";

        /// <summary>
        /// 子站默认端口(设备DevicePort为0时使用,IEC104标准端口2404)
        /// </summary>
        [DisplayName("子站默认端口(设备未配端口时使用)")]
        [ConfigParameter("子站默认端口", "设备DevicePort为0时使用,IEC104标准端口2404", false)]
        public int DefaultPort { get; set; } = 2404;

        /// <summary>
        /// k:发送方未被确认的I帧上限,到顶即停发(方案§4.4)
        /// </summary>
        [DisplayName("k发送窗口(未确认I帧上限)")]
        [ConfigParameter("k发送窗口", "发送方未被确认的I帧上限,到顶即停发,标准默认12", false)]
        public int K { get; set; } = 12;

        /// <summary>
        /// w:接收方每收w个I帧必须发S帧确认(方案§4.4)
        /// </summary>
        [DisplayName("w接收窗口(收满即发S帧确认)")]
        [ConfigParameter("w接收窗口", "接收方每收w个I帧必须发S帧确认,标准默认8", false)]
        public int W { get; set; } = 8;

        /// <summary>
        /// t1:发送APDU后等确认超时(秒),超时断链重连
        /// </summary>
        [DisplayName("t1发送确认超时(秒)")]
        [ConfigParameter("t1发送确认超时(秒)", "发送APDU后等确认超时,超时断链重连,标准默认15", false)]
        public int T1Seconds { get; set; } = 15;

        /// <summary>
        /// t2:无数据时发确认超时(秒),须小于t1
        /// </summary>
        [DisplayName("t2确认发送超时(秒,须小于t1)")]
        [ConfigParameter("t2确认发送超时(秒)", "收到I帧后最迟发S帧确认的时限,须小于t1,标准默认10", false)]
        public int T2Seconds { get; set; } = 10;

        /// <summary>
        /// t3:长期空闲发TESTFR超时(秒)
        /// </summary>
        [DisplayName("t3空闲测试超时(秒)")]
        [ConfigParameter("t3空闲测试超时(秒)", "链路空闲超时后发TESTFR_act探活,标准默认20", false)]
        public int T3Seconds { get; set; } = 20;

        /// <summary>
        /// 周期性总召唤间隔(分钟,0=仅连接建立时召唤一次;
        /// 断链重连后必发总召唤刷新全量——方案§7缓存陈旧缓解)
        /// </summary>
        [DisplayName("周期总召唤间隔(分钟,0=仅连接时)")]
        [ConfigParameter("周期总召唤间隔(分钟)", "0=仅连接建立时召唤一次;周期重召兜底突发上报丢失", false)]
        public int GiCycleMinutes { get; set; } = 15;

        /// <summary>
        /// 连接建立后下发时钟同步C_CS_NA_1
        /// </summary>
        [DisplayName("启用时钟同步(连接后下发)")]
        [ConfigParameter("启用时钟同步", "连接建立后向子站下发C_CS_NA_1时钟同步", false)]
        public bool EnableClockSync { get; set; } = false;

        /// <summary>
        /// 遥控选择后执行SBO(方案§4.6:电力行业普遍强制,关闭则直接执行)
        /// </summary>
        [DisplayName("遥控选择后执行SBO(默认开)")]
        [ConfigParameter("遥控选择后执行SBO", "先选择等激活确认再执行,电力行业普遍强制;关闭为直接执行", false)]
        public bool UseSelectBeforeOperate { get; set; } = true;

        /// <summary>
        /// 控制命令超时时间(秒)
        /// </summary>
        [DisplayName("控制命令超时时间(秒)")]
        [ConfigParameter("控制命令超时时间(秒)", "", false)]
        public int CmdTimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// 插件心跳间隔(秒)
        /// </summary>
        [DisplayName("插件心跳间隔(秒)")]
        [ConfigParameter("插件心跳间隔(秒)", "", false)]
        public int HeartSecond { get; set; } = 120;
    }
}
