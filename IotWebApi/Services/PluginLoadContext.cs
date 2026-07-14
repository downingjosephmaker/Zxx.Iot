using System.Reflection;
using System.Runtime.Loader;

namespace IotWebApi
{
    /// <summary>
    /// 插件隔离上下文定义(可卸载ALC;共享程序集强制回落Default ALC——
    /// 否则插件目录旁置副本会导致跨ALC类型不同一:ICenBoPlugin.IsAssignableFrom静默失败、
    /// 反射Attribute拿空、IotModel实体缓存分裂)
    /// </summary>
    public class PluginLoadContext : AssemblyLoadContext
    {
        /// <summary>
        /// 共享程序集黑名单(宿主与插件必须共用同一份,插件目录中的副本一律忽略)
        /// </summary>
        private static readonly HashSet<string> SharedAssemblies = new(StringComparer.OrdinalIgnoreCase)
        {
            "CenboEventBus", "IotModel", "IotDriverCore", "CenBoCommon.Zxx", "IotLog", "NewLife.Core", "XCode"
        };

        private AssemblyDependencyResolver _resolver;
        public PluginLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        /// <summary>
        /// 剥离插件目录中旁置的共享程序集副本(dll/pdb/xml)。共享程序集必须由宿主deferral统一提供,
        /// 插件包(尤其dotnet publish产物)自带的副本一旦与宿主版本漂移,会被加载进进程造成跨ALC类型不同一——
        /// 插件绑定到陈旧接口,类型校验判其未实现新增成员而加载失败。上传落位时调用,保证存储目录只留插件自身+非共享依赖。
        /// </summary>
        public static void StripSharedAssemblies(string dir)
        {
            if (!Directory.Exists(dir)) return;
            foreach (var name in SharedAssemblies)
                foreach (var ext in new[] { ".dll", ".pdb", ".xml" })
                {
                    var p = Path.Combine(dir, name + ext);
                    if (File.Exists(p)) { try { File.Delete(p); } catch { /* 占用则跳过,不阻断上传 */ } }
                }
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // 共享程序集返回null回落Default ALC(宿主已加载的同一份)
            if (assemblyName.Name != null && SharedAssemblies.Contains(assemblyName.Name)) return null;
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
            return null;
        }
    }
}
