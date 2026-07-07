using IotModel;

namespace IotPlugin.S7
{
    /// <summary>
    /// S7设备控制命令(主程序设备控制消息的反序列化载体)
    /// </summary>
    internal sealed class S7ControlCommand : PluginCommandBase
    {
        /// <summary>
        /// 控制类名(nets7write)
        /// </summary>
        public string ClassName { get; set; } = "";

        /// <summary>
        /// 控制内容JSON(NetS7Write)
        /// </summary>
        public string ConContent { get; set; } = "";
    }

    /// <summary>
    /// S7写点位内容(按参数编码定位点表寻址,值为工程值字符串)
    /// </summary>
    internal sealed class NetS7Write
    {
        /// <summary>
        /// 参数编码
        /// </summary>
        public string ParamCode { get; set; } = "";

        /// <summary>
        /// 下发值(工程值字符串)
        /// </summary>
        public string ParamValue { get; set; } = "";
    }

    /// <summary>
    /// S7待写请求(入每设备写队列,由设备循环串行消费——Plc实例非线程安全)
    /// </summary>
    internal sealed class S7WriteRequest
    {
        /// <summary>
        /// 命令ID(回执关联)
        /// </summary>
        public string CommandId { get; set; } = "";

        /// <summary>
        /// 目标点位(点表行)
        /// </summary>
        public IotModel.DeviceTypeParam Point { get; set; } = null!;

        /// <summary>
        /// 下发值(工程值字符串)
        /// </summary>
        public string ParamValue { get; set; } = "";
    }
}
