using System.Collections.Concurrent;
using System.Net.Sockets;
using IotLog;

namespace IotDriverCore
{
    /// <summary>
    /// TCP客户端通道池(平台直连型驱动拨出:Modbus TCP/OPC UA网关等;
    /// 每端点独立连接+接收循环,断线按ReconnectBackoff去相关抖动退避自动重连,
    /// 连接成功复位退避窗口;帧定界/校验由驱动在FrameReceived中完成)
    /// </summary>
    public class TcpClientChannelPool : IChannelTransport, IDisposable
    {
        /// <summary>
        /// 单端点连接状态
        /// </summary>
        private class ClientEntry
        {
            public string Endpoint = "";
            public string Host = "";
            public int Port;
            public TcpClient? Client;
            public NetworkStream? Stream;
            public readonly object SendLock = new();
            public readonly ReconnectBackoff Backoff = new();
            public volatile bool Online;
        }

        /// <summary>
        /// 端点→连接状态
        /// </summary>
        private readonly ConcurrentDictionary<string, ClientEntry> _clients = new(StringComparer.OrdinalIgnoreCase);

        private CancellationTokenSource? _cts;

        /// <summary>
        /// 连接建立回调(端点键)
        /// </summary>
        public Action<string>? SessionOpened { get; set; }

        /// <summary>
        /// 连接断开回调(端点键,上层据此发布疑似离线)
        /// </summary>
        public Action<string>? SessionClosed { get; set; }

        /// <summary>
        /// 收到数据回调(端点键,原始字节)
        /// </summary>
        public Action<string, byte[]>? FrameReceived { get; set; }

        #region 通道生命周期

        /// <summary>
        /// 登记拨出端点(Start后登记的端点自动拉起连接循环)
        /// </summary>
        public void AddEndpoint(string endpoint, string host, int port)
        {
            var entry = new ClientEntry { Endpoint = endpoint, Host = host, Port = port };
            if (!_clients.TryAdd(endpoint, entry)) return;
            var cts = _cts;
            if (cts != null && !cts.IsCancellationRequested)
            {
                _ = Task.Run(() => RunClientLoopAsync(entry, cts.Token));
            }
        }

        /// <summary>
        /// 启动通道池(为所有已登记端点拉起连接循环)
        /// </summary>
        public void Start()
        {
            _cts ??= new CancellationTokenSource();
            var cts = _cts;
            foreach (var entry in _clients.Values)
            {
                _ = Task.Run(() => RunClientLoopAsync(entry, cts.Token));
            }
        }

        /// <summary>
        /// 停止通道池并断开所有连接
        /// </summary>
        public void Stop()
        {
            var cts = _cts;
            _cts = null;
            cts?.Cancel();
            foreach (var entry in _clients.Values)
            {
                try { entry.Client?.Close(); } catch { }
                entry.Online = false;
            }
            _clients.Clear();
            cts?.Dispose();
        }

        public void Dispose() => Stop();

        #endregion

        #region 连接与接收循环

        /// <summary>
        /// 单端点连接主循环:连接成功→接收直到断开→退避等待→重连
        /// </summary>
        private async Task RunClientLoopAsync(ClientEntry entry, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(entry.Host, entry.Port, token);
                    entry.Client = client;
                    entry.Stream = client.GetStream();
                    entry.Online = true;
                    entry.Backoff.Reset();
                    LogHelper.SysLogWrite("TcpClientChannelPool", "RunClientLoopAsync", $"TCP客户端通道：{entry.Endpoint}连接建立。", "驱动核心");
                    SessionOpened?.Invoke(entry.Endpoint);

                    var buffer = new byte[4096];
                    while (!token.IsCancellationRequested)
                    {
                        int count = await entry.Stream.ReadAsync(buffer, token);
                        if (count <= 0) break;
                        var frame = new byte[count];
                        Array.Copy(buffer, frame, count);
                        try { FrameReceived?.Invoke(entry.Endpoint, frame); }
                        catch (Exception ex) { LogHelper.ErrorLogWrite("TcpClientChannelPool", "RunClientLoopAsync", ex.ToString(), "驱动核心"); }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    LogHelper.SysLogWrite("TcpClientChannelPool", "RunClientLoopAsync", $"TCP客户端通道：{entry.Endpoint}连接异常，{ex.Message}", "驱动核心");
                }
                finally
                {
                    bool wasonline = entry.Online;
                    entry.Online = false;
                    entry.Stream = null;
                    entry.Client = null;
                    if (wasonline)
                    {
                        LogHelper.SysLogWrite("TcpClientChannelPool", "RunClientLoopAsync", $"TCP客户端通道：{entry.Endpoint}连接断开。", "驱动核心");
                        try { SessionClosed?.Invoke(entry.Endpoint); }
                        catch (Exception ex) { LogHelper.ErrorLogWrite("TcpClientChannelPool", "RunClientLoopAsync", ex.ToString(), "驱动核心"); }
                    }
                }
                if (token.IsCancellationRequested) break;
                try { await Task.Delay(entry.Backoff.NextDelayMs(), token); }
                catch (OperationCanceledException) { break; }
            }
        }

        /// <summary>
        /// 端点是否在线
        /// </summary>
        public bool IsOnline(string endpoint) =>
            _clients.TryGetValue(endpoint, out var entry) && entry.Online;

        /// <summary>
        /// 强制断开指定端点(连接循环随退避自动重连;
        /// 供协议层判死使用,如IEC104的t1等确认超时/序号错乱)
        /// </summary>
        public void Disconnect(string endpoint)
        {
            if (_clients.TryGetValue(endpoint, out var entry))
            {
                try { entry.Client?.Close(); } catch { }
            }
        }

        #endregion

        #region IChannelTransport

        /// <summary>
        /// 向指定端点发送原始字节(未连接返回false;同端点写入串行化)
        /// </summary>
        public bool Send(string endpoint, byte[] payload)
        {
            if (!_clients.TryGetValue(endpoint, out var entry) || !entry.Online) return false;
            try
            {
                var stream = entry.Stream;
                if (stream == null) return false;
                lock (entry.SendLock)
                {
                    stream.Write(payload, 0, payload.Length);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("TcpClientChannelPool", "Send", ex.ToString(), "驱动核心");
                return false;
            }
        }

        #endregion
    }
}
