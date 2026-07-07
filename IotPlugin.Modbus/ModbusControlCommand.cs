using IotModel;

namespace IotPlugin.Modbus
{
    /// <summary>
    /// Modbus设备控制命令(主程序设备控制消息的反序列化载体)
    /// </summary>
    internal sealed class ModbusControlCommand : PluginCommandBase
    {
        /// <summary>
        /// 控制类名(netmodbuswrite)
        /// </summary>
        public string ClassName { get; set; } = "";

        /// <summary>
        /// 控制内容JSON(NetModbusWrite)
        /// </summary>
        public string ConContent { get; set; } = "";
    }

    /// <summary>
    /// Modbus写点位内容(按参数编码定位点表寻址,值为工程值字符串)
    /// </summary>
    internal sealed class NetModbusWrite
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
}
