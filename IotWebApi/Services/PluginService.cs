using CenBoCommon.Zxx;
using CenboEventBus;
using IotLog;
using IotModel;

namespace IotWebApi.Services
{
    /// <summary>
    /// 插件服务
    /// </summary>
    public class PluginService
    {
        private const string Service_CATEGORY = "插件服务";
        private readonly IEventBus<PluginEvent> _eventBus;
        public PluginService(IEventBus<PluginEvent> eventBus)
        {
            _eventBus = eventBus;
        }

        /// <summary>
        /// 插件加载或热更新（幂等，支持首次加载和热更新）
        /// </summary>
        public async Task<bool> LoadOrUpdatePluginsAsync()
        {
            bool result = false;
            try
            {
                var pluginList = SysPluginDAO.Instance.GetListBy(t => t.PluginStatus == 1);
                foreach (var item in pluginList)
                {
                    var PluginPath = Path.Combine(OperatorCommon.NetLocalfile, item.PluginPath);
                    if (!File.Exists(PluginPath))
                    {
                        LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件{item.PluginName}不存在，路径:{PluginPath}", Service_CATEGORY);
                        continue; // 如果文件不存在则跳过加载
                    }
                    // 1. 卸载旧的插件上下文和实例
                    if (OperatorCommon.PluginLoadContexts.TryGetValue(item.PluginGuid, out var oldContext))
                    {
                        oldContext.Unload();
                        OperatorCommon.PluginLoadContexts.Remove(item.PluginGuid);
                    }
                    if (OperatorCommon.DicPlugins.TryGetValue(item.PluginGuid, out var oldPlugin))
                    {
                        await oldPlugin.PluginStop();
                        OperatorCommon.DicPlugins.Remove(item.PluginGuid);
                    }

                    // 2. 用PluginLoadContext加载插件DLL
                    PluginLoadContext loadContext = new PluginLoadContext(PluginPath);
                    var assembly = loadContext.LoadFromAssemblyPath(PluginPath);
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => typeof(ICenBoPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
                    foreach (var pluginType in pluginTypes)
                    {
                        LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件加载成功", Service_CATEGORY);
                        var plugin = (ICenBoPlugin)Activator.CreateInstance(pluginType);
                        if (plugin.PluginGuid != item.PluginGuid) continue;
                        try
                        {
                            plugin.PluginInit(_eventBus);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
                            continue; // 注入失败跳过该插件
                        }
                        var key = plugin.PluginGuid;

                        // 3. 注册新上下文和实例
                        OperatorCommon.PluginLoadContexts[key] = loadContext;
                        OperatorCommon.DicPlugins[key] = plugin;
                        result = true;
                        try
                        {
                            if (item.PluginStatus == 1)
                            {
                                if (await plugin.PluginStart(item.PluginConfig))
                                {
                                    item.UpdateId = 1;
                                    item.UpdateName = "开发管理员";
                                    item.UpdateTime = DateTime.Now.ToDateTimeString();
                                    SysPluginDAO.Instance.UpdateColumns(item, it => new { it.UpdateId, it.UpdateName, it.UpdateTime });
                                    LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件{item.PluginName}加载且启动成功", Service_CATEGORY);
                                }
                                else
                                {
                                    LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件{item.PluginName}加载但启动失败", Service_CATEGORY);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
            }
            return result;
        }
    }

}