using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 产品命令白名单(§5:借鉴KubeEdge DeviceMethod,只能调用声明过的命令天然审计边界;
    /// 前端指令下发表单按ParamSchema动态渲染,阀控类NeedConfirm二次确认)
    ///</summary>
    [DisplayName("产品命令白名单")]
    [EntityCache]
    [SugarTable(TableName = "product_command", TableDescription = "产品命令白名单", IsDisabledUpdateAll = true)]
    public class ProductCommand : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 所属产品类型编码
        ///</summary>
        [DisplayName("所属产品类型编码")]
        [SugarColumn(ColumnName = "device_type_code", IsNullable = true, Length = 50, ColumnDescription = "所属产品类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 命令显示名称
        ///</summary>
        [DisplayName("命令显示名称")]
        [SugarColumn(ColumnName = "command_name", IsNullable = true, Length = 50, ColumnDescription = "命令显示名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string CommandName { get; set; }
        /// <summary>
        /// 下行控制类型(插件侧ClassName:netmodbuswrite/nets7write/netopcuawrite/netcjt188valve等)
        ///</summary>
        [DisplayName("下行控制类型(ClassName)")]
        [SugarColumn(ColumnName = "class_name", IsNullable = true, Length = 50, ColumnDescription = "下行控制类型(ClassName)", DefaultValue = "", ColumnDataType = "varchar")]
        public string ClassName { get; set; }
        /// <summary>
        /// 参数JSON Schema(前端动态表单渲染依据)
        ///</summary>
        [DisplayName("参数JSON Schema")]
        [SugarColumn(ColumnName = "param_schema", IsNullable = true, ColumnDescription = "参数JSON Schema", ColumnDataType = "text")]
        public string ParamSchema { get; set; }
        /// <summary>
        /// 下行内容模板(ConContent JSON模板,表单值填充占位)
        ///</summary>
        [DisplayName("下行内容模板")]
        [SugarColumn(ColumnName = "con_template", IsNullable = true, ColumnDescription = "下行内容模板", ColumnDataType = "text")]
        public string ConTemplate { get; set; }
        /// <summary>
        /// 是否二次确认(阀控等高危命令,§6.3)
        ///</summary>
        [DisplayName("是否二次确认(0:否1:是)")]
        [SugarColumn(ColumnName = "need_confirm", Length = 1, ColumnDescription = "是否二次确认(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool NeedConfirm { get; set; }
        /// <summary>
        /// 是否启用(0:否 1:是)
        ///</summary>
        [DisplayName("是否启用(0:否1:是)")]
        [SugarColumn(ColumnName = "is_enable", Length = 1, ColumnDescription = "是否启用(0:否1:是)", DefaultValue = "1", ColumnDataType = "bit")]
        public bool IsEnable { get; set; } = true;
    }
}
