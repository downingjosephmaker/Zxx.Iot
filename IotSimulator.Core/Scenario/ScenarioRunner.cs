using IotDriverCore;
using IotSimulator.Core.Faults;
using IotSimulator.Core.Slaves;
using IotSimulator.Core.Transport;

namespace IotSimulator.Core.Scenario
{
    /// <summary>
    /// 场景运行器(把从站+传输+故障注入串成一台虚拟设备:
    /// 串行协议(645/188)——收主站请求帧→FrameAccumulator切帧→逐从站尝试编应答→故障装饰→回发;
    /// Modbus TCP——FluentModbus托管,运行器仅管生命周期;
    /// 切帧复用IotDriverCore.FrameAccumulator的三提取器,不复用插件FrameHelper)
    /// </summary>
    public sealed class ScenarioRunner : IDisposable
    {
        private readonly ScenarioModel _scenario;
        private readonly Action<string> _log;

        private SimTcpClient? _dialClient;
        private SimTcpServer? _listenServer;
        private ModbusSlave? _modbusSlave;

        /// <summary>串行从站清单(645/188)</summary>
        private readonly List<IProtocolSlave> _slaves = new();

        /// <summary>从站→故障注入器(设备级故障)</summary>
        private readonly Dictionary<IProtocolSlave, FaultInjector> _injectors = new();

        /// <summary>切帧器(按协议选提取器;每连接一份缓冲由endpoint键区分)</summary>
        private FrameAccumulator? _accumulator;

        /// <summary>协议是否为Modbus TCP(走FluentModbus分支)</summary>
        private bool _isModbus;

        public ScenarioRunner(ScenarioModel scenario, Action<string>? log = null)
        {
            _scenario = scenario;
            _log = log ?? (_ => { });
        }

        /// <summary>
        /// 启动场景
        /// </summary>
        public void Start()
        {
            string protocol = (_scenario.Protocol ?? "").Trim().ToLowerInvariant();
            _isModbus = protocol.StartsWith("modbus");

            if (_isModbus)
            {
                StartModbus();
                return;
            }

            // 645/188:构造串行从站与切帧器
            bool is1997 = protocol.Contains("1997");
            foreach (var device in _scenario.Devices)
            {
                IProtocolSlave slave = protocol.StartsWith("cjt188") || protocol.StartsWith("188")
                    ? new Cjt188Slave(device)
                    : new Dlt645Slave(device, is1997);
                _slaves.Add(slave);
                _injectors[slave] = new FaultInjector(device.Faults, slave.Corrupt);
            }
            _accumulator = new FrameAccumulator(
                protocol.StartsWith("cjt188") || protocol.StartsWith("188")
                    ? FrameAccumulator.ExtractCjt188
                    : FrameAccumulator.ExtractDlt645);

            if (_scenario.Transport.Mode.Trim().Equals("listen", StringComparison.OrdinalIgnoreCase))
                StartListen();
            else
                StartDialIn();
        }

        /// <summary>
        /// 停止场景并释放资源
        /// </summary>
        public void Stop()
        {
            _dialClient?.Stop();
            _listenServer?.Stop();
            _modbusSlave?.Stop();
        }

        public void Dispose() => Stop();

        #region Modbus分支

        private void StartModbus()
        {
            _modbusSlave = new ModbusSlave(_scenario.Transport.Port, _scenario.Devices) { OnLog = _log };
            _modbusSlave.Start();
            _log($"场景[{_scenario.Name}] Modbus TCP从站已启动，{_scenario.Devices.Sum(d => d.Points.Count)}个寄存器点位。");
        }

        #endregion

        #region 串行协议分支(645/188)

        /// <summary>
        /// 拨入模式:连接插件TcpServerChannel,先发注册包,收帧编应答
        /// </summary>
        private void StartDialIn()
        {
            var t = _scenario.Transport;
            byte[]? regPacket = string.IsNullOrEmpty(t.RegisterPacket)
                ? null
                : System.Text.Encoding.ASCII.GetBytes(t.RegisterPacket);
            byte[]? heartbeat = t.Heartbeat != null && !string.IsNullOrEmpty(t.Heartbeat.Hex)
                ? HexToBytes(t.Heartbeat.Hex)
                : null;
            int heartInterval = t.Heartbeat?.IntervalMs ?? 30000;

            _dialClient = new SimTcpClient(t.Host, t.Port, regPacket, heartbeat, heartInterval)
            {
                OnLog = _log,
                OnConnected = () => _log($"场景[{_scenario.Name}] 已拨入 {t.Host}:{t.Port}，挂载 {_slaves.Count} 个从站。"),
                OnFrame = data => HandleInbound("dial", data, frame => _dialClient!.Send(frame))
            };
            _dialClient.Start();
        }

        /// <summary>
        /// 被拨模式:监听等插件拨入,收帧编应答
        /// </summary>
        private void StartListen()
        {
            var t = _scenario.Transport;
            _listenServer = new SimTcpServer(t.Port)
            {
                OnLog = _log,
                OnConnected = key => _log($"场景[{_scenario.Name}] 插件拨入 {key}，挂载 {_slaves.Count} 个从站。"),
                OnFrame = (key, data) => HandleInbound(key, data, frame => _listenServer!.Send(key, frame))
            };
            _listenServer.Start();
        }

        /// <summary>
        /// 入站数据处理:切帧→逐从站尝试→命中即编应答→故障装饰→回发
        /// </summary>
        private void HandleInbound(string endpoint, byte[] data, Func<byte[], bool> send)
        {
            if (_accumulator == null) return;
            var now = DateTime.Now;
            foreach (var frame in _accumulator.Push(endpoint, data))
            {
                _log($"← 收到请求 {ToHex(frame)}");
                foreach (var slave in _slaves)
                {
                    var reply = slave.HandleFrame(frame, now);
                    if (reply == null) continue;

                    var decision = _injectors[slave].Decorate(reply);
                    if (decision.Drop)
                    {
                        _log($"  [故障:超时] 从站[{slave.Address}]丢弃应答");
                        break;
                    }
                    if (decision.Segments.Count == 0) break;  // 粘包缓存中,本轮不发
                    SendSegments(decision, send, slave.Address);
                    break;  // 一帧只由第一个命中的从站应答
                }
            }
        }

        /// <summary>
        /// 按故障决定发送应答分片(半包分段延迟;粘包整包)
        /// </summary>
        private void SendSegments(FaultDecision decision, Func<byte[], bool> send, string slaveAddr)
        {
            for (int i = 0; i < decision.Segments.Count; i++)
            {
                send(decision.Segments[i]);
                _log($"  → 从站[{slaveAddr}]应答{(decision.Segments.Count > 1 ? $"[分片{i + 1}/{decision.Segments.Count}]" : "")} {ToHex(decision.Segments[i])}");
                if (decision.DelayMs > 0 && i < decision.Segments.Count - 1)
                    Thread.Sleep(decision.DelayMs);
            }
        }

        #endregion

        #region 工具

        private static string ToHex(byte[] data) => Convert.ToHexString(data);

        private static byte[] HexToBytes(string hex)
        {
            hex = new string(hex.Where(Uri.IsHexDigit).ToArray());
            if (hex.Length % 2 != 0) hex = "0" + hex;
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }

        #endregion
    }
}
