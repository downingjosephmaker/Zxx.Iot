using IotDriverCore;

namespace IotPlugin.Iec104.Sim
{
    /// <summary>IEC104插件的模拟人格(独立端口起104从站,平台主站可连上做端到端联调——
    /// 方案§6明言IEC104是最需要模拟器的协议,现场很难借到真实RTU)</summary>
    public sealed class Iec104Simulator : ISimulatable
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, (Iec104Slave slave, SimStatus status)> _sims = new();

        public SimCapability Capability => new()
        {
            SupportSlave = true,
            SupportSelfTest = false,
            DefaultPort = 2404,
            Protocol = "iec104"
        };

        public Action<SimLogEntry>? OnSimLog { get; set; }

        public Task<SimStatus> StartSimAsync(SimStartRequest request, CancellationToken ct)
        {
            string simId = $"iec104-{request.Port}-{Guid.NewGuid():N}".Substring(0, 20);
            var slave = new Iec104Slave(request.Port, request.Devices)
            {
                OnLog = msg => OnSimLog?.Invoke(new SimLogEntry
                {
                    SimId = simId, Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    Direction = "", Hex = "", Note = msg
                })
            };
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
            try
            {
                slave.Start();
            }
            catch (Exception ex)
            {
                slave.Dispose();
                status.Running = false;
                status.Message = "端口占用或启动失败:" + ex.Message;
                return Task.FromResult(status);
            }
            lock (_lock) { _sims[simId] = (slave, status); }
            return Task.FromResult(status);
        }

        /// <summary>停止所有从站实例(插件停止时调用,确保上报循环停止、端口释放)</summary>
        public void StopAll()
        {
            lock (_lock)
            {
                foreach (var entry in _sims.Values) entry.slave.Stop();
                _sims.Clear();
            }
        }

        public Task StopSimAsync(string simId)
        {
            lock (_lock)
            {
                if (_sims.Remove(simId, out var entry)) entry.slave.Stop();
            }
            return Task.CompletedTask;
        }

        public IReadOnlyList<SimStatus> ListSims()
        {
            lock (_lock) { return _sims.Values.Select(v => v.status).ToList(); }
        }

        public Task InjectFaultAsync(string simId, SimFaultSpec fault)
        {
            // 104从站按协议状态机应答,错帧/超时类故障注入本期不适用,空实现(与Modbus同口径)
            return Task.CompletedTask;
        }

        // DeviceTypeCodes路由判定在Iec104Plugin(持有_config)实现,SimulatorController按插件实例路由
        public bool OwnsDeviceType(string deviceTypeCode) => false;
    }
}
