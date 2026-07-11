using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 插件表
    /// </summary>
    [DisplayName("插件表")]
    [EntityCache]
    [SugarTable(TableName = "sys_plugin", TableDescription = "插件表", IsDisabledUpdateAll = true)]
    public class SysPlugin : BaseEntity
    {
        /// <summary>
        /// 插件Guid（主键）
        /// </summary>
        [DisplayName("插件Guid")]
        [SugarColumn(ColumnName = "plugin_guid", IsPrimaryKey = true, IsNullable = false, Length = 50, ColumnDescription = "插件Guid", DefaultValue = "", ColumnDataType = "varchar")]
        public string PluginGuid { get; set; }

        /// <summary>
        /// 插件名称
        /// </summary>
        [DisplayName("插件名称")]
        [SugarColumn(ColumnName = "plugin_name", IsNullable = true, Length = 100, ColumnDescription = "插件名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string PluginName { get; set; }

        /// <summary>
        /// 插件类型(系统插件,业务插件)
        /// </summary>
        [DisplayName("插件类型(系统插件,业务插件)")]
        [SugarColumn(ColumnName = "plugin_type", IsNullable = true, Length = 20, ColumnDescription = "插件类型(系统插件,业务插件)", DefaultValue = "业务插件", ColumnDataType = "varchar")]
        public string PluginType { get; set; }

        /// <summary>
        /// 插件描述
        /// </summary>
        [DisplayName("插件描述")]
        [SugarColumn(ColumnName = "plugin_desc", IsNullable = true, Length = 300, ColumnDescription = "插件描述", DefaultValue = "", ColumnDataType = "varchar")]
        public string PluginDesc { get; set; }

        /// <summary>
        /// 插件模型路径
        /// </summary>
        [DisplayName("插件模型路径")]
        [SugarColumn(ColumnName = "plugin_model_path", IsNullable = true, Length = 100, ColumnDescription = "插件模型路径", DefaultValue = "", ColumnDataType = "varchar")]
        public string PluginModelPath { get; set; }

        /// <summary>
        /// 插件版本
        /// </summary>
        [DisplayName("插件版本")]
        [SugarColumn(ColumnName = "plugin_version", IsNullable = true, Length = 10, ColumnDescription = "插件版本", DefaultValue = "", ColumnDataType = "varchar")]
        public string PluginVersion { get; set; }

        /// <summary>
        /// 插件状态(0:禁用,1:启用)
        /// </summary>
        [DisplayName("插件状态(0:禁用,1:启用)")]
        [SugarColumn(ColumnName = "plugin_status", ColumnDescription = "插件状态(0:禁用,1:启用)", DefaultValue = "0", ColumnDataType = "int")]
        public int PluginStatus { get; set; }

        /// <summary>
        /// 插件通讯状态(0:正常,1:异常)
        /// </summary>
        [DisplayName("插件通讯状态(0:正常,1:异常)")]
        [SugarColumn(ColumnName = "plugin_heart_status", ColumnDescription = "插件通讯状态(0:正常, 1:异常)", DefaultValue = "0", ColumnDataType = "int")]
        public int PluginHeartStatus { get; set; }

        /// <summary>
        /// 插件通讯状态时间
        /// </summary>
        [DisplayName("插件通讯状态时间")]
        [SugarColumn(ColumnName = "plugin_heart_time", IsNullable = true, Length = 20, ColumnDescription = "插件通讯状态时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string PluginHeartTime { get; set; }

        /// <summary>
        /// 插件参数(JSON)
        /// </summary>
        [DisplayName("插件参数(JSON)")]
        [JsonField(typeof(Expand_SysPlugin))]
        [SugarColumn(ColumnName = "plugin_config", IsNullable = true, ColumnDescription = "插件参数(JSON)", ColumnDataType = "text")]
        public string PluginConfig { get; set; }

        /// <summary>
        /// 插件自描述清单(JSON:configSchema/defaultConfig/commands/addressing,
        /// 上传或加载时反射ICenBoPlugin.PluginManifest持久化,前后端共用一份元数据)
        /// </summary>
        [DisplayName("插件自描述清单(JSON)")]
        [SugarColumn(ColumnName = "plugin_manifest", IsNullable = true, ColumnDescription = "插件自描述清单(JSON)", ColumnDataType = "text")]
        public string PluginManifest { get; set; }

        /// <summary>
        /// 插件路径
        /// </summary>
        [DisplayName("插件路径")]
        [SugarColumn(ColumnName = "plugin_path", IsNullable = true, Length = 200, ColumnDescription = "插件路径", DefaultValue = "", ColumnDataType = "varchar")]
        public string PluginPath { get; set; }
    }
}