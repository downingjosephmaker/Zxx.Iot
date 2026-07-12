using IotDriverCore.Simulation;
using Xunit;

public class SimulationCoreTests
{
    [Fact]
    public void ConstantGenerator_ReturnsBase()
    {
        var g = GeneratorFactory.Create(new GeneratorModel { Type = "constant", Base = 42 });
        Assert.Equal(42, g.Next(DateTime.Now));
    }

    [Fact]
    public void FaultInjector_Timeout_Drops()
    {
        var inj = new FaultInjector(new List<FaultModel> { new() { Type = "timeout", Probability = 1 } });
        var decision = inj.Decorate(new byte[] { 1, 2, 3 });
        Assert.True(decision.Drop);
    }

    [Fact]
    public void FaultInjector_NoFault_SendsWhole()
    {
        var inj = new FaultInjector(null);
        var decision = inj.Decorate(new byte[] { 1, 2, 3 });
        Assert.False(decision.Drop);
        Assert.Single(decision.Segments);
    }
}
