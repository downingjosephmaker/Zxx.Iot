using System.ComponentModel;

namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 设备策略视图(设备信息 + 策略内容分块)
    /// 一条设备一条记录，策略内容保留分块字段，便于查询与显示。
    /// </summary>
    public class DeviceStrategyView
    {
        /// <summary>
        /// 设备主键
        ///</summary>
        [DisplayName("设备主键")]
        public int DeviceId { get; set; }

        /// <summary>
        /// 设备名称
        ///</summary>
        [DisplayName("设备名称")]
        public string DeviceName { get; set; }

        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        public string DeviceTypeCode { get; set; }

        /// <summary>
        /// 设备类型名称
        ///</summary>
        [DisplayName("设备类型名称")]
        public string DeviceTypeName { get; set; }

        /// <summary>
        /// 建筑ID
        ///</summary>
        [DisplayName("建筑ID")]
        public int BuildId { get; set; }

        /// <summary>
        /// 建筑名称(全)
        ///</summary>
        [DisplayName("建筑名称")]
        public string BuildName { get; set; }

        /// <summary>
        /// 部门ID
        ///</summary>
        [DisplayName("部门ID")]
        public int DeptId { get; set; }

        /// <summary>
        /// 部门名称(全)
        ///</summary>
        [DisplayName("部门名称")]
        public string DeptName { get; set; }

        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        public int UnitId { get; set; }

        /// <summary>
        /// 设备状态(2:在线;1:掉电;0:离线)
        ///</summary>
        [DisplayName("设备状态(2:在线;1:掉电;0:离线)")]
        public int DeviceState { get; set; }

        /// <summary>
        /// 设备告警状态(1:告警;0:正常)
        ///</summary>
        [DisplayName("设备告警状态(1:告警;0:正常)")]
        public int DeviceAlarm { get; set; }

        /// <summary>
        /// 开关状态(0:关1:开)
        ///</summary>
        [DisplayName("开关状态(0:关1:开)")]
        public int DeviceSwitch { get; set; }

        // ===== 策略 - 关键拆分字段(直接显示 + 可查询)=====

        /// <summary>
        /// 工作模式(中文描述，如"调温、人感")
        ///</summary>
        [DisplayName("工作模式")]
        public string WorkModel { get; set; }

        /// <summary>
        /// 工作模式编码(原始值，如"0,1"，用于查询)
        ///</summary>
        [DisplayName("工作模式编码")]
        public string WorkModelCode { get; set; }

        /// <summary>
        /// 季节选择(夏季/冬季)
        ///</summary>
        [DisplayName("季节选择")]
        public string AirSeason { get; set; }

        /// <summary>
        /// 是否有时间策略(1:有 0:无)
        ///</summary>
        [DisplayName("是否有时间策略")]
        public int HasTiming { get; set; }

        /// <summary>
        /// 是否有定时任务(1:有 0:无)
        ///</summary>
        [DisplayName("是否有定时任务")]
        public int HasTask { get; set; }

        // ===== 策略 - 整体描述(直接显示，复用 InterpretXxxJson)=====

        /// <summary>
        /// 常规策略参数(完整描述)
        ///</summary>
        [DisplayName("常规策略参数")]
        public string GeneralDesc { get; set; }

        /// <summary>
        /// 时间策略(完整描述)
        ///</summary>
        [DisplayName("时间策略")]
        public string TimingDesc { get; set; }

        /// <summary>
        /// 定时任务(完整描述)
        ///</summary>
        [DisplayName("定时任务")]
        public string TaskDesc { get; set; }

        // ===== 原始 JSON(便于前端深度解析，可选)=====

        /// <summary>
        /// 常规参数原始JSON
        ///</summary>
        [DisplayName("常规参数原始JSON")]
        public string GeneralJson { get; set; }

        /// <summary>
        /// 时间参数原始JSON
        ///</summary>
        [DisplayName("时间参数原始JSON")]
        public string TimingJson { get; set; }

        /// <summary>
        /// 定时任务原始JSON
        ///</summary>
        [DisplayName("定时任务原始JSON")]
        public string TaskJson { get; set; }

        /// <summary>
        /// 是否已配置策略(1:已配置;0:未配置)
        ///</summary>
        [DisplayName("是否已配置策略")]
        public int HasStrategy { get; set; }
    }
}
