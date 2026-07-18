using System.Globalization;

namespace IotPlugin.Iec104
{
    /// <summary>
    /// 解析后的APCI帧(I/S/U三种格式之一)
    /// </summary>
    internal sealed class Iec104Apci
    {
        /// <summary>帧格式('I'信息/'S'监视/'U'控制)</summary>
        public char Kind;

        /// <summary>发送序号N(S),仅I帧</summary>
        public int Ns;

        /// <summary>接收序号N(R),I帧与S帧</summary>
        public int Nr;

        /// <summary>U帧控制字节(STARTDT/STOPDT/TESTFR的act或con)</summary>
        public byte UCtrl;

        /// <summary>ASDU载荷,仅I帧</summary>
        public byte[] Asdu = Array.Empty<byte>();
    }

    /// <summary>
    /// 解析后的ASDU(类型标识+传送原因+公共地址+信息体清单)
    /// </summary>
    internal sealed class Iec104Asdu
    {
        /// <summary>类型标识TI</summary>
        public byte Ti;

        /// <summary>传送原因COT(低6位)</summary>
        public byte Cot;

        /// <summary>P/N否定确认位</summary>
        public bool Negative;

        /// <summary>T测试位</summary>
        public bool Test;

        /// <summary>公共地址CA</summary>
        public int Ca;

        /// <summary>信息体清单</summary>
        public List<Iec104Info> Items = new();
    }

    /// <summary>
    /// 单个信息体(地址+已解码值+品质+可选时标)
    /// </summary>
    internal sealed class Iec104Info
    {
        /// <summary>信息体地址IOA</summary>
        public int Ioa;

        /// <summary>解码值字符串(单点0/1,双点DPI原值,测量值数值)</summary>
        public string Value = "";

        /// <summary>品质描述词(SIQ/DIQ高四位或QDS,0=Good;IV=0x80/NT=0x40/SB=0x20/BL=0x10/OV=0x01)</summary>
        public byte Quality;

        /// <summary>CP56Time2a时标(仅带时标TI,解码失败为null)</summary>
        public DateTime? Timestamp;
    }

    /// <summary>
    /// IEC 60870-5-104 APCI/ASDU编解码(纯函数,方案§4实现依据;
    /// APCI=0x68|长度|控制域4B,ASDU=TI|VSQ|COT2B|CA2B|信息体…;
    /// 104无校验码,完整性交给TCP——方案§1.1)
    /// </summary>
    internal static class Iec104FrameHelper
    {
        #region 常量

        /// <summary>帧起始符</summary>
        public const byte StartByte = 0x68;

        // U帧控制域首字节(方案§4.1)
        public const byte StartDtAct = 0x07;
        public const byte StartDtCon = 0x0B;
        public const byte StopDtAct = 0x13;
        public const byte StopDtCon = 0x23;
        public const byte TestFrAct = 0x43;
        public const byte TestFrCon = 0x83;

        // 监视方向类型标识(方案§4.3)
        public const byte TiSinglePoint = 1;
        public const byte TiDoublePoint = 3;
        public const byte TiNormalized = 9;
        public const byte TiScaled = 11;
        public const byte TiFloat = 13;
        public const byte TiSinglePointTime = 30;
        public const byte TiDoublePointTime = 31;
        public const byte TiNormalizedTime = 34;
        public const byte TiFloatTime = 36;

        // 控制方向类型标识
        public const byte TiSingleCommand = 45;
        public const byte TiDoubleCommand = 46;
        public const byte TiSetpointFloat = 50;

        // 系统类型标识
        public const byte TiInterrogation = 100;
        public const byte TiClockSync = 103;

        // 常用传送原因(方案§4.2)
        public const byte CotSpontaneous = 3;
        public const byte CotActivation = 6;
        public const byte CotActivationCon = 7;
        public const byte CotActivationTerm = 10;
        public const byte CotInterrogatedByStation = 20;

        /// <summary>序号模(15bit)</summary>
        public const int SeqModulo = 32768;

        /// <summary>第一版支持的监视方向TI集合(点表CollectFuncCode取值域)</summary>
        public static readonly byte[] MonitorTypes =
        {
            TiSinglePoint, TiDoublePoint, TiNormalized, TiScaled, TiFloat,
            TiSinglePointTime, TiDoublePointTime, TiNormalizedTime, TiFloatTime
        };

        /// <summary>是否监视方向数据类型</summary>
        public static bool IsMonitorType(byte ti) => MonitorTypes.Contains(ti);

        #endregion

        #region 拆帧与APCI

        /// <summary>
        /// 104帧提取器(FrameAccumulator提取器约定:返回(帧起始,帧总长),起始-1=等待更多字节;
        /// 0x68|长度(4~253)|…,总长=长度+2;长度域非法从下一字节重扫)
        /// </summary>
        public static (int Start, int Length) Extract104(byte[] buf)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i] != StartByte) continue;
                if (buf.Length - i < 2) return (-1, 0);
                int len = buf[i + 1];
                if (len < 4 || len > 253) continue;
                int total = len + 2;
                if (buf.Length - i < total) return (-1, 0);
                return (i, total);
            }
            return (-1, 0);
        }

        /// <summary>
        /// 构建U帧(6字节:68 04 控制字节 00 00 00)
        /// </summary>
        public static byte[] BuildUFrame(byte ctrl) => new byte[] { StartByte, 0x04, ctrl, 0x00, 0x00, 0x00 };

        /// <summary>
        /// 构建S帧(仅接收序号N(R),用于确认)
        /// </summary>
        public static byte[] BuildSFrame(int nr) => new byte[]
        {
            StartByte, 0x04, 0x01, 0x00,
            (byte)((nr << 1) & 0xFE), (byte)((nr >> 7) & 0xFF)
        };

        /// <summary>
        /// 构建I帧(发送序号N(S)+接收序号N(R)+ASDU)
        /// </summary>
        public static byte[] BuildIFrame(int ns, int nr, byte[] asdu)
        {
            var frame = new byte[6 + asdu.Length];
            frame[0] = StartByte;
            frame[1] = (byte)(4 + asdu.Length);
            frame[2] = (byte)((ns << 1) & 0xFE);
            frame[3] = (byte)((ns >> 7) & 0xFF);
            frame[4] = (byte)((nr << 1) & 0xFE);
            frame[5] = (byte)((nr >> 7) & 0xFF);
            Array.Copy(asdu, 0, frame, 6, asdu.Length);
            return frame;
        }

        /// <summary>
        /// 解析APCI(控制域首字节低2位分派:bit0=0→I帧,01→S帧,11→U帧——方案§4.1)
        /// </summary>
        public static bool TryParseApci(byte[] frame, out Iec104Apci apci)
        {
            apci = new Iec104Apci();
            if (frame.Length < 6 || frame[0] != StartByte || frame[1] != frame.Length - 2) return false;
            byte c1 = frame[2];
            if ((c1 & 0x01) == 0)
            {
                apci.Kind = 'I';
                apci.Ns = ((c1 >> 1) & 0x7F) | (frame[3] << 7);
                apci.Nr = ((frame[4] >> 1) & 0x7F) | (frame[5] << 7);
                apci.Asdu = frame.Skip(6).ToArray();
                return apci.Asdu.Length >= 6;
            }
            if ((c1 & 0x03) == 0x01)
            {
                apci.Kind = 'S';
                apci.Nr = ((frame[4] >> 1) & 0x7F) | (frame[5] << 7);
                return true;
            }
            apci.Kind = 'U';
            apci.UCtrl = c1;
            return true;
        }

        #endregion

        #region ASDU构建(控制/系统方向)

        /// <summary>
        /// 构建单信息体ASDU(VSQ=1,SQ=0;源发站地址固定0)
        /// </summary>
        private static byte[] BuildAsdu(byte ti, byte cot, int ca, int ioa, byte[] element)
        {
            var asdu = new byte[9 + element.Length];
            asdu[0] = ti;
            asdu[1] = 0x01;
            asdu[2] = (byte)(cot & 0x3F);
            asdu[3] = 0x00;
            asdu[4] = (byte)(ca & 0xFF);
            asdu[5] = (byte)((ca >> 8) & 0xFF);
            asdu[6] = (byte)(ioa & 0xFF);
            asdu[7] = (byte)((ioa >> 8) & 0xFF);
            asdu[8] = (byte)((ioa >> 16) & 0xFF);
            Array.Copy(element, 0, asdu, 9, element.Length);
            return asdu;
        }

        /// <summary>
        /// 总召唤C_IC_NA_1(COT=6激活,IOA=0,QOI=20站召唤——方案§4.5)
        /// </summary>
        public static byte[] BuildInterrogation(int ca) =>
            BuildAsdu(TiInterrogation, CotActivation, ca, 0, new byte[] { 20 });

        /// <summary>
        /// 时钟同步C_CS_NA_1(COT=6激活,IOA=0,CP56Time2a)
        /// </summary>
        public static byte[] BuildClockSync(int ca, DateTime time) =>
            BuildAsdu(TiClockSync, CotActivation, ca, 0, Cp56Time2a.Encode(time));

        /// <summary>
        /// 单点命令C_SC_NA_1(SCO:bit0=值,bit7=S/E选择位——方案§4.6)
        /// </summary>
        public static byte[] BuildSingleCommand(int ca, int ioa, bool on, bool select) =>
            BuildAsdu(TiSingleCommand, CotActivation, ca, ioa,
                new[] { (byte)((on ? 0x01 : 0x00) | (select ? 0x80 : 0x00)) });

        /// <summary>
        /// 双点命令C_DC_NA_1(DCO:bit0-1=值(1开0/2合1),bit7=S/E选择位)
        /// </summary>
        public static byte[] BuildDoubleCommand(int ca, int ioa, bool on, bool select) =>
            BuildAsdu(TiDoubleCommand, CotActivation, ca, ioa,
                new[] { (byte)((on ? 0x02 : 0x01) | (select ? 0x80 : 0x00)) });

        /// <summary>
        /// 短浮点设定值命令C_SE_NC_1(IEEE754小端4B+QOS:bit7=S/E选择位)
        /// </summary>
        public static byte[] BuildSetpointFloat(int ca, int ioa, float value, bool select)
        {
            var element = new byte[5];
            BitConverter.GetBytes(value).CopyTo(element, 0);
            element[4] = (byte)(select ? 0x80 : 0x00);
            return BuildAsdu(TiSetpointFloat, CotActivation, ca, ioa, element);
        }

        #endregion

        #region ASDU解析

        /// <summary>
        /// 每信息体元素字节数(不含IOA;-1=不支持的TI)
        /// </summary>
        public static int ElementSize(byte ti) => ti switch
        {
            TiSinglePoint => 1,
            TiDoublePoint => 1,
            TiNormalized => 3,
            TiScaled => 3,
            TiFloat => 5,
            TiSinglePointTime => 8,
            TiDoublePointTime => 8,
            TiNormalizedTime => 10,
            TiFloatTime => 12,
            TiSingleCommand => 1,
            TiDoubleCommand => 1,
            TiSetpointFloat => 5,
            TiInterrogation => 1,
            TiClockSync => 7,
            _ => -1
        };

        /// <summary>
        /// 解析ASDU(SQ=0每元素各带IOA/SQ=1仅首IOA后续连续递增——方案§4.2,最易出错处单测覆盖;
        /// 不支持的TI返回true但Items为空,供上层记日志跳过)
        /// </summary>
        public static bool TryParseAsdu(byte[] asdu, out Iec104Asdu result)
        {
            result = new Iec104Asdu();
            if (asdu.Length < 6) return false;
            result.Ti = asdu[0];
            bool sq = (asdu[1] & 0x80) != 0;
            int count = asdu[1] & 0x7F;
            result.Cot = (byte)(asdu[2] & 0x3F);
            result.Negative = (asdu[2] & 0x40) != 0;
            result.Test = (asdu[2] & 0x80) != 0;
            result.Ca = asdu[4] | (asdu[5] << 8);

            int size = ElementSize(result.Ti);
            if (size < 0 || count == 0) return true;

            int offset = 6;
            int ioa = 0;
            for (int i = 0; i < count; i++)
            {
                if (sq && i > 0)
                {
                    ioa++;
                }
                else
                {
                    if (asdu.Length - offset < 3) return false;
                    ioa = asdu[offset] | (asdu[offset + 1] << 8) | (asdu[offset + 2] << 16);
                    offset += 3;
                }
                if (asdu.Length - offset < size) return false;
                result.Items.Add(DecodeElement(result.Ti, asdu, offset, ioa));
                offset += size;
            }
            return true;
        }

        /// <summary>
        /// 解码单个信息元素(单点SIQ/双点DIQ/归一化NVA/标度化SVA/短浮点+QDS,带时标版追加CP56Time2a)
        /// </summary>
        private static Iec104Info DecodeElement(byte ti, byte[] data, int offset, int ioa)
        {
            var info = new Iec104Info { Ioa = ioa };
            switch (ti)
            {
                case TiSinglePoint:
                case TiSinglePointTime:
                    info.Value = (data[offset] & 0x01).ToString();
                    info.Quality = (byte)(data[offset] & 0xF0);
                    if (ti == TiSinglePointTime) info.Timestamp = Cp56Time2a.TryDecode(data, offset + 1);
                    break;
                case TiDoublePoint:
                case TiDoublePointTime:
                    info.Value = (data[offset] & 0x03).ToString();
                    info.Quality = (byte)(data[offset] & 0xF0);
                    if (ti == TiDoublePointTime) info.Timestamp = Cp56Time2a.TryDecode(data, offset + 1);
                    break;
                case TiNormalized:
                case TiNormalizedTime:
                    // 归一化值:int16小端/32768=满量程比例,满量程换算交给点表ParamFormula
                    short nva = (short)(data[offset] | (data[offset + 1] << 8));
                    info.Value = (nva / 32768.0).ToString("0.######", CultureInfo.InvariantCulture);
                    info.Quality = QdsQuality(data[offset + 2]);
                    if (ti == TiNormalizedTime) info.Timestamp = Cp56Time2a.TryDecode(data, offset + 3);
                    break;
                case TiScaled:
                    short sva = (short)(data[offset] | (data[offset + 1] << 8));
                    info.Value = sva.ToString(CultureInfo.InvariantCulture);
                    info.Quality = QdsQuality(data[offset + 2]);
                    break;
                case TiFloat:
                case TiFloatTime:
                    // 短浮点:IEEE754固定小端(DCBA)——方案§1.1唯一复用点,BitConverter小端平台直读
                    float f = BitConverter.ToSingle(data, offset);
                    info.Value = f.ToString("0.######", CultureInfo.InvariantCulture);
                    info.Quality = QdsQuality(data[offset + 4]);
                    if (ti == TiFloatTime) info.Timestamp = Cp56Time2a.TryDecode(data, offset + 5);
                    break;
                case TiSingleCommand:
                case TiDoubleCommand:
                    info.Value = (data[offset] & 0x03).ToString();
                    break;
                case TiSetpointFloat:
                    info.Value = BitConverter.ToSingle(data, offset).ToString("0.######", CultureInfo.InvariantCulture);
                    break;
                case TiInterrogation:
                    info.Value = data[offset].ToString();
                    break;
                case TiClockSync:
                    info.Timestamp = Cp56Time2a.TryDecode(data, offset);
                    break;
            }
            return info;
        }

        /// <summary>
        /// QDS品质描述词有效位(IV=0x80/NT=0x40/SB=0x20/BL=0x10/OV=0x01,非0即Bad——方案§3)
        /// </summary>
        private static byte QdsQuality(byte qds) => (byte)(qds & 0xF1);

        #endregion
    }
}
