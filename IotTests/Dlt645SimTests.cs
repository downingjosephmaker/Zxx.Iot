using System.Net;
using System.Net.Sockets;
using IotDriverCore;
using IotDriverCore.Simulation;
using IotPlugin.Dlt645.Sim;
using Xunit;

public class Dlt645SimTests
{
    [Fact]
    public async Task Dlt645Simulator_StartStop()
    {
        var sim = new Dlt645Simulator();
        var req = new SimStartRequest
        {
            Mode = SimMode.Slave, Port = 19645,
            Devices = new List<SimDevice>
            {
                new() { Address = "000000000001",
                        Points = new List<SimPoint> { new() { Di = "0x02010100", Length = 4,
                            Generator = new GeneratorModel { Type = "constant", Base = 220 } } } }
            }
        };
        var status = await sim.StartSimAsync(req, default);
        Assert.True(status.Running);
        Assert.Single(sim.ListSims());
        await sim.StopSimAsync(status.SimId);
        Assert.Empty(sim.ListSims());
    }

    [Fact]
    public async Task Dlt645Simulator_StartSimAsync_PortOccupied_ReturnsRunningFalse_NotThrows()
    {
        var sim1 = new Dlt645Simulator();
        var req = new SimStartRequest
        {
            Mode = SimMode.Slave, Port = 19648,
            Devices = new List<SimDevice>
            {
                new() { Address = "000000000001",
                        Points = new List<SimPoint> { new() { Di = "0x02010100", Length = 4,
                            Generator = new GeneratorModel { Type = "constant", Base = 1 } } } }
            }
        };
        var status1 = await sim1.StartSimAsync(req, default);
        Assert.True(status1.Running);

        var sim2 = new Dlt645Simulator();
        var status2 = await sim2.StartSimAsync(req, default); // 同端口二次启动应失败而非抛异常
        Assert.False(status2.Running);
        Assert.Contains("端口占用或启动失败", status2.Message);
        Assert.Empty(sim2.ListSims());

        await sim1.StopSimAsync(status1.SimId);
    }

    [Fact]
    public async Task Dlt645Simulator_StopAll_清空全部运行中实例()
    {
        var sim = new Dlt645Simulator();
        var req1 = new SimStartRequest
        {
            Mode = SimMode.Slave, Port = 19649,
            Devices = new List<SimDevice>
            {
                new() { Address = "000000000001",
                        Points = new List<SimPoint> { new() { Di = "0x02010100", Length = 4,
                            Generator = new GeneratorModel { Type = "constant", Base = 1 } } } }
            }
        };
        var req2 = new SimStartRequest
        {
            Mode = SimMode.Slave, Port = 19650,
            Devices = new List<SimDevice>
            {
                new() { Address = "000000000002",
                        Points = new List<SimPoint> { new() { Di = "0x02010100", Length = 4,
                            Generator = new GeneratorModel { Type = "constant", Base = 1 } } } }
            }
        };
        await sim.StartSimAsync(req1, default);
        await sim.StartSimAsync(req2, default);
        Assert.Equal(2, sim.ListSims().Count);

        sim.StopAll();
        Assert.Empty(sim.ListSims());
    }

    [Fact]
    public async Task Dlt645Simulator_按DeviceTypeCode判1997版_应答控制码为01()
    {
        var sim = new Dlt645Simulator
        {
            Codes1997 = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OLD97" }
        };
        var req = new SimStartRequest
        {
            Mode = SimMode.Slave, Port = 19651,
            Devices = new List<SimDevice>
            {
                new() { Address = "000000000001", DeviceTypeCode = "OLD97",
                        Points = new List<SimPoint> { new() { Di = "0x0001", Length = 4,
                            Generator = new GeneratorModel { Type = "constant", Base = 100 } } } }
            }
        };
        var status = await sim.StartSimAsync(req, default);
        Assert.True(status.Running);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, 19651);
        var stream = client.GetStream();

        // 构造1997版读请求(2字节DI,控制码0x01):68 addr6 68 01 02 DI2(+33) CS 16
        var addr = BuildAddr("000000000001", 12);
        var body = new List<byte> { 0x68 };
        body.AddRange(addr);
        body.Add(0x68);
        body.Add(0x01);
        body.Add(0x02);
        body.Add((byte)(0x01 + 0x33));
        body.Add((byte)(0x00 + 0x33));
        body.Add(Cs(body.ToArray(), 0, body.Count));
        body.Add(0x16);
        var frame = body.ToArray();
        await stream.WriteAsync(frame);

        var buf = new byte[64];
        var readTask = stream.ReadAsync(buf, 0, buf.Length);
        var completed = await Task.WhenAny(readTask, Task.Delay(3000));
        Assert.Same(readTask, completed);
        int n = await readTask;
        Assert.True(n > 0);
        Assert.Equal(0x81, buf[8]); // 0x01|0x80,确认按1997版控制码应答(非2007的0x91)

        await sim.StopSimAsync(status.SimId);
    }

    private static byte[] BuildAddr(string decimalAddr, int digits)
    {
        var d = decimalAddr.PadLeft(digits, '0');
        if (d.Length > digits) d = d[^digits..];
        var addr = new byte[digits / 2];
        for (int i = 0; i < addr.Length; i++)
        {
            int pos = d.Length - (i + 1) * 2;
            addr[i] = (byte)(((d[pos] - '0') << 4) | (d[pos + 1] - '0'));
        }
        return addr;
    }

    private static byte Cs(byte[] body, int offset, int count)
    {
        int sum = 0;
        for (int i = offset; i < offset + count; i++) sum += body[i];
        return (byte)(sum & 0xFF);
    }
}
