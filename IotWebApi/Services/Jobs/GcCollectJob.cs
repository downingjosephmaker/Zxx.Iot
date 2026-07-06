using CenBoCommon.Zxx;
using IotLog;
using Quartz;

namespace IotWebApi.Services.Jobs
{
    /// <summary>
    /// GC回收任务
    /// </summary>
    public class GcCollectJob : BaseJob
    {
        /// <summary>
        /// 执行GC回收任务
        /// </summary>
        protected override async Task<string> ExecuteJob(IJobExecutionContext context)
        {
            try
            {
                await Task.Delay(1000);

                // 记录GC前的内存使用情况
                long beforeMemory = GC.GetTotalMemory(false);
                string gcbeforeMemory = $"GC回收前内存使用: {beforeMemory / 1024 / 1024}MB";

                // 执行GC回收
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // 记录GC后的内存使用情况
                long afterMemory = GC.GetTotalMemory(false);
                long freedMemory = beforeMemory - afterMemory;

                string result = $"{gcbeforeMemory}；当前内存使用: {afterMemory / 1024 / 1024}MBGC回收完成释放内存: {freedMemory / 1024 / 1024}MB。";

                return result;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"GC回收任务执行失败: {ex}", "GC回收任务");
                throw;
            }
        }
    }
}