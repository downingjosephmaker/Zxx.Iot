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

    [Fact]
    public async Task ModbusSimulator_Scale_DividesGeneratorValueBeforeWrite()
    {
        var sim = new ModbusSimulator();
        var req = new SimStartRequest
        {
            Mode = SimMode.Slave,
            Port = 15021,
            Devices = new List<SimDevice>
            {
                new()
                {
                    Address = "1",
                    Points = new List<SimPoint>
                    {
                        new() { Di = "0", FuncCode = 3, DataType = "uint16", Length = 1, Scale = 0.01,
                                Generator = new GeneratorModel { Type = "constant", Base = 1.23 } }
                    }
                }
            }
        };
        var status = await sim.StartSimAsync(req, default);
        Assert.True(status.Running);

        await Task.Delay(1500); // 等一次寄存器刷新

        var client = new ModbusTcpClient();
        client.Connect(new IPEndPoint(IPAddress.Loopback, 15021), ModbusEndianness.BigEndian);
        var regs = client.ReadHoldingRegisters<ushort>(1, 0, 1);
        Assert.Equal(123, regs[0]); // 1.23/0.01=123,还原为原始值供插件解码后再套公式得回1.23
        client.Disconnect();

        await sim.StopSimAsync(status.SimId);
    }

    [Fact]
    public async Task ModbusSimulator_StartSimAsync_PortOccupied_ReturnsRunningFalse_NotThrows()
    {
        var sim1 = new ModbusSimulator();
        var req = new SimStartRequest
        {
            Mode = SimMode.Slave,
            Port = 15022,
            Devices = new List<SimDevice>
            {
                new() { Address = "1", Points = new List<SimPoint>
                    { new() { Di = "0", FuncCode = 3, DataType = "uint16", Length = 1,
                              Generator = new GeneratorModel { Type = "constant", Base = 1 } } } }
            }
        };
        var status1 = await sim1.StartSimAsync(req, default);
        Assert.True(status1.Running);

        var sim2 = new ModbusSimulator();
        var status2 = await sim2.StartSimAsync(req, default); // 同端口二次启动应失败而非抛异常
        Assert.False(status2.Running);
        Assert.Contains("端口占用或启动失败", status2.Message);
        Assert.Empty(sim2.ListSims());

        await sim1.StopSimAsync(status1.SimId);
    }
}
