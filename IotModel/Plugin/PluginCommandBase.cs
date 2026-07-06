using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 插件命令基类。
    /// 所有从主程序下发到插件的命令（参数下发、设备控制等）均应继承此类，
    /// 统一提供命令标识、操作人、来源和目标设备列表。
    /// </summary>
    public abstract class PluginCommandBase
    {
        /// <summary>
        /// 命令唯一标识
        /// </summary>
        [DisplayName("命令唯一标识")]
        public string CommandId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 用户ID
        /// </summary>
        [DisplayName("用户ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 用户名称
        /// </summary>
        [DisplayName("用户名称")]
        public string UserName { get; set; } = "";

        /// <summary>
        /// 控制来源（Web、App、API 等）
        /// </summary>
        [DisplayName("控制来源")]
        public string SourceType { get; set; } = "Web";

        /// <summary>
        /// 设备ID集合
        /// </summary>
        [DisplayName("设备ID集合")]
        public List<int> DeviceIds { get; set; } = new();
    }
}
