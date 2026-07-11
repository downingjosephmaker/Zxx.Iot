#if PLUGIN_INTERNALS
using IotPlugin.Dlt645;
using IotSimulator.Core.Scenario;
using IotSimulator.Core.Slaves;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// Dlt645FrameHelper单测(需插件加InternalsVisibleTo("IotTests")+定义PLUGIN_INTERNALS;
    /// 含往返/异常帧/33H偏移/BCD低位在前/符号位;并对独立从站Dlt645Slave做对抗性交叉验证)
    /// </summary>
    public class Dlt645FrameHelperTests
    {
        // ============ 地址BCD ============

        [Fact]
        public void 地址BCD_低位在前_12位补零()
        {
            var addr = Dlt645FrameHelper.BuildAddressBcd(1);
            // 表地址1→"000000000001"→低位在前:01 00 00 00 00 00
            Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 }, addr);
        }

        [Fact]
        public void 地址BCD_十进制转BCD()
        {
            var addr = Dlt645FrameHelper.BuildAddressBcd(123456);
            // "000000123456"→低位在前:56 34 12 00 00 00
            Assert.Equal(new byte[] { 0x56, 0x34, 0x12, 0x00, 0x00, 0x00 }, addr);
        }

        // ============ 读帧构建 ============

        [Fact]
        public void 读帧2007_结构正确_含33偏移()
        {
            var addr = Dlt645FrameHelper.BuildAddressBcd(1);
            var frame = Dlt645FrameHelper.BuildReadFrame(addr, 0x00010000, false);
            // 前导FE×4 + 68 + addr6 + 68 + 11 + 04 + DI4(+33) + CS + 16
            Assert.Equal(0xFE, frame[0]);
            Assert.Equal(0x68, frame[4]);
            Assert.Equal(0x68, frame[11]);
            Assert.Equal(0x11, frame[12]);   // 2007读码
            Assert.Equal(0x04, frame[13]);   // L=4
            Assert.Equal(0x16, frame[^1]);   // 结束符
            // DI=00010000低字节在前+33:00+33=33,00+33=33,01+33=34,00+33=33
            Assert.Equal(0x33, frame[14]);
            Assert.Equal(0x33, frame[15]);
            Assert.Equal(0x34, frame[16]);
            Assert.Equal(0x33, frame[17]);
        }

        [Fact]
        public void 读帧1997_控制码01_DI2字节()
        {
            var addr = Dlt645FrameHelper.BuildAddressBcd(1);
            var frame = Dlt645FrameHelper.BuildReadFrame(addr, 0x9010, true);
            Assert.Equal(0x01, frame[12]);   // 1997读码
            Assert.Equal(0x02, frame[13]);   // L=2
        }

        // ============ CS校验 ============

        [Fact]
        public void CS_自首个68起模256和_可被自解()
        {
            var addr = Dlt645FrameHelper.BuildAddressBcd(1);
            var frame = Dlt645FrameHelper.BuildReadFrame(addr, 0x00010000, false);
            // 用TryParseFrame反向验证CS(它内部校验CS,成功即证CS正确)
            Assert.True(Dlt645FrameHelper.TryParseFrame(frame, out _, out _, out _));
        }

        // ============ 应答解析(异常帧) ============

        [Fact]
        public void 解析_CS错_返回false()
        {
            var addr = Dlt645FrameHelper.BuildAddressBcd(1);
            var frame = Dlt645FrameHelper.BuildReadFrame(addr, 0x00010000, false);
            frame[^2] ^= 0xFF;  // 破坏CS
            Assert.False(Dlt645FrameHelper.TryParseFrame(frame, out _, out _, out _));
        }

        [Fact]
        public void 解析_结束符错_返回false()
        {
            var addr = Dlt645FrameHelper.BuildAddressBcd(1);
            var frame = Dlt645FrameHelper.BuildReadFrame(addr, 0x00010000, false);
            frame[^1] = 0x00;
            Assert.False(Dlt645FrameHelper.TryParseFrame(frame, out _, out _, out _));
        }

        [Fact]
        public void 解析_第二个68缺失_返回false()
        {
            var addr = Dlt645FrameHelper.BuildAddressBcd(1);
            var frame = Dlt645FrameHelper.BuildReadFrame(addr, 0x00010000, false);
            frame[11] = 0x00;  // 破坏第二个68
            Assert.False(Dlt645FrameHelper.TryParseFrame(frame, out _, out _, out _));
        }

        [Fact]
        public void 解析_长度不足_返回false()
        {
            var tooShort = new byte[] { 0x68, 0x01, 0x00 };
            Assert.False(Dlt645FrameHelper.TryParseFrame(tooShort, out _, out _, out _));
        }

        [Fact]
        public void 解析_跳过前导FE()
        {
            var addr = Dlt645FrameHelper.BuildAddressBcd(1);
            var frame = Dlt645FrameHelper.BuildReadFrame(addr, 0x00010000, false);
            // BuildReadFrame已含4个FE前导,能正常解析即证跳FE
            Assert.True(Dlt645FrameHelper.TryParseFrame(frame, out var a, out _, out _));
            Assert.Equal(addr, a);
        }

        // ============ DI读取(低位在前) ============

        [Fact]
        public void ReadDi_2007_4字节低位在前()
        {
            var data = new byte[] { 0x00, 0x00, 0x01, 0x00, 0xAA };  // DI=00010000
            uint di = Dlt645FrameHelper.ReadDi(data, false);
            Assert.Equal(0x00010000u, di);
        }

        [Fact]
        public void ReadDi_1997_2字节低位在前()
        {
            var data = new byte[] { 0x10, 0x90, 0xBB };  // DI=9010
            uint di = Dlt645FrameHelper.ReadDi(data, true);
            Assert.Equal(0x9010u, di);
        }

        // ============ BCD值解码(低位在前/符号位) ============

        [Fact]
        public void DecodeBcd_无符号_低位在前()
        {
            // 值500000(电能量5000.00),4字节BCD低位在前:00 00 50 00
            var bytes = new byte[] { 0x00, 0x00, 0x50, 0x00 };
            var val = Dlt645FrameHelper.DecodeBcdValue(bytes, false);
            Assert.Equal("500000", val);
        }

        [Fact]
        public void DecodeBcd_有符号_最高位为负()
        {
            // 3字节,最高字节bit7=符号;值-3256(电流3.256A)
            // 3256→"003256",低位在前:56 32 00,最高字节00|0x80=80表负
            var bytes = new byte[] { 0x56, 0x32, 0x80 };
            var val = Dlt645FrameHelper.DecodeBcdValue(bytes, true);
            Assert.Equal("-3256", val);
        }

        [Fact]
        public void DecodeBcd_前导零_TrimStart()
        {
            var bytes = new byte[] { 0x05, 0x00, 0x00, 0x00 };  // 低位在前=00000005
            var val = Dlt645FrameHelper.DecodeBcdValue(bytes, false);
            Assert.Equal("5", val);
        }

        [Fact]
        public void DecodeBcd_空字节_返回空串()
        {
            var val = Dlt645FrameHelper.DecodeBcdValue(System.Array.Empty<byte>(), false);
            Assert.Equal("", val);
        }

        // ============ 校时帧 ============

        [Fact]
        public void 校时帧_广播地址99_控制码08()
        {
            var frame = Dlt645FrameHelper.BuildTimeSyncFrame(new System.DateTime(2026, 7, 11, 12, 30, 45));
            // FE×4 + 68 + 99×6 + 68 + 08 + ...
            Assert.Equal(0x99, frame[5]);   // 地址首字节
            Assert.Equal(0x08, frame[12]);  // 校时码
        }

        // ============ 对抗性交叉验证:独立从站编的应答帧,插件Helper能正确解析 ============

        [Fact]
        public void 对抗_从站应答帧_插件解析出正确DI与值()
        {
            // 独立从站编:表地址1,DI=00010000,constant=500000(工程值5000,scale0.01)
            var device = new DeviceModel
            {
                Address = "000000000001",
                Points =
                {
                    new PointModel { Di = "0x00010000", Length = 4, Scale = 0.01,
                        Generator = new GeneratorModel { Type = "constant", Base = 5000 } }
                }
            };
            var slave = new Dlt645Slave(device, false);
            // 主站请求帧(插件构造)
            var addr = Dlt645FrameHelper.BuildAddressBcd(1);
            var request = Dlt645FrameHelper.BuildReadFrame(addr, 0x00010000, false);
            // 从站应答(独立实现)
            var reply = slave.HandleFrame(request, System.DateTime.Now);
            Assert.NotNull(reply);
            // 插件Helper解析从站应答
            Assert.True(Dlt645FrameHelper.TryParseFrame(reply!, out var a, out var code, out var data));
            Assert.Equal(addr, a);
            Assert.Equal(0x91, code);  // 0x11|0x80应答码
            // 应答DI应回显请求
            uint di = Dlt645FrameHelper.ReadDi(data, false);
            Assert.Equal(0x00010000u, di);
            // 值区(跳过4字节DI)解码应为500000
            var valueBytes = data[4..];
            var raw = Dlt645FrameHelper.DecodeBcdValue(valueBytes, false);
            Assert.Equal("500000", raw);  // 5000/0.01=500000
        }

        [Fact]
        public void 对抗_从站对错误地址不应答()
        {
            var device = new DeviceModel { Address = "000000000001",
                Points = { new PointModel { Di = "0x00010000", Length = 4,
                    Generator = new GeneratorModel { Type = "constant", Base = 100 } } } };
            var slave = new Dlt645Slave(device, false);
            // 请求另一个地址的表
            var otherAddr = Dlt645FrameHelper.BuildAddressBcd(999);
            var request = Dlt645FrameHelper.BuildReadFrame(otherAddr, 0x00010000, false);
            Assert.Null(slave.HandleFrame(request, System.DateTime.Now));
        }

        [Fact]
        public void 对抗_从站对未配置DI不应答()
        {
            var device = new DeviceModel { Address = "000000000001",
                Points = { new PointModel { Di = "0x00010000", Length = 4,
                    Generator = new GeneratorModel { Type = "constant", Base = 100 } } } };
            var slave = new Dlt645Slave(device, false);
            var addr = Dlt645FrameHelper.BuildAddressBcd(1);
            var request = Dlt645FrameHelper.BuildReadFrame(addr, 0x02010100, false);  // 未配置的DI
            Assert.Null(slave.HandleFrame(request, System.DateTime.Now));
        }
    }
}
#endif
