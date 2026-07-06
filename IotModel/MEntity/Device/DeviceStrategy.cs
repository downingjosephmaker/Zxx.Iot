using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备策略表
    ///</summary>
    [DisplayName("设备策略表")]
    [SugarTable(TableName = "device_strategy", TableDescription = "设备策略表", IsDisabledUpdateAll = true)]
    public class DeviceStrategy : BaseEntity
    {
        /// <summary>
        /// 设备主键
        ///</summary>
        [DisplayName("设备主键")]
        [SugarColumn(ColumnName = "device_id", IsPrimaryKey = true, Length = 11, ColumnDescription = "设备主键", DefaultValue = "0", ColumnDataType = "int")]
        public int DeviceId { get; set; }
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        [SugarColumn(ColumnName = "device_type_code", Length = 30, ColumnDescription = "设备类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 建筑ID
        ///</summary>
        [DisplayName("建筑ID")]
        [SugarColumn(ColumnName = "build_id", Length = 11, ColumnDescription = "建筑ID", DefaultValue = "0", ColumnDataType = "int")]
        public int BuildId { get; set; }
        /// <summary>
        /// 组织ID
        ///</summary>
        [DisplayName("组织ID")]
        [SugarColumn(ColumnName = "dept_id", Length = 11, ColumnDescription = "组织ID", DefaultValue = "0", ColumnDataType = "int")]
        public int DeptId { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "unit_id", ColumnDescription = "单位ID", DefaultValue = "0", ColumnDataType = "int")]
        public int UnitId { get; set; }
        /// <summary>
        /// 常规参数属性(json)
        ///</summary>
        [DisplayName("常规参数属性(json)")]
        [JsonField(typeof(Expand_DeviceStrategy_General))]
        [SugarColumn(ColumnName = "general_json", IsNullable = true, ColumnDescription = "常规参数属性(json)", ColumnDataType = "text")]
        public string GeneralJson { get; set; }
        /// <summary>
        /// 时间参数属性(json)
        ///</summary>
        [DisplayName("时间参数属性(json)")]
        [JsonField(typeof(Expand_DeviceStrategy_Timing))]
        [SugarColumn(ColumnName = "timing_json", IsNullable = true, ColumnDescription = "时间参数属性(json)", ColumnDataType = "text")]
        public string TimingJson { get; set; }
        /// <summary>
        /// 定时任务参数属性(json)
        ///</summary>
        [DisplayName("定时任务参数属性(json)")]
        [JsonField(typeof(Expand_DeviceStrategy_Task))]
        [SugarColumn(ColumnName = "task_json", IsNullable = true, ColumnDescription = "定时任务参数属性(json)", ColumnDataType = "text")]
        public string TaskJson { get; set; }
    }
}