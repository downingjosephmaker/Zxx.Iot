using IotSimulator.Core.Faults;
using IotSimulator.Core.Scenario;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// 故障注入器单测(超时/错帧/粘包/半包四类装饰钩子行为)
    /// </summary>
    public class FaultInjectorTests
    {
        private static readonly byte[] Frame = { 0x68, 0x01, 0x02, 0x03, 0x04, 0x16 };

        [Fact]
        public void 无故障_整帧单次发送()
        {
            var injector = new FaultInjector(null);
            var d = injector.Decorate(Frame);
            Assert.False(d.Drop);
            Assert.Single(d.Segments);
            Assert.Equal(Frame, d.Segments[0]);
        }

        [Fact]
        public void 超时故障_概率1_丢弃()
        {
            var injector = new FaultInjector(new List<FaultModel>
            {
                new() { Type = "timeout", Probability = 1.0 }
            });
            var d = injector.Decorate(Frame);
            Assert.True(d.Drop);
        }

        [Fact]
        public void 超时故障_概率0_不触发()
        {
            var injector = new FaultInjector(new List<FaultModel>
            {
                new() { Type = "timeout", Probability = 0.0 }
            });
            var d = injector.Decorate(Frame);
            Assert.False(d.Drop);
            Assert.Single(d.Segments);
        }

        [Fact]
        public void 错帧故障_调用corruptor篡改()
        {
            bool corrupted = false;
            var injector = new FaultInjector(
                new List<FaultModel> { new() { Type = "wrongcs", Probability = 1.0 } },
                frame => { corrupted = true; var bad = (byte[])frame.Clone(); bad[^2] ^= 0xFF; return bad; });
            var d = injector.Decorate(Frame);
            Assert.True(corrupted);
            Assert.Single(d.Segments);
            Assert.NotEqual(Frame, d.Segments[0]);  // 已被篡改
        }

        [Fact]
        public void 错帧故障_无corruptor_退化为原帧()
        {
            var injector = new FaultInjector(
                new List<FaultModel> { new() { Type = "wrongcs", Probability = 1.0 } });
            var d = injector.Decorate(Frame);
            Assert.Equal(Frame, d.Segments[0]);
        }

        [Fact]
        public void 半包故障_劈两段_带延迟()
        {
            var injector = new FaultInjector(new List<FaultModel>
            {
                new() { Type = "split", Probability = 1.0, DelayMs = 50 }
            });
            var d = injector.Decorate(Frame);
            Assert.Equal(2, d.Segments.Count);
            Assert.Equal(50, d.DelayMs);
            // 两段拼接=原帧
            var joined = d.Segments[0].Concat(d.Segments[1]).ToArray();
            Assert.Equal(Frame, joined);
        }

        [Fact]
        public void 粘包故障_首帧缓存_次帧合并()
        {
            var injector = new FaultInjector(new List<FaultModel>
            {
                new() { Type = "stick", Probability = 1.0 }
            });
            // 第一帧:缓存,本轮不发(空segments)
            var d1 = injector.Decorate(Frame);
            Assert.Empty(d1.Segments);
            Assert.False(d1.Drop);
            // 第二帧:与首帧合并发送
            var d2 = injector.Decorate(Frame);
            Assert.Single(d2.Segments);
            Assert.Equal(Frame.Length * 2, d2.Segments[0].Length);
        }

        [Fact]
        public void 半包_单字节帧_不劈分()
        {
            var injector = new FaultInjector(new List<FaultModel>
            {
                new() { Type = "split", Probability = 1.0 }
            });
            var d = injector.Decorate(new byte[] { 0xAA });
            Assert.Single(d.Segments);  // 太短不劈
        }
    }
}
