using IotDriverCore;

namespace IotPlugin.S7.Sim
{
    /// <summary>S7插件的模拟人格(独立端口起手写S7comm从站,平台采集器可连上读假数据;
    /// 从站编码手写而插件客户端为S7netplus库,两套实现天然独立)</summary>
    public sealed class S7Simulator : ISimulatable
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, (S7Slave Slave, SimStatus Status)> _sims = new();

        public SimCapability Capability => new()
        {
            SupportSlave = true,
            SupportSelfTest = false,
            DefaultPort = 102,
            Protocol = "s7"
        };

        public Action<SimLogEntry>? OnSimLog { get; set; }

        public Task<SimStatus> StartSimAsync(SimStartRequest request, CancellationToken ct)
        {
            string simId = $"s7-{request.Port}-{Guid.NewGuid():N}".Substring(0, 20);
            var slave = new S7Slave(request.Port, request.Devices)
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
                foreach (var entry in _sims.Values) entry.Slave.Stop();
                _sims.Clear();
            }
        }

        public Task StopSimAsync(string simId)
        {
            lock (_lock)
            {
                if (_sims.Remove(simId, out var entry)) entry.Slave.Stop();
            }
            return Task.CompletedTask;
        }

        public IReadOnlyList<SimStatus> ListSims()
        {
            lock (_lock) { return _sims.Values.Select(v => v.Status).ToList(); }
        }

        public Task InjectFaultAsync(string simId, SimFaultSpec fault)
        {
            // 从站按协议状态应答,错帧/超时类故障注入本期不适用(与Modbus同口径),空实现
            return Task.CompletedTask;
        }

        // DeviceTypeCodes路由判定在S7Plugin(持有_config)实现,SimulatorController按插件实例路由
        public bool OwnsDeviceType(string deviceTypeCode) => false;
    }
}
