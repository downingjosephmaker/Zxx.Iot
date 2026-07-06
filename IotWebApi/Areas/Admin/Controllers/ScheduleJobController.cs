using CenBoCommon.Zxx;
using IotLog;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Services;

namespace IotWebApi.Areas.Admin.Controllers
{
    /// <summary>
    /// 任务调度管理
    /// </summary>
    [ApiController]
    [ControllSort("1-29")]
    public class ScheduleJobController : ControllerBaseApi
    {
        private readonly QuartzService _quartzService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="quartzService"></param>
        public ScheduleJobController(QuartzService quartzService)
        {
            _quartzService = quartzService;
        }

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name="model">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public List<ScheduleJob> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = ScheduleJobDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 获取任务详情
        /// </summary>
        /// <param name="snowid">任务ID</param>
        /// <returns>任务详情</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public ScheduleJob GetInfoByPk(long snowid)
        {
            var job = ScheduleJobDAO.Instance.GetOneBy(t => t.SnowId == snowid);
            return job;
        }

        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="job">任务信息</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string Insert(ScheduleJob job)
        {
            Status = false;
            Message = "任务信息不能为空";

            if (job == null) return "";

            // 检查必填字段
            if (string.IsNullOrEmpty(job.JobName))
            {
                Message = "任务名称不能为空";
                return "";
            }

            if (string.IsNullOrEmpty(job.JobGroupName))
            {
                Message = "任务组名不能为空";
                return "";
            }

            if (string.IsNullOrEmpty(job.JobClassName))
            {
                Message = "任务类名不能为空";
                return "";
            }

            if (job.TriggerType == 0 && string.IsNullOrEmpty(job.JobCron))
            {
                Message = "Cron表达式不能为空";
                if (job == null) return Message;
            }

            if (job.TriggerType == 1 && job.IntervalSeconds <= 0)
            {
                Message = "执行间隔必须大于0";
                return "";
            }

            // 获取当前登录用户信息
            var optmdl = Request.GetToken();
            job.CreateId = optmdl.UserID;
            job.CreateTime = DateTime.Now.ToDateTimeString();
            job.CreateName = optmdl.UserName;
            job.UpdateId = optmdl.UserID;
            job.UpdateTime = DateTime.Now.ToDateTimeString();
            job.UpdateName = optmdl.UserName;
            job.ExecuteCount = 0;
            job.JobStatus = 0; // 默认停止
            job.SnowId = SnowModel.Instance.NewId();

            // 保存任务
            var result = ScheduleJobDAO.Instance.Insert(job);
            if (!result)
            {
                Message = "添加任务失败";
                return "";
            }

            Status = true;
            Message = "添加任务成功";
            return "";
        }

        /// <summary>
        /// 修改任务
        /// </summary>
        /// <param name="job">任务信息</param>
        /// <returns>修改结果</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string Update(ScheduleJob job)
        {
            Status = false;
            Message = "任务信息不能为空";

            // 获取原任务信息
            var oldjob = ScheduleJobDAO.Instance.GetOneBy(t => t.SnowId == job.SnowId);
            if (oldjob == null)
            {
                Message = "任务不存在";
                return Message;
            }

            // 获取当前登录用户信息
            var optmdl = Request.GetToken();
            job.UpdateId = optmdl.UserID;
            job.UpdateTime = DateTime.Now.ToDateTimeString();
            job.UpdateName = optmdl.UserName;

            // 更新任务
            var updateResult = ScheduleJobDAO.Instance.UpdateIgnoreColumns(job, it => new
            {
                it.CreateId,
                it.CreateName,
                it.CreateTime,
                it.PrevFireTime,
                it.NextFireTime,
                it.JobStatus
            });
            if (!updateResult)
            {
                Message = "更新任务失败";
                return Message;
            }

            Status = true;
            Message = "更新任务成功";
            return Message;
        }

        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="snowid">任务ID</param>
        /// <returns>删除结果</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public async Task<string> Delete(long snowid)
        {
            Status = false;
            Message = "任务不存在";
            var job = ScheduleJobDAO.Instance.GetOneBy(t => t.SnowId == snowid);
            if (job == null) return Message;

            // 先删除调度任务
            await _quartzService.DeleteJob(job.JobName, job.JobGroupName);

            // 再删除数据库记录
            var result = ScheduleJobDAO.Instance.DeleteBy(t => t.SnowId == snowid);
            if (!result)
            {
                Message = "删除任务失败";
                return Message;
            }

            Status = true;
            Message = "删除任务成功";
            return Message;
        }

        /// <summary>
        /// 任务状态变更(停止/运行)
        /// </summary>
        /// <param name="snowid">任务ID</param>
        /// <param name="actiontype">动作(0:停止1:运行)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public async Task<string> JobQiTingChange(long snowid, int actiontype)
        {
            Status = false;
            Message = "任务不存在";
            var job = ScheduleJobDAO.Instance.GetOneBy(t => t.SnowId == snowid);
            if (job == null) return "";
            // 校验 actiontype 取值范围（原 && 恒为 false 导致校验完全失效，非法值会跳过所有分支返回"成功"）
            if (actiontype < 0 || actiontype > 1)
            {
                Message = "任务动作参数传递不正确";
                return Message;
            }
            if (actiontype == 1 && job.JobStatus == 1)
            {
                Message = "任务已经启动";
                return Message;
            }
            if (actiontype == 0 && job.JobStatus == 0)
            {
                Message = "任务已经停止";
                return Message;
            }

            var optmdl = Request.GetToken();
            if (actiontype == 0)
            {
                // 停止任务
                var pauseResult = await _quartzService.DeleteJob(job.JobName, job.JobGroupName, optmdl);
                if (!pauseResult)
                {
                    Message = "任务停止失败";
                    return Message;
                }
            }
            else if (actiontype == 1)
            {
                // 运行任务
                var deleteResult = await _quartzService.ScheduleJob(job, optmdl);
                if (!deleteResult)
                {
                    Message = "任务启动失败";
                    return Message;
                }
            }

            Status = true;
            Message = "任务处理成功";
            return Message;
        }

        /// <summary>
        /// 任务状态变更(暂停/恢复)
        /// </summary>
        /// <param name="snowid">任务ID</param>
        /// <param name="actiontype">动作(2:暂停1:恢复)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public async Task<string> JobZanHuiChange(long snowid, int actiontype)
        {
            Status = false;
            Message = "任务不存在";
            var job = ScheduleJobDAO.Instance.GetOneBy(t => t.SnowId == snowid);
            if (job == null) return "";
            // 校验 actiontype 取值范围（原 && 恒为 false 导致校验完全失效，非法值会跳过所有分支返回"成功"）
            if (actiontype < 1 || actiontype > 2)
            {
                Message = "任务动作参数传递不正确";
                return Message;
            }
            if (job.JobStatus == 0)
            {
                Message = "任务未启动";
                return Message;
            }
            if (actiontype == 2 && job.JobStatus == 2)
            {
                Message = "任务已经暂停";
                return Message;
            }
            if (actiontype == 1 && job.JobStatus == 1)
            {
                Message = "任务已经启动";
                return Message;
            }

            var optmdl = Request.GetToken();
            if (actiontype == 2)
            {
                // 暂停任务
                var pauseResult = await _quartzService.PauseJob(job.JobName, job.JobGroupName, optmdl);
                if (!pauseResult)
                {
                    Message = "任务停止失败";
                    return Message;
                }
            }
            else if (actiontype == 1)
            {
                // 运行任务
                var deleteResult = await _quartzService.ResumeJob(job.JobName, job.JobGroupName);
                if (!deleteResult)
                {
                    Message = "任务恢复失败";
                    return Message;
                }
            }

            Status = true;
            Message = "任务处理成功";
            return Message;
        }

        /// <summary>
        /// 立即执行任务
        /// </summary>
        /// <param name="snowid">任务ID</param>
        /// <returns>执行结果</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public async Task<string> JobExecute(long snowid)
        {
            Status = false;
            Message = "任务不存在";
            var job = ScheduleJobDAO.Instance.GetOneBy(t => t.SnowId == snowid);
            if (job == null) return "";

            // 获取当前登录用户信息（原调用未传 model，导致审计字段写死为"开发管理员"）
            var optmdl = Request.GetToken();

            // 立即执行任务
            var triggerResult = await _quartzService.TriggerJob(job.JobName, job.JobGroupName, optmdl);
            if (!triggerResult)
            {
                Message = "执行任务失败";
                return "";
            }

            Status = true;
            Message = "已触发任务执行";
            return "";
        }

        /// <summary>
        /// 获取Cron表达式示例
        /// </summary>
        /// <returns>Cron表达式示例</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string GetCronExamples()
        {
            try
            {
                var examples = new List<object>
                {
                    new { description = "每5秒执行一次", cron = CronHelper.CreateEveryNSecondsExpression(5) },
                    new { description = "每6分钟执行一次", cron = CronHelper.CreateEveryNMinutesExpression(6) },
                    new { description = "每1小时执行一次", cron = CronHelper.CreateEveryNHoursExpression(1) },
                    new { description = "每天8点30分执行一次", cron = CronHelper.CreateDailyExpression(8, 30) },
                    new { description = "每周一8点执行一次", cron = CronHelper.CreateWeeklyExpression(2, 8, 0) },
                    new { description = "每月1日0点执行一次", cron = CronHelper.CreateMonthlyExpression(1, 0, 0) }
                };

                Status = true;
                Message = "获取Cron表达式示例成功";
                return examples.ToJson();
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "错误");
                Status = false;
                Message = "获取Cron表达式示例失败：" + ex.Message;
                return "";
            }
        }

    }

}