using System.ComponentModel;

namespace CenboEventBus
{
    /// <summary>
    /// 消息传递模型
    /// </summary>
    public class PluginMessage
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        [DisplayName("消息类型")]
        public PluginMessageEnum MessageType { get; set; }
        /// <summary>
        /// 消息时间
        /// </summary>
        [DisplayName("消息时间")]
        public DateTime CurrentTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 消息内容(Json)
        /// </summary>
        [DisplayName("消息内容(Json)")]
        public string? MessageJson { get; set; }
    }

    /// <summary>
    /// 插件与主程序之间的通用消息类型枚举。
    /// 设计为覆盖所有插件场景的通用消息分类，新增插件时应优先复用已有类型。
    /// </summary>
    public enum PluginMessageEnum
    {
        /// <summary>插件心跳，用于通知主程序插件仍在运行</summary>
        [Description("心跳")]
        心跳 = 1,

        /// <summary>协议解析后的实时数据上报</summary>
        [Description("协议解析")]
        协议解析 = 2,

        /// <summary>主程序向插件下发参数写入指令</summary>
        [Description("参数下发")]
        参数下发 = 3,

        /// <summary>主程序向插件下发设备控制指令</summary>
        [Description("设备控制")]
        设备控制 = 4,

        /// <summary>插件向主程序回执参数下发结果</summary>
        [Description("参数结果")]
        参数结果 = 5,

        /// <summary>插件向主程序回执设备控制结果</summary>
        [Description("控制结果")]
        控制结果 = 6,

        /// <summary>设备运行状态变更（在线/离线等）</summary>
        [Description("运行状态")]
        运行状态 = 7,

        /// <summary>告警事件推送（设备告警、阈值越限等）</summary>
        [Description("告警通知")]
        告警通知 = 8,

        /// <summary>插件配置热更新通知</summary>
        [Description("配置更新")]
        配置更新 = 9,

        /// <summary>主程序向插件发起数据查询请求</summary>
        [Description("数据查询")]
        数据查询 = 10,

        /// <summary>插件向主程序回执数据查询结果</summary>
        [Description("查询结果")]
        查询结果 = 11,
    }
}
