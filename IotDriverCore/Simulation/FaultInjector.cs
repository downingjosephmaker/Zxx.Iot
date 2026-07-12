namespace IotDriverCore.Simulation
{
    /// <summary>
    /// 故障注入结果(从站编好应答后经装饰钩子产出:决定是否发送/如何发送)
    /// </summary>
    public sealed class FaultDecision
    {
        /// <summary>本次是否静默(true=不回复,模拟超时)</summary>
        public bool Drop { get; init; }

        /// <summary>实际发送的分片序列(每个元素是一次物理send;正常单帧即单元素;
        /// 粘包=多帧合一次;半包=一帧劈两段;元素间延迟见DelayMs)</summary>
        public List<byte[]> Segments { get; init; } = new();

        /// <summary>分片间延迟毫秒(仅半包场景>0)</summary>
        public int DelayMs { get; init; }

        /// <summary>正常整帧一次发送</summary>
        public static FaultDecision Whole(byte[] frame) => new() { Segments = { frame } };

        /// <summary>静默(不回复)</summary>
        public static FaultDecision Dropped() => new() { Drop = true };
    }

    /// <summary>
    /// 故障注入器(方案§4:发送前的装饰钩子,按概率触发;
    /// 不回复/错校验/粘包/半包四类,KISS——只做发送侧装饰,不改从站编码逻辑;
    /// 错校验交给从站的frameCorruptor回调实现,因为校验字节位置各协议不同)
    /// </summary>
    public sealed class FaultInjector
    {
        private readonly List<FaultModel> _faults;
        private readonly Random _random = new();

        /// <summary>
        /// 错帧改造器(从站按协议篡改校验字节;为null时错校验故障退化为原帧)
        /// </summary>
        private readonly Func<byte[], byte[]>? _corruptor;

        /// <summary>
        /// 上一次编好的帧(粘包故障把上一帧与本帧合并一次发)
        /// </summary>
        private byte[]? _pendingStick;

        public FaultInjector(List<FaultModel>? faults, Func<byte[], byte[]>? corruptor = null)
        {
            _faults = faults ?? new List<FaultModel>();
            _corruptor = corruptor;
        }

        /// <summary>
        /// 对一帧应答做故障装饰(命中概率的第一条故障生效;无故障或未命中→整帧发送)
        /// </summary>
        public FaultDecision Decorate(byte[] frame)
        {
            foreach (var fault in _faults)
            {
                if (_random.NextDouble() > Math.Clamp(fault.Probability, 0, 1)) continue;
                switch ((fault.Type ?? "").Trim().ToLowerInvariant())
                {
                    case "timeout":
                        return FaultDecision.Dropped();
                    case "wrongcs":
                    case "wrongcrc":
                        return FaultDecision.Whole(_corruptor?.Invoke(frame) ?? frame);
                    case "stick":
                        // 粘包:缓存本帧,与下一帧合并发送(本帧先不发)
                        if (_pendingStick == null)
                        {
                            _pendingStick = frame;
                            return new FaultDecision();  // 空segments=本轮不发,等下一帧
                        }
                        var merged = new byte[_pendingStick.Length + frame.Length];
                        Array.Copy(_pendingStick, 0, merged, 0, _pendingStick.Length);
                        Array.Copy(frame, 0, merged, _pendingStick.Length, frame.Length);
                        _pendingStick = null;
                        return FaultDecision.Whole(merged);
                    case "split":
                        // 半包:整帧劈两段,段间延迟
                        if (frame.Length < 2) return FaultDecision.Whole(frame);
                        int mid = frame.Length / 2;
                        var first = new byte[mid];
                        var second = new byte[frame.Length - mid];
                        Array.Copy(frame, 0, first, 0, mid);
                        Array.Copy(frame, mid, second, 0, second.Length);
                        return new FaultDecision { Segments = { first, second }, DelayMs = Math.Max(1, fault.DelayMs) };
                }
            }
            return FaultDecision.Whole(frame);
        }
    }
}
