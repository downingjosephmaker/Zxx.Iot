using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 系统状态检查记录
    ///</summary>
    [DisplayName("系统状态检查记录")]
    [SplitTable(SplitType.Month, typeof(SnowSplitService))]
    [SugarTable(TableName = "sys_status_check", TableDescription = "系统状态检查记录", IsDisabledUpdateAll = true)]
    public class SysStatusCheck
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [SplitField] //分表字段
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 检查时间
        ///</summary>
        [DisplayName("检查时间")]
        [SugarColumn(ColumnName = "check_time", Length = 20, ColumnDescription = "检查时间", DefaultValue = "0", ColumnDataType = "varchar")]
        public string CheckTime { get; set; }
        /// <summary>
        /// 操作系统版本
        ///</summary>
        [DisplayName("操作系统版本")]
        [SugarColumn(ColumnName = "os_version", IsNullable = true, Length = 80, ColumnDescription = "操作系统版本", DefaultValue = "", ColumnDataType = "varchar")]
        public string OsVersion { get; set; }
        /// <summary>
        /// 运行时长
        ///</summary>
        [DisplayName("运行时长")]
        [SugarColumn(ColumnName = "run_time", IsNullable = true, Length = 30, ColumnDescription = "运行时长", DefaultValue = "", ColumnDataType = "varchar")]
        public string RunTime { get; set; }
        /// <summary>
        /// 磁盘空间
        ///</summary>
        [DisplayName("磁盘空间")]
        [SugarColumn(ColumnName = "disk_space", IsNullable = true, Length = 500, ColumnDescription = "磁盘空间", DefaultValue = "", ColumnDataType = "varchar")]
        public string DiskSpace { get; set; }
        /// <summary>
        /// 总磁盘容量(GB)
        ///</summary>
        [DisplayName("总磁盘容量(GB)")]
        [SugarColumn(ColumnName = "total_disk_gb", Length = 18, DecimalDigits = 2, ColumnDescription = "总磁盘容量(GB)", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal TotalDiskGb { get; set; }
        /// <summary>
        /// 可用磁盘容量(GB)
        ///</summary>
        [DisplayName("可用磁盘容量(GB)")]
        [SugarColumn(ColumnName = "free_disk_gb", Length = 18, DecimalDigits = 2, ColumnDescription = "可用磁盘容量(GB)", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal FreeDiskGb { get; set; }
        /// <summary>
        /// 磁盘使用率(%)
        ///</summary>
        [DisplayName("磁盘使用率(%)")]
        [SugarColumn(ColumnName = "disk_usage_percent", Length = 5, DecimalDigits = 2, ColumnDescription = "磁盘使用率(%)", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal DiskUsagePercent { get; set; }
        /// <summary>
        /// 内存使用
        ///</summary>
        [DisplayName("内存使用")]
        [SugarColumn(ColumnName = "memory_usage", IsNullable = true, Length = 300, ColumnDescription = "内存使用", DefaultValue = "", ColumnDataType = "varchar")]
        public string MemoryUsage { get; set; }
        /// <summary>
        /// 总物理内存(MB)
        ///</summary>
        [DisplayName("总物理内存(MB)")]
        [SugarColumn(ColumnName = "total_memory_mb", Length = 18, DecimalDigits = 2, ColumnDescription = "总物理内存(MB)", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal TotalMemoryMb { get; set; }
        /// <summary>
        /// 可用物理内存(MB)
        ///</summary>
        [DisplayName("可用物理内存(MB)")]
        [SugarColumn(ColumnName = "available_memory_mb", Length = 18, DecimalDigits = 2, ColumnDescription = "可用物理内存(MB)", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal AvailableMemoryMb { get; set; }
        /// <summary>
        /// 内存使用率(%)
        ///</summary>
        [DisplayName("内存使用率(%)")]
        [SugarColumn(ColumnName = "memory_usage_percent", Length = 5, DecimalDigits = 2, ColumnDescription = "内存使用率(%)", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal MemoryUsagePercent { get; set; }
        /// <summary>
        /// 当前应用内存使用(MB)
        ///</summary>
        [DisplayName("当前应用内存使用(MB)")]
        [SugarColumn(ColumnName = "app_memory_mb", Length = 18, DecimalDigits = 2, ColumnDescription = "当前应用内存使用(MB)", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal AppMemoryMb { get; set; }
        /// <summary>
        /// CPU使用率
        ///</summary>
        [DisplayName("CPU使用率")]
        [SugarColumn(ColumnName = "cpu_usage", IsNullable = true, Length = 100, ColumnDescription = "CPU使用率", DefaultValue = "", ColumnDataType = "varchar")]
        public string CpuUsage { get; set; }
        /// <summary>
        /// 系统CPU使用率(%)
        ///</summary>
        [DisplayName("系统CPU使用率(%)")]
        [SugarColumn(ColumnName = "system_cpu_percent", Length = 5, DecimalDigits = 2, ColumnDescription = "系统CPU使用率(%)", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal SystemCpuPercent { get; set; }
        /// <summary>
        /// 应用CPU使用率(%)
        ///</summary>
        [DisplayName("应用CPU使用率(%)")]
        [SugarColumn(ColumnName = "app_cpu_percent", Length = 5, DecimalDigits = 2, ColumnDescription = "应用CPU使用率(%)", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal AppCpuPercent { get; set; }
        /// <summary>
        /// 最后备份时间
        ///</summary>
        [DisplayName("最后备份时间")]
        [SugarColumn(ColumnName = "last_backup", IsNullable = true, Length = 50, ColumnDescription = "最后备份时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string LastBackup { get; set; }
        /// <summary>
        /// .NET版本
        ///</summary>
        [DisplayName(".NET版本")]
        [SugarColumn(ColumnName = "dotnet_version", IsNullable = true, Length = 30, ColumnDescription = ".NET版本", DefaultValue = "", ColumnDataType = "varchar")]
        public string DotnetVersion { get; set; }
        /// <summary>
        /// 应用版本
        ///</summary>
        [DisplayName("应用版本")]
        [SugarColumn(ColumnName = "app_version", IsNullable = true, Length = 50, ColumnDescription = "应用版本", DefaultValue = "", ColumnDataType = "varchar")]
        public string AppVersion { get; set; }
        /// <summary>
        /// 数据库连接数
        ///</summary>
        [DisplayName("数据库连接数")]
        [SugarColumn(ColumnName = "db_connections", Length = 11, ColumnDescription = "数据库连接数", DefaultValue = "0", ColumnDataType = "int")]
        public int DbConnections { get; set; }
        /// <summary>
        /// 数据库查询响应时间(ms)
        ///</summary>
        [DisplayName("数据库查询响应时间(ms)")]
        [SugarColumn(ColumnName = "db_response_time", Length = 11, ColumnDescription = "数据库查询响应时间(ms)", DefaultValue = "0", ColumnDataType = "int")]
        public int DbResponseTime { get; set; }
        /// <summary>
        /// 网络接收速率(KB/s)
        ///</summary>
        [DisplayName("网络接收速率(KB/s)")]
        [SugarColumn(ColumnName = "network_receive_kbps", Length = 18, DecimalDigits = 2, ColumnDescription = "网络接收速率(KB/s)", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal NetworkReceiveKbps { get; set; }
        /// <summary>
        /// 网络发送速率(KB/s)
        ///</summary>
        [DisplayName("网络发送速率(KB/s)")]
        [SugarColumn(ColumnName = "network_send_kbps", Length = 18, DecimalDigits = 2, ColumnDescription = "网络发送速率(KB/s)", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal NetworkSendKbps { get; set; }
    }
}