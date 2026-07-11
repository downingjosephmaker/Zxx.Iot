#if PLUGIN_INTERNALS
using IotPlugin.Modbus;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// ModbusFrameHelper单测(需插件加InternalsVisibleTo("IotTests")+定义PLUGIN_INTERNALS;
    /// RTU CRC16低字节在前/MBAP七字节头/异常码|0x80/往返/异常帧)
    /// </summary>
    public class ModbusFrameHelperTests
    {
        // ============ RTU帧 ============

        [Fact]
        public void RTU读帧_结构_从站功能码地址计数()
        {
            var frame = ModbusFrameHelper.BuildReadRtu(1, 3, 0x0000, 2);
            Assert.Equal(0x01, frame[0]);   // 从站
            Assert.Equal(0x03, frame[1]);   // 功能码
            Assert.Equal(0x00, frame[2]);   // 地址高
            Assert.Equal(0x00, frame[3]);   // 地址低
            Assert.Equal(0x00, frame[4]);   // 计数高
            Assert.Equal(0x02, frame[5]);   // 计数低
            Assert.Equal(8, frame.Length);  // 6PDU+2CRC
        }

        [Fact]
        public void RTU往返_CRC自洽()
        {
            // 构造一个应答帧:slave func bytecount data... CRC
            var pdu = new byte[] { 0x01, 0x03, 0x02, 0x12, 0x34 };
            // 用BuildReadRtu的CRC逻辑难直接构造应答,改测请求帧往返
            var request = ModbusFrameHelper.BuildReadRtu(1, 3, 0, 2);
            Assert.True(ModbusFrameHelper.TryParseRtu(request, out var slave, out var func, out _));
            Assert.Equal(1, slave);
            Assert.Equal(3, func);
        }

        [Fact]
        public void RTU解析_CRC错_返回false()
        {
            var frame = ModbusFrameHelper.BuildReadRtu(1, 3, 0, 2);
            frame[^1] ^= 0xFF;  // 破坏CRC
            Assert.False(ModbusFrameHelper.TryParseRtu(frame, out _, out _, out _));
        }

        [Fact]
        public void RTU解析_长度不足_返回false()
        {
            Assert.False(ModbusFrameHelper.TryParseRtu(new byte[] { 0x01, 0x03 }, out _, out _, out _));
        }

        [Fact]
        public void RTU写单寄存器_功能码06()
        {
            var frame = ModbusFrameHelper.BuildWriteSingleRtu(1, 0x0010, 0x00FF);
            Assert.Equal(0x06, frame[1]);
            Assert.Equal(0x00, frame[4]);
            Assert.Equal(0xFF, frame[5]);
        }

        [Fact]
        public void RTU写多寄存器_功能码16_字节数正确()
        {
            var regbytes = new byte[] { 0x12, 0x34, 0x56, 0x78 };  // 2寄存器
            var frame = ModbusFrameHelper.BuildWriteMultiRtu(1, 0x0010, regbytes);
            Assert.Equal(0x10, frame[1]);   // FC16
            Assert.Equal(0x00, frame[4]);   // 寄存器数高
            Assert.Equal(0x02, frame[5]);   // 寄存器数低=2
            Assert.Equal(0x04, frame[6]);   // 字节数=4
        }

        // ============ TCP帧(MBAP) ============

        [Fact]
        public void TCP读帧_MBAP七字节头()
        {
            var frame = ModbusFrameHelper.BuildReadTcp(0x0001, 1, 3, 0x0000, 2);
            Assert.Equal(0x00, frame[0]);   // TID高
            Assert.Equal(0x01, frame[1]);   // TID低
            Assert.Equal(0x00, frame[2]);   // 协议标识高
            Assert.Equal(0x00, frame[3]);   // 协议标识低
            Assert.Equal(0x00, frame[4]);   // 长度高
            Assert.Equal(0x06, frame[5]);   // 长度低=6
            Assert.Equal(0x01, frame[6]);   // 单元标识
            Assert.Equal(0x03, frame[7]);   // 功能码
        }

        [Fact]
        public void TCP往返_取TID单元功能码()
        {
            var frame = ModbusFrameHelper.BuildReadTcp(0x1234, 5, 4, 0x0010, 8);
            Assert.True(ModbusFrameHelper.TryParseTcp(frame, out var tid, out var unit, out var func, out _));
            Assert.Equal(0x1234, tid);
            Assert.Equal(5, unit);
            Assert.Equal(4, func);
        }

        [Fact]
        public void TCP解析_协议标识非零_返回false()
        {
            var frame = ModbusFrameHelper.BuildReadTcp(1, 1, 3, 0, 2);
            frame[2] = 0x01;  // 破坏协议标识
            Assert.False(ModbusFrameHelper.TryParseTcp(frame, out _, out _, out _, out _));
        }

        [Fact]
        public void TCP解析_长度不足_返回false()
        {
            Assert.False(ModbusFrameHelper.TryParseTcp(new byte[] { 0x00, 0x01, 0x00 }, out _, out _, out _, out _));
        }

        [Fact]
        public void TCP写单寄存器_功能码06()
        {
            var frame = ModbusFrameHelper.BuildWriteSingleTcp(1, 1, 0x0010, 0x1234);
            Assert.Equal(0x06, frame[7]);
            Assert.Equal(0x12, frame[10]);
            Assert.Equal(0x34, frame[11]);
        }

        [Fact]
        public void TCP写多寄存器_功能码16()
        {
            var regbytes = new byte[] { 0xAA, 0xBB };
            var frame = ModbusFrameHelper.BuildWriteMultiTcp(1, 1, 0x0010, regbytes);
            Assert.Equal(0x10, frame[7]);
            Assert.Equal(0x01, frame[6]);   // 单元
            Assert.Equal(0x01, frame[11]);  // 寄存器数=1
            Assert.Equal(0x02, frame[12]);  // 字节数=2
        }

        [Fact]
        public void TCP解析_异常应答_功能码带0x80()
        {
            // 手工构造异常应答:TID 0000 len=3 unit=1 func=0x83 errcode=2
            var frame = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x03, 0x01, 0x83, 0x02 };
            Assert.True(ModbusFrameHelper.TryParseTcp(frame, out _, out _, out var func, out var data));
            Assert.Equal(0x83, func);       // 3|0x80
            Assert.Equal(0x02, data[0]);    // 异常码
        }
    }
}
#endif
