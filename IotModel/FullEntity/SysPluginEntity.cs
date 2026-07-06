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
        /// 插件表拓展类
        ///</summary>
        [DisplayName("插件表拓展类")]
        public Expand_SysPlugin ExpandObject { get; set; } = new Expand_SysPlugin();
    }
}
