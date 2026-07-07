namespace IotDriverCore
{
    /// <summary>
    /// 驱动点位(点表一行:采集地址与解码规则,§6.2字段清单)
    /// </summary>
    public class DriverPoint
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 参数编码(对应物模型ParamCode)
        /// </summary>
        public string ParamCode { get; set; } = "";

        /// <summary>
        /// 从站地址(Modbus从站号/645表地址索引等协议寻址键)
        /// </summary>
        public int SlaveAddr { get; set; }

        /// <summary>
        /// 功能码/寄存器区(FC01/02/03/04,或协议自定义语义)
        /// </summary>
        public byte FuncCode { get; set; }

        /// <summary>
        /// 起始地址(寄存器地址/DI标识/DB偏移)
        /// </summary>
        public int Address { get; set; }

        /// <summary>
        /// 占用长度(寄存器数/字节数,按协议语义)
        /// </summary>
        public int Length { get; set; } = 1;

        /// <summary>
        /// 数据类型(int16/uint16/int32/uint32/int64/float32/float64/bcd/string)
        /// </summary>
        public string DataType { get; set; } = "";

        /// <summary>
        /// 字节序四选一(ABCD/CDAB/BADC/DCBA)
        /// </summary>
        public string ByteOrder { get; set; } = "ABCD";

        /// <summary>
        /// 位偏移(-1=整字取值,>=0按位取布尔)
        /// </summary>
        public int BitOffset { get; set; } = -1;

        /// <summary>
        /// 倍率(原始值×倍率=工程值)
        /// </summary>
        public double Scale { get; set; } = 1;

        /// <summary>
        /// 点位采集周期(毫秒,0=继承设备/产品级)
        /// </summary>
        public int CollectCycleMs { get; set; }
    }

    /// <summary>
    /// 合包批次(框架按从站→功能码→地址连续性分组后的一次物理读,见PointBatchBuilder)
    /// </summary>
    public class PointBatch
    {
        /// <summary>
        /// 从站地址
        /// </summary>
        public int SlaveAddr { get; set; }

        /// <summary>
        /// 功能码/寄存器区
        /// </summary>
        public byte FuncCode { get; set; }

        /// <summary>
        /// 批次起始地址
        /// </summary>
        public int StartAddress { get; set; }

        /// <summary>
        /// 批次总长度(含空洞)
        /// </summary>
        public int TotalLength { get; set; }

        /// <summary>
        /// 批次内点位清单(驱动按点位地址-起始地址偏移解码)
        /// </summary>
        public List<DriverPoint> Points { get; set; } = new();
    }

    /// <summary>
    /// 单点采集值
    /// </summary>
    public class DriverPointValue
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 参数编码
        /// </summary>
        public string ParamCode { get; set; } = "";

        /// <summary>
        /// 工程值字符串(数值型可解析为double)
        /// </summary>
        public string Value { get; set; } = "";

        /// <summary>
        /// 质量码(0=Good,非0=Bad原因)
        /// </summary>
        public short Quality { get; set; }

        /// <summary>
        /// 采集时刻
        /// </summary>
        public DateTime CollectTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 批量读结果
    /// </summary>
    public class DriverReadResult
    {
        /// <summary>
        /// 是否读取成功(false时Values为空,Message含原因)
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 失败原因
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// 点位值清单
        /// </summary>
        public List<DriverPointValue> Values { get; set; } = new();

        /// <summary>
        /// 构造成功结果
        /// </summary>
        public static DriverReadResult Ok(List<DriverPointValue> values) => new() { Success = true, Values = values };

        /// <summary>
        /// 构造失败结果
        /// </summary>
        public static DriverReadResult Fail(string message) => new() { Success = false, Message = message };
    }

    /// <summary>
    /// 下行命令(平台控制对象,协议驱动解析ContentJson编帧)
    /// </summary>
    public class DeviceCommand
    {
        /// <summary>
        /// 命令唯一标识(控制结果回执路由)
        /// </summary>
        public string CommandId { get; set; } = "";

        /// <summary>
        /// 目标设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 控制类名(NetAirConRun等,回执路由)
        /// </summary>
        public string ClassName { get; set; } = "";

        /// <summary>
        /// 控制内容JSON(协议驱动自行反序列化)
        /// </summary>
        public string ContentJson { get; set; } = "";
    }

    /// <summary>
    /// 下行写结果
    /// </summary>
    public class DriverWriteResult
    {
        /// <summary>
        /// 是否写入成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 结果说明(失败原因/回执信息)
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// 构造成功结果
        /// </summary>
        public static DriverWriteResult Ok(string message = "") => new() { Success = true, Message = message };

        /// <summary>
        /// 构造失败结果
        /// </summary>
        public static DriverWriteResult Fail(string message) => new() { Success = false, Message = message };
    }
}
