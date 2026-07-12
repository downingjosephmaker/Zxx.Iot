using IotDriverCore;
using IotDriverCore.Simulation;
using Xunit;

public class SimContractTests
{
    [Fact]
    public void SimStartRequest_DefaultsToSlaveMode()
    {
        var req = new SimStartRequest();
        Assert.Equal(SimMode.Slave, req.Mode);
    }

    [Fact]
    public void SimPoint_DefaultGenerator_NotNull()
    {
        var p = new SimPoint();
        Assert.NotNull(p.Generator);
    }
}
