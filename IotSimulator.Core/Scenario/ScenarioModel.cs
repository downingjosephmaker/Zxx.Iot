namespace IotSimulator.Core.Scenario
{
    /// <summary>
    /// 场景根模型(对齐方案§4.4:传输模式+协议+设备清单;System.Text.Json反序列化,
    /// 字段命名走camelCase由加载器配置忽略大小写)
    /// </summary>
    public class ScenarioModel
    {
        /// <summary>场景名(仅日志展示)</summary>
        public string Name { get; set; } = "";

        /// <summary>传输配置(拨入/被拨)</summary>
        public TransportModel Transport { get; set; } = new();

        /// <summary>协议标识(dlt645-2007/dlt645-1997/cjt188/modbus-tcp)</summary>
        public string Protocol { get; set; } = "";

        /// <summary>设备清单(一条连接可挂多设备,如DTU挂多表)</summary>
        public List<DeviceModel> Devices { get; set; } = new();
    }

    /// <summary>
    /// 传输配置(mode=dial-in拨入连插件服务端/listen被拨等插件拨入)
    /// </summary>
    public class TransportModel
    {
        /// <summary>模式:dial-in(模拟器拨入插件TcpServerChannel)或listen(模拟器监听等插件拨入)</summary>
        public string Mode { get; set; } = "dial-in";

        /// <summary>目标主机(dial-in连接目标;listen模式忽略)</summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>端口(dial-in为插件监听端口;listen为本模拟器监听端口)</summary>
        public int Port { get; set; }

        /// <summary>DTU注册包(dial-in可选:连接后先发的ASCII注册ID,匹配插件DeviceGateway)</summary>
        public string? RegisterPacket { get; set; }

        /// <summary>周期心跳(dial-in可选)</summary>
        public HeartbeatModel? Heartbeat { get; set; }
    }

    /// <summary>
    /// 心跳配置(dial-in周期发送hex,插件HeartbeatFilter吞掉)
    /// </summary>
    public class HeartbeatModel
    {
        /// <summary>心跳报文hex(如"AA")</summary>
        public string Hex { get; set; } = "";

        /// <summary>发送间隔毫秒</summary>
        public int IntervalMs { get; set; } = 30000;
    }

    /// <summary>
    /// 设备模型(一个从站:645/188为表地址,modbus为从站号)
    /// </summary>
    public class DeviceModel
    {
        /// <summary>设备地址(645为12位十进制表地址/188为14位/modbus为从站号)</summary>
        public string Address { get; set; } = "";

        /// <summary>表型(仅cjt188:如0x10冷水表,十进制或0x前缀hex)</summary>
        public string? MeterType { get; set; }

        /// <summary>点位清单</summary>
        public List<PointModel> Points { get; set; } = new();

        /// <summary>设备级故障注入(作用于该设备全部点位应答)</summary>
        public List<FaultModel> Faults { get; set; } = new();
    }

    /// <summary>
    /// 点位模型(di=645/188标识/modbus寄存器地址;generator决定值;scale仅日志摘要展示)
    /// </summary>
    public class PointModel
    {
        /// <summary>标识(645为DI十进制或0x前缀hex/188为DI十进制或hex/modbus为寄存器地址)</summary>
        public string Di { get; set; } = "";

        /// <summary>值字节数(645/188 BCD值字节数;modbus寄存器数)</summary>
        public int Length { get; set; } = 4;

        /// <summary>modbus功能码(仅modbus:3保持寄存器/4输入寄存器,默认3)</summary>
        public int FuncCode { get; set; } = 3;

        /// <summary>modbus数据类型(仅modbus:uint16/int16/int32/float32等)</summary>
        public string? DataType { get; set; }

        /// <summary>值生成器</summary>
        public GeneratorModel Generator { get; set; } = new();

        /// <summary>定标倍率(仅摘要展示,值本身由generator产出)</summary>
        public double Scale { get; set; } = 1;
    }

    /// <summary>
    /// 生成器模型(type=constant/random/sine/step)
    /// </summary>
    public class GeneratorModel
    {
        /// <summary>类型:constant/random/sine/step</summary>
        public string Type { get; set; } = "constant";

        /// <summary>常量值/正弦基线/阶梯基线</summary>
        public double Base { get; set; }

        /// <summary>随机下限</summary>
        public double Min { get; set; }

        /// <summary>随机上限</summary>
        public double Max { get; set; }

        /// <summary>正弦振幅</summary>
        public double Amp { get; set; }

        /// <summary>正弦周期秒</summary>
        public double PeriodS { get; set; } = 60;

        /// <summary>阶梯步长(每StepEverySs递增Step)</summary>
        public double Step { get; set; } = 1;

        /// <summary>阶梯递增间隔秒</summary>
        public double StepEveryS { get; set; } = 10;
    }

    /// <summary>
    /// 故障注入模型(type=timeout/wrongcs/stick/split)
    /// </summary>
    public class FaultModel
    {
        /// <summary>类型:timeout(不回复)/wrongcs(错校验)/stick(粘包)/split(半包)</summary>
        public string Type { get; set; } = "";

        /// <summary>触发概率(0~1)</summary>
        public double Probability { get; set; } = 1;

        /// <summary>半包分片延迟毫秒(仅split)</summary>
        public int DelayMs { get; set; } = 50;
    }
}
