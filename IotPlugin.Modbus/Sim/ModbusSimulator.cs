using IotDriverCore;

namespace IotPlugin.Modbus.Sim
{
    /// <summary>Modbus插件的模拟人格(独立端口起FluentModbus从站,平台采集器可连上读假数据)</summary>
    public sealed class ModbusSimulator : ISimulatable
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, (ModbusSlave slave, SimStatus status)> _sims = new();

        public SimCapability Capability => new()
        {
            SupportSlave = true,
            SupportSelfTest = false,
            DefaultPort = 502,
            Protocol = "modbus"
        };

        public Action<SimLogEntry>? OnSimLog { get; set; }

        public Task<SimStatus> StartSimAsync(SimStartRequest request, CancellationToken ct)
        {
            string simId = $"modbus-{request.Port}-{Guid.NewGuid():N}".Substring(0, 20);
            var slave = new ModbusSlave(request.Port, request.Devices)
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

        /// <summary>停止所有从站实例(插件停止时调用,确保刷新循环停止、端口释放)</summary>
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
            // Modbus从站为FluentModbus托管寄存器区,故障注入本期不适用(仅串行协议支持),空实现
            return Task.CompletedTask;
        }

        // 本模拟器无插件配置(_config)可查,DeviceTypeCodes路由判定在ModbusPlugin(持有_config)实现,
        // SimulatorController按插件实例路由,不会调用此处
        public bool OwnsDeviceType(string deviceTypeCode) => false;
    }
}
