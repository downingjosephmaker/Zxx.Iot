using NewLife.Configuration;
using System.ComponentModel;

namespace IotPlugin.Air.VRF.GuoXiang
{
    /// <summary>
    /// 国祥VRF空调插件配置。
    /// </summary>
    [Description("国祥VRF空调插件配置")]
    [Config("Config/GuoXiangAirPluginConfig.config")]
    public class GuoXiangAirPluginConfig : Config<GuoXiangAirPluginConfig>
    {
        /// <summary>
        /// 网络监听端口
        /// </summary>
        [DisplayName("网络监听端口")]
        public int NetPort { get; set; } = 20003;

        /// <summary>
        /// 同连接指令下发时间间隔(毫秒)
        /// </summary>
        [DisplayName("同连接指令下发时间间隔(毫秒)")]
        public int SendSecond { get; set; } = 300;

        /// <summary>
        /// 指令超时时间间隔(秒)
        /// </summary>
        [DisplayName("指令超时时间间隔(秒)")]
        public int CmdTimeOut { get; set; } = 10;

        /// <summary>
        /// 指令超时重发次数限制
        /// </summary>
        [DisplayName("指令超时重发次数限制")]
        public int TimeOutLimitCount { get; set; } = 2;

        /// <summary>
        /// 实时数据采集时间间隔(秒)
        /// </summary>
        [DisplayName("实时数据采集时间间隔(秒)")]
        public int CollectSleepSecond { get; set; } = 60;

        /// <summary>
        /// 控制成功后执行相应数据读取操作时间间隔(秒)
        /// </summary>
        [DisplayName("控制成功后执行相应数据读取操作时间间隔(秒)")]
        public int ControlSuccess { get; set; } = 10;

        /// <summary>
        /// 插件心跳间隔(秒)
        /// </summary>
        [DisplayName("插件心跳间隔(秒)")]
        public int HeartSecond { get; set; } = 120;
    }
}
