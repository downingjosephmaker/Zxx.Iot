using CenBoCommon.Zxx;
using CenboEventBus;
using IotLog;
using IotModel;
using Newtonsoft.Json.Linq;

namespace IotWebApi.Services
{
    /// <summary>
    /// 插件服务(B-1.6:上传/启停/删除/配置保存/每日任务多入口经同一闸门串行装卸;
    /// 单插件粒度ReloadOne/UnloadOne支撑"保存即生效";
    /// 控制命令白名单从已加载插件Manifest聚合(B-1.4单源化),装卸时刷新)
    /// </summary>
    public class PluginService
    {
        private const string Service_CATEGORY = "插件服务";

        /// <summary>
        /// 装卸闸门(全局唯一:控制器即时装卸与SysPluginJob每日窗口互斥)
        /// </summary>
        private static readonly SemaphoreSlim _gate = new(1, 1);

        /// <summary>
        /// 已加载插件声明的控制命令聚合缓存(装卸时整体替换,读方无锁)
        /// </summary>
        private static volatile List<PluginCommandInfo> _supportedCommands = new();

        private readonly IEventBus<PluginEvent> _eventBus;
        public PluginService(IEventBus<PluginEvent> eventBus)
        {
            _eventBus = eventBus;
        }

        /// <summary>
        /// 插件声明的控制命令(ClassName即控制白名单键)
        /// </summary>
        public sealed class PluginCommandInfo
        {
            /// <summary>控制类名</summary>
            public string ClassName { get; set; } = "";
            /// <summary>命令说明</summary>
            public string Description { get; set; } = "";
            /// <summary>所属插件Guid</summary>
            public string PluginGuid { get; set; } = "";
            /// <summary>所属插件名称</summary>
            public string PluginName { get; set; } = "";
        }

        /// <summary>
        /// 全部已加载插件声明的控制命令(SysPlugin/GetSupportedCommands与前端下拉共用)
        /// </summary>
        public static List<PluginCommandInfo> GetSupportedCommands() => _supportedCommands;

        /// <summary>
        /// 控制类名是否在已加载插件声明的白名单内
        /// (B-1.4:替代DeviceControlController/RuleLinkageService两份硬编码HashSet)
        /// </summary>
        public static bool IsCommandAllowed(string classname)
        {
            if (classname.IsZxxNullOrEmpty()) return false;
            var commands = _supportedCommands;
            return commands.Any(t => string.Equals(t.ClassName, classname.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 插件加载或热更新(幂等:全量遍历启用插件逐个重载,SysPluginJob每日窗口调用)
        /// </summary>
        public async Task<bool> LoadOrUpdatePluginsAsync()
        {
            bool result = false;
            await _gate.WaitAsync();
            try
            {
                var pluginList = SysPluginDAO.Instance.GetListBy(t => t.PluginStatus == 1);
                foreach (var item in pluginList)
                {
                    if (await ReloadOneCoreAsync(item)) result = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
            }
            finally
            {
                _gate.Release();
            }
            return result;
        }

        /// <summary>
        /// 单插件重载(上传/启用/配置保存即时生效入口;库中不存在或已停用时等价卸载)
        /// </summary>
        public async Task<bool> ReloadOneAsync(string guid)
        {
            await _gate.WaitAsync();
            try
            {
                var item = SysPluginDAO.Instance.GetOneBy(t => t.PluginGuid == guid);
                if (item == null || item.PluginStatus != 1)
                {
                    await UnloadOneCoreAsync(guid);
                    return true;
                }
                return await ReloadOneCoreAsync(item);
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
                return false;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// 单插件卸载(停用/删除即时生效入口,D8/D11修复:先Stop运行实例再Unload上下文)
        /// </summary>
        public async Task UnloadOneAsync(string guid)
        {
            await _gate.WaitAsync();
            try
            {
                await UnloadOneCoreAsync(guid);
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// 重载核心(闸门内调用):卸旧→独立ALC加载→登记→回写Manifest与配置回填→启动
        /// </summary>
        private async Task<bool> ReloadOneCoreAsync(SysPluginEntity item)
        {
            var pluginPath = Path.Combine(OperatorCommon.NetLocalfile, item.PluginPath);
            if (!File.Exists(pluginPath))
            {
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件{item.PluginName}不存在，路径:{pluginPath}", Service_CATEGORY);
                return false;
            }

            // 1. 卸载旧实例与上下文
            await UnloadOneCoreAsync(item.PluginGuid);

            // 2. 独立ALC加载并实例化(仅登记Guid匹配的插件类型)
            PluginLoadContext loadContext = new PluginLoadContext(pluginPath);
            try
            {
                var assembly = loadContext.LoadFromAssemblyPath(pluginPath);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(ICenBoPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
                foreach (var pluginType in pluginTypes)
                {
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

                    // 3. 登记新上下文和实例,刷新控制命令白名单
                    OperatorCommon.PluginLoadContexts[item.PluginGuid] = loadContext;
                    OperatorCommon.DicPlugins[item.PluginGuid] = plugin;
                    RefreshCommandRegistry();

                    // 4. Manifest持久化+DB配置为空时用缺省配置回填(B-1.1本地文件一次性迁移)
                    PersistMetadata(item, plugin);

                    // 5. 启动(B-1.1:传DB plugin_config,插件内不再直读本地Config文件)
                    try
                    {
                        if (await plugin.PluginStart(item.PluginConfig))
                        {
                            item.PluginHeartTime = DateTime.Now.ToDateTimeString();
                            item.PluginHeartStatus = 0;
                            item.UpdateTime = DateTime.Now.ToDateTimeString();
                            item.ExpandObject = null;
                            SysPluginDAO.Instance.UpdateColumns(item, it => new { it.PluginHeartTime, it.PluginHeartStatus, it.UpdateTime });
                            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件{item.PluginName}加载且启动成功", Service_CATEGORY);
                        }
                        else
                        {
                            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件{item.PluginName}加载但启动失败", Service_CATEGORY);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
                    }
                    return true;
                }

                // DLL中无Guid匹配的插件类型:卸载空上下文
                loadContext.Unload();
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件{item.PluginName}的DLL中未找到Guid匹配的ICenBoPlugin实现", Service_CATEGORY);
                return false;
            }
            catch (Exception ex)
            {
                loadContext.Unload();
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
                return false;
            }
        }

        /// <summary>
        /// 卸载核心(闸门内调用):摘除实例→Stop→刷新白名单→摘除上下文→Unload(协作式,GC回收)
        /// </summary>
        private static async Task UnloadOneCoreAsync(string guid)
        {
            if (guid.IsZxxNullOrEmpty()) return;
            if (OperatorCommon.DicPlugins.TryRemove(guid, out var oldPlugin))
            {
                try { await oldPlugin.PluginStop(); }
                catch (Exception ex) { LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY); }
                RefreshCommandRegistry();
            }
            if (OperatorCommon.PluginLoadContexts.TryRemove(guid, out var oldContext))
            {
                try { oldContext.Unload(); }
                catch (Exception ex) { LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY); }
            }
        }

        /// <summary>
        /// 回写插件自描述元数据(B-1.2/B-1.3):Manifest变化即持久化;
        /// DB配置为空时用Manifest.defaultConfig回填(本地Config文件仅作首次迁移来源)
        /// </summary>
        private static void PersistMetadata(SysPluginEntity item, ICenBoPlugin plugin)
        {
            try
            {
                string manifest = "";
                try { manifest = plugin.PluginManifest; }
                catch (Exception ex) { LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件[{item.PluginGuid}]Manifest读取失败：{ex}", Service_CATEGORY); }
                if (manifest.IsZxxNullOrEmpty()) return;

                bool manifestchanged = manifest != item.PluginManifest;
                bool configfilled = false;
                if (item.PluginConfig.IsZxxNullOrEmpty())
                {
                    var defaultconfig = JObject.Parse(manifest)["defaultConfig"];
                    if (defaultconfig != null)
                    {
                        item.PluginConfig = defaultconfig.ToString(Newtonsoft.Json.Formatting.None);
                        configfilled = true;
                    }
                }
                if (!manifestchanged && !configfilled) return;

                item.PluginManifest = manifest;
                item.ExpandObject = null;  //防FullEntity写路径用空壳拓展对象覆写plugin_config
                if (configfilled)
                {
                    SysPluginDAO.Instance.UpdateColumns(item, it => new { it.PluginManifest, it.PluginConfig });
                }
                else
                {
                    SysPluginDAO.Instance.UpdateColumns(item, it => new { it.PluginManifest });
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
            }
        }

        /// <summary>
        /// 从已加载插件Manifest重建控制命令缓存(装卸时调用,整体替换保证读方一致性)
        /// </summary>
        private static void RefreshCommandRegistry()
        {
            var list = new List<PluginCommandInfo>();
            foreach (var kv in OperatorCommon.DicPlugins)
            {
                try
                {
                    var manifest = kv.Value.PluginManifest;
                    if (manifest.IsZxxNullOrEmpty()) continue;
                    if (JObject.Parse(manifest)["commands"] is not JArray commands) continue;
                    foreach (var cmd in commands)
                    {
                        var classname = cmd?["className"]?.ToString();
                        if (classname.IsZxxNullOrEmpty()) continue;
                        list.Add(new PluginCommandInfo
                        {
                            ClassName = classname,
                            Description = cmd?["description"]?.ToString() ?? "",
                            PluginGuid = kv.Key,
                            PluginName = kv.Value.PluginName
                        });
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件[{kv.Key}]Manifest解析失败：{ex}", Service_CATEGORY);
                }
            }
            _supportedCommands = list;
        }
    }

}
