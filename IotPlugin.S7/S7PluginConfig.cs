using NewLife.Configuration;
using System.ComponentModel;

namespace IotPlugin.S7
{
    /// <summary>
    /// 西门子S7采集插件配置。
    /// </summary>
    [Description("西门子S7采集插件配置")]
    [Config("Config/S7PluginConfig.config")]
    public class S7PluginConfig : Config<S7PluginConfig>
    {
        /// <summary>
        /// 归属本插件的设备类型编码清单(逗号分隔,空=插件不启用)
        /// </summary>
        [DisplayName("设备类型编码清单(逗号分隔,空=不启用)")]
        public string DeviceTypeCodes { get; set; } = "";

        /// <summary>
        /// 类型编码→CPU型号映射(格式"类型编码:型号"逗号分隔,型号取S7200Smart/S7300/S7400/S71200/S71500;
        /// 未配置默认S71200)
        /// </summary>
        [DisplayName("类型编码→CPU型号映射(默认S71200)")]
        public string CpuTypeMap { get; set; } = "";

        /// <summary>
        /// 机架号(默认0)
        /// </summary>
        [DisplayName("机架号")]
        public short Rack { get; set; } = 0;

        /// <summary>
        /// 槽号(300/400常用2,1200/1500常用0或1,默认0)
        /// </summary>
        [DisplayName("槽号")]
        public short Slot { get; set; } = 0;

        /// <summary>
        /// 默认采集周期(毫秒)
        /// </summary>
        [DisplayName("默认采集周期(毫秒)")]
        public int CollectCycleMs { get; set; } = 5_000;

        /// <summary>
        /// 单批次最大字节数(按地址连续性合包上限,S7 PDU常见240字节)
        /// </summary>
        [DisplayName("单批次最大字节数")]
        public int MaxBatchBytes { get; set; } = 200;

        /// <summary>
        /// 合包空洞容忍字节数(0=禁止跨洞)
        /// </summary>
        [DisplayName("合包空洞容忍字节数(0=禁止跨洞)")]
        public int GapTolerance { get; set; } = 16;

        /// <summary>
        /// 插件心跳间隔(秒)
        /// </summary>
        [DisplayName("插件心跳间隔(秒)")]
        public int HeartSecond { get; set; } = 120;
    }
}
