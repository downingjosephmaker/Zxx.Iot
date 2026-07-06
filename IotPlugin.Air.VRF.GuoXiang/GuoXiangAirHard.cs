using IotModel;
using System.ComponentModel;
using System.Text;
using CenBoCommon.Zxx;

namespace IotPlugin.Air.VRF.GuoXiang
{
    /// <summary>
    /// 国祥VRF空调 Modbus-TCP 协议解析与指令构建。
    /// 文档：多拖一-恒温Modbus数据表-V2.5-国祥空调
    /// 功能码：FC04（读输入寄存器）/ FC03（读保持寄存器）/ FC06（写单寄存器）/ FC16（写多寄存器）
    /// </summary>
    internal static class GuoXiangAirHard
    {
        // 输入寄存器采集范围（FC04，只读取地址 58/61/62/63，一次读6个覆盖 58~63）
        private const int StatusReadAddr = 58;
        private const int StatusReadCount = 6;

        // 保持寄存器地址（用户参数10~14，系统参数100~116）
        private const int UserParamAddr = 10;
        private const int UserParamCount = 5;
        private const int SysParamAddr = 100;
        private const int SysParamCount = 17;

        #region 采集指令构建

        /// <summary>
        /// FC04 读输入寄存器 58~63（6个），取地址 58/61/62/63 的数据。
        /// addr 为该压缩机（外机）的 Modbus 从站地址（DeviceAdr）。
        /// </summary>
        public static string GetRealReadCmd(int addr)
        {
            string cmd = $"{addr.Change10To16(2)}04{StatusReadAddr.Change10To16(4)}{StatusReadCount.Change10To16(4)}";
            return cmd + cmd.ToZxxHex().YjCrc16LHStr();
        }

        /// <summary>
        /// FC03 读指定内机用户参数保持寄存器。
        /// 寄存器起始地址 = (unitSlot-1)×5 + 10，读 5 个寄存器。
        /// </summary>
        /// <param name="gatewayAddr">外机网关 Modbus 从站地址。</param>
        /// <param name="unitSlot">内机槽位序号（DeviceAdr，1-based）。</param>
        public static string GetUserParamReadCmd(int gatewayAddr, int unitSlot)
        {
            int startReg = (unitSlot - 1) * 5 + UserParamAddr;
            string cmd = $"{gatewayAddr.Change10To16(2)}03{startReg.Change10To16(4)}{UserParamCount.Change10To16(4)}";
            return cmd + cmd.ToZxxHex().YjCrc16LHStr();
        }

        /// <summary>FC03 读保持寄存器100~116（系统参数）。</summary>
        public static string GetSysParamReadCmd(int addr)
        {
            string cmd = $"{addr.Change10To16(2)}03{SysParamAddr.Change10To16(4)}{SysParamCount.Change10To16(4)}";
            return cmd + cmd.ToZxxHex().YjCrc16LHStr();
        }

        #endregion

        #region 控制指令构建

        /// <summary>
        /// 根据 NetAirConRun 构建单条 FC16 内机控制指令。<br/>
        /// gatewayAddr = 外机网关 Modbus 从站地址；unitSlot = 内机 DeviceAdr（1-based）。<br/>
        /// 寄存器基址 base = (unitSlot-1)×5 + 10，一次写入 3 个寄存器：
        /// <list type="bullet">
        ///   <item>base+0：工作模式(低字节 bit0~2) + 风速(高字节 bit8~10)</item>
        ///   <item>base+1：设定温度 × 10（16℃~30℃）</item>
        ///   <item>base+2：开关机（高字节 bit8; 0=关，1=开）</item>
        /// </list>
        /// AirModel、AirSpeed、AirModelTemp、AirSwitch 由调用方保证有效（非255）。
        /// </summary>
        public static (string CmdStr, string ConReturnCmd) BuildRunControlCmds(int gatewayAddr, int unitSlot, NetAirConRun model)
        {
            int baseReg = (unitSlot - 1) * 5 + UserParamAddr;

            int modeVal = MapModeToProtocol(model.AirModel);
            int fanVal = MapFanToProtocol(model.AirSpeed);
            int reg0 = (modeVal & 0x07) | ((fanVal & 0x07) << 8);
            int reg1 = model.AirModelTemp * 10;
            int reg2 = (model.AirSwitch == 1 || model.AirSwitch == 3) ? 0x0100 : 0x0000;

            return BuildFC16(gatewayAddr, baseReg, new[] { reg0, reg1, reg2 });
        }

        #endregion

        #region 数据解析

        /// <summary>
        /// 解析 FC04 读输入寄存器58~63 的应答数据（只取 58/61/62/63）。
        /// 调用前已跳过帧头 [addr][04][byteCount]，buffer 为纯数据字节（至少12字节）。
        /// </summary>
        public static AirStatusInfo? ParseStatusData(List<byte> data, int devAdr)
        {
            if (data == null || data.Count < StatusReadCount * 2) return null;

            var info = new AirStatusInfo { DevAdr = devAdr };

            info.FaultRaw = ReadWord(data, 0);   // Reg58：故障码
            // Reg59(offset 2)、Reg60(offset 4) 不采集
            info.EnvirTempRaw = ReadInt16(data, 6);  // Reg61：环境温度(×10)
            info.EnvirTemp = info.EnvirTempRaw / 10m;
            info.OutdoorTempRaw = ReadInt16(data, 8);  // Reg62：室外温度(×10)
            info.OutdoorTemp = info.OutdoorTempRaw / 10m;
            info.SystemModeRaw = ReadWord(data, 10);   // Reg63：系统模式 bit8~10
            info.SystemMode = (info.SystemModeRaw >> 8) & 0x07; // 0=停机,1=制冷,2=制热,3=通风,4=除湿,5=自动

            info.HasAlarm = info.FaultRaw != 0;
            return info;
        }

        /// <summary>
        /// 解析 FC03 读保持寄存器应答中的内机用户参数（5 个寄存器，10 字节）。
        /// 调用前已跳过帧头 [addr][03][byteCount]，buffer 为纯数据字节（至少 10 字节）。
        /// </summary>
        public static UserParamInfo? ParseUserParamData(List<byte> data)
        {
            if (data == null || data.Count < UserParamCount * 2) return null;
            var info = new UserParamInfo();
            int modeRaw = ReadWord(data, 0);   // offset 0: 模式(bit0~2) + 风速(bit8~10)
            info.ModeRaw = modeRaw;
            info.AirModel = ProtocolModeToAirModel(modeRaw & 0x07);
            info.AirSpeed = ProtocolFanToAirSpeed((modeRaw >> 8) & 0x07);
            int tempRaw = ReadWord(data, 2);   // offset 2: 设定温度(×10)
            info.SetTemp = tempRaw / 10m;
            int onoffRaw = ReadWord(data, 4);   // offset 4: 开关机(bit8=1 为开)
            info.AirSwitch = ((onoffRaw >> 8) & 0x01) == 1 ? 1 : 0;
            return info;
        }

        #endregion

        #region 值映射

        // NetAirConRun.AirModel(0=自动,1=制冷,2=除湿,3=送风,4=制热) → 协议(0=停机,1=制冷,2=制热,3=通风,4=除湿,5=自动)
        public static int MapModeToProtocol(int airModel) =>
            airModel switch { 0 => 5, 1 => 1, 2 => 4, 3 => 3, 4 => 2, _ => 0 };

        // NetAirConRun.AirSpeed(0=自动,1=低,2=中,3=高) → 协议(0=停,1=低,2=中,3=高,4=自动)
        public static int MapFanToProtocol(int airSpeed) =>
            airSpeed switch { 0 => 4, 1 => 1, 2 => 2, 3 => 3, _ => 4 };

        // 协议 SystemMode → NetAirConRun.AirModel
        public static int ProtocolModeToAirModel(int sysMode) =>
            sysMode switch { 1 => 1, 2 => 4, 3 => 3, 4 => 2, 5 => 0, _ => 0 };

        // 协议 FanSpeed(0/无效=停,1=低,2=中,3=高,4=自动) → NetAirConRun.AirSpeed(0=自动,1=低,2=中,3=高)
        public static int ProtocolFanToAirSpeed(int protocolFan) =>
            protocolFan switch { 4 => 0, 1 => 1, 2 => 2, 3 => 3, _ => 0 };

        public static string GetModeName(int sysMode) =>
            sysMode switch { 0 => "停机", 1 => "制冷", 2 => "制热", 3 => "通风", 4 => "除湿", 5 => "自动", _ => "未知" };

        #endregion

        #region Modbus 报文工具

        // FC06 写单寄存器；echo = request（相同帧）
        private static (string CmdStr, string ConReturnCmd) BuildFC06(int addr, int regAddr, int value)
        {
            string cmd = $"{addr.Change10To16(2)}06{regAddr.Change10To16(4)}{(value & 0xFFFF).Change10To16(4)}";
            string full = cmd + cmd.ToZxxHex().YjCrc16LHStr();
            return (full, full);
        }

        // FC16 写多寄存器；echo 不含数据字节
        public static (string CmdStr, string ConReturnCmd) BuildFC16(int addr, int startReg, int[] values)
        {
            var sb = new StringBuilder();
            sb.Append(addr.Change10To16(2));
            sb.Append("10");
            sb.Append(startReg.Change10To16(4));
            sb.Append(values.Length.Change10To16(4));
            sb.Append((values.Length * 2).Change10To16(2));
            foreach (var v in values)
                sb.Append((v & 0xFFFF).Change10To16(4));
            string cmdBody = sb.ToString();
            string cmdFull = cmdBody + cmdBody.ToZxxHex().YjCrc16LHStr();
            string echoBody = $"{addr.Change10To16(2)}10{startReg.Change10To16(4)}{values.Length.Change10To16(4)}";
            string echoFull = echoBody + echoBody.ToZxxHex().YjCrc16LHStr();
            return (cmdFull, echoFull);
        }

        private static int ReadWord(List<byte> buf, int off) => (buf[off] << 8) | buf[off + 1];
        private static short ReadInt16(List<byte> buf, int off) => (short)((buf[off] << 8) | buf[off + 1]);

        #endregion

        #region 数据模型

        [DisplayName("国祥空调压缩机状态")]
        public class AirStatusInfo
        {
            public int DevAdr { get; set; }
            public int FaultRaw { get; set; }  // Reg58 故障码
            public short EnvirTempRaw { get; set; }  // Reg61 环境温度(×10)
            public decimal EnvirTemp { get; set; }  // 环境温度(℃)
            public short OutdoorTempRaw { get; set; }  // Reg62 室外温度(×10)
            public decimal OutdoorTemp { get; set; }  // 室外温度(℃)
            public int SystemModeRaw { get; set; }  // Reg63
            public int SystemMode { get; set; }  // 0=停机,1=制冷,2=制热,3=通风,4=除湿,5=自动
            public bool HasAlarm { get; set; }
        }

        [DisplayName("国祥空调内机用户参数")]
        public class UserParamInfo
        {
            public int ModeRaw { get; set; }  // offset0 原始值
            public int AirModel { get; set; }  // 0=自动,1=制冷,2=除湿,3=送风,4=制热
            public int AirSpeed { get; set; }  // 0=自动,1=低,2=中,3=高
            public decimal SetTemp { get; set; }  // 设定温度(℃)
            public int AirSwitch { get; set; }  // 1=开,0=关
        }

        #endregion
    }
}