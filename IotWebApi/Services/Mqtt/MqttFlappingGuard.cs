using System.Collections.Concurrent;
using CenBoCommon.Zxx;
using IotLog;

namespace IotWebApi.Services.Mqtt
{
    /// <summary>
    /// MQTT连接抖动封禁(§6.5借鉴EMQX flapping_detect:时间窗内断连超阈值自动封禁ClientId一段时间,
    /// 认证失败同窗限流防爆破;封禁/解封只在状态翻转时记日志。
    /// 注:§6.5认证三级缓存暂不适用——当前Broker为内存单一全局账号比对无每连接DB查询,
    /// 待每设备凭据模型落地时再引入)
    /// </summary>
    public static class MqttFlappingGuard
    {
        /// <summary>统计窗口(秒)</summary>
        private const int WindowSeconds = 60;

        /// <summary>窗口内断连次数阈值</summary>
        private const int DisconnectThreshold = 15;

        /// <summary>窗口内认证失败次数阈值</summary>
        private const int AuthFailThreshold = 5;

        /// <summary>封禁时长(秒)</summary>
        private const int BanSeconds = 300;

        /// <summary>跟踪表容量上限(超限清理过期窗口防内存膨胀)</summary>
        private const int TrackLimit = 10_000;

        /// <summary>
        /// 单客户端窗口计数
        /// </summary>
        private class Track
        {
            public DateTime WindowStart;
            public int Disconnects;
            public int AuthFails;
        }

        /// <summary>ClientId→窗口计数</summary>
        private static readonly ConcurrentDictionary<string, Track> Tracks = new();

        /// <summary>ClientId→封禁截止时间</summary>
        private static readonly ConcurrentDictionary<string, DateTime> Bans = new();

        /// <summary>
        /// 是否处于封禁期(过期自动解封)
        /// </summary>
        public static bool IsBanned(string clientid)
        {
            if (clientid.IsZxxNullOrEmpty()) return false;
            if (!Bans.TryGetValue(clientid, out var until)) return false;
            if (DateTime.Now < until) return true;
            if (Bans.TryRemove(clientid, out _))
            {
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"客户端ID=【{clientid}】封禁到期自动解封", "MQTT服务端");
            }
            return false;
        }

        /// <summary>
        /// 记一次断连(窗口内超阈值即封禁)
        /// </summary>
        public static void OnDisconnected(string clientid) => Bump(clientid, isauthfail: false);

        /// <summary>
        /// 记一次认证失败(窗口内超阈值即封禁,防凭据爆破)
        /// </summary>
        public static void OnAuthFailed(string clientid) => Bump(clientid, isauthfail: true);

        private static void Bump(string clientid, bool isauthfail)
        {
            if (clientid.IsZxxNullOrEmpty()) return;
            PruneIfOversize();
            var track = Tracks.GetOrAdd(clientid, _ => new Track { WindowStart = DateTime.Now });
            bool banned = false;
            lock (track)
            {
                if ((DateTime.Now - track.WindowStart).TotalSeconds > WindowSeconds)
                {
                    track.WindowStart = DateTime.Now;
                    track.Disconnects = 0;
                    track.AuthFails = 0;
                }
                if (isauthfail)
                {
                    if (++track.AuthFails >= AuthFailThreshold) banned = true;
                }
                else
                {
                    if (++track.Disconnects >= DisconnectThreshold) banned = true;
                }
                if (banned)
                {
                    track.Disconnects = 0;
                    track.AuthFails = 0;
                }
            }
            if (banned && Bans.TryAdd(clientid, DateTime.Now.AddSeconds(BanSeconds)))
            {
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"客户端ID=【{clientid}】{WindowSeconds}秒内{(isauthfail ? "认证失败" : "断连")}超阈值,封禁{BanSeconds}秒", "MQTT服务端");
            }
        }

        /// <summary>
        /// 跟踪表超限时清理过期窗口
        /// </summary>
        private static void PruneIfOversize()
        {
            if (Tracks.Count <= TrackLimit) return;
            var now = DateTime.Now;
            foreach (var pair in Tracks)
            {
                if ((now - pair.Value.WindowStart).TotalSeconds > WindowSeconds) Tracks.TryRemove(pair.Key, out _);
            }
        }
    }
}
