using IotDriverCore;
using IotDriverCore.Simulation;
using IotPlugin.Cjt188.Sim;
using Xunit;

public class Cjt188SimTests
{
    [Fact]
    public async Task Cjt188Simulator_StartStop()
    {
        var sim = new Cjt188Simulator();
        var req = new SimStartRequest
        {
            Mode = SimMode.Slave, Port = 19188,
            Devices = new List<SimDevice>
            {
                new() { Address = "00000000000001", MeterType = "0x10",
                        Points = new List<SimPoint> { new() { Di = "0x1F90", Length = 4,
                            Generator = new GeneratorModel { Type = "constant", Base = 100 } } } }
            }
        };
        var status = await sim.StartSimAsync(req, default);
        Assert.True(status.Running);
        await sim.StopSimAsync(status.SimId);
        Assert.Empty(sim.ListSims());
    }
}
