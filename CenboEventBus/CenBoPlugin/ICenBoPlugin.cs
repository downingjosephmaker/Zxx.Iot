using CenboEventBus;
using System.ComponentModel;

namespace CenboEventBus
{
    /// <summary>
    /// 插件接口，所有插件必须实现此接口
    /// </summary>
    public interface ICenBoPlugin
    {
        /// <summary>
        /// 插件Guid
        /// </summary>
        [DisplayName("插件Guid")]
        string PluginGuid { get; }
        /// <summary>
        /// 插件类型
        /// </summary>
        [DisplayName("插件Guid")]
        string PluginType { get; }
        /// <summary>
        /// 插件名称
        /// </summary>
        [DisplayName("插件名称")]
        string PluginName { get; }
        /// <summary>
        /// 插件模型路径
        /// </summary>
        [DisplayName("插件模型路径")]
        string PluginModelPath { get; }
        /// <summary>
        /// 插件版本号
        /// </summary>
        [DisplayName("插件版本号")]
        string PluginVersion { get; }
        /// <summary>
        /// 插件描述
        /// </summary>
        [DisplayName("插件描述")]
        string PluginDesc { get; }
        /// <summary>
        /// 插件自描述清单(JSON:configSchema配置表单schema/defaultConfig缺省配置/
        /// commands控制命令清单/addressing寻址说明;宿主在上传或加载时持久化到
        /// sys_plugin.plugin_manifest,前后端共用一份元数据;默认实现兼容旧插件)
        /// </summary>
        [DisplayName("插件自描述清单")]
        string PluginManifest => "";
        /// <summary>
        /// 初始化插件，注入事件总线
        /// </summary>
        void PluginInit(IEventBus<PluginEvent> eventBus);
        /// <summary>
        /// 启动插件
        /// </summary>
        /// <param name="_PluginConfig">插件参数(JSON)</param>
        Task<bool> PluginStart(string _PluginConfig);
        /// <summary>
        /// 停止插件
        /// </summary>
        Task<bool> PluginStop();
        /// <summary>
        /// 插件上报消息到主程序
        /// </summary>
        Task SendMessageAsync(PluginMessage mess);
        /// <summary>
        /// 插件接收主程序消息
        /// </summary>
        Task ReceiveMessageAsync(PluginMessage mess);
    }
}