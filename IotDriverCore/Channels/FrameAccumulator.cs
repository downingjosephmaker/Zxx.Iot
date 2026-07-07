namespace IotDriverCore
{
    /// <summary>
    /// 声明式拆帧定界器(§6.4/§6.6:TCP流粘包/半包按协议边界重组,每端点独立缓冲;
    /// 提取器约定:返回(帧起始,帧总长),起始-1表示等待更多字节;起始前的字节视为噪声丢弃;
    /// 缓冲超限整体清空防脏数据积压;内置MBAP/DLT645/CJT188三种提取器)
    /// </summary>
    public class FrameAccumulator
    {
        /// <summary>
        /// 单端点缓冲上限(超限清空,防噪声积压)
        /// </summary>
        private const int MaxBuffer = 8192;

        private readonly object _lock = new();

        /// <summary>
        /// 端点→接收缓冲
        /// </summary>
        private readonly Dictionary<string, List<byte>> _buffers = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 帧提取器(缓冲字节→(帧起始,帧总长))
        /// </summary>
        private readonly Func<byte[], (int Start, int Length)> _extractor;

        public FrameAccumulator(Func<byte[], (int Start, int Length)> extractor)
        {
            _extractor = extractor;
        }

        /// <summary>
        /// 推入新收字节并返回0..N个完整帧(粘包拆出多帧,半包留缓冲等待)
        /// </summary>
        public List<byte[]> Push(string endpoint, byte[] data)
        {
            var frames = new List<byte[]>();
            lock (_lock)
            {
                if (!_buffers.TryGetValue(endpoint, out var buf))
                {
                    buf = new List<byte>();
                    _buffers[endpoint] = buf;
                }
                buf.AddRange(data);
                while (buf.Count > 0)
                {
                    var (start, length) = _extractor(buf.ToArray());
                    if (start < 0 || length <= 0)
                    {
                        if (buf.Count > MaxBuffer) buf.Clear();
                        break;
                    }
                    var frame = new byte[length];
                    buf.CopyTo(start, frame, 0, length);
                    buf.RemoveRange(0, start + length);
                    frames.Add(frame);
                }
            }
            return frames;
        }

        /// <summary>
        /// 清空端点缓冲(连接断开时调用,防旧数据串入新连接)
        /// </summary>
        public void Reset(string endpoint)
        {
            lock (_lock) { _buffers.Remove(endpoint); }
        }

        #region 内置提取器

        /// <summary>
        /// Modbus TCP(MBAP)提取器:协议标识0x0000+长度域校验,长度域异常逐字节重扫
        /// </summary>
        public static (int Start, int Length) ExtractMbap(byte[] buf)
        {
            for (int i = 0; i + 6 <= buf.Length; i++)
            {
                if (buf[i + 2] != 0 || buf[i + 3] != 0) continue;
                int len = (buf[i + 4] << 8) | buf[i + 5];
                if (len < 2 || len > 260) continue;
                int total = 6 + len;
                if (buf.Length - i < total) return (-1, 0);
                return (i, total);
            }
            return (-1, 0);
        }

        /// <summary>
        /// DL/T 645提取器:跳过FE前导,双68定界+结束符16校验,伪起始符从下一字节重扫(§6.3)
        /// </summary>
        public static (int Start, int Length) ExtractDlt645(byte[] buf)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i] == 0xFE) continue;
                if (buf[i] != 0x68) continue;
                if (buf.Length - i < 10) return (-1, 0);
                if (buf[i + 7] != 0x68) continue;
                int total = 12 + buf[i + 9];
                if (buf.Length - i < total) return (-1, 0);
                if (buf[i + total - 1] != 0x16) continue;
                return (i, total);
            }
            return (-1, 0);
        }

        /// <summary>
        /// CJ/T 188提取器:跳过FE前导,68定界+长度域(偏移10)+结束符16校验
        /// </summary>
        public static (int Start, int Length) ExtractCjt188(byte[] buf)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i] == 0xFE) continue;
                if (buf[i] != 0x68) continue;
                if (buf.Length - i < 11) return (-1, 0);
                int total = 13 + buf[i + 10];
                if (buf.Length - i < total) return (-1, 0);
                if (buf[i + total - 1] != 0x16) continue;
                return (i, total);
            }
            return (-1, 0);
        }

        #endregion
    }
}
