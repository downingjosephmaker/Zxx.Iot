using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// mqtt配置
    ///</summary>
    [DisplayName("mqtt配置")]
    [SugarTable(TableName = "admin_mqttparam", TableDescription = "mqtt配置", IsDisabledUpdateAll = true)]
    public class AdminMqttparam : ITenantEntity
    {
        /// <summary>
        /// 配置ID
        ///</summary>
        [DisplayName("配置ID")]
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "配置ID", DefaultValue = "0", ColumnDataType = "int")]
        public int Id { get; set; }
        /// <summary>
        /// 系统标题
        ///</summary>
        [DisplayName("系统标题")]
        [SugarColumn(ColumnName = "system_title", IsNullable = true, Length = 30, ColumnDescription = "系统标题", DefaultValue = "", ColumnDataType = "varchar")]
        public string SystemTitle { get; set; }
        /// <summary>
        /// Mqtt服务地址
        ///</summary>
        [DisplayName("Mqtt服务地址")]
        [SugarColumn(ColumnName = "mqtt_server", IsNullable = true, Length = 20, ColumnDescription = "Mqtt服务地址", DefaultValue = "", ColumnDataType = "varchar")]
        public string MqttServer { get; set; }
        /// <summary>
        /// Mqtt客户端端口
        ///</summary>
        [DisplayName("Mqtt客户端端口")]
        [SugarColumn(ColumnName = "mqtt_client_port", ColumnDescription = "Mqtt客户端端口", DefaultValue = "0", ColumnDataType = "int")]
        public int MqttClientPort { get; set; }
        /// <summary>
        /// Mqtt客户端Web端口
        ///</summary>
        [DisplayName("Mqtt客户端Web端口")]
        [SugarColumn(ColumnName = "mqtt_web_client_port", ColumnDescription = "Mqtt客户端Web端口", DefaultValue = "0", ColumnDataType = "int")]
        public int MqttWebClientPort { get; set; }
        /// <summary>
        /// Mqtt服务端端口
        ///</summary>
        [DisplayName("Mqtt服务端端口")]
        [SugarColumn(ColumnName = "mqtt_server_port", ColumnDescription = "Mqtt服务端端口", DefaultValue = "0", ColumnDataType = "int")]
        public int MqttServerPort { get; set; }
        /// <summary>
        /// 内网绑定地址（默认0.0.0.0等价IPAddress.Any，可收窄到指定网卡）
        ///</summary>
        [DisplayName("内网绑定地址")]
        [SugarColumn(ColumnName = "lan_bind_address", Length = 64, IsNullable = true, ColumnDataType = "varchar", DefaultValue = "0.0.0.0")]
        public string LanBindAddress { get; set; }
        /// <summary>
        /// Mqtt用户名
        ///</summary>
        [DisplayName("Mqtt用户名")]
        [SugarColumn(ColumnName = "mqtt_user", IsNullable = true, Length = 20, ColumnDescription = "Mqtt用户名", DefaultValue = "", ColumnDataType = "varchar")]
        public string MqttUser { get; set; }
        /// <summary>
        /// Mqtt密码
        ///</summary>
        [DisplayName("Mqtt密码")]
        [SugarColumn(ColumnName = "mqtt_pass", IsNullable = true, Length = 20, ColumnDescription = "Mqtt密码", DefaultValue = "", ColumnDataType = "varchar")]
        public string MqttPass { get; set; }
        /// <summary>
        /// Key：WebApi
        ///</summary>
        [DisplayName("Key：WebApi")]
        [SugarColumn(ColumnName = "mqtt_sub_topic_web_api", IsNullable = true, Length = 50, ColumnDescription = "Key：WebApi", DefaultValue = "", ColumnDataType = "varchar")]
        public string MqttSubTopicWebApi { get; set; }
        /// <summary>
        /// Key：实时数据
        ///</summary>
        [DisplayName("Key：实时数据")]
        [SugarColumn(ColumnName = "mqtt_sub_topic_web_real", IsNullable = true, Length = 50, ColumnDescription = "Key：实时数据", DefaultValue = "", ColumnDataType = "varchar")]
        public string MqttSubTopicWebReal { get; set; }
        /// <summary>
        /// Key：实时告警
        ///</summary>
        [DisplayName("Key：实时告警")]
        [SugarColumn(ColumnName = "mqtt_sub_topic_web_alarm", IsNullable = true, Length = 50, ColumnDescription = "Key：实时告警", DefaultValue = "", ColumnDataType = "varchar")]
        public string MqttSubTopicWebAlarm { get; set; }
        /// <summary>
        /// Key：交互中转
        ///</summary>
        [DisplayName("Key：交互中转")]
        [SugarColumn(ColumnName = "mqtt_zz_topic", IsNullable = true, Length = 50, ColumnDescription = "Key：交互中转", DefaultValue = "", ColumnDataType = "varchar")]
        public string MqttZzTopic { get; set; }
        /// <summary>
        /// 租户ID
        ///</summary>
        [DisplayName("租户ID")]
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }
    }
}