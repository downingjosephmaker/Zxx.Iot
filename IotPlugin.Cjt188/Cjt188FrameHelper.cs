using System.Text;

namespace IotPlugin.Cjt188
{
    /// <summary>
    /// CJ/T 188帧构建与解析(帧结构:FE×n,68,T表型,地址[7],C,L,DATA,CS,16;
    /// 地址7字节BCD低位在前含厂商代码;数据域明文BCD无33H偏移;CS为68起模256和;
    /// 读计量C=01H应答C=81H,数据域=DI[2低位在前]+SER[1]+值;阀控C=04H DI=A017H;
    /// 2018版帧骨架兼容2004,DI表可配置)
    /// </summary>
    internal static class Cjt188FrameHelper
    {
        /// <summary>
        /// 读数据控制码
        /// </summary>
        public const byte ReadCode = 0x01;

        /// <summary>
        /// 写数据控制码(阀控等)
        /// </summary>
        public const byte WriteCode = 0x04;

        /// <summary>
        /// 阀控DI(A017H)
        /// </summary>
        public const ushort ValveDi = 0xA017;

        /// <summary>
        /// 表地址转7字节BCD(低位在前;DeviceAdr按十进制左补零到14位,超14位截断高位)
        /// </summary>
        public static byte[] BuildAddressBcd(long adr)
        {
            var digits = Math.Abs(adr).ToString().PadLeft(14, '0');
            if (digits.Length > 14) digits = digits[^14..];
            var addr = new byte[7];
            for (int i = 0; i < 7; i++)
            {
                int pos = digits.Length - (i + 1) * 2;
                addr[i] = (byte)(((digits[pos] - '0') << 4) | (digits[pos + 1] - '0'));
            }
            return addr;
        }

        /// <summary>
        /// 构建读数据帧(C=01H,L=03,DATA=DI低字节在前+SER)
        /// </summary>
        public static byte[] BuildReadFrame(byte metertype, byte[] addr7, ushort di, byte ser)
        {
            var body = new byte[16];
            int pos = 0;
            body[pos++] = 0x68;
            body[pos++] = metertype;
            Array.Copy(addr7, 0, body, pos, 7);
            pos += 7;
            body[pos++] = ReadCode;
            body[pos++] = 3;
            body[pos++] = (byte)(di & 0xFF);
            body[pos++] = (byte)(di >> 8);
            body[pos++] = ser;
            body[pos] = CheckSum(body, 0, pos);
            body[pos + 1] = 0x16;
            return WithWakePrefix(body);
        }

        /// <summary>
        /// 构建阀控帧(C=04H,DATA=DI A017H+SER+阀门状态,0x55开/0x99关)
        /// </summary>
        public static byte[] BuildValveFrame(byte metertype, byte[] addr7, byte ser, bool open)
        {
            var body = new byte[17];
            int pos = 0;
            body[pos++] = 0x68;
            body[pos++] = metertype;
            Array.Copy(addr7, 0, body, pos, 7);
            pos += 7;
            body[pos++] = WriteCode;
            body[pos++] = 4;
            body[pos++] = ValveDi & 0xFF;
            body[pos++] = ValveDi >> 8;
            body[pos++] = ser;
            body[pos++] = open ? (byte)0x55 : (byte)0x99;
            body[pos] = CheckSum(body, 0, pos);
            body[pos + 1] = 0x16;
            return WithWakePrefix(body);
        }

        /// <summary>
        /// 解析应答帧:跳过任意前导FE定位68,校验帧结构/CS/结束符,
        /// 返回表型、地址(7字节BCD低位在前)、控制码、数据域(明文无偏移)
        /// </summary>
        public static bool TryParseFrame(byte[] buffer, out byte metertype, out byte[] addr7, out byte code, out byte[] data)
        {
            metertype = 0;
            addr7 = Array.Empty<byte>();
            code = 0;
            data = Array.Empty<byte>();
            int start = 0;
            while (start < buffer.Length && buffer[start] == 0xFE) start++;
            if (buffer.Length - start < 13) return false;
            if (buffer[start] != 0x68) return false;
            int datalen = buffer[start + 10];
            int framelen = 13 + datalen;
            if (buffer.Length - start < framelen) return false;
            if (buffer[start + framelen - 1] != 0x16) return false;
            if (CheckSum(buffer, start, framelen - 2) != buffer[start + framelen - 2]) return false;

            metertype = buffer[start + 1];
            addr7 = new byte[7];
            Array.Copy(buffer, start + 2, addr7, 0, 7);
            code = buffer[start + 9];
            data = new byte[datalen];
            Array.Copy(buffer, start + 11, data, 0, datalen);
            return true;
        }

        /// <summary>
        /// 解码BCD值字节(低字节在前;signed时最高字节bit7为符号位;
        /// 返回原始整数数字串,小数定标由ParamFormula公式完成)
        /// </summary>
        public static string DecodeBcdValue(byte[] valuebytes, bool signed)
        {
            if (valuebytes.Length == 0) return "";
            var bytes = (byte[])valuebytes.Clone();
            Array.Reverse(bytes);
            bool negative = false;
            if (signed && (bytes[0] & 0x80) != 0)
            {
                negative = true;
                bytes[0] &= 0x7F;
            }
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append((b >> 4) & 0xF).Append(b & 0xF);
            var text = sb.ToString().TrimStart('0');
            if (text.Length == 0) text = "0";
            return negative ? $"-{text}" : text;
        }

        /// <summary>
        /// 解码二进制小端整数(状态字ST等非BCD字段)
        /// </summary>
        public static string DecodeBinValue(byte[] valuebytes)
        {
            long val = 0;
            for (int i = valuebytes.Length - 1; i >= 0; i--)
            {
                val = (val << 8) | valuebytes[i];
            }
            return val.ToString();
        }

        /// <summary>
        /// 模256和校验(自68起,不含前导FE)
        /// </summary>
        private static byte CheckSum(byte[] buffer, int offset, int count)
        {
            int sum = 0;
            for (int i = offset; i < offset + count; i++) sum += buffer[i];
            return (byte)(sum & 0xFF);
        }

        /// <summary>
        /// 帧前追加4个FE唤醒前导
        /// </summary>
        private static byte[] WithWakePrefix(byte[] body)
        {
            var frame = new byte[body.Length + 4];
            frame[0] = frame[1] = frame[2] = frame[3] = 0xFE;
            Array.Copy(body, 0, frame, 4, body.Length);
            return frame;
        }
    }
}
