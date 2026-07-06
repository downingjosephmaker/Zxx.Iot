using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 操作日志(周分)
    ///</summary>
    [DisplayName("操作日志(周分)")]
    //[SplitTable(SplitType.Day)]//按日分表 (自带分表支持 年、季、月、周、日)
    //[SugarTable("sysyoptlog_{year}{month}{day}")]
    [SplitTable(SplitType.Week, typeof(SnowSplitService))] //自定义分表
    [SugarTable(TableName = "sysyopt_log", TableDescription = "操作日志", IsDisabledUpdateAll = true)]
    public class SysyoptLog
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [SplitField] //分表字段 在插入的时候会根据这个字段插入哪个表，在更新删除的时候用这个字段找出相关表
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 日志类型(登录,控制,查询,新增,删除,修改,...)
        ///</summary>
        [DisplayName("日志类型(登录,控制,查询,新增,删除,修改,...)")]
        [SugarColumn(ColumnName = "log_type", IsNullable = true, Length = 20, ColumnDescription = "日志类型(登录,控制,查询,新增,删除,修改,...)", DefaultValue = "", ColumnDataType = "varchar")]
        public string LogType { get; set; }
        /// <summary>
        /// 菜单全路径
        ///</summary>
        [DisplayName("菜单全路径")]
        [SugarColumn(ColumnName = "menu_full_name", IsNullable = true, Length = 300, ColumnDescription = "菜单全路径", DefaultValue = "", ColumnDataType = "varchar")]
        public string MenuFullName { get; set; }
        /// <summary>
        /// 方法描述
        ///</summary>
        [DisplayName("方法描述")]
        [SugarColumn(ColumnName = "opt_fun", IsNullable = true, Length = 200, ColumnDescription = "方法描述", DefaultValue = "", ColumnDataType = "varchar")]
        public string OptFun { get; set; }
        /// <summary>
        /// 参数描述
        ///</summary>
        [DisplayName("参数描述")]
        [SugarColumn(ColumnName = "opt_content", IsNullable = true, ColumnDescription = "参数描述", ColumnDataType = "text")]
        public string OptContent { get; set; }
        /// <summary>
        /// 控制来源(Web、Android、APP)
        ///</summary>
        [DisplayName("控制来源(Web、Android、APP)")]
        [SugarColumn(ColumnName = "source_type", IsNullable = true, Length = 10, ColumnDescription = "控制来源(Web、Android、APP)", DefaultValue = "", ColumnDataType = "varchar")]
        public string SourceType { get; set; }
        /// <summary>
        /// 接口名称
        ///</summary>
        [DisplayName("接口名称")]
        [SugarColumn(ColumnName = "interface_name", IsNullable = true, Length = 300, ColumnDescription = "接口名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string InterfaceName { get; set; }
        /// <summary>
        /// 操作人ID
        ///</summary>
        [DisplayName("操作人ID")]
        [SugarColumn(ColumnName = "create_id", ColumnDescription = "操作人ID", DefaultValue = "0", ColumnDataType = "int")]
        public int CreateId { get; set; }
        /// <summary>
        /// 操作时间
        ///</summary>
        [DisplayName("操作时间")]
        [SugarColumn(ColumnName = "create_time", IsNullable = true, Length = 20, ColumnDescription = "操作时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string CreateTime { get; set; }
        /// <summary>
        /// 操作人名称
        ///</summary>
        [DisplayName("操作人名称")]
        [SugarColumn(ColumnName = "create_name", IsNullable = true, Length = 50, ColumnDescription = "操作人名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string CreateName { get; set; }
        /// <summary>
        /// 操作人IP
        ///</summary>
        [DisplayName("操作人IP")]
        [SugarColumn(ColumnName = "create_ip", IsNullable = true, Length = 30, ColumnDescription = "操作人IP", DefaultValue = "", ColumnDataType = "varchar")]
        public string CreateIp { get; set; }
    }
}