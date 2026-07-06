using CenBoCommon.Zxx;
using IotLog;
using System.Reflection;
using IotModel;

namespace IotWebApi.Services.Jobs
{
    /// <summary>
    /// 任务调度初始化器
    /// </summary>
    public static class JobInitializer
    {
        /// <summary>
        /// 初始化所有Job，确保数据库中有对应的调度任务
        /// </summary>
        public static void InitializeJobs(QuartzService quartzService)
        {
            try
            {
                // 系统内置的任务及其Cron表达式
                List<ScheduleInfo> jobConfigs = new List<ScheduleInfo>();
                jobConfigs.Add(new ScheduleInfo() { JobType = typeof(BackupDBJob), JobLimit = 1, JobStatus = 1, JobLog = 0, JobCron = "0 0 2 */2 * ?" });// 每隔2天2点执行一次
                jobConfigs.Add(new ScheduleInfo() { JobType = typeof(GcCollectJob), JobLimit = 0, JobStatus = 1, JobLog = 0, JobCron = "0 0 0/2 * * ?" });// 每2小时执行一次
                jobConfigs.Add(new ScheduleInfo() { JobType = typeof(MqttClientJob), JobLimit = 0, JobStatus = 1, JobLog = 0, JobCron = "0 0/5 * * * ?" });// 每5分钟执行一次
                jobConfigs.Add(new ScheduleInfo() { JobType = typeof(MqttServerJob), JobLimit = 1, JobStatus = 0, JobLog = 0, JobCron = "0 0/5 * * * ?" });// 每5分钟执行一次
                jobConfigs.Add(new ScheduleInfo() { JobType = typeof(SysPluginJob), JobLimit = 1, JobStatus = 1, JobLog = 0, JobCron = "0 0/20 * * * ?" });// 每20分钟执行一次

                var joblist = ScheduleJobDAO.Instance.GetList();
                // 检查数据库中的调度任务
                foreach (var job in jobConfigs)
                {
                    string jobClassName = job.JobType.Name;
                    string jobGroup = "System";
                    ScheduleJob existingJob = null;
                    // 检查任务是否存在
                    if (joblist.IsZxxAny()) existingJob = joblist.Find(j => j.JobClassName == jobClassName && j.JobGroupName == jobGroup);
                    if (existingJob == null)
                    {
                        var scheduleJob = new ScheduleJob
                        {
                            SnowId = SnowModel.Instance.NewId(),
                            JobName = GetJobDescription(job.JobType),
                            JobGroupName = jobGroup,
                            JobClassName = jobClassName,
                            JobDescription = GetJobDescription(job.JobType),
                            TriggerType = 0, // Cron触发器
                            JobCron = job.JobCron,
                            IntervalSeconds = 0,
                            JobLimit = job.JobLimit,
                            JobStatus = job.JobStatus,
                            JobLog = job.JobLog,
                            ExecuteCount = 0,
                            CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            CreateId = 1,
                            UpdateId = 1,
                            CreateName = "开发管理员",
                            UpdateName = "开发管理员"
                        };

                        // 保存到数据库
                        var result = ScheduleJobDAO.Instance.Insert(scheduleJob);
                        if (result)
                        {
                            LogHelper.SysLogWrite("JobInitializer", "InitializeJobs", $"成功注册任务: {scheduleJob.JobName}", "任务调度");

                            // 调度任务
                            quartzService.ScheduleJob(scheduleJob).Wait();
                            // 触发作业
                            quartzService.TriggerJob(scheduleJob.JobName, scheduleJob.JobGroupName).Wait();
                        }
                        else
                        {
                            LogHelper.ErrorLogWrite("JobInitializer", "InitializeJobs", $"注册任务失败: {scheduleJob.JobName}", "任务调度");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("JobInitializer", "InitializeJobs", $"初始化调度任务失败: {ex}", "任务调度");
            }
        }

        /// <summary>
        /// 获取任务的描述信息
        /// </summary>
        private static string GetJobDescription(Type jobType)
        {
            // 尝试从类的Summary注释中获取描述
            var descriptionAttribute = jobType.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            if (descriptionAttribute != null)
            {
                return descriptionAttribute.Description;
            }

            // 根据任务类型返回默认描述
            if (jobType == typeof(MqttServerJob))
                return "MQTT服务端状态检查任务";
            else if (jobType == typeof(MqttClientJob))
                return "MQTT客户端状态检查任务";
            else if (jobType == typeof(BackupDBJob))
                return "数据库备份任务";
            else if (jobType == typeof(GcCollectJob))
                return "内存GC回收任务";
            else if (jobType == typeof(SysPluginJob))
                return "插件加载任务";
            else
                return "系统任务";
        }
    }

    public class ScheduleInfo
    {
        public Type JobType { get; set; }
        /// <summary>
        /// Cron表达式
        /// </summary>
        public string JobCron { get; set; }
        /// <summary>
        /// 作业限制(0:全部，1:主程序，2:副程序)
        /// </summary>
        public int JobLimit { get; set; }
        /// <summary>
        /// 作业状态(0:停止，1:运行，2:暂停)
        /// </summary>
        public int JobStatus { get; set; }
        /// <summary>
        /// 是否记录日志(0:是，1:否)
        /// </summary>
        public int JobLog { get; set; }
    }
}