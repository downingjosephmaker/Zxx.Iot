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
}
