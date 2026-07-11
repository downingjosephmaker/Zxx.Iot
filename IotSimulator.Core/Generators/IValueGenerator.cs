using IotSimulator.Core.Scenario;

namespace IotSimulator.Core.Generators
{
    /// <summary>
    /// 点值生成器(按当前时刻产出工程值,从站再按协议编码为BCD/寄存器字节)
    /// </summary>
    public interface IValueGenerator
    {
        /// <summary>按时刻产出下一个值</summary>
        double Next(DateTime now);
    }

    /// <summary>
    /// 生成器工厂(按场景GeneratorModel.Type构造对应实现)
    /// </summary>
    public static class GeneratorFactory
    {
        /// <summary>
        /// 构造生成器(未知类型回退constant)
        /// </summary>
        public static IValueGenerator Create(GeneratorModel model)
        {
            return (model.Type ?? "").Trim().ToLowerInvariant() switch
            {
                "random" => new RandomGenerator(model.Min, model.Max),
                "sine" => new SineGenerator(model.Base, model.Amp, model.PeriodS),
                "step" => new StepGenerator(model.Base, model.Step, model.StepEveryS),
                _ => new ConstantGenerator(model.Base)
            };
        }
    }

    /// <summary>
    /// 常量生成器(恒定返回base)
    /// </summary>
    public sealed class ConstantGenerator : IValueGenerator
    {
        private readonly double _value;
        public ConstantGenerator(double value) => _value = value;
        public double Next(DateTime now) => _value;
    }

    /// <summary>
    /// 随机生成器([min,max]均匀分布)
    /// </summary>
    public sealed class RandomGenerator : IValueGenerator
    {
        private readonly double _min;
        private readonly double _max;
        private readonly Random _random = new();
        public RandomGenerator(double min, double max)
        {
            // 容错:min>max时对调
            _min = Math.Min(min, max);
            _max = Math.Max(min, max);
        }
        public double Next(DateTime now) => _min + _random.NextDouble() * (_max - _min);
    }

    /// <summary>
    /// 正弦生成器(base+amp*sin(2π*t/periodS),t取当日秒数保证跨调用连续)
    /// </summary>
    public sealed class SineGenerator : IValueGenerator
    {
        private readonly double _base;
        private readonly double _amp;
        private readonly double _periodS;
        public SineGenerator(double baseline, double amp, double periodS)
        {
            _base = baseline;
            _amp = amp;
            _periodS = periodS <= 0 ? 60 : periodS;
        }
        public double Next(DateTime now)
        {
            double t = now.TimeOfDay.TotalSeconds;
            return _base + _amp * Math.Sin(2 * Math.PI * t / _periodS);
        }
    }

    /// <summary>
    /// 阶梯生成器(base + step*floor(经过秒数/stepEveryS),单调递增便于观察定标)
    /// </summary>
    public sealed class StepGenerator : IValueGenerator
    {
        private readonly double _base;
        private readonly double _step;
        private readonly double _stepEveryS;
        private readonly DateTime _origin = DateTime.Now;
        public StepGenerator(double baseline, double step, double stepEveryS)
        {
            _base = baseline;
            _step = step;
            _stepEveryS = stepEveryS <= 0 ? 10 : stepEveryS;
        }
        public double Next(DateTime now)
        {
            double elapsed = (now - _origin).TotalSeconds;
            return _base + _step * Math.Floor(elapsed / _stepEveryS);
        }
    }
}
