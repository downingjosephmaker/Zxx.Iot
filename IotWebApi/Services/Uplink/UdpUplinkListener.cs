using System.Net;
using System.Net.Sockets;
using CenboEventBus;
using IotLog;
using IotModel;
using IotWebApi.Services.Mqtt;

namespace IotWebApi.Services.Uplink
{
    /// <summary>
    /// UDP 南向上报监听器:每个数据报即一帧(天然定界),来源IP匹配 DeviceInfo.DeviceIp。
    /// 只接受已登记 IP(白名单),未登记的包直接丢。设备主动上报,不实现 Send。
    /// </summary>
    public class UdpUplinkListener : IHostedService
    {
        private UdpClient? _udp;
        private CancellationTokenSource? _cts;
        private readonly int _port;

        public UdpUplinkListener()
        {
            _port = int.TryParse(AppSetting.GetConfig("Uplink:UdpPort"), out var p) ? p : 0;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_port <= 0) return Task.CompletedTask;   // 未配置端口=不启用
            // 绑定失败(端口占用/BindAddress 畸形)只记日志返回,不抛出——IHostedService.StartAsync 抛异常会中止整个 Host 启动
            if (!IPAddress.TryParse(AppSetting.GetConfig("Uplink:BindAddress") ?? "0.0.0.0", out var bindAddr))
                bindAddr = IPAddress.Any;
            try
            {
                _udp = new UdpClient(new IPEndPoint(bindAddr, _port));
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("UdpUplinkListener", "StartAsync", $"UDP 南向监听启动失败(端口 {_port}):{ex.Message}", "UdpUplink");
                return Task.CompletedTask;
            }
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoop(_cts.Token));
            LogHelper.SysLogWrite("UdpUplinkListener", "StartAsync", $"UDP 南向监听已启动:端口 {_port}", "UdpUplink");
            return Task.CompletedTask;
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _udp != null)
            {
                try
                {
                    var result = await _udp.ReceiveAsync(token);
                    string ip = result.RemoteEndPoint.Address.MapToIPv4().ToString();
                    var device = DeviceInfoDAO.Instance.GetOneBy(t => t.DeviceIp == ip);
                    if (device == null)   // IP 白名单:未登记来源直接丢
                    {
                        LogHelper.SysLogWrite("UdpUplinkListener", "ReceiveLoop", $"丢弃未登记来源 UDP 包:{ip}", "UdpUplink");
                        continue;
                    }
                    var message = UplinkPayloadRouter.Route(device.DeviceGateway ?? device.DeviceId.ToString(), result.Buffer);
                    if (message != null)
                        MqttClientService.EventBus?.Publish(new PluginEvent(MqttClientService.UdpUplinkPluginGuid, message));
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite("UdpUplinkListener", "ReceiveLoop", ex.ToString(), "UdpUplink");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            _udp?.Dispose();
            return Task.CompletedTask;
        }
    }
}
