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
}
