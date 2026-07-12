using IotDriverCore.Simulation;

namespace IotDriverCore
{
    /// <summary>模拟点位(数据源为平台DeviceTypeParam点表快照)</summary>
    public class SimPoint
    {
        /// <summary>参数编码(平台ParamCode)</summary>
        public string ParamCode { get; set; } = "";
        /// <summary>标识(645/188为DI;modbus为寄存器地址,十进制或0x前缀)</summary>
        public string Di { get; set; } = "";
        /// <summary>modbus功能码(3保持/4输入;645/188忽略)</summary>
        public int FuncCode { get; set; } = 3;
        /// <summary>值字节数/寄存器数</summary>
        public int Length { get; set; } = 4;
        /// <summary>数据类型(uint16/int16/int32/float32等)</summary>
        public string DataType { get; set; } = "uint16";
        /// <summary>值生成器(默认随机游走,UI可覆盖,不落库)</summary>
        public GeneratorModel Generator { get; set; } = new();
    }

    /// <summary>模拟设备(一个从站)</summary>
    public class SimDevice
    {
        /// <summary>设备地址(645/188表地址;modbus从站号)</summary>
        public string Address { get; set; } = "";
        /// <summary>表型(仅cjt188)</summary>
        public string? MeterType { get; set; }
        /// <summary>点位清单</summary>
        public List<SimPoint> Points { get; set; } = new();
        /// <summary>设备级故障注入</summary>
        public List<FaultModel> Faults { get; set; } = new();
    }

    /// <summary>模拟能力元信息(前端渲染用)</summary>
    public class SimCapability
    {
        public bool SupportSlave { get; set; }
        public bool SupportSelfTest { get; set; }
        public int DefaultPort { get; set; }
        public string Protocol { get; set; } = "";
    }

    /// <summary>模拟模式</summary>
    public enum SimMode { Slave, SelfTest }

    /// <summary>启动模拟请求</summary>
    public class SimStartRequest
    {
        public SimMode Mode { get; set; } = SimMode.Slave;
        public int Port { get; set; }
        public List<SimDevice> Devices { get; set; } = new();
    }

    /// <summary>模拟实例状态</summary>
    public class SimStatus
    {
        public string SimId { get; set; } = "";
        public int DeviceId { get; set; }
        public SimMode Mode { get; set; }
        public bool Running { get; set; }
        public int Port { get; set; }
        public string StartedAt { get; set; } = "";
        public string Message { get; set; } = "";
    }

    /// <summary>故障注入规格(UI→运行中实例)</summary>
    public class SimFaultSpec
    {
        /// <summary>类型:timeout/wrongcs/stick/split;空串=清除</summary>
        public string Kind { get; set; } = "";
        public double Probability { get; set; } = 1;
        public int DelayMs { get; set; } = 50;
    }

    /// <summary>实时帧日志(经SignalR回流)</summary>
    public class SimLogEntry
    {
        public string SimId { get; set; } = "";
        public string Time { get; set; } = "";
        /// <summary>方向:← 收 / → 发</summary>
        public string Direction { get; set; } = "";
        public string Hex { get; set; } = "";
        public string Note { get; set; } = "";
    }
}
