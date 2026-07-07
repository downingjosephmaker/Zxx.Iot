namespace IotDriverCore
{
    /// <summary>
    /// 断线重连退避(Decorrelated Jitter:delay=min(cap,random(base,prev*3));
    /// 随机抖动防大面积断电恢复后的重连风暴,连接成功后调用Reset复位窗口)
    /// </summary>
    public class ReconnectBackoff
    {
        /// <summary>
        /// 基础等待时长(毫秒)
        /// </summary>
        private readonly int _baseMs;

        /// <summary>
        /// 等待时长上限(毫秒)
        /// </summary>
        private readonly int _capMs;

        private readonly Random _random = new();
        private int _prevMs;

        public ReconnectBackoff(int basems = 1000, int capms = 60_000)
        {
            _baseMs = Math.Max(1, basems);
            _capMs = Math.Max(_baseMs, capms);
            _prevMs = _baseMs;
        }

        /// <summary>
        /// 取下一次重连等待时长(毫秒,逐次抖动放大直至上限)
        /// </summary>
        public int NextDelayMs()
        {
            lock (_random)
            {
                _prevMs = Math.Min(_capMs, _random.Next(_baseMs, Math.Max(_baseMs + 1, _prevMs * 3)));
                return _prevMs;
            }
        }

        /// <summary>
        /// 连接成功后复位退避窗口
        /// </summary>
        public void Reset()
        {
            lock (_random) { _prevMs = _baseMs; }
        }
    }
}
