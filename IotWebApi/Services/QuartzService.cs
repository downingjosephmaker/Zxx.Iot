using CenBoCommon.Zxx;
using IotLog;
using Quartz;
using Quartz.Impl.Matchers;
using System.Reflection;
using IotModel;

namespace IotWebApi.Services
{
    /// <summary>
    /// Quartz任务调度服务
    /// </summary>
    public class QuartzService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private IScheduler _scheduler;
        private bool _isInitialized = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        public QuartzService(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }

        /// <summary>
        /// 初始化调度器
        /// </summary>
        private async Task InitScheduler()
        {
            if (!_isInitialized)
            {
                try
                {
                    _scheduler = await _schedulerFactory.GetScheduler();

                    // 如果调度器未启动，则启动
                    if (!_scheduler.IsStarted)
                    {
                        await _scheduler.Start();
                        LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                            "Quartz调度器启动成功", "任务调度");
                    }

                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"Quartz调度器初始化失败: {ex}", "任务调度");
                    throw;
                }
            }
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    "Quartz任务调度服务启动中...", "任务调度");

                // 确保调度器已初始化
                await InitScheduler();

                // 延迟5秒后加载任务，确保系统其他服务已启动
                await Task.Delay(5000, cancellationToken);

                // 获取需要启动的任务
                List<int> limits = new List<int>() { 0 };
                var ismain = AppSetting.GetConfig("DataSync:IsMain").ToLower();
                if (ismain == "true")
                {
                    limits.Add(1);
                }
                else
                {
                    limits.Add(2);
                }

                var jobs = ScheduleJobDAO.Instance.GetListBy(t => limits.Contains(t.JobLimit));
                if (jobs.IsZxxAny())
                {
                    foreach (var job in jobs)
                    {
                        if (job.JobStatus == 1) // 只启动状态为运行中的任务
                        {
                            await ScheduleJob(job);
                        }
                    }
                }

                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"Quartz任务调度服务启动成功，共加载 {jobs.Count} 个任务", "任务调度");
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"Quartz任务调度服务启动失败: {ex}", "任务调度");
                throw;
            }
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_scheduler != null)
            {
                try
                {
                    await _scheduler.Shutdown(true, cancellationToken);
                    LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        "Quartz任务调度服务已停止", "任务调度");
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"Quartz任务调度服务停止失败: {ex}", "任务调度");
                }
            }
        }

        /// <summary>
        /// 调度任务
        /// </summary>
        /// <param name="job">任务信息</param>
        /// <param name="model">token</param>
        /// <returns>是否成功</returns>
        public async Task<bool> ScheduleJob(ScheduleJob job, OperatorModel model = null)
        {
            try
            {
                // 确保scheduler已初始化
                if (_scheduler == null)
                {
                    await InitScheduler();
                }

                var jk = new JobKey(job.JobName, job.JobGroupName);
                // 检查任务是否已存在
                var existingJob = await _scheduler.GetJobDetail(jk);
                if (existingJob != null)
                {
                    // 如果任务已存在，先删除
                    await _scheduler.DeleteJob(jk);
                    LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"任务 {job.JobName} 已存在，先删除后重新创建", "任务调度");
                }

                // 创建作业信息
                Type jobType = GetJobType(job.JobClassName);
                if (jobType == null)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"未找到任务类 {job.JobClassName}", "任务调度");
                    return false;
                }

                // 创建作业明细
                IJobDetail jobDetail = JobBuilder.Create(jobType)
                    .WithIdentity(job.JobName, job.JobGroupName)
                    .WithDescription(job.JobDescription)
                    .UsingJobData("JobSnowId", job.SnowId)
                    .Build();

                // 创建触发器
                ITrigger trigger;
                if (job.TriggerType == 0) // Cron触发器
                {
                    trigger = TriggerBuilder.Create()
                        .WithIdentity($"{job.JobName}_Trigger", $"{job.JobGroupName}_Trigger")
                        .WithCronSchedule(job.JobCron)
                        .WithDescription(job.JobDescription)
                        .Build();
                }
                else // 简单触发器
                {
                    trigger = TriggerBuilder.Create()
                        .WithIdentity($"{job.JobName}_Trigger", $"{job.JobGroupName}_Trigger")
                        .WithSimpleSchedule(x => x
                            .WithIntervalInSeconds(job.IntervalSeconds)
                            .RepeatForever())
                        .WithDescription(job.JobDescription)
                        .Build();
                }

                // 调度作业
                await _scheduler.ScheduleJob(jobDetail, trigger);

                // 不再启动即触发：立即触发会与 [DisallowConcurrentExecution] 叠加，
                // 当上次实例未结束时，触发被 Quartz 静默丢弃（"有日志没执行"的根因之一）。
                // 服务重启 StartAsync 也会批量调用本方法，立即触发会导致瞬间所有任务同时跑。
                // 让 Cron 按自身节奏调度即可。

                // 更新任务状态为运行中(1)
                job.JobStatus = 1;
                job.UpdateId = 1;
                job.UpdateName = "开发管理员";
                job.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                if (model != null)
                {
                    job.UpdateId = model.UserID;
                    job.UpdateName = model.UserName;
                }
                ScheduleJobDAO.Instance.UpdateColumns(job, it => new { it.JobStatus, it.UpdateId, it.UpdateTime, it.UpdateName });

                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务 {job.JobName} 启动成功", "任务调度");

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务 {job.JobName} 启动失败: {ex}", "任务调度");
                return false;
            }
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="JobGroupName">任务组名</param>
        /// <param name="model">token</param>
        /// <returns>是否成功</returns>
        public async Task<bool> PauseJob(string jobName, string JobGroupName, OperatorModel model = null)
        {
            try
            {
                // 确保scheduler已初始化
                if (_scheduler == null)
                {
                    await InitScheduler();
                }

                // 暂停作业
                await _scheduler.PauseJob(new JobKey(jobName, JobGroupName));

                // 更新任务状态为暂停(2)
                var job = ScheduleJobDAO.Instance.GetOneBy(t => t.JobName == jobName && t.JobGroupName == JobGroupName);
                if (job != null)
                {
                    job.JobStatus = 2;
                    job.UpdateId = 1;
                    job.UpdateName = "开发管理员";
                    job.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    if (model != null)
                    {
                        job.UpdateId = model.UserID;
                        job.UpdateName = model.UserName;
                    }
                    ScheduleJobDAO.Instance.UpdateColumns(job, it => new { it.JobStatus, it.UpdateId, it.UpdateTime, it.UpdateName });
                }

                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务 {jobName} 已暂停", "任务调度");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务 {jobName} 暂停失败: {ex}", "任务调度");
                return false;
            }
        }

        /// <summary>
        /// 恢复任务
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="JobGroupName">任务组名</param>
        /// <param name="model">token</param>
        /// <returns>是否成功</returns>
        public async Task<bool> ResumeJob(string jobName, string JobGroupName, OperatorModel model = null)
        {
            try
            {
                // 确保scheduler已初始化
                if (_scheduler == null)
                {
                    await InitScheduler();
                }

                var jk = new JobKey(jobName, JobGroupName);
                if (!await _scheduler.CheckExists(jk))
                {
                    // scheduler 中不存在该作业：通常是之前通过"停止"(DeleteJob)删除过，
                    // 再用"恢复"(ResumeJob) 对已删除的作业无效，会导致 DB 显示运行中但实际不调度。
                    // 兜底：从 DB 查出任务信息，走 ScheduleJob 重建调度。
                    LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"任务 {jobName} 恢复时发现 scheduler 中不存在，自动重建调度", "任务调度");
                    var jobInfo = ScheduleJobDAO.Instance.GetOneBy(t => t.JobName == jobName && t.JobGroupName == JobGroupName);
                    if (jobInfo == null)
                    {
                        LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                            $"任务 {jobName} 恢复失败：数据库中未找到该任务记录", "任务调度");
                        return false;
                    }
                    return await ScheduleJob(jobInfo, model);
                }

                // 恢复作业
                await _scheduler.ResumeJob(jk);

                // 更新任务状态为运行中(1)
                var job = ScheduleJobDAO.Instance.GetOneBy(t => t.JobName == jobName && t.JobGroupName == JobGroupName);
                if (job != null)
                {
                    job.JobStatus = 1;
                    job.UpdateId = 1;
                    job.UpdateName = "开发管理员";
                    job.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    if (model != null)
                    {
                        job.UpdateId = model.UserID;
                        job.UpdateName = model.UserName;
                    }
                    ScheduleJobDAO.Instance.UpdateColumns(job, it => new { it.JobStatus, it.UpdateId, it.UpdateTime, it.UpdateName });
                }

                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务 {jobName} 已恢复", "任务调度");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务 {jobName} 恢复失败: {ex}", "任务调度");
                return false;
            }
        }

        /// <summary>
        /// 停止任务
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="JobGroupName">任务组名</param>
        /// <param name="model">token</param>
        /// <returns>是否成功</returns>
        public async Task<bool> DeleteJob(string jobName, string JobGroupName, OperatorModel model = null)
        {
            try
            {
                // 确保scheduler已初始化
                if (_scheduler == null)
                {
                    await InitScheduler();
                }

                // 停止作业
                await _scheduler.DeleteJob(new JobKey(jobName, JobGroupName));

                var job = ScheduleJobDAO.Instance.GetOneBy(t => t.JobName == jobName && t.JobGroupName == JobGroupName);
                if (job != null)
                {
                    job.JobStatus = 0;
                    job.UpdateId = 1;
                    job.UpdateName = "开发管理员";
                    job.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    if (model != null)
                    {
                        job.UpdateId = model.UserID;
                        job.UpdateName = model.UserName;
                    }
                    ScheduleJobDAO.Instance.UpdateColumns(job, it => new { it.JobStatus, it.UpdateId, it.UpdateTime, it.UpdateName });
                }

                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务 {jobName} 已删除", "任务调度");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务 {jobName} 删除失败: {ex}", "任务调度");
                return false;
            }
        }

        /// <summary>
        /// 立即执行任务
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="JobGroupName">任务组名</param>
        /// <param name="model">token</param>
        /// <returns>是否成功</returns>
        public async Task<bool> TriggerJob(string jobName, string JobGroupName, OperatorModel model = null)
        {
            try
            {
                // 确保scheduler已初始化
                if (_scheduler == null)
                {
                    await InitScheduler();
                }

                // 检查任务状态
                var job = ScheduleJobDAO.Instance.GetOneBy(t => t.JobName == jobName && t.JobGroupName == JobGroupName);
                if (job == null || job.JobStatus == 0)
                {
                    LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"任务 {jobName} 状态不允许立即执行", "任务调度");
                    return false;
                }
                var jk = new JobKey(jobName, JobGroupName);
                if (await _scheduler.CheckExists(jk))
                {
                    // 触发作业
                    await _scheduler.TriggerJob(jk);

                    if (job != null)
                    {
                        job.JobStatus = 1;
                        job.UpdateId = 1;
                        job.UpdateName = "开发管理员";
                        job.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        if (model != null)
                        {
                            job.UpdateId = model.UserID;
                            job.UpdateName = model.UserName;
                        }
                        ScheduleJobDAO.Instance.UpdateColumns(job, it => new { it.JobStatus, it.UpdateId, it.UpdateTime, it.UpdateName });
                    }
                    LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"任务 {jobName} 已触发立即执行", "任务调度");
                    return true;
                }
                // scheduler 内存中不存在该 job（可能被 DeleteJob 删除或启动加载失败），
                // 但数据库 JobStatus 可能为 1（运行中），导致"显示运行中但不执行"。
                // 补日志便于排查 DB 与 scheduler 状态不一致。
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务 {jobName} 触发失败：scheduler 中不存在该作业（DB状态与调度器不一致，请重新启动任务）", "任务调度");
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务 {jobName} 触发失败: {ex}", "任务调度");
                return false;
            }
        }

        /// <summary>
        /// 获取作业类型
        /// </summary>
        /// <param name="jobClassName">作业类名</param>
        /// <returns>作业类型</returns>
        private Type GetJobType(string jobClassName)
        {
            try
            {
                // 从当前程序集中查找类型
                var assembly = Assembly.GetExecutingAssembly();
                Type jobType = assembly.GetTypes().Find(t => t.Name == jobClassName);
                //Type jobType = assembly.GetType(jobClassName);

                if (jobType != null && typeof(IJob).IsAssignableFrom(jobType))
                {
                    return jobType;
                }

                // 尝试从所有已加载的程序集中查找
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    jobType = asm.GetType(jobClassName);
                    if (jobType != null && typeof(IJob).IsAssignableFrom(jobType))
                    {
                        return jobType;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "任务调度");
                return null;
            }
        }

        /// <summary>
        /// 获取作业状态
        /// </summary>
        public async Task<TriggerState> GetTriggerState(string triggerName, string TriggerGroupName, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _scheduler.GetTriggerState(new TriggerKey(triggerName, TriggerGroupName), cancellationToken);
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"获取触发器[{triggerName}]状态失败: {ex}", "任务调度");
                return TriggerState.None;
            }
        }

        /// <summary>
        /// 获取所有作业
        /// </summary>
        public async Task<List<JobKey>> GetAllJobs(CancellationToken cancellationToken = default)
        {
            List<JobKey> result = new List<JobKey>();
            try
            {
                var JobGroupNames = await _scheduler.GetJobGroupNames(cancellationToken);
                foreach (var group in JobGroupNames)
                {
                    var groupMatcher = GroupMatcher<JobKey>.GroupEquals(group);
                    var jobKeys = await _scheduler.GetJobKeys(groupMatcher, cancellationToken);
                    result.AddRange(jobKeys);
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"获取所有作业失败: {ex}", "任务调度");
            }
            return result;
        }

        /// <summary>
        /// 获取Scheduler实例
        /// </summary>
        public IScheduler GetScheduler()
        {
            return _scheduler;
        }

    }
}