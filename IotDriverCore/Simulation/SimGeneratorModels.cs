namespace IotDriverCore.Simulation
{
    /// <summary>值生成器模型(type=constant/random/sine/step;从模拟器场景模型下沉,协议无关)</summary>
    public class GeneratorModel
    {
        public string Type { get; set; } = "constant";
        public double Base { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Amp { get; set; }
        public double PeriodS { get; set; } = 60;
        public double Step { get; set; } = 1;
        public double StepEveryS { get; set; } = 10;
    }

    /// <summary>故障注入模型(type=timeout/wrongcs/stick/split)</summary>
    public class FaultModel
    {
        public string Type { get; set; } = "";
        public double Probability { get; set; } = 1;
        public int DelayMs { get; set; } = 50;
    }
}
