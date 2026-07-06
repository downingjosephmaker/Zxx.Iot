using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 任务调度日志表
    /// </summary>
    [SplitTable(SplitType.Month, typeof(SnowSplitService))]
    [SugarTable(TableName = "schedule_job_log", TableDescription = "任务调度日志表", IsDisabledUpdateAll = true)]
    public class ScheduleJobLog
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
        /// 任务主键
        /// </summary>
        [DisplayName("任务ID")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "job_snow_id", Length = 20, ColumnDescription = "任务主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long JobSnowId { get; set; }

        /// <summary>
        /// 作业名称
        /// </summary>
        [DisplayName("作业名称")]
        [SugarColumn(ColumnName = "job_name", Length = 50, ColumnDescription = "作业名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string JobName { get; set; }

        /// <summary>
        /// 作业组名
        /// </summary>
        [DisplayName("作业组名")]
        [SugarColumn(ColumnName = "job_group_name", Length = 50, ColumnDescription = "作业组名", DefaultValue = "", ColumnDataType = "varchar")]
        public string JobGroupName { get; set; }

        /// <summary>
        /// 触发器名称
        /// </summary>
        [DisplayName("触发器名称")]
        [SugarColumn(ColumnName = "trigger_name", IsNullable = true, Length = 50, ColumnDescription = "触发器名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string TriggerName { get; set; }

        /// <summary>
        /// 触发器组名
        /// </summary>
        [DisplayName("触发器组名")]
        [SugarColumn(ColumnName = "trigger_group_name", IsNullable = true, Length = 50, ColumnDescription = "触发器组名", DefaultValue = "", ColumnDataType = "varchar")]
        public string TriggerGroupName { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        [DisplayName("开始时间")]
        [SugarColumn(ColumnName = "start_time", IsNullable = true, Length = 20, ColumnDescription = "开始时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        [DisplayName("结束时间")]
        [SugarColumn(ColumnName = "end_time", IsNullable = true, Length = 20, ColumnDescription = "结束时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string EndTime { get; set; }

        /// <summary>
        /// 作业状态(0:失败，1:成功)
        /// </summary>
        [DisplayName("作业状态(0:失败，1:成功)")]
        [SugarColumn(ColumnName = "execute_status", ColumnDescription = "作业状态(0:失败，1:成功)", DefaultValue = "0", ColumnDataType = "int")]
        public int ExecuteStatus { get; set; }

        /// <summary>
        /// 任务执行结果
        /// </summary>
        [DisplayName("任务执行结果")]
        [SugarColumn(ColumnName = "execute_result", IsNullable = true, Length = 300, ColumnDescription = "任务执行结果", DefaultValue = "", ColumnDataType = "varchar")]
        public string ExecuteResult { get; set; }

        /// <summary>
        /// 执行耗时(毫秒)
        /// </summary>
        [DisplayName("执行耗时(毫秒)")]
        [SugarColumn(ColumnName = "execute_time", ColumnDescription = "执行耗时(毫秒)", DefaultValue = "0", ColumnDataType = "bigint")]
        public long ExecuteTime { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        [DisplayName("错误信息")]
        [SugarColumn(ColumnName = "error_msg", IsNullable = true, Length = 1000, ColumnDescription = "错误信息", DefaultValue = "", ColumnDataType = "varchar")]
        public string ErrorMsg { get; set; }

    }
}