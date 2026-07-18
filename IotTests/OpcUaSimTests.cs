#if PLUGIN_INTERNALS
using IotDriverCore;
using IotDriverCore.Simulation;
using IotPlugin.OpcUa.Sim;
using Opc.Ua;
using Opc.Ua.Client;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// OPC UA模拟服务器回环测试(客户端以插件同款方式接入:SelectEndpoint(useSecurity:false)+匿名;
    /// 覆盖:启动/读生成器值/写节点后复读生效/停止释放端口;
    /// 协议编解码由官方栈两侧承担,测试验证的是地址空间托管与值刷新语义)
    /// </summary>
    public class OpcUaSimTests
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
        /// 组一台模拟设备:温度节点(常量25.5)+开关节点(常量0,写目标)
        /// </summary>
        private static SimDevice BuildDevice() => new()
        {
            Address = "1",
            DeviceTypeCode = "opcuatest",
            Points = new List<SimPoint>
            {
                new() { ParamCode = "temp", Di = "ns=2;s=Sim.Temp", Generator = new GeneratorModel { Type = "constant", Base = 25.5 } },
                new() { ParamCode = "switch1", Di = "ns=2;s=Sim.Switch", Generator = new GeneratorModel { Type = "constant", Base = 0 } }
            }
        };

        /// <summary>
        /// 构建客户端配置并建会话(与插件BuildApplicationConfiguration同口径:匿名+不校验服务器证书)
        /// </summary>
        private static async Task<Session> ConnectAsync(int port)
        {
            var config = new ApplicationConfiguration
            {
                ApplicationName = "ZxxIotOpcUaSimTest",
                ApplicationUri = "urn:ZxxIot:OpcUaSimTest",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "Config/OpcUaSimTestPki/own",
                        SubjectName = "CN=ZxxIotOpcUaSimTest"
                    },
                    TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = "Config/OpcUaSimTestPki/issuer" },
                    TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = "Config/OpcUaSimTestPki/trusted" },
                    RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = "Config/OpcUaSimTestPki/rejected" },
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15_000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60_000 }
            };
            await config.Validate(ApplicationType.Client);
            config.CertificateValidator.CertificateValidation += (_, e) =>
            {
                if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) e.Accept = true;
            };

            var endpoint = CoreClientUtils.SelectEndpoint(config, $"opc.tcp://127.0.0.1:{port}/ZxxSim", useSecurity: false);
            return await Session.Create(config, new ConfiguredEndpoint(null, endpoint,
                EndpointConfiguration.Create(config)), false, "OpcUaSimTest",
                60_000, new UserIdentity(new AnonymousIdentityToken()), null);
        }

        [Fact]
        public async Task 模拟服务器_匿名None接入_读生成器值_写节点复读生效()
        {
            int port = GetFreePort();
            var simulator = new OpcUaSimulator();
            var status = await simulator.StartSimAsync(new SimStartRequest
            {
                Mode = SimMode.Slave,
                Port = port,
                Devices = new List<SimDevice> { BuildDevice() }
            }, CancellationToken.None);
            Assert.True(status.Running, status.Message);

            try
            {
                using var session = await ConnectAsync(port);

                // 等一个刷新周期后读温度节点:生成器常量25.5应到位
                await Task.Delay(1500);
                var tempvalue = session.ReadValue(NodeId.Parse("ns=2;s=Sim.Temp"));
                Assert.True(StatusCode.IsGood(tempvalue.StatusCode));
                Assert.Equal(25.5, Convert.ToDouble(tempvalue.Value), 3);

                // 写开关节点为1:写回执Good,复读取回写入值(写后生成器停止刷新该点)
                session.Write(null, new WriteValueCollection
                {
                    new WriteValue
                    {
                        NodeId = NodeId.Parse("ns=2;s=Sim.Switch"),
                        AttributeId = Attributes.Value,
                        Value = new DataValue(new Variant(1.0))
                    }
                }, out StatusCodeCollection writeresults, out _);
                Assert.True(StatusCode.IsGood(writeresults[0]), writeresults[0].ToString());

                await Task.Delay(1500);   // 跨过一个刷新周期,验证写入值未被生成器覆盖
                var switchvalue = session.ReadValue(NodeId.Parse("ns=2;s=Sim.Switch"));
                Assert.True(StatusCode.IsGood(switchvalue.StatusCode));
                Assert.Equal(1.0, Convert.ToDouble(switchvalue.Value), 3);

                session.Close();
            }
            finally
            {
                await simulator.StopSimAsync(status.SimId);
            }
        }

        [Fact]
        public async Task 模拟服务器_停止后端口释放_可再次启动()
        {
            int port = GetFreePort();
            var simulator = new OpcUaSimulator();
            var first = await simulator.StartSimAsync(new SimStartRequest
            {
                Mode = SimMode.Slave, Port = port, Devices = new List<SimDevice> { BuildDevice() }
            }, CancellationToken.None);
            Assert.True(first.Running, first.Message);
            await simulator.StopSimAsync(first.SimId);

            var second = await simulator.StartSimAsync(new SimStartRequest
            {
                Mode = SimMode.Slave, Port = port, Devices = new List<SimDevice> { BuildDevice() }
            }, CancellationToken.None);
            Assert.True(second.Running, second.Message);
            await simulator.StopSimAsync(second.SimId);
        }
    }
}
#endif
