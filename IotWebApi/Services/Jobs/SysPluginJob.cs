using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using Newtonsoft.Json.Linq;
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
        /// 插件心跳检测(B-1.5+补审修复:仅启用插件参与超时打标,停用插件复位不误标;
        /// 阈值自适应max(5分钟,2×HeartSecond);写前按行重读避免旧快照覆写消费线程刚回写的恢复;
        /// 状态无变化不写库;PluginHeartTime由DataPointIngestService消费心跳消息回写)
        /// </summary>
        private async Task CheckPluginHeartbeatAsync()
        {
            var snapshots = SysPluginDAO.Instance.GetList();
            foreach (var snapshot in snapshots)
            {
                // 写前重读当前行:循环期间消费线程可能刚回写心跳恢复,不能拿循环前的快照做判定
                var plugin = SysPluginDAO.Instance.GetOneBy(t => t.PluginGuid == snapshot.PluginGuid);
                if (plugin == null) continue;
                int expected = ComputeExpectedHeartStatus(plugin, DateTime.Now);
                if (plugin.PluginHeartStatus == expected) continue;
                bool wasdisabled = plugin.PluginStatus != 1;
                plugin.PluginHeartStatus = expected;
                plugin.ExpandObject = null;
                SysPluginDAO.Instance.UpdateColumns(plugin, it => new { it.PluginHeartStatus });
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    expected == 1 ? $"插件[{plugin.PluginName}]心跳超时"
                    : wasdisabled ? $"插件[{plugin.PluginName}]已停用,心跳异常标记复位"
                    : $"插件[{plugin.PluginName}]心跳恢复", "插件心跳");
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// 期望心跳状态:停用插件恒正常(新上传默认停用无心跳属正常态,不再误标"心跳异常");
        /// 启用插件按自适应阈值判定——至少5分钟,取2倍HeartSecond,长心跳周期插件不再被周期性误标翻转
        /// </summary>
        private static int ComputeExpectedHeartStatus(SysPluginEntity plugin, DateTime now)
        {
            if (plugin.PluginStatus != 1) return 0;
            int heartsecond = 0;
            if (!plugin.PluginConfig.IsZxxNullOrEmpty())
            {
                try { heartsecond = JObject.Parse(plugin.PluginConfig)["HeartSecond"]?.Value<int>() ?? 0; }
                catch { /* 配置非法时按默认阈值 */ }
            }
            int staleseconds = Math.Max(300, heartsecond * 2);
            bool stale = (now - plugin.PluginHeartTime.ToZxxDateTime()).TotalSeconds > staleseconds;
            return stale ? 1 : 0;
        }
    }
}