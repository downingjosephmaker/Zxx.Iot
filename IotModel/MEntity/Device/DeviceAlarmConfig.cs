using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备告警配置
    ///</summary>
    [DisplayName("设备告警配置")]
    [SugarTable(TableName = "device_alarm_config", TableDescription = "设备告警配置", IsDisabledUpdateAll = true)]
    public class DeviceAlarmConfig : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 设备ID
        ///</summary>
        [DisplayName("设备ID")]
        [SugarColumn(ColumnName = "device_id", ColumnDescription = "设备ID", DefaultValue = "0", ColumnDataType = "int")]
        public int DeviceId { get; set; }
        /// <summary>
        /// 设备名称
        ///</summary>
        [DisplayName("设备名称")]
        [SugarColumn(ColumnName = "device_name", IsNullable = true, Length = 50, ColumnDescription = "设备名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceName { get; set; }
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        [SugarColumn(ColumnName = "device_type_code", Length = 30, ColumnDescription = "设备类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备ID
        ///</summary>
        [DisplayName("设备ID")]
        [SugarColumn(ColumnName = "build_id", ColumnDescription = "设备ID", DefaultValue = "0", ColumnDataType = "int")]
        public int BuildId { get; set; }
        /// <summary>
        /// 部门ID
        ///</summary>
        [DisplayName("部门ID")]
        [SugarColumn(ColumnName = "dept_id", ColumnDescription = "部门ID", DefaultValue = "0", ColumnDataType = "int")]
        public int DeptId { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "unit_id", ColumnDescription = "单位ID", DefaultValue = "0", ColumnDataType = "int")]
        public int UnitId { get; set; }
        /// <summary>
        /// 配置类型(1:组合;0:单个)
        ///</summary>
        [DisplayName("配置类型(1:组合;0:单个)")]
        [SugarColumn(ColumnName = "config_type", ColumnDescription = "配置类型(1:组合;0:单个)", DefaultValue = "0", ColumnDataType = "int")]
        public int ConfigType { get; set; }
        /// <summary>
        /// 参数编码(组合|隔开)
        ///</summary>
        [DisplayName("参数编码(组合|隔开)")]
        [SugarColumn(ColumnName = "param_code", IsNullable = true, Length = 80, ColumnDescription = "参数编码(组合|隔开)", DefaultValue = "", ColumnDataType = "varchar")]
        public string ParamCode { get; set; }
        /// <summary>
        /// 参数名称(组合|隔开)
        ///</summary>
        [DisplayName("参数名称(组合|隔开)")]
        [SugarColumn(ColumnName = "param_name", IsNullable = true, Length = 100, ColumnDescription = "参数名称(组合|隔开)", DefaultValue = "", ColumnDataType = "varchar")]
        public string ParamName { get; set; }
        /// <summary>
        /// 公式名称
        ///</summary>
        [DisplayName("公式名称")]
        [SugarColumn(ColumnName = "formula_name", IsNullable = true, Length = 30, ColumnDescription = "公式名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string FormulaName { get; set; }
        /// <summary>
        /// 公式启用(1:启用 0:不启用)
        ///</summary>
        [DisplayName("公式启用(1:启用 0:不启用)")]
        [SugarColumn(ColumnName = "is_formula_enable", ColumnDescription = "公式启用(1:启用 0:不启用)", DefaultValue = "0", ColumnDataType = "int")]
        public int IsFormulaEnable { get; set; }
        /// <summary>
        /// 计算公式
        ///</summary>
        [DisplayName("计算公式")]
        [SugarColumn(ColumnName = "jisuan_formula", IsNullable = true, Length = 30, ColumnDescription = "计算公式", DefaultValue = "", ColumnDataType = "varchar")]
        public string JisuanFormula { get; set; }
        /// <summary>
        /// 文字模板
        ///</summary>
        [DisplayName("文字模板")]
        [SugarColumn(ColumnName = "text_template", IsNullable = true, Length = 300, ColumnDescription = "文字模板", DefaultValue = "", ColumnDataType = "varchar")]
        public string TextTemplate { get; set; }
        /// <summary>
        /// 是否通知(0:否 1:是)
        ///</summary>
        [DisplayName("是否通知(0:否 1:是)")]
        [SugarColumn(ColumnName = "is_note", Length = 1, ColumnDescription = "是否通知(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsNote { get; set; }
        /// <summary>
        /// 报警配置ID
        ///</summary>
        [DisplayName("报警配置ID")]
        [SugarColumn(ColumnName = "alarm_config_id", ColumnDescription = "报警配置ID", DefaultValue = "0", ColumnDataType = "int")]
        public int AlarmConfigId { get; set; }
        /// <summary>
        /// 设备类型配置ID
        ///</summary>
        [DisplayName("设备类型配置ID")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "type_snow_id", IsNullable = true, Length = 20, ColumnDescription = "设备类型配置ID", DefaultValue = "0", ColumnDataType = "bigint")]
        public long TypeSnowId { get; set; }
    }
}
