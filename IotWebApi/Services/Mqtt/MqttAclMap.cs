using System.Collections.Concurrent;

namespace IotWebApi.Services.Mqtt
{
    /// <summary>
    /// ClientId → 绑定的 deviceGateway(deviceKey)。连接认证通过时 Bind,断开时 Unbind。
    /// Topic ACL:校验 topic 末段 == 该连接绑定的 deviceGateway,即使凭据泄露也只能冒充自己那一台。
    /// 全局账号(gateway=null/未绑定)不参与 ACL(存量内网设备维持现状)。
    /// </summary>
    public static class MqttAclMap
    {
        private static readonly ConcurrentDictionary<string, string> Bindings = new();

        public static void Bind(string clientId, string? deviceGateway)
        {
            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(deviceGateway))
                Bindings[clientId] = deviceGateway!;
        }

        public static void Unbind(string clientId) => Bindings.TryRemove(clientId, out _);

        /// <summary>true=放行。未绑定(全局账号)一律放行;绑定则要求 topic 末段==deviceGateway。</summary>
        public static bool Match(string clientId, string topic)
        {
            if (!Bindings.TryGetValue(clientId, out var gateway)) return true;   // 全局账号,不管 ACL
            var segments = topic.Split('/', System.StringSplitOptions.RemoveEmptyEntries);
            string last = segments.Length > 0 ? segments[^1] : "";
            return last == gateway;
        }
    }
}
