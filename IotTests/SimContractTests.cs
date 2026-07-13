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

    [Fact]
    public void SimPoint_DefaultScaleAndBitOffset()
    {
        var p = new SimPoint();
        Assert.Equal(1, p.Scale);
        Assert.Equal(-1, p.BitOffset);
    }

    [Fact]
    public void SimDevice_DefaultDeviceTypeCode_Empty()
    {
        var d = new SimDevice();
        Assert.Equal("", d.DeviceTypeCode);
    }
}
