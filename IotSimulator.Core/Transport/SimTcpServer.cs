using System.Net;
using System.Net.Sockets;

namespace IotSimulator.Core.Transport
{
    /// <summary>
    /// 被拨模式传输(监听端口,接受插件TcpClientChannelPool拨入;
    /// 多连接各自独立收发循环,收帧按连接标识回调;
    /// 原生Socket实现,不依赖IotDriverCore通道类)
    /// </summary>
    public sealed class SimTcpServer : IDisposable
    {
        private readonly int _port;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        /// <summary>连接标识→活动连接(远端"IP:Port"为键)</summary>
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, TcpClient> _conns = new();

        /// <summary>收到原始字节回调(连接标识,原始帧)</summary>
        public Action<string, byte[]>? OnFrame { get; set; }

        /// <summary>连接建立回调(连接标识)</summary>
        public Action<string>? OnConnected { get; set; }

        /// <summary>连接断开回调(连接标识)</summary>
        public Action<string>? OnDisconnected { get; set; }

        /// <summary>日志回调</summary>
        public Action<string>? OnLog { get; set; }

        public SimTcpServer(int port) => _port = port;

        /// <summary>
        /// 启动监听
        /// </summary>
        public void Start()
        {
            _cts ??= new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            OnLog?.Invoke($"被拨监听启动，端口 {_port}");
            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        /// <summary>
        /// 停止监听并断开所有连接
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
            try { _listener?.Stop(); } catch { }
            foreach (var c in _conns.Values)
            {
                try { c.Close(); } catch { }
            }
            _conns.Clear();
        }

        public void Dispose() => Stop();

        /// <summary>
        /// 向指定连接发送原始字节(连接不存在返回false)
        /// </summary>
        public bool Send(string connKey, byte[] payload)
        {
            if (!_conns.TryGetValue(connKey, out var client)) return false;
            try
            {
                var stream = client.GetStream();
                lock (client) { stream.Write(payload, 0, payload.Length); }
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 向任意一个在线连接发送(单拨入场景便捷发送)
        /// </summary>
        public bool SendAny(byte[] payload)
        {
            foreach (var key in _conns.Keys)
            {
                if (Send(key, payload)) return true;
            }
            return false;
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var client = await _listener!.AcceptTcpClientAsync(token);
                    var remote = client.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();
                    _conns[remote] = client;
                    OnLog?.Invoke($"拨入连接接受 ← {remote}");
                    OnConnected?.Invoke(remote);
                    _ = Task.Run(() => ReceiveLoopAsync(remote, client, token));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { OnLog?.Invoke($"接受连接异常:{ex.Message}"); }
        }

        private async Task ReceiveLoopAsync(string connKey, TcpClient client, CancellationToken token)
        {
            var buffer = new byte[4096];
            try
            {
                var stream = client.GetStream();
                while (!token.IsCancellationRequested)
                {
                    int count = await stream.ReadAsync(buffer, token);
                    if (count <= 0) break;
                    var frame = new byte[count];
                    Array.Copy(buffer, frame, count);
                    try { OnFrame?.Invoke(connKey, frame); }
                    catch (Exception ex) { OnLog?.Invoke($"收帧处理异常:{ex.Message}"); }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { OnLog?.Invoke($"连接[{connKey}]接收异常:{ex.Message}"); }
            finally
            {
                _conns.TryRemove(connKey, out _);
                try { client.Close(); } catch { }
                OnDisconnected?.Invoke(connKey);
            }
        }
    }
}
