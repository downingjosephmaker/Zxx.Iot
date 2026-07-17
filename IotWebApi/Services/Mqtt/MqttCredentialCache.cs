using System.Collections.Concurrent;
using IotModel;

namespace IotWebApi.Services.Mqtt
{
    /// <summary>
    /// MQTT 每设备凭据认证的两级缓存:L1 进程内 ConcurrentDictionary(TTL),L2 数据库。
    /// PBKDF2 是慢哈希,连接风暴下靠 L1 命中放行。凭据变更/吊销调 Invalidate 清 L1。
    /// 缓存存的是凭据记录(hash/salt),不是认证结果——每次都跑 Pbkdf2Verify,错误口令不会被误缓存为通过。
    /// </summary>
    public static class MqttCredentialCache
    {
        private sealed class Cached { public string PassHash = ""; public string Salt = ""; public string? DeviceGateway; public DateTime Expire; }
        private static readonly ConcurrentDictionary<string, Cached> L1 = new();
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
        private const int Limit = 10_000;

        /// <summary>校验用户名+口令。返回 (是否通过, 绑定的 deviceGateway)。未知用户不做负缓存。</summary>
        public static (bool ok, string? deviceGateway) Validate(string mqttUser, string password)
        {
            if (string.IsNullOrEmpty(mqttUser)) return (false, null);
            var now = DateTime.Now;
            if (L1.TryGetValue(mqttUser, out var c) && c.Expire > now)
                return (EncryptsHelper.Pbkdf2Verify(password, c.Salt, c.PassHash), c.DeviceGateway);

            // L2:查库(仅启用的凭据)
            var cred = DeviceMqttCredentialDAO.Instance.GetOneBy(t => t.MqttUser == mqttUser && t.IsEnable);
            if (cred == null) return (false, null);
            var entry = new Cached { PassHash = cred.PassHash, Salt = cred.Salt, DeviceGateway = cred.DeviceGateway, Expire = now.Add(Ttl) };
            PruneIfOversize();
            L1[mqttUser] = entry;
            return (EncryptsHelper.Pbkdf2Verify(password, entry.Salt, entry.PassHash), entry.DeviceGateway);
        }

        public static void Invalidate(string mqttUser) => L1.TryRemove(mqttUser, out _);

        private static void PruneIfOversize()
        {
            if (L1.Count < Limit) return;
            var now = DateTime.Now;
            foreach (var kv in L1) if (kv.Value.Expire <= now) L1.TryRemove(kv.Key, out _);
        }
    }
}
