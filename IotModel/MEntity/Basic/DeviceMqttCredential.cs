using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备MQTT凭据(外网MQTT接入用户名/PBKDF2密码哈希,独立于DeviceInfo避免每次设备查询把哈希带进内存)
    ///</summary>
    [DisplayName("设备MQTT凭据")]
    [SugarTable(TableName = "device_mqtt_credential", TableDescription = "设备MQTT凭据", IsDisabledUpdateAll = true)]
    public class DeviceMqttCredential : ITenantEntity
    {
        /// <summary>
        /// 凭据ID
        ///</summary>
        [DisplayName("凭据ID")]
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "凭据ID", DefaultValue = "0", ColumnDataType = "int")]
        public int Id { get; set; }
        /// <summary>
        /// 设备MQTT用户名
        ///</summary>
        [DisplayName("设备MQTT用户名")]
        [SugarColumn(ColumnName = "mqtt_user", IsNullable = true, Length = 64, ColumnDescription = "设备MQTT用户名", DefaultValue = "", ColumnDataType = "varchar")]
        public string MqttUser { get; set; }
        /// <summary>
        /// PBKDF2密码哈希(base64)
        ///</summary>
        [DisplayName("PBKDF2密码哈希(base64)")]
        [SugarColumn(ColumnName = "pass_hash", IsNullable = true, Length = 200, ColumnDescription = "PBKDF2密码哈希(base64)", DefaultValue = "", ColumnDataType = "varchar")]
        public string PassHash { get; set; }
        /// <summary>
        /// 每设备盐(base64)
        ///</summary>
        [DisplayName("每设备盐(base64)")]
        [SugarColumn(ColumnName = "salt", IsNullable = true, Length = 64, ColumnDescription = "每设备盐(base64)", DefaultValue = "", ColumnDataType = "varchar")]
        public string Salt { get; set; }
        /// <summary>
        /// 绑定设备(deviceKey)
        ///</summary>
        [DisplayName("绑定设备(deviceKey)")]
        [SugarColumn(ColumnName = "device_gateway", IsNullable = true, Length = 30, ColumnDescription = "绑定设备(deviceKey)", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceGateway { get; set; }
        /// <summary>
        /// 启用(吊销开关)
        ///</summary>
        [DisplayName("启用(吊销开关)")]
        [SugarColumn(ColumnName = "is_enable", ColumnDescription = "启用(吊销开关)", DefaultValue = "1", ColumnDataType = "tinyint")]
        public bool IsEnable { get; set; }
        /// <summary>
        /// 租户ID
        ///</summary>
        [DisplayName("租户ID")]
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }
    }
}
