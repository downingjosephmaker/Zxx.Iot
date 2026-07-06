using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备类型参数
    ///</summary>
    [DisplayName("设备类型参数")]
    [SugarTable(TableName = "device_type_param", TableDescription = "设备类型参数", IsDisabledUpdateAll = true)]
    public class DeviceTypeParam : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        [SugarColumn(ColumnName = "device_type_code", Length = 30, ColumnDescription = "设备类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备路数(总路,1路/A,2路/B,3路/C)
        ///</summary>
        [DisplayName("设备路数(总路,1路/A,2路/B,3路/C)")]
        [SugarColumn(ColumnName = "sub_channel", Length = 30, ColumnDescription = "设备路数(总路,1路/A,2路/B,3路/C)", DefaultValue = "总路", ColumnDataType = "varchar")]
        public string SubChannel { get; set; }
        /// <summary>
        /// 参数编码
        ///</summary>
        [DisplayName("参数编码")]
        [SugarColumn(ColumnName = "param_code", IsNullable = true, Length = 50, ColumnDescription = "参数编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string ParamCode { get; set; }
        /// <summary>
        /// 参数名称
        ///</summary>
        [DisplayName("参数名称")]
        [SugarColumn(ColumnName = "param_name", IsNullable = true, Length = 50, ColumnDescription = "参数名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string ParamName { get; set; }
        /// <summary>
        /// 参数分类名称
        ///</summary>
        [DisplayName("参数分类名称")]
        [SugarColumn(ColumnName = "param_type_name", IsNullable = true, Length = 30, ColumnDescription = "参数分类名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string ParamTypeName { get; set; }
        /// <summary>
        /// 参数地址
        ///</summary>
        [DisplayName("参数地址")]
        [SugarColumn(ColumnName = "param_addr", Length = 0, ColumnDescription = "参数地址", DefaultValue = "0", ColumnDataType = "int")]
        public int ParamAddr { get; set; }
        /// <summary>
        /// 参数修正公式(a*1)
        ///</summary>
        [DisplayName("参数修正公式(a*1)")]
        [SugarColumn(ColumnName = "param_formula", IsNullable = true, Length = 50, ColumnDescription = "参数修正公式(a*1)", DefaultValue = "", ColumnDataType = "varchar")]
        public string ParamFormula { get; set; }
        /// <summary>
        /// 值类型(数值,状态)
        ///</summary>
        [DisplayName("值类型(数值,状态)")]
        [SugarColumn(ColumnName = "value_type", IsNullable = true, Length = 10, ColumnDescription = "值类型(数值,状态)", DefaultValue = "", ColumnDataType = "varchar")]
        public string ValueType { get; set; }
        /// <summary>
        /// 状态值集合
        ///</summary>
        [DisplayName("状态值集合")]
        [JsonField(typeof(Expand_ParamStatusValue))]
        [SugarColumn(ColumnName = "status_value", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string StatusValues { get; set; }
        /// <summary>
        /// 值单位
        ///</summary>
        [DisplayName("值单位")]
        [SugarColumn(ColumnName = "value_unit", IsNullable = true, Length = 10, ColumnDescription = "值单位", DefaultValue = "", ColumnDataType = "varchar")]
        public string ValueUnit { get; set; }
        /// <summary>
        /// 最大合法值
        ///</summary>
        [DisplayName("最大合法值")]
        [SugarColumn(ColumnName = "param_max_value", Length = 18, DecimalDigits = 2, ColumnDescription = "最大合法值", DefaultValue = "0.00", ColumnDataType = "decimal")]
        public decimal ParamMaxValue { get; set; }
        /// <summary>
        /// 最小合法值
        ///</summary>
        [DisplayName("最小合法值")]
        [SugarColumn(ColumnName = "param_min_value", Length = 18, DecimalDigits = 2, ColumnDescription = "最小合法值", DefaultValue = "0.00", ColumnDataType = "decimal")]
        public decimal ParamMinValue { get; set; }
        /// <summary>
        /// 最大跳变量
        ///</summary>
        [DisplayName("最大跳变量")]
        [SugarColumn(ColumnName = "param_change_value", Length = 18, DecimalDigits = 2, ColumnDescription = "最大跳变量", DefaultValue = "0.00", ColumnDataType = "decimal")]
        public decimal ParamChangeValue { get; set; }
        /// <summary>
        /// 是否显示(0:否1:是)
        ///</summary>
        [DisplayName("是否显示(0:否1:是)")]
        [SugarColumn(ColumnName = "is_show", Length = 1, ColumnDescription = "是否显示(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsShow { get; set; }
        /// <summary>
        /// 是否主显示(0:否1:是)
        ///</summary>
        [DisplayName("是否主显示(0:否1:是)")]
        [SugarColumn(ColumnName = "is_main_show", Length = 1, ColumnDescription = "是否主显示(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsMainShow { get; set; }
        /// <summary>
        /// 是否配置(0:否1:是)
        ///</summary>
        [DisplayName("是否配置(0:否1:是)")]
        [SugarColumn(ColumnName = "is_set", Length = 1, ColumnDescription = "是否配置(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsSet { get; set; }
        /// <summary>
        /// 极值计算(0:否1:是)
        ///</summary>
        [DisplayName("极值计算(0:否1:是)")]
        [SugarColumn(ColumnName = "is_peak", Length = 1, ColumnDescription = "极值计算(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsPeak { get; set; }
        /// <summary>
        /// 统计计算(0:否1:是)
        ///</summary>
        [DisplayName("统计计算(0:否1:是)")]
        [SugarColumn(ColumnName = "is_report", Length = 1, ColumnDescription = "统计计算(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsReport { get; set; }
        /// <summary>
        /// 电子图默认(0:否1:是)
        ///</summary>
        [DisplayName("电子图默认(0:否1:是)")]
        [SugarColumn(ColumnName = "is_map_default", Length = 1, ColumnDescription = "电子图默认(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsMapDefault { get; set; }
        /// <summary>
        /// 小数显示位数
        ///</summary>
        [DisplayName("小数显示位数")]
        [SugarColumn(ColumnName = "decimal_digit", Length = 0, ColumnDescription = "小数显示位数", DefaultValue = "0", ColumnDataType = "int")]
        public int DecimalDigit { get; set; }
        /// <summary>
        /// 是否乘PT(0:否1:是)
        ///</summary>
        [DisplayName("是否乘PT(0:否1:是)")]
        [SugarColumn(ColumnName = "is_pt", Length = 1, ColumnDescription = "是否乘PT(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsPt { get; set; }
        /// <summary>
        /// 是否乘CT(0:否1:是)
        ///</summary>
        [DisplayName("是否乘CT(0:否1:是)")]
        [SugarColumn(ColumnName = "is_ct", Length = 1, ColumnDescription = "是否乘CT(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsCt { get; set; }
        /// <summary>
        /// 是否自定义告警显示(0:否1:是)
        ///</summary>
        [DisplayName("是否自定义告警显示(0:否1:是)")]
        [SugarColumn(ColumnName = "is_custom_alarm", Length = 1, ColumnDescription = "是否自定义告警显示(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsCustomAlarm { get; set; }
    }
}