using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using Quartz;

namespace IotWebApi.Services.Jobs
{
    /// <summary>
    /// 插件加载任务
    /// </summary>
    [DisallowConcurrentExecution] // 禁止并发执行
    public class SysPluginJob : BaseJob
    {
        private DateTime _lastResetDate = DateTime.MinValue; // 记录上次重置日期
        private readonly PluginService _pluginService;
        /// <summary>
        /// 构造函数-获取依赖注入
        /// </summary>
        /// <param name="pluginService"></param>
        public SysPluginJob(PluginService pluginService)
        {
            _pluginService = pluginService;
        }

        /// <summary>
        /// 插件加载任务+心跳检测+热更新
        /// </summary>
        protected override async Task<string> ExecuteJob(IJobExecutionContext context)
        {
            try
            {
                // 1. 插件加载或热更新 每天加载一次
                if (DateTime.Now.Date != _lastResetDate.Date)
                {
                    if (await _pluginService.LoadOrUpdatePluginsAsync())
                    {
                        _lastResetDate = DateTime.Now.Date; // 更新上次重置日期
                        LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "插件加载方法执行成功。", "插件加载");
                    }
                }

                // 2. 心跳检测
                await CheckPluginHeartbeatAsync();
                return $"插件加载/热更新/心跳检测任务执行成功";
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "插件心跳");
                throw; // 将异常抛出，由基类记录错误日志
            }
        }

        /// <summary>
        /// 插件心跳检测(B-1.5:超时标记异常,恢复置回正常;状态无变化不写库;
        /// PluginHeartTime由DataPointIngestService消费心跳消息回写)
        /// </summary>
        private async Task CheckPluginHeartbeatAsync()
        {
            var plugins = SysPluginDAO.Instance.GetList();
            var now = DateTime.Now;
            foreach (var plugin in plugins)
            {
                bool stale = (now - plugin.PluginHeartTime.ToZxxDateTime()).TotalMinutes > 5;
                int expected = stale ? 1 : 0;
                if (plugin.PluginHeartStatus == expected) continue;
                plugin.PluginHeartStatus = expected;
                plugin.ExpandObject = null;
                SysPluginDAO.Instance.UpdateColumns(plugin, it => new { it.PluginHeartStatus });
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    stale ? $"插件[{plugin.PluginName}]心跳超时" : $"插件[{plugin.PluginName}]心跳恢复", "插件心跳");
            }
            await Task.CompletedTask;
        }
    }
}