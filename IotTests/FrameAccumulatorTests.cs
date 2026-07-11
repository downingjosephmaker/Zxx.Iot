using IotDriverCore;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// FrameAccumulator三提取器单测(粘包/半包/脏前缀/长度域异常——TCP流切帧的核心正确性)
    /// </summary>
    public class FrameAccumulatorTests
    {
        // ============ 构造真实线格式帧的辅助 ============

        /// <summary>
        /// 构造一个645帧:68 [addr6] 68 C L [data(+33)] CS 16(不含前导FE)
        /// </summary>
        private static byte[] Make645(byte[] addr6, byte code, byte[] rawData)
        {
            var body = new List<byte> { 0x68 };
            body.AddRange(addr6);
            body.Add(0x68);
            body.Add(code);
            body.Add((byte)rawData.Length);
            foreach (var b in rawData) body.Add((byte)(b + 0x33));
            byte cs = 0;
            foreach (var b in body) cs += b;
            body.Add(cs);
            body.Add(0x16);
            return body.ToArray();
        }

        /// <summary>
        /// 构造一个188帧:68 T [addr7] C L [data] CS 16
        /// </summary>
        private static byte[] Make188(byte meterType, byte[] addr7, byte code, byte[] data)
        {
            var body = new List<byte> { 0x68, meterType };
            body.AddRange(addr7);
            body.Add(code);
            body.Add((byte)data.Length);
            body.AddRange(data);
            byte cs = 0;
            foreach (var b in body) cs += b;
            body.Add(cs);
            body.Add(0x16);
            return body.ToArray();
        }

        /// <summary>
        /// 构造一个Modbus TCP帧:tid(2) 0000 len(2) unit func [data]
        /// </summary>
        private static byte[] MakeMbap(ushort tid, byte unit, byte func, byte[] data)
        {
            int len = 2 + data.Length;
            var frame = new List<byte>
            {
                (byte)(tid >> 8), (byte)tid, 0, 0, (byte)(len >> 8), (byte)len, unit, func
            };
            frame.AddRange(data);
            return frame.ToArray();
        }

        private static readonly byte[] Addr6 = { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private static readonly byte[] Addr7 = { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // ============ DLT645 提取器 ============

        [Fact]
        public void Dlt645_单帧_完整切出()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractDlt645);
            var frame = Make645(Addr6, 0x91, new byte[] { 0x00, 0x00, 0x01, 0x00, 0x11, 0x22 });
            var frames = acc.Push("ep", frame);
            Assert.Single(frames);
            Assert.Equal(frame, frames[0]);
        }

        [Fact]
        public void Dlt645_粘包_两帧一次推入_切出两帧()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractDlt645);
            var f1 = Make645(Addr6, 0x91, new byte[] { 0x00, 0x00, 0x01, 0x00 });
            var f2 = Make645(Addr6, 0x91, new byte[] { 0x00, 0x01, 0x01, 0x00 });
            var glued = f1.Concat(f2).ToArray();
            var frames = acc.Push("ep", glued);
            Assert.Equal(2, frames.Count);
            Assert.Equal(f1, frames[0]);
            Assert.Equal(f2, frames[1]);
        }

        [Fact]
        public void Dlt645_半包_分两段推入_第二段补齐才切出()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractDlt645);
            var frame = Make645(Addr6, 0x91, new byte[] { 0x00, 0x00, 0x01, 0x00, 0xAB, 0xCD });
            int mid = frame.Length / 2;
            var part1 = frame.Take(mid).ToArray();
            var part2 = frame.Skip(mid).ToArray();
            Assert.Empty(acc.Push("ep", part1));      // 半包等待
            var frames = acc.Push("ep", part2);
            Assert.Single(frames);
            Assert.Equal(frame, frames[0]);
        }

        [Fact]
        public void Dlt645_脏前缀FE_跳过并切出()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractDlt645);
            var frame = Make645(Addr6, 0x91, new byte[] { 0x00, 0x00, 0x01, 0x00 });
            var withPrefix = new byte[] { 0xFE, 0xFE, 0xFE, 0xFE }.Concat(frame).ToArray();
            var frames = acc.Push("ep", withPrefix);
            Assert.Single(frames);
            Assert.Equal(frame, frames[0]);
        }

        [Fact]
        public void Dlt645_伪起始符_下个字节重扫定位真帧()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractDlt645);
            var frame = Make645(Addr6, 0x91, new byte[] { 0x00, 0x00, 0x01, 0x00 });
            // 前面塞一个孤立0x68(第8字节非0x68→伪起始),真帧紧随
            var noisy = new byte[] { 0x68, 0x11, 0x22 }.Concat(frame).ToArray();
            var frames = acc.Push("ep", noisy);
            Assert.Single(frames);
            Assert.Equal(frame, frames[0]);
        }

        [Fact]
        public void Dlt645_结束符错_不切出()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractDlt645);
            var frame = Make645(Addr6, 0x91, new byte[] { 0x00, 0x00, 0x01, 0x00 });
            frame[^1] = 0x00;  // 破坏结束符
            var frames = acc.Push("ep", frame);
            Assert.Empty(frames);
        }

        [Fact]
        public void Dlt645_数据长度域超缓冲_等待更多()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractDlt645);
            // 声明L=20但只给前12字节,应等待
            var head = new byte[] { 0x68, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x68, 0x91, 20, 0x33, 0x33 };
            var frames = acc.Push("ep", head);
            Assert.Empty(frames);
        }

        [Fact]
        public void Dlt645_多端点缓冲隔离()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractDlt645);
            var frame = Make645(Addr6, 0x91, new byte[] { 0x00, 0x00, 0x01, 0x00 });
            int mid = frame.Length / 2;
            acc.Push("epA", frame.Take(mid).ToArray());
            // epB推入完整帧不受epA半包影响
            var framesB = acc.Push("epB", frame);
            Assert.Single(framesB);
            // epA补齐后也能切出
            var framesA = acc.Push("epA", frame.Skip(mid).ToArray());
            Assert.Single(framesA);
        }

        // ============ CJT188 提取器 ============

        [Fact]
        public void Cjt188_单帧_完整切出()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractCjt188);
            var frame = Make188(0x10, Addr7, 0x81, new byte[] { 0x01, 0x90, 0x01, 0x12, 0x34, 0x56, 0x78 });
            var frames = acc.Push("ep", frame);
            Assert.Single(frames);
            Assert.Equal(frame, frames[0]);
        }

        [Fact]
        public void Cjt188_粘包_切出两帧()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractCjt188);
            var f1 = Make188(0x10, Addr7, 0x81, new byte[] { 0x01, 0x90, 0x01, 0x11, 0x22 });
            var f2 = Make188(0x10, Addr7, 0x81, new byte[] { 0x01, 0x90, 0x02, 0x33, 0x44 });
            var frames = acc.Push("ep", f1.Concat(f2).ToArray());
            Assert.Equal(2, frames.Count);
            Assert.Equal(f1, frames[0]);
            Assert.Equal(f2, frames[1]);
        }

        [Fact]
        public void Cjt188_半包_补齐才切出()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractCjt188);
            var frame = Make188(0x10, Addr7, 0x81, new byte[] { 0x01, 0x90, 0x01, 0xAA, 0xBB, 0xCC });
            int mid = frame.Length / 2;
            Assert.Empty(acc.Push("ep", frame.Take(mid).ToArray()));
            var frames = acc.Push("ep", frame.Skip(mid).ToArray());
            Assert.Single(frames);
            Assert.Equal(frame, frames[0]);
        }

        [Fact]
        public void Cjt188_脏前缀FE_跳过切出()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractCjt188);
            var frame = Make188(0x10, Addr7, 0x81, new byte[] { 0x01, 0x90, 0x01, 0x11 });
            var withPrefix = new byte[] { 0xFE, 0xFE }.Concat(frame).ToArray();
            var frames = acc.Push("ep", withPrefix);
            Assert.Single(frames);
            Assert.Equal(frame, frames[0]);
        }

        [Fact]
        public void Cjt188_结束符错_不切出()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractCjt188);
            var frame = Make188(0x10, Addr7, 0x81, new byte[] { 0x01, 0x90, 0x01, 0x11 });
            frame[^1] = 0x00;
            Assert.Empty(acc.Push("ep", frame));
        }

        // ============ MBAP(Modbus TCP)提取器 ============

        [Fact]
        public void Mbap_单帧_完整切出()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractMbap);
            var frame = MakeMbap(1, 1, 3, new byte[] { 0x02, 0x12, 0x34 });
            var frames = acc.Push("ep", frame);
            Assert.Single(frames);
            Assert.Equal(frame, frames[0]);
        }

        [Fact]
        public void Mbap_粘包_切出两帧()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractMbap);
            var f1 = MakeMbap(1, 1, 3, new byte[] { 0x02, 0x12, 0x34 });
            var f2 = MakeMbap(2, 1, 3, new byte[] { 0x02, 0x56, 0x78 });
            var frames = acc.Push("ep", f1.Concat(f2).ToArray());
            Assert.Equal(2, frames.Count);
            Assert.Equal(f1, frames[0]);
            Assert.Equal(f2, frames[1]);
        }

        [Fact]
        public void Mbap_半包_补齐才切出()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractMbap);
            var frame = MakeMbap(1, 1, 3, new byte[] { 0x04, 0x11, 0x22, 0x33, 0x44 });
            int mid = frame.Length / 2;
            Assert.Empty(acc.Push("ep", frame.Take(mid).ToArray()));
            var frames = acc.Push("ep", frame.Skip(mid).ToArray());
            Assert.Single(frames);
            Assert.Equal(frame, frames[0]);
        }

        [Fact]
        public void Mbap_协议标识非零_跳过该位置继续扫描()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractMbap);
            var frame = MakeMbap(1, 1, 3, new byte[] { 0x02, 0x12, 0x34 });
            // 前置一个协议标识非0的假头(第2/3字节非0),真帧紧随
            var noisy = new byte[] { 0x00, 0x00, 0x01, 0x02, 0x00, 0x03 }.Concat(frame).ToArray();
            var frames = acc.Push("ep", noisy);
            Assert.Contains(frames, f => f.SequenceEqual(frame));
        }

        [Fact]
        public void Mbap_长度域超范围_跳过()
        {
            var acc = new FrameAccumulator(FrameAccumulator.ExtractMbap);
            // 长度域=300(超260上限),应被跳过,不误切
            var bad = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x01, 0x2C, 0x01, 0x03 };
            var frames = acc.Push("ep", bad);
            Assert.Empty(frames);
        }
    }
}
