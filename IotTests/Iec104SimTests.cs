#if PLUGIN_INTERNALS
using System.Net;
using System.Net.Sockets;
using IotDriverCore;
using IotDriverCore.Simulation;
using IotPlugin.Iec104;
using IotPlugin.Iec104.Sim;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// IEC104从站模拟器对抗性回环测试(铁律:从站Iec104Slave编解码独立实现,
    /// 测试用主站Iec104FrameHelper驱动从站,两套编解码互为裁判——共用代码即自证;
    /// 覆盖STARTDT握手/总召唤全量/遥控SBO选择执行链/发送序号递增)
    /// </summary>
    public class Iec104SimTests
    {
        /// <summary>
        /// 取一个空闲端口(先绑0取系统分配再释放,测试场景可接受微小竞态)
        /// </summary>
        private static int GetFreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        /// <summary>
        /// 组一台模拟设备(CA=1):IOA=100短浮点25.5 / IOA=200单点1 / IOA=300单点0(遥控目标)
        /// </summary>
        private static SimDevice BuildDevice() => new()
        {
            Address = "1",
            DeviceTypeCode = "iec104test",
            Points = new List<SimPoint>
            {
                new() { ParamCode = "temp", Di = "100", FuncCode = 13, Generator = new GeneratorModel { Type = "constant", Base = 25.5 } },
                new() { ParamCode = "alarmbit", Di = "200", FuncCode = 1, Generator = new GeneratorModel { Type = "constant", Base = 1 } },
                new() { ParamCode = "switch1", Di = "300", FuncCode = 1, Generator = new GeneratorModel { Type = "constant", Base = 0 } }
            }
        };

        /// <summary>
        /// 从流中读一个完整104帧(0x68+长度域)
        /// </summary>
        private static byte[] ReadFrame(NetworkStream stream)
        {
            int start = stream.ReadByte();
            int len = stream.ReadByte();
            Assert.Equal(0x68, start);
            Assert.InRange(len, 4, 253);
            var rest = new byte[len];
            int offset = 0;
            while (offset < len)
            {
                int n = stream.Read(rest, offset, len - offset);
                Assert.True(n > 0, "连接被从站关闭");
                offset += n;
            }
            return new byte[] { (byte)start, (byte)len }.Concat(rest).ToArray();
        }

        /// <summary>
        /// 读到下一个I帧(跳过从站的S帧确认),用主站编解码解析
        /// </summary>
        private static (Iec104Apci Apci, Iec104Asdu Asdu) ReadNextI(NetworkStream stream)
        {
            for (int guard = 0; guard < 50; guard++)
            {
                var frame = ReadFrame(stream);
                Assert.True(Iec104FrameHelper.TryParseApci(frame, out var apci), "主站编解码无法解析从站帧:" + Convert.ToHexString(frame));
                if (apci.Kind != 'I') continue;
                Assert.True(Iec104FrameHelper.TryParseAsdu(apci.Asdu, out var asdu), "主站编解码无法解析从站ASDU:" + Convert.ToHexString(apci.Asdu));
                return (apci, asdu);
            }
            throw new Xunit.Sdk.XunitException("50帧内未等到I帧");
        }

        /// <summary>
        /// 建立连接并完成STARTDT握手,返回(客户端,流)
        /// </summary>
        private static (TcpClient Client, NetworkStream Stream) Handshake(int port)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, port);
            var stream = client.GetStream();
            stream.ReadTimeout = 5000;
            stream.Write(Iec104FrameHelper.BuildUFrame(Iec104FrameHelper.StartDtAct));
            var frame = ReadFrame(stream);
            Assert.True(Iec104FrameHelper.TryParseApci(frame, out var apci));
            Assert.Equal('U', apci.Kind);
            Assert.Equal(Iec104FrameHelper.StartDtCon, apci.UCtrl);
            return (client, stream);
        }

        [Fact]
        public void 握手_总召唤_全量数据往返_主站编解码裁判()
        {
            int port = GetFreePort();
            using var slave = new Iec104Slave(port, new[] { BuildDevice() }) { SpontaneousIntervalS = 3600 };
            slave.Start();
            var (client, stream) = Handshake(port);
            using var clientguard = client;

            // 主站发总召唤(CA=1)
            stream.Write(Iec104FrameHelper.BuildIFrame(0, 0, Iec104FrameHelper.BuildInterrogation(1)));

            // 期望:激活确认→数据帧×3(COT=20)→激活终止;从站I帧发送序号从0递增
            var (apci, asdu) = ReadNextI(stream);
            Assert.Equal(Iec104FrameHelper.TiInterrogation, asdu.Ti);
            Assert.Equal(Iec104FrameHelper.CotActivationCon, asdu.Cot);
            Assert.False(asdu.Negative);
            Assert.Equal(0, apci.Ns);

            var datavalues = new Dictionary<int, string>();
            int expectseq = 1;
            while (true)
            {
                (apci, asdu) = ReadNextI(stream);
                Assert.Equal(expectseq++, apci.Ns);   // 从站发送序号连续递增
                if (asdu.Ti == Iec104FrameHelper.TiInterrogation)
                {
                    Assert.Equal(Iec104FrameHelper.CotActivationTerm, asdu.Cot);
                    break;
                }
                Assert.Equal(Iec104FrameHelper.CotInterrogatedByStation, asdu.Cot);
                Assert.Single(asdu.Items);
                datavalues[asdu.Items[0].Ioa] = asdu.Items[0].Value;
            }

            Assert.Equal(3, datavalues.Count);
            Assert.Equal("25.5", datavalues[100]);   // TI13短浮点经两套编解码往返保真
            Assert.Equal("1", datavalues[200]);      // TI1单点
            Assert.Equal("0", datavalues[300]);
            slave.Stop();
        }

        [Fact]
        public void 遥控SBO_选择执行链_值写入生效()
        {
            int port = GetFreePort();
            using var slave = new Iec104Slave(port, new[] { BuildDevice() }) { SpontaneousIntervalS = 3600 };
            slave.Start();
            var (client, stream) = Handshake(port);
            using var clientguard = client;
            int ns = 0;

            // 选择(S/E=1):期望镜像激活确认且非否定
            stream.Write(Iec104FrameHelper.BuildIFrame(ns++, 0, Iec104FrameHelper.BuildSingleCommand(1, 300, true, true)));
            var (_, asdu) = ReadNextI(stream);
            Assert.Equal(Iec104FrameHelper.TiSingleCommand, asdu.Ti);
            Assert.Equal(Iec104FrameHelper.CotActivationCon, asdu.Cot);
            Assert.False(asdu.Negative);

            // 执行(S/E=0):期望激活确认+激活终止
            stream.Write(Iec104FrameHelper.BuildIFrame(ns++, 0, Iec104FrameHelper.BuildSingleCommand(1, 300, true, false)));
            (_, asdu) = ReadNextI(stream);
            Assert.Equal(Iec104FrameHelper.CotActivationCon, asdu.Cot);
            Assert.False(asdu.Negative);
            (_, asdu) = ReadNextI(stream);
            Assert.Equal(Iec104FrameHelper.CotActivationTerm, asdu.Cot);

            // 复召确认遥控值已写入:IOA=300应从0变1
            stream.Write(Iec104FrameHelper.BuildIFrame(ns++, 0, Iec104FrameHelper.BuildInterrogation(1)));
            string? switchvalue = null;
            while (true)
            {
                (_, asdu) = ReadNextI(stream);
                if (asdu.Ti == Iec104FrameHelper.TiInterrogation && asdu.Cot == Iec104FrameHelper.CotActivationTerm) break;
                if (asdu.Items.Count == 1 && asdu.Items[0].Ioa == 300) switchvalue = asdu.Items[0].Value;
            }
            Assert.Equal("1", switchvalue);
            slave.Stop();
        }

        [Fact]
        public void 遥控_未知IOA_否定确认()
        {
            int port = GetFreePort();
            using var slave = new Iec104Slave(port, new[] { BuildDevice() }) { SpontaneousIntervalS = 3600 };
            slave.Start();
            var (client, stream) = Handshake(port);
            using var clientguard = client;

            stream.Write(Iec104FrameHelper.BuildIFrame(0, 0, Iec104FrameHelper.BuildSingleCommand(1, 999, true, false)));
            var (_, asdu) = ReadNextI(stream);
            Assert.Equal(Iec104FrameHelper.TiSingleCommand, asdu.Ti);
            Assert.True(asdu.Negative);
            slave.Stop();
        }

        [Fact]
        public void TESTFR探活_从站回con()
        {
            int port = GetFreePort();
            using var slave = new Iec104Slave(port, new[] { BuildDevice() }) { SpontaneousIntervalS = 3600 };
            slave.Start();
            var (client, stream) = Handshake(port);
            using var clientguard = client;

            stream.Write(Iec104FrameHelper.BuildUFrame(Iec104FrameHelper.TestFrAct));
            var frame = ReadFrame(stream);
            Assert.True(Iec104FrameHelper.TryParseApci(frame, out var apci));
            Assert.Equal('U', apci.Kind);
            Assert.Equal(Iec104FrameHelper.TestFrCon, apci.UCtrl);
            slave.Stop();
        }
    }
}
#endif
