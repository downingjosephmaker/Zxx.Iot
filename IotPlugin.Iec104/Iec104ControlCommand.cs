using IotModel;

namespace IotPlugin.Iec104
{
    /// <summary>
    /// IEC104设备控制命令(主程序设备控制消息的反序列化载体——方案§5的ContentJson模型)
    /// </summary>
    internal sealed class Iec104ControlCommand : PluginCommandBase
    {
        /// <summary>
        /// 控制类名(netiec104write)
        /// </summary>
        public string ClassName { get; set; } = "";

        /// <summary>
        /// 控制内容JSON(NetIec104Write)
        /// </summary>
        public string ConContent { get; set; } = "";
    }

    /// <summary>
    /// IEC104写点位内容(按参数编码定位点表寻址;
    /// 点表TI为1/30走单点命令45,3/31走双点命令46,9/11/13/34/36走短浮点设定值50)
    /// </summary>
    internal sealed class NetIec104Write
    {
        /// <summary>
        /// 参数编码
        /// </summary>
        public string ParamCode { get; set; } = "";

        /// <summary>
        /// 下发值(遥控点非0即合/短浮点设定值为数值字符串)
        /// </summary>
        public string ParamValue { get; set; } = "";
    }
}
