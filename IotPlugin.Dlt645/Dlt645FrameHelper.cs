using System.Text;

namespace IotPlugin.Dlt645
{
    /// <summary>
    /// DL/T 645帧构建与解析(帧结构:FE×n,68,地址[6],68,C,L,DATA(+33H),CS,16;
    /// 地址6字节BCD低位在前,99×6广播;CS为首个68起模256和;数据域每字节+33H偏移;
    /// 2007版4字节DI/控制码11H读,1997版2字节DI/控制码01H读;应答C|0x80正常,C|0xC0异常带ERR)
    /// </summary>
    internal static class Dlt645FrameHelper
    {
        /// <summary>
        /// 2007版读数据控制码
        /// </summary>
        public const byte ReadCode2007 = 0x11;

        /// <summary>
        /// 1997版读数据控制码
        /// </summary>
        public const byte ReadCode1997 = 0x01;

        /// <summary>
        /// 广播校时控制码(无应答)
        /// </summary>
        public const byte TimeSyncCode = 0x08;

        /// <summary>
        /// 表地址转6字节BCD(低位在前;DeviceAdr按十进制左补零到12位,超12位截断高位)
        /// </summary>
        public static byte[] BuildAddressBcd(long adr)
        {
            var digits = Math.Abs(adr).ToString().PadLeft(12, '0');
            if (digits.Length > 12) digits = digits[^12..];
            var addr = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                // 低位字节在前:digits尾部两位是最低字节
                int pos = digits.Length - (i + 1) * 2;
                addr[i] = (byte)(((digits[pos] - '0') << 4) | (digits[pos + 1] - '0'));
            }
            return addr;
        }

        /// <summary>
        /// 构建读数据帧(2007:C=11H,L=4,DATA=DI低字节在前各+33H;1997:C=01H,L=2)
        /// </summary>
        public static byte[] BuildReadFrame(byte[] addr6, uint di, bool is1997)
        {
            int dilen = is1997 ? 2 : 4;
            var body = new byte[10 + dilen + 2];
            int pos = 0;
            body[pos++] = 0x68;
            Array.Copy(addr6, 0, body, pos, 6);
            pos += 6;
            body[pos++] = 0x68;
            body[pos++] = is1997 ? ReadCode1997 : ReadCode2007;
            body[pos++] = (byte)dilen;
            for (int i = 0; i < dilen; i++)
            {
                body[pos++] = (byte)(((di >> (i * 8)) & 0xFF) + 0x33);
            }
            body[pos] = CheckSum(body, 0, pos);
            body[pos + 1] = 0x16;
            return WithWakePrefix(body);
        }

        /// <summary>
        /// 构建广播校时帧(地址99×6,C=08H,L=6,DATA=秒分时日月年BCD各+33H,无应答)
        /// </summary>
        public static byte[] BuildTimeSyncFrame(DateTime time)
        {
            var body = new byte[18];
            int pos = 0;
            body[pos++] = 0x68;
            for (int i = 0; i < 6; i++) body[pos++] = 0x99;
            body[pos++] = 0x68;
            body[pos++] = TimeSyncCode;
            body[pos++] = 6;
            body[pos++] = (byte)(ToBcd(time.Second) + 0x33);
            body[pos++] = (byte)(ToBcd(time.Minute) + 0x33);
            body[pos++] = (byte)(ToBcd(time.Hour) + 0x33);
            body[pos++] = (byte)(ToBcd(time.Day) + 0x33);
            body[pos++] = (byte)(ToBcd(time.Month) + 0x33);
            body[pos++] = (byte)(ToBcd(time.Year % 100) + 0x33);
            body[pos] = CheckSum(body, 0, pos);
            body[pos + 1] = 0x16;
            return WithWakePrefix(body);
        }

        /// <summary>
        /// 解析应答帧:跳过任意前导FE定位68,校验帧结构/CS/结束符,
        /// 返回地址(6字节BCD低位在前)、控制码、已减33H的数据域
        /// </summary>
        public static bool TryParseFrame(byte[] buffer, out byte[] addr6, out byte code, out byte[] data)
        {
            addr6 = Array.Empty<byte>();
            code = 0;
            data = Array.Empty<byte>();
            int start = 0;
            while (start < buffer.Length && buffer[start] == 0xFE) start++;
            if (buffer.Length - start < 12) return false;
            if (buffer[start] != 0x68 || buffer[start + 7] != 0x68) return false;
            int datalen = buffer[start + 9];
            int framelen = 12 + datalen;
            if (buffer.Length - start < framelen) return false;
            if (buffer[start + framelen - 1] != 0x16) return false;
            if (CheckSum(buffer, start, framelen - 2) != buffer[start + framelen - 2]) return false;

            addr6 = new byte[6];
            Array.Copy(buffer, start + 1, addr6, 0, 6);
            code = buffer[start + 8];
            data = new byte[datalen];
            for (int i = 0; i < datalen; i++)
            {
                data[i] = (byte)(buffer[start + 10 + i] - 0x33);
            }
            return true;
        }

        /// <summary>
        /// 解码BCD值字节(帧内低字节在前;signed时最高字节bit7为符号位,如2007版功率;
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
        /// 从数据域取DI值(低字节在前,2007为4字节,1997为2字节)
        /// </summary>
        public static uint ReadDi(byte[] data, bool is1997)
        {
            int dilen = is1997 ? 2 : 4;
            if (data.Length < dilen) return 0;
            uint di = 0;
            for (int i = dilen - 1; i >= 0; i--)
            {
                di = (di << 8) | data[i];
            }
            return di;
        }

        /// <summary>
        /// 模256和校验(自首个68起,不含前导FE)
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

        /// <summary>
        /// 十进制转单字节BCD(0~99)
        /// </summary>
        private static byte ToBcd(int value) => (byte)(((value / 10) << 4) | (value % 10));
    }
}
