using IotDriverCore;
using IotDriverCore.Simulation;

namespace IotPlugin.Cjt188.Sim
{
    /// <summary>CJ/T188插件的模拟人格(独立端口监听,收主站请求帧→逐从站编应答→故障装饰→回发)</summary>
    public sealed class Cjt188Simulator : ISimulatable
    {
        private sealed class SimInstance
        {
            public TcpServerChannel Channel = null!;
            public FrameAccumulator Accumulator = null!;
            public List<IProtocolSlave> Slaves = new();
            public Dictionary<IProtocolSlave, FaultInjector> Injectors = new();
            public SimStatus Status = null!;
        }

        private readonly object _lock = new();
        private readonly Dictionary<string, SimInstance> _sims = new();

        public SimCapability Capability => new()
        {
            SupportSlave = true, SupportSelfTest = false, DefaultPort = 9188, Protocol = "cjt188"
        };

        public Action<SimLogEntry>? OnSimLog { get; set; }

        public Task<SimStatus> StartSimAsync(SimStartRequest request, CancellationToken ct)
        {
            string simId = $"cjt188-{request.Port}-{Guid.NewGuid():N}"[..22];
            var inst = new SimInstance
            {
                Accumulator = new FrameAccumulator(FrameAccumulator.ExtractCjt188),
                Status = new SimStatus
                {
                    SimId = simId, Mode = SimMode.Slave, Running = true, Port = request.Port,
                    StartedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Message = "已启动"
                }
            };
            foreach (var dev in request.Devices)
            {
                var slave = new Cjt188Slave(dev);
                inst.Slaves.Add(slave);
                inst.Injectors[slave] = new FaultInjector(dev.Faults, slave.Corrupt);
            }
            inst.Channel = new TcpServerChannel(request.Port)
            {
                FrameReceived = (ep, data) => OnInbound(simId, inst, ep, data)
            };
            inst.Channel.Start();
            lock (_lock) { _sims[simId] = inst; }
            return Task.FromResult(inst.Status);
        }

        private void OnInbound(string simId, SimInstance inst, string endpoint, byte[] data)
        {
            var now = DateTime.Now;
            foreach (var frame in inst.Accumulator.Push(endpoint, data))
            {
                Log(simId, "←", frame, "收到请求");
                foreach (var slave in inst.Slaves)
                {
                    var reply = slave.HandleFrame(frame, now);
                    if (reply == null) continue;
                    var decision = inst.Injectors[slave].Decorate(reply);
                    if (decision.Drop) { Log(simId, "→", Array.Empty<byte>(), $"从站[{slave.Address}]故障丢弃"); break; }
                    foreach (var seg in decision.Segments)
                    {
                        inst.Channel.Send(endpoint, seg);
                        Log(simId, "→", seg, $"从站[{slave.Address}]应答");
                        if (decision.DelayMs > 0) Thread.Sleep(decision.DelayMs);
                    }
                    break;
                }
            }
        }

        private void Log(string simId, string dir, byte[] frame, string note) =>
            OnSimLog?.Invoke(new SimLogEntry
            {
                SimId = simId, Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                Direction = dir, Hex = frame.Length > 0 ? Convert.ToHexString(frame) : "", Note = note
            });

        public Task StopSimAsync(string simId)
        {
            lock (_lock)
            {
                if (_sims.Remove(simId, out var inst)) inst.Channel.Dispose();
            }
            return Task.CompletedTask;
        }

        public IReadOnlyList<SimStatus> ListSims()
        {
            lock (_lock) { return _sims.Values.Select(v => v.Status).ToList(); }
        }

        public Task InjectFaultAsync(string simId, SimFaultSpec fault)
        {
            lock (_lock)
            {
                if (!_sims.TryGetValue(simId, out var inst)) return Task.CompletedTask;
                var model = fault.Kind.Length == 0 ? new List<FaultModel>() :
                    new List<FaultModel> { new() { Type = fault.Kind, Probability = fault.Probability, DelayMs = fault.DelayMs } };
                foreach (var slave in inst.Slaves)
                    inst.Injectors[slave] = new FaultInjector(model, slave.Corrupt);
            }
            return Task.CompletedTask;
        }
    }
}
