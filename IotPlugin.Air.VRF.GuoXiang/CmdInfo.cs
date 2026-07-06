namespace IotPlugin.Air.VRF.GuoXiang
{
    /// <summary>
    /// 指令下发和回复关联（国祥空调插件）。
    /// CmdType：
    ///   1  = 实时数据采集（FC04 读输入寄存器）
    ///   20 = 运行控制（NetAirConRun → FC16 写保持寄存器 10-12）
    ///   21 = 系统参数（NetAirParam → FC16 写保持寄存器 100+）
    ///   22 = 用户参数（NetAirUser → FC16 写保持寄存器 10-14）
    ///   23 = 策略下发（NetAirStrategy → FC16 写保持寄存器 100+）
    ///   24 = 通用指令（NetAirCommonSet → 读取相应寄存器）
    /// </summary>
    public class CmdInfo
    {
        public int CmdType { get; set; } = 1;

        /// <summary>
        /// 指令方式(1：下发 2：上报)
        /// </summary>
        public int CmdMode { get; set; } = 1;

        /// <summary>
        /// ip + 端口
        /// </summary>
        public string IpPort { get; set; } = "";

        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 设备地址（Modbus 从机地址 1-16）
        /// </summary>
        public int DeviceAddr { get; set; }

        /// <summary>
        /// 下发指令（原始 HEX 字符串，发出去后不覆写）
        /// </summary>
        public string CmdStr { get; set; } = "";

        /// <summary>
        /// 最近一次收到的应答报文
        /// </summary>
        public string ReceiveCmdStr { get; set; } = "";

        /// <summary>
        /// 控制成功的预期返回指令（FC16 echo 前缀，用于精确匹配）
        /// </summary>
        public string ConReturnCmd { get; set; } = "";

        /// <summary>
        /// 预期响应功能码
        /// </summary>
        public byte? ExpectFuncCode { get; set; }

        /// <summary>
        /// 是否等待设备响应
        /// </summary>
        public bool WaitForResponse { get; set; } = true;

        /// <summary>
        /// 是否采集返回(0：未下发 1：下发中 2：有返回 3：超时)
        /// </summary>
        public int CmdResult { get; set; }

        /// <summary>
        /// 下发时间
        /// </summary>
        public DateTime SendTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 下发时间间隔(秒)
        /// </summary>
        public int SleepSecond { get; set; }

        /// <summary>
        /// 下次下发时间
        /// </summary>
        public DateTime SendNextTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 超时时间间隔(秒)
        /// </summary>
        public int OutSecond { get; set; }

        /// <summary>
        /// 超时时间
        /// </summary>
        public DateTime SendOutTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 超时下发次数限制
        /// </summary>
        public int TimeOutLimitCount { get; set; } = 1;

        /// <summary>
        /// 连续超时次数统计
        /// </summary>
        public int TimeOutSendCount { get; set; }

        /// <summary>
        /// 收到时间
        /// </summary>
        public DateTime RevTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 启用下发次数限制
        /// </summary>
        public bool IsStartLimit { get; set; }

        /// <summary>
        /// 次数限制
        /// </summary>
        public int LimitCount { get; set; } = 1;

        /// <summary>
        /// 下发次数
        /// </summary>
        public int SendCount { get; set; }

        /// <summary>
        /// 命令唯一标识（控制指令回执时填充）
        /// </summary>
        public string CommandId { get; set; } = "";

        /// <summary>
        /// 控制类名（NetAirConRun / NetAirParam 等），用于回执路由
        /// </summary>
        public string ClassName { get; set; } = "";

        /// <summary>
        /// 外机 Modbus 从站地址。控制指令使用内机地址发出，
        /// 成功后需通过此字段找到对应的外机采集指令来加速下一次采集。
        /// </summary>
        public int OutdoorDeviceAddr { get; set; }
    }
}
