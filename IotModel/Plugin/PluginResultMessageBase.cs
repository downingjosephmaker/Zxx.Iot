using System;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 插件操作结果回执基类。
    /// 所有插件向主程序回传的结果消息（参数结果、控制结果等）均应继承此类，
    /// 统一提供命令标识和回执时间戳。
    /// </summary>
    public abstract class PluginResultMessageBase
    {
        /// <summary>
        /// 命令唯一标识，与下发命令的 CommandId 对应
        /// </summary>
        [DisplayName("命令唯一标识")]
        public string CommandId { get; set; } = "";

        /// <summary>
        /// 回执时间
        /// </summary>
        [DisplayName("回执时间")]
        public string ResultTime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
