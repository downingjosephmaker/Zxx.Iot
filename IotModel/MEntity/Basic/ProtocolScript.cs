using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// JS协议解析脚本(§6.4:长尾私有协议兜底,三段式API=splitFrames/decode/encode;
    /// 安全教训默认禁用,需管理员显式启用;版本号保存自增,历史入protocol_script_history)
    ///</summary>
    [DisplayName("JS协议解析脚本")]
    [EntityCache]
    [SugarTable(TableName = "protocol_script", TableDescription = "JS协议解析脚本", IsDisabledUpdateAll = true)]
    public class ProtocolScript : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 脚本名称
        ///</summary>
        [DisplayName("脚本名称")]
        [SugarColumn(ColumnName = "script_name", IsNullable = true, Length = 50, ColumnDescription = "脚本名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string ScriptName { get; set; }
        /// <summary>
        /// 挂靠产品类型编码(该产品的非JSON载荷/透传帧用此脚本解析)
        ///</summary>
        [DisplayName("挂靠产品类型编码")]
        [SugarColumn(ColumnName = "device_type_code", IsNullable = true, Length = 50, ColumnDescription = "挂靠产品类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 脚本内容(JS,三段式函数)
        ///</summary>
        [DisplayName("脚本内容")]
        [SugarColumn(ColumnName = "script_content", IsNullable = true, ColumnDescription = "脚本内容", ColumnDataType = "text")]
        public string ScriptContent { get; set; }
        /// <summary>
        /// 版本号(保存自增,升级热切换依据)
        ///</summary>
        [DisplayName("版本号")]
        [SugarColumn(ColumnName = "version", ColumnDescription = "版本号", DefaultValue = "1", ColumnDataType = "int")]
        public int Version { get; set; } = 1;
        /// <summary>
        /// 试运行样例帧(hex)
        ///</summary>
        [DisplayName("试运行样例帧hex")]
        [SugarColumn(ColumnName = "sample_hex", IsNullable = true, Length = 500, ColumnDescription = "试运行样例帧hex", DefaultValue = "", ColumnDataType = "varchar")]
        public string SampleHex { get; set; }
        /// <summary>
        /// 试运行样例上下文(JSON)
        ///</summary>
        [DisplayName("试运行样例上下文JSON")]
        [SugarColumn(ColumnName = "sample_context", IsNullable = true, Length = 500, ColumnDescription = "试运行样例上下文JSON", DefaultValue = "", ColumnDataType = "varchar")]
        public string SampleContext { get; set; }
        /// <summary>
        /// 是否启用(0:否 1:是;§6.4安全教训默认禁用)
        ///</summary>
        [DisplayName("是否启用(0:否1:是,默认禁用)")]
        [SugarColumn(ColumnName = "is_enable", Length = 1, ColumnDescription = "是否启用(0:否1:是,默认禁用)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsEnable { get; set; } = false;
    }
}
