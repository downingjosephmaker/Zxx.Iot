using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 任务调度信息表
    /// </summary>
    [DisplayName("任务调度信息表")]
    [EntityCache]
    [SugarTable(TableName = "schedule_job", TableDescription = "任务调度信息表", IsDisabledUpdateAll = true)]
    public class ScheduleJob : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }

        /// <summary>
        /// 作业名称
        /// </summary>
        [DisplayName("作业名称")]
        [SugarColumn(ColumnName = "job_name", IsNullable = true, Length = 50, ColumnDescription = "作业名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string JobName { get; set; }

        /// <summary>
        /// 作业组名
        /// </summary>
        [DisplayName("作业组名")]
        [SugarColumn(ColumnName = "job_group_name", IsNullable = true, Length = 30, ColumnDescription = "作业组名", DefaultValue = "", ColumnDataType = "varchar")]
        public string JobGroupName { get; set; }

        /// <summary>
        /// 作业类称
        /// </summary>
        [DisplayName("作业类称")]
        [SugarColumn(ColumnName = "job_class_name", IsNullable = true, Length = 30, ColumnDescription = "作业类称", DefaultValue = "", ColumnDataType = "varchar")]
        public string JobClassName { get; set; }

        /// <summary>
        /// 作业描述
        /// </summary>
        [DisplayName("作业描述")]
        [SugarColumn(ColumnName = "job_description", IsNullable = true, Length = 300, ColumnDescription = "作业描述", DefaultValue = "", ColumnDataType = "varchar")]
        public string JobDescription { get; set; }

        /// <summary>
        /// 触发器类型(0:Cron触发器，1:简单触发器)
        /// </summary>
        [DisplayName("触发器类型(0:Cron触发器，1:简单触发器)")]
        [SugarColumn(ColumnName = "trigger_type", ColumnDescription = "简单触发器执行间隔(秒)", DefaultValue = "0", ColumnDataType = "int")]
        public int TriggerType { get; set; }

        /// <summary>
        /// Cron表达式
        /// </summary>
        [DisplayName("Cron表达式")]
        [SugarColumn(ColumnName = "job_cron", IsNullable = true, Length = 30, ColumnDescription = "Cron表达式", DefaultValue = "", ColumnDataType = "varchar")]
        public string JobCron { get; set; }

        /// <summary>
        /// 简单触发器执行间隔(秒)
        /// </summary>
        [DisplayName("简单触发器执行间隔(秒)")]
        [SugarColumn(ColumnName = "interval_seconds", ColumnDescription = "简单触发器执行间隔(秒)", DefaultValue = "0", ColumnDataType = "int")]
        public int IntervalSeconds { get; set; }

        /// <summary>
        /// 作业限制(0:全部，1:主程序，2:副程序)
        /// </summary>
        [DisplayName("作业限制(0:全部，1:主程序，2:副程序)")]
        [SugarColumn(ColumnName = "job_limit", ColumnDescription = "作业限制(0:全部，1:主程序，2:副程序)", DefaultValue = "0", ColumnDataType = "int")]
        public int JobLimit { get; set; }

        /// <summary>
        /// 是否记录日志(0:是，1:否)
        /// </summary>
        [DisplayName("是否记录日志(0:是，1:否)")]
        [SugarColumn(ColumnName = "job_log", ColumnDescription = "是否记录日志(0:是，1:否)", DefaultValue = "0", ColumnDataType = "int")]
        public int JobLog { get; set; }

        /// <summary>
        /// 已执行次数
        /// </summary>
        [DisplayName("已执行次数")]
        [SugarColumn(ColumnName = "execute_count", ColumnDescription = "已执行次数", DefaultValue = "0", ColumnDataType = "int")]
        public int ExecuteCount { get; set; }

        /// <summary>
        /// 作业状态(0:停止，1:运行，2:暂停)
        /// </summary>
        [DisplayName("作业状态(0:停止，1:运行中，2:暂停)")]
        [SugarColumn(ColumnName = "job_status", ColumnDescription = "作业状态(0:停止，1:运行，2:暂停)", DefaultValue = "0", ColumnDataType = "int")]
        public int JobStatus { get; set; }

        /// <summary>
        /// 上次执行时间
        /// </summary>
        [DisplayName("上次执行时间")]
        [SugarColumn(ColumnName = "prev_fire_time", IsNullable = true, Length = 20, ColumnDescription = "上次执行时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string PrevFireTime { get; set; }

        /// <summary>
        /// 下次执行时间
        /// </summary>
        [DisplayName("开始时间")]
        [SugarColumn(ColumnName = "next_fire_time", IsNullable = true, Length = 20, ColumnDescription = "下次执行时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string NextFireTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        [SugarColumn(ColumnName = "job_remark", IsNullable = true, Length = 300, ColumnDescription = "备注", DefaultValue = "", ColumnDataType = "varchar")]
        public string JobRemark { get; set; }
    }
}