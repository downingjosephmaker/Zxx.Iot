using IotDriverCore;
using IotModel;
using IotWebApi.Areas.Device.Controllers;
using Xunit;

public class SimulatorMappingTests
{
    [Fact]
    public void Map_ModbusPoint_CopiesFuncCodeAndAddress()
    {
        var dev = new DeviceInfoEntity { DeviceAdr = 1 };
        var points = new List<DeviceTypeParam>
        {
            new() { ParamCode = "temp", ParamAddr = 40001, CollectFuncCode = 3,
                    CollectDataType = "float32", CollectRegLength = 2 }
        };
        var sim = SimDeviceMapper.Map(dev, points);
        Assert.Equal("1", sim.Address);
        Assert.Single(sim.Points);
        Assert.Equal("temp", sim.Points[0].ParamCode);
        Assert.Equal(3, sim.Points[0].FuncCode);
        Assert.Equal("40001", sim.Points[0].Di);
        Assert.Equal("float32", sim.Points[0].DataType);
    }

    [Fact]
    public void Map_CopiesDeviceTypeCodeAndBitOffset()
    {
        var dev = new DeviceInfoEntity { DeviceAdr = 1, DeviceTypeCode = "M-645-07" };
        var points = new List<DeviceTypeParam>
        {
            new() { ParamCode = "p", ParamAddr = 1, CollectFuncCode = 3, CollectBitOffset = 4 }
        };
        var sim = SimDeviceMapper.Map(dev, points);
        Assert.Equal("M-645-07", sim.DeviceTypeCode);
        Assert.Equal(4, sim.Points[0].BitOffset);
    }

    [Theory]
    [InlineData("a*0.01", 0.01)]
    [InlineData("0.01*a", 0.01)]
    [InlineData("a * 0.5", 0.5)]
    [InlineData("a", 1)]
    [InlineData("", 1)]
    [InlineData(null, 1)]
    [InlineData("a*0.01+5", 1)]
    [InlineData("sin(a)", 1)]
    public void Map_ParsesLinearScaleFromParamFormula(string? formula, double expected)
    {
        var dev = new DeviceInfoEntity { DeviceAdr = 1 };
        var points = new List<DeviceTypeParam>
        {
            new() { ParamCode = "p", ParamAddr = 1, CollectFuncCode = 3, ParamFormula = formula! }
        };
        var sim = SimDeviceMapper.Map(dev, points);
        Assert.Equal(expected, sim.Points[0].Scale);
    }
}
