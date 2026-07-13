using System.Globalization;
using IotDriverCore;
using IotDriverCore.Simulation;

namespace IotPlugin.Dlt645.Sim
{
    /// <summary>
    /// DL/T645-2007/1997从站独立实现(按国标线协议自研,不复用插件FrameHelper——
    /// 方案§4.3对抗性验证:模拟器与插件互为独立实现,才能测出编解码对称性bug;
    /// 帧结构:68,地址6(BCD低位在前),68,C,L,DATA(+0x33偏移),CS(自首个68起模256和),16;
    /// 一条总线可挂多表,按帧内6字节地址路由;广播帧地址99×6或AA×6)
    /// </summary>
    public sealed class Dlt645Slave : IProtocolSlave
    {
        /// <summary>2007版读控制码</summary>
        private const byte ReadCode2007 = 0x11;

        /// <summary>1997版读控制码</summary>
        private const byte ReadCode1997 = 0x01;

        /// <summary>广播校时控制码</summary>
        private const byte TimeSyncCode = 0x08;

        /// <summary>表地址6字节BCD(低位在前)</summary>
        private readonly byte[] _addrBcd;

        /// <summary>是否1997版(2字节DI)</summary>
        private readonly bool _is1997;

        /// <summary>DI(十进制)→点位运行态</summary>
        private readonly Dictionary<uint, SlavePoint> _points = new();

        public string Address { get; }

        public Dlt645Slave(SimDevice device, bool is1997)
        {
            Address = device.Address;
            _is1997 = is1997;
            _addrBcd = BuildAddressBcd(device.Address, 12);
            foreach (var pm in device.Points)
            {
                uint di = ParseUint(pm.Di);
                _points[di] = new SlavePoint
                {
                    Di = di,
                    ValueBytes = Math.Max(1, pm.Length),
                    Scale = pm.Scale,
                    Signed = false,
                    Generator = GeneratorFactory.Create(pm.Generator)
                };
            }
        }

        /// <summary>
        /// 处理一个入站请求帧,返回应答帧(null=不应答:地址不匹配/广播/DI未配置/校验失败)
        /// </summary>
        public byte[]? HandleFrame(byte[] frame, DateTime now)
        {
            if (!TryParseRequest(frame, out var reqAddr, out byte code, out var data))
                return null;

            // 广播地址(99×6/AA×6)校时等无应答
            if (IsBroadcast(reqAddr)) return null;

            // 地址路由:仅应答本表地址(逐字节相等)
            if (!reqAddr.AsSpan().SequenceEqual(_addrBcd)) return null;

            byte readcode = _is1997 ? ReadCode1997 : ReadCode2007;
            if (code == TimeSyncCode) return null;  // 广播校时无应答
            if (code != readcode) return null;       // 仅支持读

            uint di = ReadDi(data);
            if (!_points.TryGetValue(di, out var point)) return null;

            return BuildReadReply(readcode, di, point, now);
        }

        /// <summary>
        /// 篡改校验字节(错帧故障注入用:破坏CS)
        /// </summary>
        public byte[] Corrupt(byte[] frame)
        {
            if (frame.Length < 2) return frame;
            var bad = (byte[])frame.Clone();
            // 结束符前一字节即CS,翻转其低位
            bad[^2] ^= 0xFF;
            return bad;
        }

        #region 帧编解码(独立实现)

        /// <summary>
        /// 解析请求帧:跳过前导FE定位68,双68定界+CS+结束符校验,
        /// 返回请求地址(6字节)、控制码、已减0x33的数据域
        /// </summary>
        private bool TryParseRequest(byte[] buffer, out byte[] addr, out byte code, out byte[] data)
        {
            addr = Array.Empty<byte>();
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

            addr = new byte[6];
            Array.Copy(buffer, start + 1, addr, 0, 6);
            code = buffer[start + 8];
            data = new byte[datalen];
            for (int i = 0; i < datalen; i++)
            {
                data[i] = (byte)(buffer[start + 10 + i] - 0x33);
            }
            return true;
        }

        /// <summary>
        /// 构建读数据应答帧(C=读码|0x80正常;数据域=DI+值字节,全部低位在前;整体+0x33)
        /// </summary>
        private byte[] BuildReadReply(byte readcode, uint di, SlavePoint point, DateTime now)
        {
            int dilen = _is1997 ? 2 : 4;
            var value = EncodeBcdValue(point.Generator.Next(now) / point.Scale, point.ValueBytes, point.Signed);

            // 数据域(减33前):DI低字节在前 + 值字节低位在前
            var payload = new byte[dilen + value.Length];
            for (int i = 0; i < dilen; i++) payload[i] = (byte)((di >> (i * 8)) & 0xFF);
            Array.Copy(value, 0, payload, dilen, value.Length);

            var body = new byte[10 + payload.Length + 2];
            int pos = 0;
            body[pos++] = 0x68;
            Array.Copy(_addrBcd, 0, body, pos, 6);
            pos += 6;
            body[pos++] = 0x68;
            body[pos++] = (byte)(readcode | 0x80);  // 应答正常控制码
            body[pos++] = (byte)payload.Length;
            for (int i = 0; i < payload.Length; i++) body[pos++] = (byte)(payload[i] + 0x33);
            body[pos] = CheckSum(body, 0, pos);
            body[pos + 1] = 0x16;
            return body;
        }

        /// <summary>
        /// 从数据域取DI(低字节在前,2007为4字节/1997为2字节)
        /// </summary>
        private uint ReadDi(byte[] data)
        {
            int dilen = _is1997 ? 2 : 4;
            if (data.Length < dilen) return 0;
            uint di = 0;
            for (int i = dilen - 1; i >= 0; i--) di = (di << 8) | data[i];
            return di;
        }

        /// <summary>
        /// 工程值编码为BCD值字节(低位在前;signed时负值最高字节bit7置1;
        /// 与插件DecodeBcdValue对称:插件Reverse后拼十六进制数字并TrimStart('0'))
        /// </summary>
        private static byte[] EncodeBcdValue(double engineeringValue, int valueBytes, bool signed)
        {
            bool negative = engineeringValue < 0;
            long intval = (long)Math.Round(Math.Abs(engineeringValue));
            // 数字串左补零到valueBytes*2位BCD
            string digits = intval.ToString(CultureInfo.InvariantCulture);
            int maxdigits = valueBytes * 2;
            if (digits.Length > maxdigits) digits = digits[^maxdigits..];
            digits = digits.PadLeft(maxdigits, '0');

            // 高字节在前构造后再反转为低位在前
            var big = new byte[valueBytes];
            for (int i = 0; i < valueBytes; i++)
            {
                int hi = digits[i * 2] - '0';
                int lo = digits[i * 2 + 1] - '0';
                big[i] = (byte)((hi << 4) | lo);
            }
            if (signed && negative) big[0] |= 0x80;
            Array.Reverse(big);  // 低位在前
            return big;
        }

        #endregion

        #region 工具

        /// <summary>
        /// 表地址十进制串转BCD(低位在前;左补零到digits位)
        /// </summary>
        private static byte[] BuildAddressBcd(string address, int digitCount)
        {
            var digits = new string(address.Where(char.IsDigit).ToArray());
            if (digits.Length == 0) digits = "0";
            if (digits.Length > digitCount) digits = digits[^digitCount..];
            digits = digits.PadLeft(digitCount, '0');
            int bytes = digitCount / 2;
            var addr = new byte[bytes];
            for (int i = 0; i < bytes; i++)
            {
                int pos = digits.Length - (i + 1) * 2;
                addr[i] = (byte)(((digits[pos] - '0') << 4) | (digits[pos + 1] - '0'));
            }
            return addr;
        }

        /// <summary>
        /// 广播地址判定(全99或全AA)
        /// </summary>
        private static bool IsBroadcast(byte[] addr) =>
            addr.All(b => b == 0x99) || addr.All(b => b == 0xAA);

        /// <summary>
        /// 模256和校验(自offset起count字节)
        /// </summary>
        private static byte CheckSum(byte[] buffer, int offset, int count)
        {
            int sum = 0;
            for (int i = offset; i < offset + count; i++) sum += buffer[i];
            return (byte)(sum & 0xFF);
        }

        /// <summary>
        /// 解析uint(支持0x前缀hex或十进制)
        /// </summary>
        private static uint ParseUint(string text)
        {
            text = (text ?? "").Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Convert.ToUInt32(text[2..], 16);
            return uint.TryParse(text, out var v) ? v : 0;
        }

        #endregion

        /// <summary>
        /// 从站点位运行态
        /// </summary>
        private sealed class SlavePoint
        {
            public uint Di;
            public int ValueBytes;
            public double Scale;
            public bool Signed;
            public IValueGenerator Generator = null!;
        }
    }
}
