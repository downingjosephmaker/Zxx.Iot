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
        /// 合理范围过滤开关(0:否1:是,配合最大/最小合法值,越界丢弃)
        ///</summary>
        [DisplayName("合理范围过滤开关(0:否1:是)")]
        [SugarColumn(ColumnName = "range_filter_enable", Length = 1, ColumnDescription = "合理范围过滤开关(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool RangeFilterEnable { get; set; }
        /// <summary>
        /// 幅度过滤开关(0:否1:是,绝对差用最大跳变量,百分比用最大跳变百分比)
        ///</summary>
        [DisplayName("幅度过滤开关(0:否1:是)")]
        [SugarColumn(ColumnName = "amplitude_filter_enable", Length = 1, ColumnDescription = "幅度过滤开关(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool AmplitudeFilterEnable { get; set; }
        /// <summary>
        /// 最大跳变百分比(相对前值,0=不启用百分比判定)
        ///</summary>
        [DisplayName("最大跳变百分比")]
        [SugarColumn(ColumnName = "max_amplitude_percent", Length = 18, DecimalDigits = 2, ColumnDescription = "最大跳变百分比(0=不启用)", DefaultValue = "0.00", ColumnDataType = "decimal")]
        public decimal MaxAmplitudePercent { get; set; }
        /// <summary>
        /// 连续异常容错开关(0:否1:是,连续N次幅度异常认定真实阶跃接受该值)
        ///</summary>
        [DisplayName("连续异常容错开关(0:否1:是)")]
        [SugarColumn(ColumnName = "continuous_filter_enable", Length = 1, ColumnDescription = "连续异常容错开关(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool ContinuousFilterEnable { get; set; }
        /// <summary>
        /// 连续异常次数阈值(默认3)
        ///</summary>
        [DisplayName("连续异常次数阈值")]
        [SugarColumn(ColumnName = "max_continuous_count", ColumnDescription = "连续异常次数阈值(默认3)", DefaultValue = "3", ColumnDataType = "int")]
        public int MaxContinuousCount { get; set; } = 3;
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
        /// <summary>
        /// 采集功能码(0:不采集,1/2/3/4:Modbus读区;寄存器地址复用参数地址ParamAddr)
        ///</summary>
        [DisplayName("采集功能码(0:不采集,1/2/3/4:Modbus读区)")]
        [SugarColumn(ColumnName = "collect_func_code", Length = 0, ColumnDescription = "采集功能码(0:不采集,1/2/3/4:Modbus读区)", DefaultValue = "0", ColumnDataType = "int")]
        public int CollectFuncCode { get; set; }
        /// <summary>
        /// 采集数据类型(int16/uint16/int32/uint32/int64/float32/float64/bcd/string/bool,空=uint16)
        ///</summary>
        [DisplayName("采集数据类型")]
        [SugarColumn(ColumnName = "collect_data_type", IsNullable = true, Length = 10, ColumnDescription = "采集数据类型(空=uint16)", DefaultValue = "", ColumnDataType = "varchar")]
        public string CollectDataType { get; set; }
        /// <summary>
        /// 字节序四选一(ABCD/CDAB/BADC/DCBA,空=ABCD)
        ///</summary>
        [DisplayName("字节序(ABCD/CDAB/BADC/DCBA)")]
        [SugarColumn(ColumnName = "collect_byte_order", IsNullable = true, Length = 10, ColumnDescription = "字节序四选一(空=ABCD)", DefaultValue = "", ColumnDataType = "varchar")]
        public string CollectByteOrder { get; set; }
        /// <summary>
        /// 位偏移(-1:整字取值,>=0:按位取布尔)
        ///</summary>
        [DisplayName("位偏移(-1整字)")]
        [SugarColumn(ColumnName = "collect_bit_offset", Length = 0, ColumnDescription = "位偏移(-1整字,>=0按位取布尔)", DefaultValue = "-1", ColumnDataType = "int")]
        public int CollectBitOffset { get; set; } = -1;
        /// <summary>
        /// 占用寄存器数(0:按数据类型推导,bcd/string须显式配置)
        ///</summary>
        [DisplayName("占用寄存器数(0按类型推导)")]
        [SugarColumn(ColumnName = "collect_reg_length", Length = 0, ColumnDescription = "占用寄存器数(0按数据类型推导)", DefaultValue = "0", ColumnDataType = "int")]
        public int CollectRegLength { get; set; }
        /// <summary>
        /// 是否可写(0:否1:是,FC03保持寄存器经FC06/16下发)
        ///</summary>
        [DisplayName("是否可写(0:否1:是)")]
        [SugarColumn(ColumnName = "collect_writable", Length = 1, ColumnDescription = "是否可写(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool CollectWritable { get; set; }
    }
}