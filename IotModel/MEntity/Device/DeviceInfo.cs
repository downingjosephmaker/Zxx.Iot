using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备表
    ///</summary>
    [DisplayName("设备表")]
    [SugarTable(TableName = "device_info", TableDescription = "设备表", IsDisabledUpdateAll = true)]
    public class DeviceInfo : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// 设备主键
        ///</summary>
        [DisplayName("设备主键")]
        [SugarColumn(ColumnName = "device_id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "设备主键", DefaultValue = "0", ColumnDataType = "int")]
        public int DeviceId { get; set; }
        /// <summary>
        /// 设备名称
        ///</summary>
        [DisplayName("设备名称")]
        [SugarColumn(ColumnName = "device_name", IsNullable = true, Length = 50, ColumnDescription = "设备名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceName { get; set; }
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        [SugarColumn(ColumnName = "device_type_code", Length = 30, ColumnDescription = "设备类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备全类型编码
        ///</summary>
        [DisplayName("设备全类型编码")]
        [SugarColumn(ColumnName = "device_type_full_code", Length = 200, ColumnDescription = "设备全类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeFullCode { get; set; }
        /// <summary>
        /// 设备编号
        ///</summary>
        [DisplayName("设备编号")]
        [SugarColumn(ColumnName = "device_guid", IsNullable = true, Length = 30, ColumnDescription = "设备编号", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceGuid { get; set; }
        /// <summary>
        /// 设备网关编号
        ///</summary>
        [DisplayName("设备网关编号")]
        [SugarColumn(ColumnName = "device_gateway", IsNullable = true, Length = 30, ColumnDescription = "设备网关编号", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceGateway { get; set; }
        /// <summary>
        /// 设备IP地址
        ///</summary>
        [DisplayName("设备IP地址")]
        [SugarColumn(ColumnName = "device_ip", IsNullable = true, Length = 30, ColumnDescription = "设备IP地址", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceIp { get; set; }
        /// <summary>
        /// 设备端口号
        ///</summary>
        [DisplayName("设备端口号")]
        [SugarColumn(ColumnName = "device_port", ColumnDescription = "设备端口号", DefaultValue = "0", ColumnDataType = "int")]
        public int DevicePort { get; set; }
        /// <summary>
        /// 串口通道号
        ///</summary>
        [DisplayName("串口通道号")]
        [SugarColumn(ColumnName = "device_com", ColumnDescription = "串口通道号", DefaultValue = "0", ColumnDataType = "int")]
        public int DeviceCom { get; set; }
        /// <summary>
        /// 设备协议地址
        ///</summary>
        [DisplayName("设备协议地址")]
        [SugarColumn(ColumnName = "device_adr", ColumnDescription = "设备协议地址", DefaultValue = "0", ColumnDataType = "int")]
        public int DeviceAdr { get; set; }
        /// <summary>
        /// 是否采集(1:采集;0:不采集)
        ///</summary>
        [DisplayName("是否采集(1:采集;0:不采集)")]
        [SugarColumn(ColumnName = "is_collection", ColumnDescription = "是否采集(1:采集;0:不采集)", DefaultValue = "1", ColumnDataType = "int")]
        public int IsCollection { get; set; }
        /// <summary>
        /// 虚拟设备(0:不是 1:是)
        ///</summary>
        [DisplayName("虚拟设备(0:不是 1:是)")]
        [SugarColumn(ColumnName = "is_virtual", ColumnDescription = "虚拟设备(0:不是 1:是)", DefaultValue = "0", ColumnDataType = "int")]
        public int IsVirtual { get; set; }
        /// <summary>
        /// 最后在线时间
        ///</summary>
        [DisplayName("最后在线时间")]
        [SugarColumn(ColumnName = "last_online_time", IsNullable = true, Length = 20, ColumnDescription = "最后在线时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string LastOnlineTime { get; set; }
        /// <summary>
        /// 设备状态(2:在线;1:掉电;0:离线)
        ///</summary>
        [DisplayName("设备状态(2:在线;1:掉电;0:离线)")]
        [SugarColumn(ColumnName = "device_state", ColumnDescription = "设备状态(2:在线;1:掉电;0:离线)", DefaultValue = "0", ColumnDataType = "int")]
        public int DeviceState { get; set; }
        /// <summary>
        /// 设备告警状态(1:告警;0:正常)
        ///</summary>
        [DisplayName("设备告警状态(1:告警;0:正常)")]
        [SugarColumn(ColumnName = "device_alarm", ColumnDescription = "设备告警状态(1:告警;0:正常)", DefaultValue = "0", ColumnDataType = "int")]
        public int DeviceAlarm { get; set; }
        /// <summary>
        /// 开关状态(0:关1:开)
        ///</summary>
        [DisplayName("开关状态(0:关1:开)")]
        [SugarColumn(ColumnName = "device_switch", ColumnDescription = "开关状态(0:关1:开)", DefaultValue = "0", ColumnDataType = "int")]
        public int DeviceSwitch { get; set; }
        /// <summary>
        /// 设备图标
        ///</summary>
        [DisplayName("设备图标")]
        [SugarColumn(ColumnName = "icon_type", IsNullable = true, Length = 20, ColumnDescription = "设备图标", DefaultValue = "", ColumnDataType = "varchar")]
        public string IconType { get; set; }
        /// <summary>
        /// 排序序号
        ///</summary>
        [DisplayName("排序序号")]
        [SugarColumn(ColumnName = "sort_border", IsNullable = true, Length = 10, ColumnDescription = "排序序号", DefaultValue = "", ColumnDataType = "varchar")]
        public string SortBorder { get; set; }
        /// <summary>
        /// 设备级别
        ///</summary>
        [DisplayName("设备级别")]
        [SugarColumn(ColumnName = "tree_level", ColumnDescription = "设备级别", DefaultValue = "0", ColumnDataType = "int")]
        public int TreeLevel { get; set; }
        /// <summary>
        /// 上级设备ID
        ///</summary>
        [DisplayName("上级设备ID")]
        [SugarColumn(ColumnName = "parent_id", ColumnDescription = "上级设备ID", DefaultValue = "0", ColumnDataType = "int")]
        public int ParentId { get; set; }
        /// <summary>
        /// 设备名称(全)
        ///</summary>
        [DisplayName("设备名称(全)")]
        [SugarColumn(ColumnName = "full_name", IsNullable = true, Length = 400, ColumnDescription = "设备名称(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullName { get; set; }
        /// <summary>
        /// 设备ID(全)
        ///</summary>
        [DisplayName("设备ID(全)")]
        [SugarColumn(ColumnName = "full_code", IsNullable = true, Length = 200, ColumnDescription = "设备ID(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullCode { get; set; }
        /// <summary>
        /// 是否有子集
        ///</summary>
        [DisplayName("是否有子集")]
        [SugarColumn(ColumnName = "has_child", Length = 1, ColumnDescription = "是否有子集", DefaultValue = "0", ColumnDataType = "bit")]
        public bool HasChild { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [JsonField(typeof(Expand_DeviceInfo))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }
    }
}