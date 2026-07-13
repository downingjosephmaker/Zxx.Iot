using System.Globalization;
using IotDriverCore;
using IotDriverCore.Simulation;

namespace IotPlugin.Cjt188.Sim
{
    /// <summary>
    /// CJ/T188从站独立实现(按国标线协议自研,不复用插件FrameHelper——方案§4.3对抗性验证;
    /// 帧结构:68,T表型,地址7(BCD低位在前),C,L,DATA,CS(自68起模256和),16;
    /// 数据域明文BCD无0x33偏移;读计量C=01H应答C=81H,应答数据域=DI[2低位在前]+SER[1回显]+值区;
    /// 值区内多点位按配置字节偏移顺序拼接,BCD低位在前)
    /// </summary>
    public sealed class Cjt188Slave : IProtocolSlave
    {
        /// <summary>读控制码</summary>
        private const byte ReadCode = 0x01;

        /// <summary>表型T</summary>
        private readonly byte _meterType;

        /// <summary>表地址7字节BCD(低位在前)</summary>
        private readonly byte[] _addrBcd;

        /// <summary>DI(十进制)→该DI下按值区顺序排列的点位</summary>
        private readonly Dictionary<ushort, List<SlavePoint>> _diPoints = new();

        public string Address { get; }

        public Cjt188Slave(SimDevice device)
        {
            Address = device.Address;
            _meterType = ParseByte(device.MeterType, 0x10);  // 默认0x10冷水表
            _addrBcd = BuildAddressBcd(device.Address, 14);
            foreach (var pm in device.Points)
            {
                ushort di = (ushort)ParseUint(pm.Di);
                if (!_diPoints.TryGetValue(di, out var list))
                {
                    list = new List<SlavePoint>();
                    _diPoints[di] = list;
                }
                list.Add(new SlavePoint
                {
                    ValueBytes = Math.Max(1, pm.Length),
                    Scale = pm.Scale,
                    Offset = pm.BitOffset,
                    Generator = GeneratorFactory.Create(pm.Generator)
                });
            }
        }

        /// <summary>
        /// 处理入站请求帧,返回应答帧(null=不应答)
        /// </summary>
        public byte[]? HandleFrame(byte[] frame, DateTime now)
        {
            if (!TryParseRequest(frame, out byte meterType, out var reqAddr, out byte code, out var data))
                return null;

            // 地址路由(逐字节相等;表型不做强匹配,部分主站表型可为通配)
            if (!reqAddr.AsSpan().SequenceEqual(_addrBcd)) return null;
            if (code != ReadCode) return null;
            if (data.Length < 3) return null;

            ushort di = (ushort)(data[0] | (data[1] << 8));
            byte ser = data[2];
            if (!_diPoints.TryGetValue(di, out var points)) return null;

            return BuildReadReply(meterType == 0 ? _meterType : meterType, di, ser, points, now);
        }

        /// <summary>
        /// 篡改校验字节(错帧故障注入)
        /// </summary>
        public byte[] Corrupt(byte[] frame)
        {
            if (frame.Length < 2) return frame;
            var bad = (byte[])frame.Clone();
            bad[^2] ^= 0xFF;  // 结束符前一字节即CS
            return bad;
        }

        #region 帧编解码(独立实现)

        /// <summary>
        /// 解析请求帧:跳过前导FE定位68,长度域(偏移10)+CS+结束符校验,
        /// 返回表型、地址7、控制码、数据域(明文无偏移)
        /// </summary>
        private static bool TryParseRequest(byte[] buffer, out byte meterType, out byte[] addr, out byte code, out byte[] data)
        {
            meterType = 0;
            addr = Array.Empty<byte>();
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

            meterType = buffer[start + 1];
            addr = new byte[7];
            Array.Copy(buffer, start + 2, addr, 0, 7);
            code = buffer[start + 9];
            data = new byte[datalen];
            Array.Copy(buffer, start + 11, data, 0, datalen);
            return true;
        }

        /// <summary>
        /// 构建读应答帧(C=01H|0x80=81H;数据域=DI2低位在前+SER回显+值区)
        /// 值区布局:该DI下所有点均配置字节偏移(Offset>=0)时按偏移落位、空隙留0,与采集侧Array.Copy(valuearea,offset,..)对齐;
        /// 存在未配置偏移(Offset<0)时退回按点序连续拼接(向后兼容)
        /// </summary>
        private byte[] BuildReadReply(byte meterType, ushort di, byte ser, List<SlavePoint> points, DateTime now)
        {
            byte[] valueArea;
            if (points.All(p => p.Offset >= 0))
            {
                int len = points.Max(p => Math.Max(0, p.Offset) + p.ValueBytes);
                valueArea = new byte[len];
                foreach (var p in points)
                {
                    var bcd = EncodeBcdValue(p.Generator.Next(now) / p.Scale, p.ValueBytes);
                    bcd.CopyTo(valueArea, Math.Max(0, p.Offset));
                }
            }
            else
            {
                var list = new List<byte>();
                foreach (var p in points)
                {
                    list.AddRange(EncodeBcdValue(p.Generator.Next(now) / p.Scale, p.ValueBytes));
                }
                valueArea = list.ToArray();
            }

            var payload = new byte[3 + valueArea.Length];
            payload[0] = (byte)(di & 0xFF);
            payload[1] = (byte)(di >> 8);
            payload[2] = ser;
            valueArea.CopyTo(payload, 3);

            var body = new byte[11 + payload.Length + 2];
            int pos = 0;
            body[pos++] = 0x68;
            body[pos++] = meterType;
            Array.Copy(_addrBcd, 0, body, pos, 7);
            pos += 7;
            body[pos++] = (byte)(ReadCode | 0x80);  // 应答控制码81H
            body[pos++] = (byte)payload.Length;
            Array.Copy(payload, 0, body, pos, payload.Length);
            pos += payload.Length;
            body[pos] = CheckSum(body, 0, pos);
            body[pos + 1] = 0x16;
            return body;
        }

        /// <summary>
        /// 工程值编码为BCD值字节(明文低位在前,与插件DecodeBcdValue对称)
        /// </summary>
        private static byte[] EncodeBcdValue(double engineeringValue, int valueBytes)
        {
            long intval = (long)Math.Round(Math.Abs(engineeringValue));
            string digits = intval.ToString(CultureInfo.InvariantCulture);
            int maxdigits = valueBytes * 2;
            if (digits.Length > maxdigits) digits = digits[^maxdigits..];
            digits = digits.PadLeft(maxdigits, '0');

            var big = new byte[valueBytes];
            for (int i = 0; i < valueBytes; i++)
            {
                int hi = digits[i * 2] - '0';
                int lo = digits[i * 2 + 1] - '0';
                big[i] = (byte)((hi << 4) | lo);
            }
            Array.Reverse(big);  // 低位在前
            return big;
        }

        #endregion

        #region 工具

        /// <summary>
        /// 表地址十进制串转BCD(低位在前;左补零到digitCount位)
        /// </summary>
        private static byte[] BuildAddressBcd(string address, int digitCount)
        {
            var digits = new string((address ?? "").Where(char.IsDigit).ToArray());
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

        /// <summary>
        /// 解析byte(支持0x前缀hex或十进制;空/失败取默认)
        /// </summary>
        private static byte ParseByte(string? text, byte fallback)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return fallback;
            try
            {
                return text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToByte(text[2..], 16)
                    : byte.Parse(text);
            }
            catch { return fallback; }
        }

        #endregion

        /// <summary>
        /// 从站点位运行态(188一个值区可含多点位,按顺序切片)
        /// </summary>
        private sealed class SlavePoint
        {
            public int ValueBytes;
            public double Scale;
            /// <summary>值区字节偏移(映射自CollectBitOffset;-1表示按点序连续)</summary>
            public int Offset;
            public IValueGenerator Generator = null!;
        }
    }
}
