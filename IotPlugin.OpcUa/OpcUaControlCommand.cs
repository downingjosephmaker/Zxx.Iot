using IotModel;

namespace IotPlugin.OpcUa
{
    /// <summary>
    /// OPC UA设备控制命令(主程序设备控制消息的反序列化载体)
    /// </summary>
    internal sealed class OpcUaControlCommand : PluginCommandBase
    {
        /// <summary>
        /// 控制类名(netopcuawrite)
        /// </summary>
        public string ClassName { get; set; } = "";

        /// <summary>
        /// 控制内容JSON(NetOpcUaWrite)
        /// </summary>
        public string ConContent { get; set; } = "";
    }

    /// <summary>
    /// OPC UA写点位内容(按参数编码定位点表NodeId,值为工程值字符串)
    /// </summary>
    internal sealed class NetOpcUaWrite
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
    /// OPC UA待写请求(入每设备写队列,由会话循环串行消费——Session实例非线程安全)
    /// </summary>
    internal sealed class OpcUaWriteRequest
    {
        /// <summary>
        /// 命令ID(回执关联)
        /// </summary>
        public string CommandId { get; set; } = "";

        /// <summary>
        /// 目标点位(点表行,NodeId取CollectNodeId)
        /// </summary>
        public IotModel.DeviceTypeParam Point { get; set; } = null!;

        /// <summary>
        /// 下发值(工程值字符串)
        /// </summary>
        public string ParamValue { get; set; } = "";
    }
}
