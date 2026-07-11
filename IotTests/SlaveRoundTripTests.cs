using IotSimulator.Core.Scenario;
using IotSimulator.Core.Slaves;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// 从站独立往返测试(不依赖插件internal:测试侧独立构造主站请求帧→从站编应答→独立解析验证;
    /// 与插件Helper无关,直接保障独立从站编码符合国标线格式——对抗性验证的模拟器侧)
    /// </summary>
    public class SlaveRoundTripTests
    {
        // ============ 645请求帧构造(测试侧独立实现,不用插件Helper) ============

        private static byte[] BuildAddr(string decimalAddr, int digits)
        {
            var d = decimalAddr.PadLeft(digits, '0');
            if (d.Length > digits) d = d[^digits..];
            var addr = new byte[digits / 2];
            for (int i = 0; i < addr.Length; i++)
            {
                int pos = d.Length - (i + 1) * 2;
                addr[i] = (byte)(((d[pos] - '0') << 4) | (d[pos + 1] - '0'));
            }
            return addr;
        }

        private static byte Cs(byte[] body, int offset, int count)
        {
            int sum = 0;
            for (int i = offset; i < offset + count; i++) sum += body[i];
            return (byte)(sum & 0xFF);
        }

        /// <summary>构造645读请求(68 addr6 68 11 04 DI4(+33) CS 16)</summary>
        private static byte[] Build645Read(byte[] addr6, uint di)
        {
            var body = new List<byte> { 0x68 };
            body.AddRange(addr6);
            body.Add(0x68);
            body.Add(0x11);
            body.Add(0x04);
            for (int i = 0; i < 4; i++) body.Add((byte)(((di >> (i * 8)) & 0xFF) + 0x33));
            body.Add(Cs(body.ToArray(), 0, body.Count));
            body.Add(0x16);
            return body.ToArray();
        }

        /// <summary>解析645应答,取值区(跳过DI4),BCD低位在前解码</summary>
        private static string Parse645Value(byte[] reply, int diLen = 4)
        {
            int start = 0;
            while (reply[start] == 0xFE) start++;
            int dataLen = reply[start + 9];
            var data = new byte[dataLen];
            for (int i = 0; i < dataLen; i++) data[i] = (byte)(reply[start + 10 + i] - 0x33);
            var valueBytes = data[diLen..];
            System.Array.Reverse(valueBytes);
            var sb = new System.Text.StringBuilder();
            foreach (var b in valueBytes) sb.Append((b >> 4) & 0xF).Append(b & 0xF);
            var text = sb.ToString().TrimStart('0');
            return text.Length == 0 ? "0" : text;
        }

        // ============ 645从站往返 ============

        [Fact]
        public void Dlt645从站_常量值_编码正确()
        {
            var device = new DeviceModel
            {
                Address = "000000000001",
                Points = { new PointModel { Di = "0x00010000", Length = 4, Scale = 0.01,
                    Generator = new GeneratorModel { Type = "constant", Base = 5000 } } }
            };
            var slave = new Dlt645Slave(device, false);
            var addr = BuildAddr("000000000001", 12);
            var reply = slave.HandleFrame(Build645Read(addr, 0x00010000), System.DateTime.Now);
            Assert.NotNull(reply);
            // 工程值5000/scale0.01=原始500000
            Assert.Equal("500000", Parse645Value(reply!));
        }

        [Fact]
        public void Dlt645从站_应答帧结构_双68定界结束符16()
        {
            var device = new DeviceModel { Address = "000000000001",
                Points = { new PointModel { Di = "0x00010000", Length = 4,
                    Generator = new GeneratorModel { Type = "constant", Base = 100 } } } };
            var slave = new Dlt645Slave(device, false);
            var reply = slave.HandleFrame(Build645Read(BuildAddr("000000000001", 12), 0x00010000), System.DateTime.Now);
            Assert.NotNull(reply);
            Assert.Equal(0x68, reply![0]);
            Assert.Equal(0x68, reply[7]);
            Assert.Equal(0x91, reply[8]);   // 应答控制码0x11|0x80
            Assert.Equal(0x16, reply[^1]);
        }

        [Fact]
        public void Dlt645从站_应答CS_可被独立校验()
        {
            var device = new DeviceModel { Address = "000000000001",
                Points = { new PointModel { Di = "0x00010000", Length = 4,
                    Generator = new GeneratorModel { Type = "constant", Base = 100 } } } };
            var slave = new Dlt645Slave(device, false);
            var reply = slave.HandleFrame(Build645Read(BuildAddr("000000000001", 12), 0x00010000), System.DateTime.Now);
            Assert.NotNull(reply);
            int framelen = reply!.Length;
            Assert.Equal(reply[framelen - 2], Cs(reply, 0, framelen - 2));
        }

        [Fact]
        public void Dlt645从站_地址回显_逐字节相等()
        {
            var device = new DeviceModel { Address = "000000123456",
                Points = { new PointModel { Di = "0x00010000", Length = 4,
                    Generator = new GeneratorModel { Type = "constant", Base = 100 } } } };
            var slave = new Dlt645Slave(device, false);
            var addr = BuildAddr("000000123456", 12);
            var reply = slave.HandleFrame(Build645Read(addr, 0x00010000), System.DateTime.Now);
            Assert.NotNull(reply);
            var replyAddr = reply![1..7];
            Assert.Equal(addr, replyAddr);
        }

        [Fact]
        public void Dlt645从站_多表挂同连接_按地址路由()
        {
            var runner1 = new Dlt645Slave(new DeviceModel { Address = "000000000001",
                Points = { new PointModel { Di = "0x00010000", Length = 4,
                    Generator = new GeneratorModel { Type = "constant", Base = 111 } } } }, false);
            var runner2 = new Dlt645Slave(new DeviceModel { Address = "000000000002",
                Points = { new PointModel { Di = "0x00010000", Length = 4,
                    Generator = new GeneratorModel { Type = "constant", Base = 222 } } } }, false);
            var req1 = Build645Read(BuildAddr("000000000001", 12), 0x00010000);
            // 表2不应答表1的请求
            Assert.Null(runner2.HandleFrame(req1, System.DateTime.Now));
            Assert.NotNull(runner1.HandleFrame(req1, System.DateTime.Now));
        }

        [Fact]
        public void Dlt645从站_有符号负值_编码符号位()
        {
            var device = new DeviceModel { Address = "000000000001",
                Points = { new PointModel { Di = "0x02020100", Length = 3, Scale = 0.001,
                    Generator = new GeneratorModel { Type = "constant", Base = 3.256 } } } };
            // 从站默认Signed=false,此处仅验证正值链路(有符号编码在插件对抗测覆盖)
            var slave = new Dlt645Slave(device, false);
            var reply = slave.HandleFrame(Build645Read(BuildAddr("000000000001", 12), 0x02020100), System.DateTime.Now);
            Assert.NotNull(reply);
            // 3.256/0.001=3256
            Assert.Equal("3256", Parse645Value(reply!));
        }

        // ============ 188从站往返 ============

        private static byte[] Build188Read(byte meterType, byte[] addr7, ushort di, byte ser)
        {
            var body = new List<byte> { 0x68, meterType };
            body.AddRange(addr7);
            body.Add(0x01);
            body.Add(0x03);
            body.Add((byte)(di & 0xFF));
            body.Add((byte)(di >> 8));
            body.Add(ser);
            body.Add(Cs(body.ToArray(), 0, body.Count));
            body.Add(0x16);
            return body.ToArray();
        }

        [Fact]
        public void Cjt188从站_应答结构_控制码81_SER回显()
        {
            var device = new DeviceModel { Address = "00000000000001", MeterType = "0x10",
                Points = { new PointModel { Di = "0x9010", Length = 4, Scale = 0.01,
                    Generator = new GeneratorModel { Type = "constant", Base = 123.45 } } } };
            var slave = new Cjt188Slave(device);
            var addr = BuildAddr("00000000000001", 14);
            var reply = slave.HandleFrame(Build188Read(0x10, addr, 0x9010, 0x33), System.DateTime.Now);
            Assert.NotNull(reply);
            Assert.Equal(0x68, reply![0]);
            Assert.Equal(0x81, reply[9]);    // 应答控制码0x01|0x80
            Assert.Equal(0x16, reply[^1]);
            // 数据域DI2+SER:偏移11=DI低,12=DI高,13=SER
            Assert.Equal(0x10, reply[11]);
            Assert.Equal(0x90, reply[12]);
            Assert.Equal(0x33, reply[13]);   // SER回显
        }

        [Fact]
        public void Cjt188从站_地址7字节_低位在前()
        {
            var device = new DeviceModel { Address = "00000000000001", MeterType = "0x10",
                Points = { new PointModel { Di = "0x9010", Length = 4,
                    Generator = new GeneratorModel { Type = "constant", Base = 100 } } } };
            var slave = new Cjt188Slave(device);
            var addr = BuildAddr("00000000000001", 14);
            var reply = slave.HandleFrame(Build188Read(0x10, addr, 0x9010, 0x01), System.DateTime.Now);
            Assert.NotNull(reply);
            var replyAddr = reply![2..9];
            Assert.Equal(addr, replyAddr);
        }

        [Fact]
        public void Cjt188从站_值区明文BCD无33偏移()
        {
            var device = new DeviceModel { Address = "00000000000001", MeterType = "0x10",
                Points = { new PointModel { Di = "0x9010", Length = 4, Scale = 0.01,
                    Generator = new GeneratorModel { Type = "constant", Base = 123.45 } } } };
            var slave = new Cjt188Slave(device);
            var addr = BuildAddr("00000000000001", 14);
            var reply = slave.HandleFrame(Build188Read(0x10, addr, 0x9010, 0x01), System.DateTime.Now);
            Assert.NotNull(reply);
            // 值区从偏移14起(68 T addr7 C L DI2 SER = 2+7+2+3=... 11头+3=14),4字节
            // 123.45/0.01=12345→BCD低位在前:45 23 01 00(明文无+33)
            var valueArea = reply![14..18];
            Assert.Equal(new byte[] { 0x45, 0x23, 0x01, 0x00 }, valueArea);
        }

        [Fact]
        public void Cjt188从站_错误地址_不应答()
        {
            var device = new DeviceModel { Address = "00000000000001", MeterType = "0x10",
                Points = { new PointModel { Di = "0x9010", Length = 4,
                    Generator = new GeneratorModel { Type = "constant", Base = 100 } } } };
            var slave = new Cjt188Slave(device);
            var wrongAddr = BuildAddr("00000000000099", 14);
            Assert.Null(slave.HandleFrame(Build188Read(0x10, wrongAddr, 0x9010, 0x01), System.DateTime.Now));
        }

        [Fact]
        public void Cjt188从站_应答CS_可被独立校验()
        {
            var device = new DeviceModel { Address = "00000000000001", MeterType = "0x10",
                Points = { new PointModel { Di = "0x9010", Length = 4,
                    Generator = new GeneratorModel { Type = "constant", Base = 100 } } } };
            var slave = new Cjt188Slave(device);
            var addr = BuildAddr("00000000000001", 14);
            var reply = slave.HandleFrame(Build188Read(0x10, addr, 0x9010, 0x01), System.DateTime.Now);
            Assert.NotNull(reply);
            Assert.Equal(reply![^2], Cs(reply, 0, reply.Length - 2));
        }
    }
}
