using System.Text;
using Xunit;
using IotDriverCore;

namespace IotTests
{
    public class TcpFrameSplitTests
    {
        private static FrameAccumulator NewlineAcc() => new FrameAccumulator(buf =>
        {
            int idx = System.Array.IndexOf(buf, (byte)'\n');
            return idx < 0 ? (-1, 0) : (0, idx + 1);
        });

        [Fact]
        public void Splits_stuck_and_half_packets()
        {
            var acc = NewlineAcc();
            var f1 = acc.Push("ep", Encoding.UTF8.GetBytes("aaa\nbbb"));   // 一整帧 + 半帧
            Assert.Single(f1);
            Assert.Equal("aaa\n", Encoding.UTF8.GetString(f1[0]));

            var f2 = acc.Push("ep", Encoding.UTF8.GetBytes("ccc\nddd\n")); // 补齐 bbbccc + 一整帧
            Assert.Equal(2, f2.Count);
            Assert.Equal("bbbccc\n", Encoding.UTF8.GetString(f2[0]));
            Assert.Equal("ddd\n", Encoding.UTF8.GetString(f2[1]));
        }
    }
}
