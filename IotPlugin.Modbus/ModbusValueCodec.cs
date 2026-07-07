using System.Globalization;
using System.Text;

namespace IotPlugin.Modbus
{
    /// <summary>
    /// Modbus值编解码(数据类型×字节序四选一×位偏移;寄存器线序视为ABCD大端,
    /// 按配置字节序归一为标准大端后解值——三种重排均为对合变换,编码复用同一重排)
    /// </summary>
    internal static class ModbusValueCodec
    {
        /// <summary>
        /// 按数据类型推导占用寄存器数(显式配置优先;bcd/string须显式配置)
        /// </summary>
        public static int InferRegLength(string datatype, int configlength)
        {
            if (configlength > 0) return configlength;
            return (datatype ?? "").ToLowerInvariant() switch
            {
                "int32" or "uint32" or "float32" => 2,
                "int64" or "float64" => 4,
                _ => 1
            };
        }

        /// <summary>
        /// 字节序归一:把线序字节重排为标准大端ABCD(CDAB字交换/BADC字内换/DCBA全反转,均为对合)
        /// </summary>
        private static byte[] Normalize(byte[] raw, string byteorder)
        {
            var order = (byteorder ?? "").Trim().ToUpperInvariant();
            if (raw.Length <= 1 || order.Length == 0 || order == "ABCD") return raw;
            var result = (byte[])raw.Clone();
            if (raw.Length == 2)
            {
                // 单寄存器只有字内高低之分
                if (order == "BADC" || order == "DCBA")
                {
                    result[0] = raw[1];
                    result[1] = raw[0];
                }
                return result;
            }
            switch (order)
            {
                case "CDAB":
                    int words = raw.Length / 2;
                    for (int w = 0; w < words; w++)
                    {
                        result[w * 2] = raw[(words - 1 - w) * 2];
                        result[w * 2 + 1] = raw[(words - 1 - w) * 2 + 1];
                    }
                    break;
                case "BADC":
                    for (int i = 0; i + 1 < raw.Length; i += 2)
                    {
                        result[i] = raw[i + 1];
                        result[i + 1] = raw[i];
                    }
                    break;
                case "DCBA":
                    Array.Reverse(result);
                    break;
            }
            return result;
        }

        /// <summary>
        /// 解码寄存器字节为工程值字符串(raw为已按点位切片的线序字节)
        /// </summary>
        public static string Decode(byte[] raw, string datatype, string byteorder, int bitoffset)
        {
            var bytes = Normalize(raw, byteorder);
            switch ((datatype ?? "").Trim().ToLowerInvariant())
            {
                case "bool":
                case "bit":
                {
                    int val = bytes.Length >= 2 ? (bytes[0] << 8) | bytes[1] : bytes[0];
                    return ((val >> Math.Max(0, bitoffset)) & 1).ToString();
                }
                case "int16":
                    return ((short)ReadU16(bytes)).ToString(CultureInfo.InvariantCulture);
                case "int32":
                    return ((int)ReadU32(bytes)).ToString(CultureInfo.InvariantCulture);
                case "uint32":
                    return ReadU32(bytes).ToString(CultureInfo.InvariantCulture);
                case "int64":
                    return ((long)ReadU64(bytes)).ToString(CultureInfo.InvariantCulture);
                case "float32":
                    return BitConverter.Int32BitsToSingle((int)ReadU32(bytes)).ToString("R", CultureInfo.InvariantCulture);
                case "float64":
                    return BitConverter.Int64BitsToDouble((long)ReadU64(bytes)).ToString("R", CultureInfo.InvariantCulture);
                case "bcd":
                {
                    var sb = new StringBuilder(bytes.Length * 2);
                    foreach (var b in bytes) sb.Append((b >> 4) & 0xF).Append(b & 0xF);
                    var text = sb.ToString().TrimStart('0');
                    return text.Length > 0 ? text : "0";
                }
                case "string":
                    return Encoding.ASCII.GetString(bytes).TrimEnd('\0', ' ');
                default:
                {
                    // uint16(默认):支持位偏移取布尔
                    int val = ReadU16(bytes);
                    if (bitoffset >= 0) return ((val >> bitoffset) & 1).ToString();
                    return val.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>
        /// 编码工程值为寄存器线序字节(写下发用;bcd/string/位偏移写不支持返回null)
        /// </summary>
        public static byte[]? Encode(string value, string datatype, string byteorder, int reglength)
        {
            try
            {
                var type = (datatype ?? "").Trim().ToLowerInvariant();
                byte[] big;
                switch (type)
                {
                    case "bool":
                    case "bit":
                    {
                        ushort v = (ushort)(value.Trim() == "1" || value.Trim().ToLowerInvariant() == "true" ? 1 : 0);
                        big = new[] { (byte)(v >> 8), (byte)v };
                        break;
                    }
                    case "int16":
                    {
                        var v = (short)decimal.Parse(value, CultureInfo.InvariantCulture);
                        big = new[] { (byte)((ushort)v >> 8), (byte)v };
                        break;
                    }
                    case "int32":
                    case "uint32":
                    {
                        var v = (uint)(long)decimal.Parse(value, CultureInfo.InvariantCulture);
                        big = new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };
                        break;
                    }
                    case "int64":
                    {
                        var v = (ulong)(long)decimal.Parse(value, CultureInfo.InvariantCulture);
                        big = WriteU64(v);
                        break;
                    }
                    case "float32":
                    {
                        var v = (uint)BitConverter.SingleToInt32Bits(float.Parse(value, CultureInfo.InvariantCulture));
                        big = new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };
                        break;
                    }
                    case "float64":
                    {
                        var v = (ulong)BitConverter.DoubleToInt64Bits(double.Parse(value, CultureInfo.InvariantCulture));
                        big = WriteU64(v);
                        break;
                    }
                    case "bcd":
                    case "string":
                        return null;
                    default:
                    {
                        var v = (ushort)decimal.Parse(value, CultureInfo.InvariantCulture);
                        big = new[] { (byte)(v >> 8), (byte)v };
                        break;
                    }
                }
                return Normalize(big, byteorder);
            }
            catch
            {
                return null;
            }
        }

        private static int ReadU16(byte[] bytes) =>
            bytes.Length >= 2 ? (bytes[0] << 8) | bytes[1] : bytes[0];

        private static uint ReadU32(byte[] bytes)
        {
            uint val = 0;
            for (int i = 0; i < 4 && i < bytes.Length; i++) val = (val << 8) | bytes[i];
            return val;
        }

        private static ulong ReadU64(byte[] bytes)
        {
            ulong val = 0;
            for (int i = 0; i < 8 && i < bytes.Length; i++) val = (val << 8) | bytes[i];
            return val;
        }

        private static byte[] WriteU64(ulong v) =>
            new[]
            {
                (byte)(v >> 56), (byte)(v >> 48), (byte)(v >> 40), (byte)(v >> 32),
                (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v
            };
    }
}
