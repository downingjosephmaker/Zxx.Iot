using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;

namespace IotWebApi.Areas.Control.Models
{
    public class IotControlData<T>
    {
        /// <summary>
        /// 下发消息类型(1:控制 9:设备配置 100:重启应用 101:重启设备)
        /// </summary>
        public int messageType { get; set; } = 1;
        /// <summary>
        /// 设备类型(6:曼顿断路器)
        /// </summary>
        public int deviceType { get; set; } = 6;
        /// <summary>
        /// 数据集
        /// </summary>
        public List<T> deviceMessage { get; set; } = new List<T>();
    }

    /// <summary>
    /// Iot控制模型
    /// </summary>
    public class IotControl
    {
        /// <summary>
        /// 指令序列(每个链接下唯一)
        /// </summary>
        public string cmdNo { get; set; }
        /// <summary>
        /// 指令类型
        /// </summary>
        public string action { get; set; } = "Req_SendMessage";
        /// <summary>
        /// /传""就行，为后面预留
        /// </summary>
        public string gatewayCode { get; set; }
        /// <summary>
        /// 数据内容
        /// </summary>
        public IotControlData<T> data { get; set; }
    }

    /// <summary>
    /// 银行断路器
    /// </summary>
    public class IotDlqControlYh
    {
        /// <summary>
        /// 设备名称(可为空)
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 设备类型名称(可为空)
        /// </summary>
        public string typeName { get; set; }
        /// <summary>
        /// 类别(默认1)
        /// </summary>
        public int type { get; set; } = 1;
        /// <summary>
        /// 设备节点ID(用-隔开,3-4-5)
        /// </summary>
        public string deviceIds { get; set; }
        /// <summary>
        /// 曼顿空开MAC地址
        /// </summary>
        public string gatewayID { get; set; }
        /// <summary>
        /// 控制(0:分闸 1:合闸)
        /// </summary>
        public int onOff { get; set; }
    }

}
