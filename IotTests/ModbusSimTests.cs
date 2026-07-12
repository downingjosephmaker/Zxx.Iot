using FluentModbus;
using IotDriverCore;
using IotDriverCore.Simulation;
using IotPlugin.Modbus.Sim;
using System.Net;
using Xunit;

public class ModbusSimTests
{
    [Fact]
    public async Task ModbusSimulator_StartStop_Roundtrip()
    {
        var sim = new ModbusSimulator();
        var req = new SimStartRequest
        {
            Mode = SimMode.Slave,
            Port = 15020,
            Devices = new List<SimDevice>
            {
                new()
                {
                    Address = "1",
                    Points = new List<SimPoint>
                    {
                        new() { Di = "0", FuncCode = 3, DataType = "uint16", Length = 1,
                                Generator = new GeneratorModel { Type = "constant", Base = 123 } }
                    }
                }
            }
        };
        var status = await sim.StartSimAsync(req, default);
        Assert.True(status.Running);

        await Task.Delay(1500); // 等一次寄存器刷新

        var client = new ModbusTcpClient();
        client.Connect(new IPEndPoint(IPAddress.Loopback, 15020), ModbusEndianness.BigEndian);
        var regs = client.ReadHoldingRegisters<ushort>(1, 0, 1);
        Assert.Equal(123, regs[0]);
        client.Disconnect();

        await sim.StopSimAsync(status.SimId);
        Assert.Empty(sim.ListSims());
    }
}
