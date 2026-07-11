using System.Net.Sockets;

namespace IotSimulator.Core.Transport
{
    /// <summary>
    /// 拨入模式传输(模拟DTU:连接插件的TcpServerChannel,连上先发ASCII注册包,
    /// 可选周期心跳hex;收到主站下发的读帧经OnFrame回调交从站编应答;
    /// 原生Socket实现,不依赖IotDriverCore通道类,保持模拟器与被测代码独立)
    /// </summary>
    public sealed class SimTcpClient : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly byte[]? _registerPacket;
        private readonly byte[]? _heartbeat;
        private readonly int _heartbeatIntervalMs;

        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private readonly object _sendLock = new();

        /// <summary>收到原始字节回调(主站下发的帧,可能粘包/半包,由上层交FrameAccumulator重组)</summary>
        public Action<byte[]>? OnFrame { get; set; }

        /// <summary>连接建立回调</summary>
        public Action? OnConnected { get; set; }

        /// <summary>连接断开回调</summary>
        public Action? OnDisconnected { get; set; }

        /// <summary>日志回调(控制台打印用)</summary>
        public Action<string>? OnLog { get; set; }

        public SimTcpClient(string host, int port, byte[]? registerPacket = null,
            byte[]? heartbeat = null, int heartbeatIntervalMs = 30000)
        {
            _host = host;
            _port = port;
            _registerPacket = registerPacket;
            _heartbeat = heartbeat;
            _heartbeatIntervalMs = heartbeatIntervalMs <= 0 ? 30000 : heartbeatIntervalMs;
        }

        /// <summary>
        /// 启动拨入(后台连接循环,断线自动重连)
        /// </summary>
        public void Start()
        {
            _cts ??= new CancellationTokenSource();
            _ = Task.Run(() => RunLoopAsync(_cts.Token));
        }

        /// <summary>
        /// 停止并断开
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
            try { _client?.Close(); } catch { }
        }

        public void Dispose() => Stop();

        /// <summary>
        /// 向主站发送原始字节(未连接返回false;同连接写入串行化)
        /// </summary>
        public bool Send(byte[] payload)
        {
            var stream = _stream;
            if (stream == null) return false;
            try
            {
                lock (_sendLock) { stream.Write(payload, 0, payload.Length); }
                return true;
            }
            catch { return false; }
        }

        private async Task RunLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(_host, _port, token);
                    _client = client;
                    _stream = client.GetStream();
                    OnLog?.Invoke($"拨入连接建立 → {_host}:{_port}");

                    // 连接后先发注册包(DTU握手)
                    if (_registerPacket is { Length: > 0 })
                    {
                        Send(_registerPacket);
                        OnLog?.Invoke($"发送注册包 {ToHex(_registerPacket)}");
                    }
                    OnConnected?.Invoke();

                    // 心跳定时器
                    CancellationTokenSource? heartCts = null;
                    if (_heartbeat is { Length: > 0 })
                    {
                        heartCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                        _ = HeartbeatLoopAsync(heartCts.Token);
                    }

                    var buffer = new byte[4096];
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            int count = await _stream.ReadAsync(buffer, token);
                            if (count <= 0) break;
                            var frame = new byte[count];
                            Array.Copy(buffer, frame, count);
                            try { OnFrame?.Invoke(frame); }
                            catch (Exception ex) { OnLog?.Invoke($"收帧处理异常:{ex.Message}"); }
                        }
                    }
                    finally { heartCts?.Cancel(); }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { OnLog?.Invoke($"拨入连接异常:{ex.Message}"); }
                finally
                {
                    _stream = null;
                    _client = null;
                    OnDisconnected?.Invoke();
                }
                if (token.IsCancellationRequested) break;
                try { await Task.Delay(2000, token); } catch { break; }
            }
        }

        private async Task HeartbeatLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(_heartbeatIntervalMs, token);
                    if (_heartbeat is { Length: > 0 } && Send(_heartbeat))
                    {
                        OnLog?.Invoke($"发送心跳 {ToHex(_heartbeat)}");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private static string ToHex(byte[] data) => Convert.ToHexString(data);
    }
}
