using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 通知渠道(§9.5:邮件/Webhook/钉钉/企微/短信预留;
    /// 告警IsNote=1时按启用渠道逐一外发;引擎为后台组件全局加载,不挂IUnitEntity)
    ///</summary>
    [DisplayName("通知渠道")]
    [EntityCache]
    [SugarTable(TableName = "notify_channel", TableDescription = "通知渠道", IsDisabledUpdateAll = true)]
    public class NotifyChannel : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 渠道名称
        ///</summary>
        [DisplayName("渠道名称")]
        [SugarColumn(ColumnName = "channel_name", IsNullable = true, Length = 50, ColumnDescription = "渠道名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string ChannelName { get; set; }
        /// <summary>
        /// 渠道类型(1:邮件 2:Webhook 3:钉钉机器人 4:企微机器人 5:短信预留)
        ///</summary>
        [DisplayName("渠道类型(1邮件2Webhook3钉钉4企微5短信)")]
        [SugarColumn(ColumnName = "channel_type", ColumnDescription = "渠道类型(1邮件2Webhook3钉钉4企微5短信)", DefaultValue = "2", ColumnDataType = "int")]
        public int ChannelType { get; set; } = 2;
        /// <summary>
        /// 目标地址(邮件外发接口/Webhook地址/机器人Webhook)
        ///</summary>
        [DisplayName("目标地址")]
        [SugarColumn(ColumnName = "target_url", IsNullable = true, Length = 300, ColumnDescription = "目标地址", DefaultValue = "", ColumnDataType = "varchar")]
        public string TargetUrl { get; set; }
        /// <summary>
        /// 密钥(钉钉加签secret等,空=不加签)
        ///</summary>
        [DisplayName("密钥(空=不加签)")]
        [SugarColumn(ColumnName = "secret", IsNullable = true, Length = 100, ColumnDescription = "密钥(空=不加签)", DefaultValue = "", ColumnDataType = "varchar")]
        public string Secret { get; set; }
        /// <summary>
        /// 接收人(邮件收件人/短信手机号,逗号分隔)
        ///</summary>
        [DisplayName("接收人(逗号分隔)")]
        [SugarColumn(ColumnName = "receivers", IsNullable = true, Length = 300, ColumnDescription = "接收人(逗号分隔)", DefaultValue = "", ColumnDataType = "varchar")]
        public string Receivers { get; set; }
        /// <summary>
        /// 告警等级过滤(逗号分隔,只通知命中等级;空=全部)
        ///</summary>
        [DisplayName("告警等级过滤(空=全部)")]
        [SugarColumn(ColumnName = "grade_filter", IsNullable = true, Length = 50, ColumnDescription = "告警等级过滤(空=全部)", DefaultValue = "", ColumnDataType = "varchar")]
        public string GradeFilter { get; set; }
        /// <summary>
        /// 是否启用(0:否 1:是)
        ///</summary>
        [DisplayName("是否启用(0:否1:是)")]
        [SugarColumn(ColumnName = "is_enable", Length = 1, ColumnDescription = "是否启用(0:否1:是)", DefaultValue = "1", ColumnDataType = "bit")]
        public bool IsEnable { get; set; } = true;
    }
}
