using CenBoCommon.Zxx;
using IotLog;
using Quartz;
using System.Diagnostics;
using IotModel;

namespace IotWebApi.Services.Jobs
{
    /// <summary>
    /// 任务基类
    /// </summary>
    public abstract class BaseJob : IJob
    {
        /// <summary>
        /// 任务执行
        /// </summary>
        public async Task Execute(IJobExecutionContext context)
        {
            var JobSnowId = context.JobDetail.JobDataMap.GetLong("JobSnowId");
            // 判断任务ID是否存在
            if (JobSnowId == 0)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务ID不存在或无效", "Quartz作业");
                return;
            }

            // 获取任务信息
            var jobInfo = ScheduleJobDAO.Instance.GetOneBy(t => t.SnowId == JobSnowId);
            if (jobInfo == null)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"找不到任务信息，ID: {JobSnowId}", "Quartz作业");
                return;
            }

            // 创建任务日志
            ScheduleJobLog jobLog = new ScheduleJobLog
            {
                SnowId = SnowModel.Instance.NewId(),
                JobSnowId = JobSnowId,
                JobName = jobInfo.JobName,
                JobGroupName = jobInfo.JobGroupName,
                TriggerName = $"{jobInfo.JobName}_Trigger",
                TriggerGroupName = $"{jobInfo.JobGroupName}_Trigger",
                StartTime = DateTime.Now.ToDateTimeString(),
                ExecuteStatus = 0
            };

            // 任务计时
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务[{jobInfo.JobName}]开始执行", "Quartz作业");

                // 执行任务
                string result = await ExecuteJob(context);

                // 停止计时
                stopwatch.Stop();

                // 设置任务执行结果
                jobLog.EndTime = DateTime.Now.ToDateTimeString();
                jobLog.ExecuteTime = stopwatch.ElapsedMilliseconds;
                jobLog.ExecuteStatus = 1; // 执行成功
                // 截断执行结果，避免超过数据库字段长度限制（ExecuteResult varchar(500)）
                jobLog.ExecuteResult = result != null && result.Length > 500 ? result.Substring(0, 500) : result;

                // 更新任务信息
                jobInfo.PrevFireTime = DateTime.Now.ToDateTimeString();
                jobInfo.ExecuteCount++;

                // 更新下次执行时间
                DateTimeOffset? nextFireTime = context.Trigger.GetNextFireTimeUtc();
                if (nextFireTime.HasValue)
                {
                    jobInfo.NextFireTime = nextFireTime.Value.LocalDateTime.ToDateTimeString();
                }
                //else
                //{
                //    // 如果没有下次执行时间，说明任务已经执行完毕
                //    jobInfo.JobStatus = 0; // 停止任务
                //}

                // 更新任务信息
                ScheduleJobDAO.Instance.UpdateColumns(jobInfo, it => new
                {
                    it.PrevFireTime,
                    it.ExecuteCount,
                    it.NextFireTime,
                    it.JobStatus
                });

                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务[{jobInfo.JobName}]执行完成，耗时: {stopwatch.ElapsedMilliseconds}毫秒", "Quartz作业");
            }
            catch (Exception ex)
            {
                // 停止计时
                stopwatch.Stop();

                // 设置任务执行结果
                jobLog.EndTime = DateTime.Now.ToDateTimeString();
                jobLog.ExecuteTime = stopwatch.ElapsedMilliseconds;
                jobLog.ExecuteStatus = 0; // 执行失败
                // 截断异常信息，避免超过数据库字段长度限制（ErrorMsg varchar(500)）
                string errStr = ex.ToString();
                jobLog.ErrorMsg = errStr.Length > 500 ? errStr.Substring(0, 500) : errStr;

                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"任务[{jobInfo.JobName}]执行失败: {ex}", "Quartz作业");
            }
            finally
            {
                // 保存任务日志
                if (jobInfo.JobLog == 0) ScheduleJobLogDAO.Instance.Insert(jobLog);
            }
        }

        /// <summary>
        /// 执行具体任务
        /// </summary>
        /// <param name="context">任务执行上下文</param>
        /// <returns>执行结果</returns>
        protected abstract Task<string> ExecuteJob(IJobExecutionContext context);
    }
}