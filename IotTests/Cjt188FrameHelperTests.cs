#if PLUGIN_INTERNALS
using IotPlugin.Cjt188;
using IotSimulator.Core.Scenario;
using IotSimulator.Core.Slaves;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// Cjt188FrameHelper单测(需插件加InternalsVisibleTo("IotTests")+定义PLUGIN_INTERNALS;
    /// 含往返/异常帧/7字节地址BCD/SER回显/明文BCD无偏移;对独立从站Cjt188Slave做对抗性交叉验证)
    /// </summary>
    public class Cjt188FrameHelperTests
    {
        private static readonly byte[] Addr7 = { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // ============ 地址BCD ============

        [Fact]
        public void 地址BCD_7字节_低位在前_14位补零()
        {
            var addr = Cjt188FrameHelper.BuildAddressBcd(1);
            Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, addr);
        }

        [Fact]
        public void 地址BCD_十进制转BCD()
        {
            var addr = Cjt188FrameHelper.BuildAddressBcd(12345678);
            // "00000012345678"→低位在前:78 56 34 12 00 00 00
            Assert.Equal(new byte[] { 0x78, 0x56, 0x34, 0x12, 0x00, 0x00, 0x00 }, addr);
        }

        // ============ 读帧构建 ============

        [Fact]
        public void 读帧_结构_控制码01_L3_DI低在前()
        {
            var frame = Cjt188FrameHelper.BuildReadFrame(0x10, Addr7, 0x9010, 0x33);
            // FE×4 + 68 + T + addr7 + 01 + 03 + DI2(低在前) + SER + CS + 16
            Assert.Equal(0x68, frame[4]);
            Assert.Equal(0x10, frame[5]);    // 表型T
            Assert.Equal(0x01, frame[13]);   // 读码
            Assert.Equal(0x03, frame[14]);   // L=3
            Assert.Equal(0x10, frame[15]);   // DI低
            Assert.Equal(0x90, frame[16]);   // DI高
            Assert.Equal(0x33, frame[17]);   // SER
            Assert.Equal(0x16, frame[^1]);
        }

        // ============ 应答解析 ============

        [Fact]
        public void 解析_往返_取表型地址控制码数据域()
        {
            var frame = Cjt188FrameHelper.BuildReadFrame(0x10, Addr7, 0x9010, 0x33);
            Assert.True(Cjt188FrameHelper.TryParseFrame(frame, out var mt, out var addr, out var code, out var data));
            Assert.Equal(0x10, mt);
            Assert.Equal(Addr7, addr);
            Assert.Equal(0x01, code);
            Assert.Equal(new byte[] { 0x10, 0x90, 0x33 }, data);  // DI2+SER明文
        }

        [Fact]
        public void 解析_CS错_返回false()
        {
            var frame = Cjt188FrameHelper.BuildReadFrame(0x10, Addr7, 0x9010, 0x33);
            frame[^2] ^= 0xFF;
            Assert.False(Cjt188FrameHelper.TryParseFrame(frame, out _, out _, out _, out _));
        }

        [Fact]
        public void 解析_结束符错_返回false()
        {
            var frame = Cjt188FrameHelper.BuildReadFrame(0x10, Addr7, 0x9010, 0x33);
            frame[^1] = 0x00;
            Assert.False(Cjt188FrameHelper.TryParseFrame(frame, out _, out _, out _, out _));
        }

        [Fact]
        public void 解析_长度不足_返回false()
        {
            Assert.False(Cjt188FrameHelper.TryParseFrame(new byte[] { 0x68, 0x10 }, out _, out _, out _, out _));
        }

        [Fact]
        public void 解析_跳过前导FE()
        {
            var frame = Cjt188FrameHelper.BuildReadFrame(0x10, Addr7, 0x9010, 0x33);
            Assert.True(Cjt188FrameHelper.TryParseFrame(frame, out _, out var a, out _, out _));
            Assert.Equal(Addr7, a);
        }

        // ============ BCD值解码 ============

        [Fact]
        public void DecodeBcd_明文无偏移_低位在前()
        {
            // 12345→BCD低位在前:45 23 01 00
            var bytes = new byte[] { 0x45, 0x23, 0x01, 0x00 };
            Assert.Equal("12345", Cjt188FrameHelper.DecodeBcdValue(bytes, false));
        }

        [Fact]
        public void DecodeBcd_有符号负值()
        {
            var bytes = new byte[] { 0x56, 0x32, 0x80 };  // 低位在前+符号
            Assert.Equal("-3256", Cjt188FrameHelper.DecodeBcdValue(bytes, true));
        }

        [Fact]
        public void DecodeBin_小端整数()
        {
            var bytes = new byte[] { 0x01, 0x00 };  // 小端=1
            Assert.Equal("1", Cjt188FrameHelper.DecodeBinValue(bytes));
        }

        [Fact]
        public void DecodeBin_多字节小端()
        {
            var bytes = new byte[] { 0x00, 0x01 };  // 小端=256
            Assert.Equal("256", Cjt188FrameHelper.DecodeBinValue(bytes));
        }

        // ============ 阀控帧 ============

        [Fact]
        public void 阀控帧_控制码04_DI_A017()
        {
            var frame = Cjt188FrameHelper.BuildValveFrame(0x10, Addr7, 0x01, true);
            Assert.Equal(0x04, frame[13]);   // 写码
            Assert.Equal(0x17, frame[15]);   // DI低(A017)
            Assert.Equal(0xA0, frame[16]);   // DI高
            Assert.Equal(0x55, frame[18]);   // 开阀0x55
        }

        [Fact]
        public void 阀控帧_关阀_99()
        {
            var frame = Cjt188FrameHelper.BuildValveFrame(0x10, Addr7, 0x01, false);
            Assert.Equal(0x99, frame[18]);   // 关阀0x99
        }

        // ============ 对抗性交叉验证:独立从站应答→插件Helper解析 ============

        [Fact]
        public void 对抗_从站应答_插件解析出正确DI值()
        {
            var device = new DeviceModel { Address = "00000000000001", MeterType = "0x10",
                Points = { new PointModel { Di = "0x9010", Length = 4, Scale = 0.01,
                    Generator = new GeneratorModel { Type = "constant", Base = 123.45 } } } };
            var slave = new Cjt188Slave(device);
            var addr = Cjt188FrameHelper.BuildAddressBcd(1);
            var request = Cjt188FrameHelper.BuildReadFrame(0x10, addr, 0x9010, 0x33);
            var reply = slave.HandleFrame(request, System.DateTime.Now);
            Assert.NotNull(reply);
            // 插件解析从站应答
            Assert.True(Cjt188FrameHelper.TryParseFrame(reply!, out _, out var a, out var code, out var data));
            Assert.Equal(addr, a);
            Assert.Equal(0x81, code);  // 应答码
            // 数据域=DI2+SER+值区
            Assert.Equal(0x10, data[0]);  // DI低
            Assert.Equal(0x90, data[1]);  // DI高
            Assert.Equal(0x33, data[2]);  // SER回显
            var valueArea = data[3..];
            // 123.45/0.01=12345
            Assert.Equal("12345", Cjt188FrameHelper.DecodeBcdValue(valueArea, false));
        }

        [Fact]
        public void 对抗_从站SER回显_与请求一致()
        {
            var device = new DeviceModel { Address = "00000000000001", MeterType = "0x10",
                Points = { new PointModel { Di = "0x9010", Length = 4,
                    Generator = new GeneratorModel { Type = "constant", Base = 100 } } } };
            var slave = new Cjt188Slave(device);
            var addr = Cjt188FrameHelper.BuildAddressBcd(1);
            var request = Cjt188FrameHelper.BuildReadFrame(0x10, addr, 0x9010, 0x77);  // SER=0x77
            var reply = slave.HandleFrame(request, System.DateTime.Now);
            Assert.NotNull(reply);
            Cjt188FrameHelper.TryParseFrame(reply!, out _, out _, out _, out var data);
            Assert.Equal(0x77, data[2]);  // SER必须回显请求值
        }
    }
}
#endif
