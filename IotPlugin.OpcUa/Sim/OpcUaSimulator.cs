using IotDriverCore;
using Opc.Ua;
using Opc.Ua.Configuration;

namespace IotPlugin.OpcUa.Sim
{
    /// <summary>OPC UA插件的模拟人格(独立端口起官方Server栈托管模拟节点,
    /// 端点SecurityPolicy None+匿名认证,与插件客户端SelectEndpoint(useSecurity:false)对齐;
    /// 编解码由官方栈两侧各自承担,协议正确性交给标准实现,模拟器只负责地址空间与值刷新)</summary>
    public sealed class OpcUaSimulator : ISimulatable
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, (ApplicationInstance App, OpcUaSimServer Server, SimStatus Status, string PkiPath)> _sims = new();

        public SimCapability Capability => new()
        {
            SupportSlave = true,
            SupportSelfTest = false,
            DefaultPort = 4840,
            Protocol = "opcua"
        };

        public Action<SimLogEntry>? OnSimLog { get; set; }

        public async Task<SimStatus> StartSimAsync(SimStartRequest request, CancellationToken ct)
        {
            string simId = $"opcua-{request.Port}-{Guid.NewGuid():N}".Substring(0, 20);
            var status = new SimStatus
            {
                SimId = simId,
                DeviceId = 0,
                Mode = SimMode.Slave,
                Running = true,
                Port = request.Port,
                StartedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Message = "已启动"
            };
            // 每实例独立临时证书库:官方栈同进程复用同一证书库会因上一实例释放私钥句柄
            // 报"Cannot access private key",独立库+停止清理彻底绕开
            string pkipath = $"Config/OpcUaSimPki/{simId}";
            try
            {
                var config = new ApplicationConfiguration
                {
                    ApplicationName = "ZxxIotOpcUaSim",
                    ApplicationUri = $"urn:ZxxIot:OpcUaSim:{request.Port}",
                    ApplicationType = ApplicationType.Server,
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier
                        {
                            StoreType = "Directory",
                            StorePath = $"{pkipath}/own",
                            SubjectName = "CN=ZxxIotOpcUaSim"
                        },
                        TrustedIssuerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = $"{pkipath}/issuer"
                        },
                        TrustedPeerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = $"{pkipath}/trusted"
                        },
                        RejectedCertificateStore = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = $"{pkipath}/rejected"
                        },
                        AutoAcceptUntrustedCertificates = true
                    },
                    TransportConfigurations = new TransportConfigurationCollection(),
                    TransportQuotas = new TransportQuotas(),
                    ServerConfiguration = new ServerConfiguration
                    {
                        BaseAddresses = { $"opc.tcp://0.0.0.0:{request.Port}/ZxxSim" },
                        // 与插件客户端对齐:None安全策略+匿名令牌(模拟联调场景,不做加密)
                        SecurityPolicies = new ServerSecurityPolicyCollection
                        {
                            new ServerSecurityPolicy
                            {
                                SecurityMode = MessageSecurityMode.None,
                                SecurityPolicyUri = SecurityPolicies.None
                            }
                        },
                        UserTokenPolicies = new UserTokenPolicyCollection
                        {
                            new UserTokenPolicy(UserTokenType.Anonymous)
                        },
                        DiagnosticsEnabled = false
                    }
                };
                await config.Validate(ApplicationType.Server);

                var application = new ApplicationInstance
                {
                    ApplicationName = config.ApplicationName,
                    ApplicationType = ApplicationType.Server,
                    ApplicationConfiguration = config
                };
                // 服务器证书缺失时自动生成自签名(仅传输层需要,端点本身为None不加密)
                await application.CheckApplicationInstanceCertificates(true);

                var server = new OpcUaSimServer(request.Devices);
                await application.Start(server);

                lock (_lock) { _sims[simId] = (application, server, status, pkipath); }
                OnSimLog?.Invoke(new SimLogEntry
                {
                    SimId = simId,
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    Note = $"OPC UA模拟服务器启动，端点 opc.tcp://0.0.0.0:{request.Port}/ZxxSim，" +
                           $"{request.Devices.Sum(d => d.Points.Count)}个节点(ns=2)，None安全策略+匿名"
                });
            }
            catch (Exception ex)
            {
                status.Running = false;
                status.Message = "端口占用或启动失败:" + ex.Message;
            }
            return status;
        }

        /// <summary>停止所有模拟服务器实例(插件停止时调用,确保端口释放)</summary>
        public void StopAll()
        {
            lock (_lock)
            {
                foreach (var entry in _sims.Values) StopEntry(entry);
                _sims.Clear();
            }
        }

        public Task StopSimAsync(string simId)
        {
            lock (_lock)
            {
                if (_sims.Remove(simId, out var entry)) StopEntry(entry);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止单实例并清理其临时证书库
        /// </summary>
        private static void StopEntry((ApplicationInstance App, OpcUaSimServer Server, SimStatus Status, string PkiPath) entry)
        {
            try { entry.Server.Stop(); } catch { }
            try { entry.Server.Dispose(); } catch { }
            try { if (Directory.Exists(entry.PkiPath)) Directory.Delete(entry.PkiPath, true); } catch { }
        }

        public IReadOnlyList<SimStatus> ListSims()
        {
            lock (_lock) { return _sims.Values.Select(v => v.Status).ToList(); }
        }

        public Task InjectFaultAsync(string simId, SimFaultSpec fault)
        {
            // 官方栈托管协议栈,错帧/超时类故障注入不适用(与Modbus同口径),空实现
            return Task.CompletedTask;
        }

        // DeviceTypeCodes路由判定在OpcUaPlugin(持有_config)实现,SimulatorController按插件实例路由
        public bool OwnsDeviceType(string deviceTypeCode) => false;
    }
}
