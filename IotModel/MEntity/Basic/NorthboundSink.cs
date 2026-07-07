using Newtonsoft.Json;
using SqlSugar;
using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 北向转发目的地(§10.2:MQTT/HTTP Webhook,Kafka预留;
    /// 三段式断线续传由NorthboundForwardService承载;转发器为后台组件全局加载,不挂IUnitEntity)
    ///</summary>
    [DisplayName("北向转发目的地")]
    [EntityCache]
    [SugarTable(TableName = "northbound_sink", TableDescription = "北向转发目的地", IsDisabledUpdateAll = true)]
    public class NorthboundSink : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 目的地名称
        ///</summary>
        [DisplayName("目的地名称")]
        [SugarColumn(ColumnName = "sink_name", IsNullable = true, Length = 50, ColumnDescription = "目的地名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string SinkName { get; set; }
        /// <summary>
        /// 目的地类型(1:MQTT 2:HTTP Webhook 3:Kafka预留)
        ///</summary>
        [DisplayName("目的地类型(1MQTT2HTTP3Kafka预留)")]
        [SugarColumn(ColumnName = "sink_type", ColumnDescription = "目的地类型(1MQTT2HTTP3Kafka预留)", DefaultValue = "2", ColumnDataType = "int")]
        public int SinkType { get; set; } = 2;
        /// <summary>
        /// 连接配置JSON(MQTT=SinkMqttConfig,HTTP=SinkHttpConfig)
        ///</summary>
        [DisplayName("连接配置JSON")]
        [SugarColumn(ColumnName = "conn_config", IsNullable = true, ColumnDescription = "连接配置JSON", ColumnDataType = "text")]
        public string ConnConfig { get; set; }
        /// <summary>
        /// 转发内容(1:仅遥测 2:仅告警 3:遥测+告警)
        ///</summary>
        [DisplayName("转发内容(1遥测2告警3全部)")]
        [SugarColumn(ColumnName = "content_mode", ColumnDescription = "转发内容(1遥测2告警3全部)", DefaultValue = "3", ColumnDataType = "int")]
        public int ContentMode { get; set; } = 3;
        /// <summary>
        /// 推送范围(0:全部 1:按产品类型编码 2:按设备ID)
        ///</summary>
        [DisplayName("推送范围(0全部1按产品2按设备)")]
        [SugarColumn(ColumnName = "scope_type", ColumnDescription = "推送范围(0全部1按产品2按设备)", DefaultValue = "0", ColumnDataType = "int")]
        public int ScopeType { get; set; }
        /// <summary>
        /// 范围清单JSON(按产品=类型编码数组,按设备=设备ID数组)
        ///</summary>
        [DisplayName("范围清单JSON")]
        [SugarColumn(ColumnName = "scope_json", IsNullable = true, ColumnDescription = "范围清单JSON", ColumnDataType = "text")]
        public string ScopeJson { get; set; }
        /// <summary>
        /// 是否启用(0:否 1:是)
        ///</summary>
        [DisplayName("是否启用(0:否1:是)")]
        [SugarColumn(ColumnName = "is_enable", Length = 1, ColumnDescription = "是否启用(0:否1:是)", DefaultValue = "1", ColumnDataType = "bit")]
        public bool IsEnable { get; set; } = true;
    }

    /// <summary>
    /// MQTT目的地连接配置(ConnConfig载荷,SinkType=1)
    /// </summary>
    public class SinkMqttConfig
    {
        /// <summary>
        /// Broker地址
        /// </summary>
        public string Host { get; set; } = "";
        /// <summary>
        /// Broker端口
        /// </summary>
        public int Port { get; set; } = 1883;
        /// <summary>
        /// 客户端ID(空=自动生成)
        /// </summary>
        public string ClientId { get; set; } = "";
        /// <summary>
        /// 账号(空=匿名)
        /// </summary>
        public string UserName { get; set; } = "";
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = "";
        /// <summary>
        /// 遥测主题模板({deviceId}占位)
        /// </summary>
        public string DataTopic { get; set; } = "iot/data/{deviceId}";
        /// <summary>
        /// 告警事件主题模板({deviceId}占位)
        /// </summary>
        public string EventTopic { get; set; } = "iot/event/{deviceId}";
    }

    /// <summary>
    /// HTTP Webhook目的地连接配置(ConnConfig载荷,SinkType=2;POST批量JSON数组)
    /// </summary>
    public class SinkHttpConfig
    {
        /// <summary>
        /// 目标URL
        /// </summary>
        public string Url { get; set; } = "";
        /// <summary>
        /// 附加请求头
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}
