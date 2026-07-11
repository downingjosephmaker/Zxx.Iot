using CenBoCommon.Zxx;
using CenboEventBus;
using IotLog;
using IotModel;

namespace IotWebApi.Services
{
    /// <summary>
    /// 设备/点表变更后的插件配置热刷新通知器(C-4/D9:保存→PluginMessageEnum.配置更新→插件内部全量重启;
    /// 3秒滑动去抖合并连续保存——批量导入/连续编辑只触发一次重启,向全部已加载插件广播,
    /// 插件对与己无关的变更重启一次属可接受开销,增量调和的复杂度不划算见方案§6.2-4)
    /// </summary>
    public sealed class ConfigReloadNotifier : IDisposable
    {
        private const string CATEGORY = "配置热刷新";
        private const int DebounceMs = 3000;

        private readonly IEventBus<PluginCommandEvent> _commandBus;
        private readonly object _lock = new();
        private Timer _timer;
        private string _lastReason = "";

        public ConfigReloadNotifier(IEventBus<PluginCommandEvent> commandBus)
        {
            _commandBus = commandBus;
        }

        /// <summary>
        /// 登记一次配置变更(滑动去抖:静默3秒后广播一次,期间的连续变更合并)
        /// </summary>
        /// <param name="reason">变更来源(日志与插件侧载荷,如"设备修改"/"点表保存")</param>
        public void Notify(string reason)
        {
            lock (_lock)
            {
                _lastReason = reason;
                _timer ??= new Timer(_ => Broadcast(), null, Timeout.Infinite, Timeout.Infinite);
                _timer.Change(DebounceMs, Timeout.Infinite);
            }
        }

        private void Broadcast()
        {
            try
            {
                var guids = OperatorCommon.DicPlugins.Keys.ToList();
                if (!guids.IsZxxAny()) return;
                var message = new PluginMessage
                {
                    MessageType = PluginMessageEnum.配置更新,
                    MessageJson = new { Reason = _lastReason, Time = DateTime.Now.ToDateTimeString() }.ToJson()
                };
                foreach (var guid in guids)
                {
                    _commandBus.Publish(new PluginCommandEvent(guid, message));
                }
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"配置变更[{_lastReason}]已广播至{guids.Count}个插件。", CATEGORY);
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), CATEGORY);
            }
        }

        public void Dispose() => _timer?.Dispose();
    }
}
