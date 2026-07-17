using CenboEventBus;
using IotDriverCore;
using IotLog;
using IotModel;
using IotWebApi.Services.Mqtt;

namespace IotWebApi.Services.Uplink
{
    /// <summary>
    /// TCP 南向上报监听器:复用 TcpServerChannel(连接管理/心跳/空闲踢连接现成),
    /// 来源IP 经 EndpointResolver 映射到 deviceKey。字节流按换行符 '\n' 定界。
    /// 未登记 IP 由 EndpointResolver 返回 null → 走 "IP:Port" 兜底端点,该端点查不到设备则丢帧。
    /// </summary>
    public class TcpUplinkListener : IHostedService
    {
        private TcpServerChannel? _channel;
        private readonly int _port;
        // 换行定界:提取器找第一个 '\n',返回 [0, idx+1]
        private readonly FrameAccumulator _acc = new FrameAccumulator(buf =>
        {
            int idx = System.Array.IndexOf(buf, (byte)'\n');
            return idx < 0 ? (-1, 0) : (0, idx + 1);
        });

        public TcpUplinkListener()
        {
            _port = int.TryParse(AppSetting.GetConfig("Uplink:TcpPort"), out var p) ? p : 0;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_port <= 0) return Task.CompletedTask;
            _channel = new TcpServerChannel(_port)
            {
                IdleTimeoutSeconds = 300,
                EndpointResolver = ip =>
                {
                    var device = DeviceInfoDAO.Instance.GetOneBy(t => t.DeviceIp == ip);
                    return device?.DeviceGateway ?? device?.DeviceId.ToString();   // deviceKey;null → 兜底 IP:Port
                }
            };
            _channel.FrameReceived = OnFrame;
            _channel.SessionClosed = ep => _acc.Reset(ep);
            _channel.Start();
            LogHelper.SysLogWrite("TcpUplinkListener", "StartAsync", $"TCP 南向监听已启动:端口 {_port}", "TcpUplink");
            return Task.CompletedTask;
        }

        private void OnFrame(string endpoint, byte[] raw)
        {
            foreach (var frame in _acc.Push(endpoint, raw))
            {
                var message = UplinkPayloadRouter.Route(endpoint, frame);
                if (message != null)
                    MqttClientService.EventBus?.Publish(new PluginEvent(MqttClientService.TcpUplinkPluginGuid, message));
                else
                    LogHelper.SysLogWrite("TcpUplinkListener", "OnFrame", $"无法识别的TCP上行帧,来源端点={endpoint}", "TcpUplink");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _channel?.Stop();
            _channel?.Dispose();
            return Task.CompletedTask;
        }
    }
}
