using System.Net;
using FluentModbus;
using IotDriverCore;
using IotDriverCore.Simulation;

namespace IotPlugin.Modbus.Sim
{
    /// <summary>
    /// Modbus TCP从站(直接用FluentModbus的ModbusTcpServer托管寄存器区,§4.3既定选型;
    /// 保持寄存器(FC03)与输入寄存器(FC04)按点位配置周期刷新;
    /// 寄存器线序为大端(ABCD),对应插件ModbusValueCodec的默认字节序;
    /// 从站号(unitId)由FluentModbus默认接受;RTU over TCP见ModbusRtuSlaveSpike说明)
    /// </summary>
    public sealed class ModbusSlave : IDisposable
    {
        private readonly ModbusTcpServer _server = new();
        private readonly int _port;
        private readonly List<RegisterPoint> _points = new();

        /// <summary>响应的从站号集合(设备address=从站号;FluentModbus默认仅0,须逐个AddUnit)</summary>
        private readonly HashSet<byte> _unitIds = new();

        private CancellationTokenSource? _cts;

        /// <summary>日志回调</summary>
        public Action<string>? OnLog { get; set; }

        public ModbusSlave(int port, IEnumerable<SimDevice> devices)
        {
            _port = port;
            foreach (var device in devices)
            {
                // 设备address即Modbus从站号(unit id),FluentModbus每unit独立寄存器区
                byte unit = (byte)ParseUint(device.Address);
                _unitIds.Add(unit);
                foreach (var pm in device.Points)
                {
                    _points.Add(new RegisterPoint
                    {
                        UnitId = unit,
                        FuncCode = pm.FuncCode <= 0 ? 3 : pm.FuncCode,
                        Address = (int)ParseUint(pm.Di),
                        DataType = (pm.DataType ?? "uint16").Trim().ToLowerInvariant(),
                        RegLength = InferRegLength(pm.DataType, pm.Length),
                        Generator = GeneratorFactory.Create(pm.Generator),
                        Scale = pm.Scale
                    });
                }
            }
        }

        /// <summary>
        /// 启动从站(先Start再AddUnit——FluentModbus约束AddUnit须在Start后;
        /// 每个从站号对应独立寄存器区,刷新循环按unit写入)
        /// </summary>
        public void Start()
        {
            _server.Start(new IPEndPoint(IPAddress.Any, _port));
            // FluentModbus约束:AddUnit须在Start之后;unit=0默认存在,重复添加抛异常故幂等处理
            foreach (var unit in _unitIds)
            {
                if (unit == 0) continue;
                try { _server.AddUnit(unit); } catch { /* 已存在,忽略 */ }
            }
            OnLog?.Invoke($"Modbus TCP从站监听启动，端口 {_port}，响应从站号 [{string.Join(",", _unitIds.OrderBy(u => u))}]");
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => RefreshLoopAsync(_cts.Token));
        }

        /// <summary>
        /// 停止从站
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
            try { _server.Stop(); } catch { }
        }

        public void Dispose() => Stop();

        /// <summary>
        /// 寄存器刷新循环(每秒按生成器更新一次,与Modbus客户端读取用server.Lock互斥)
        /// </summary>
        private async Task RefreshLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var now = DateTime.Now;
                    lock (_server.Lock)
                    {
                        foreach (var point in _points) WritePoint(point, now);
                    }
                    await Task.Delay(1000, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { OnLog?.Invoke($"寄存器刷新异常:{ex.Message}"); }
        }

        /// <summary>
        /// 把一个点位的当前值按数据类型大端写入对应寄存器区(FC03保持/FC04输入)
        /// </summary>
        private void WritePoint(RegisterPoint point, DateTime now)
        {
            var registers = point.FuncCode == 4
                ? _server.GetInputRegisters(point.UnitId)
                : _server.GetHoldingRegisters(point.UnitId);
            double value = point.Generator.Next(now) / point.Scale;
            switch (point.DataType)
            {
                case "int16":
                    registers.SetBigEndian(point.Address, (short)Math.Round(value));
                    break;
                case "int32":
                    registers.SetBigEndian(point.Address, (int)Math.Round(value));
                    break;
                case "uint32":
                    registers.SetBigEndian(point.Address, (uint)Math.Round(value));
                    break;
                case "float32":
                    registers.SetBigEndian(point.Address, (float)value);
                    break;
                case "float64":
                    registers.SetBigEndian(point.Address, value);
                    break;
                case "int64":
                    registers.SetBigEndian(point.Address, (long)Math.Round(value));
                    break;
                default:  // uint16
                    registers.SetBigEndian(point.Address, (ushort)Math.Round(value));
                    break;
            }
        }

        /// <summary>
        /// 按数据类型推导寄存器数(与插件ModbusValueCodec.InferRegLength同口径)
        /// </summary>
        private static int InferRegLength(string? datatype, int configLength)
        {
            if (configLength > 0) return configLength;
            return (datatype ?? "").Trim().ToLowerInvariant() switch
            {
                "int32" or "uint32" or "float32" => 2,
                "int64" or "float64" => 4,
                _ => 1
            };
        }

        private static uint ParseUint(string text)
        {
            text = (text ?? "").Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Convert.ToUInt32(text[2..], 16);
            return uint.TryParse(text, out var v) ? v : 0;
        }

        /// <summary>
        /// 寄存器点位运行态
        /// </summary>
        private sealed class RegisterPoint
        {
            public byte UnitId;
            public int FuncCode;
            public int Address;
            public string DataType = "uint16";
            public int RegLength;
            public IValueGenerator Generator = null!;
            public double Scale = 1;
        }
    }
}
