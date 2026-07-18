#if PLUGIN_INTERNALS
using IotDriverCore;
using IotDriverCore.Simulation;
using IotPlugin.S7.Sim;
using S7.Net;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// S7从站模拟器对抗性回环测试(从站S7comm编码手写,测试用插件同款S7netplus客户端库
    /// 经真实TCP驱动,两套实现互为裁判;覆盖:COTP+Setup握手/DB区float32与位读/
    /// M区读/写字节跨刷新周期保持/位写生效)
    /// </summary>
    public class S7SimTests
    {
        /// <summary>
        /// 取一个空闲端口
        /// </summary>
        private static int GetFreePort()
        {
            var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            probe.Start();
            int port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        /// <summary>
        /// 组一台模拟PLC:DB1.DBD4 float32=25.5 / DB1.DBX0.1 bool=1 / MW100 uint16=1234
        /// (Di=ParamAddr语义:DB号×1000000+字节地址,M区DB=0)
        /// </summary>
        private static SimDevice BuildDevice() => new()
        {
            Address = "1",
            DeviceTypeCode = "s7test",
            Points = new List<SimPoint>
            {
                new() { ParamCode = "temp", Di = "1000004", FuncCode = 1, DataType = "float32", Generator = new GeneratorModel { Type = "constant", Base = 25.5 } },
                new() { ParamCode = "runbit", Di = "1000000", FuncCode = 1, DataType = "bool", BitOffset = 1, Generator = new GeneratorModel { Type = "constant", Base = 1 } },
                new() { ParamCode = "counter", Di = "100", FuncCode = 2, DataType = "uint16", Generator = new GeneratorModel { Type = "constant", Base = 1234 } }
            }
        };

        [Fact]
        public async Task 握手_DB区float与位_M区uint16_客户端库裁判()
        {
            int port = GetFreePort();
            using var slave = new S7Slave(port, new[] { BuildDevice() });
            slave.Start();
            using var plc = new Plc(CpuType.S71200, "127.0.0.1", port, 0, 0);
            await plc.OpenAsync();
            Assert.True(plc.IsConnected);

            // DB1.DBD4 float32(S7大端)
            var tempbytes = await plc.ReadBytesAsync(DataType.DataBlock, 1, 4, 4);
            float temp = BitConverter.Int32BitsToSingle(
                (tempbytes[0] << 24) | (tempbytes[1] << 16) | (tempbytes[2] << 8) | tempbytes[3]);
            Assert.Equal(25.5f, temp, 3);

            // DB1.DBX0.1 位=1
            var bitbyte = await plc.ReadBytesAsync(DataType.DataBlock, 1, 0, 1);
            Assert.Equal(1, (bitbyte[0] >> 1) & 1);

            // MW100 uint16=1234
            var mem = await plc.ReadBytesAsync(DataType.Memory, 0, 100, 2);
            Assert.Equal(1234, (mem[0] << 8) | mem[1]);

            plc.Close();
            slave.Stop();
        }

        [Fact]
        public async Task 写字节_跨刷新周期保持_不被生成器覆盖()
        {
            int port = GetFreePort();
            using var slave = new S7Slave(port, new[] { BuildDevice() });
            slave.Start();
            using var plc = new Plc(CpuType.S71200, "127.0.0.1", port, 0, 0);
            await plc.OpenAsync();

            // 写DB1.DBD4=30.25(大端)
            uint raw = (uint)BitConverter.SingleToInt32Bits(30.25f);
            await plc.WriteBytesAsync(DataType.DataBlock, 1, 4,
                new[] { (byte)(raw >> 24), (byte)(raw >> 16), (byte)(raw >> 8), (byte)raw });

            await Task.Delay(1500);   // 跨过一个刷新周期,验证写入值未被生成器覆盖
            var readback = await plc.ReadBytesAsync(DataType.DataBlock, 1, 4, 4);
            float value = BitConverter.Int32BitsToSingle(
                (readback[0] << 24) | (readback[1] << 16) | (readback[2] << 8) | readback[3]);
            Assert.Equal(30.25f, value, 3);

            plc.Close();
            slave.Stop();
        }

        [Fact]
        public async Task 位写_置位与清位均生效()
        {
            int port = GetFreePort();
            using var slave = new S7Slave(port, new[] { BuildDevice() });
            slave.Start();
            using var plc = new Plc(CpuType.S71200, "127.0.0.1", port, 0, 0);
            await plc.OpenAsync();

            // 置位DB1.DBX0.3
            await plc.WriteBitAsync(DataType.DataBlock, 1, 0, 3, true);
            var afterset = await plc.ReadBytesAsync(DataType.DataBlock, 1, 0, 1);
            Assert.Equal(1, (afterset[0] >> 3) & 1);

            // 清位后复读
            await plc.WriteBitAsync(DataType.DataBlock, 1, 0, 3, false);
            var afterclear = await plc.ReadBytesAsync(DataType.DataBlock, 1, 0, 1);
            Assert.Equal(0, (afterclear[0] >> 3) & 1);

            plc.Close();
            slave.Stop();
        }
    }
}
#endif
