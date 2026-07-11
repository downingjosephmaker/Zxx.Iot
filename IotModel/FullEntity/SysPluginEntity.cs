using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 插件表完整类
    ///</summary>
    [DisplayName("插件表完整类")]
    [FullEntity]
    public class SysPluginEntity : SysPlugin
    {
        /// <summary>
        /// 插件表拓展类(不可非空初始化:FullEntityContext写路径会把非空ExpandObject
        /// 序列化覆写PluginConfig列,空壳对象会将插件配置清成"{}";
        /// plugin_config按各插件配置schema直存JSON字符串,不经本拓展类)
        ///</summary>
        [DisplayName("插件表拓展类")]
        public Expand_SysPlugin ExpandObject { get; set; }
    }
}
