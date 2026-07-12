using System.Net;
using CenBoCommon.Zxx;
using IotLog;
using NewLife;
using NewLife.Data;
using NewLife.Net;

namespace IotDriverCore
{
    /// <summary>
    /// DTU注册包处理器(注册数据→端点键,null=非注册包继续等待;
    /// consumed返回已消费字节数,注册包与首帧业务数据粘连时剩余字节回灌收帧回调)
    /// </summary>
    public delegate string? DtuRegistrationHandler(byte[] data, out int consumed);

    /// <summary>
    /// TCP服务端通道(自GuoXiang插件NetServer会话管理上提:endpoint会话表/断线通知/新连接替换旧会话;
    /// §6.6 DTU透传接入:RegistrationResolver非空时启用注册模式——连接进入未认证态限时等注册包,
    /// 注册包解析出设备标识绑定会话,粘连的首帧业务字节回灌;心跳包经HeartbeatFilter吞掉;
    /// 应用层空闲超时踢半开连接;RegistrationResolver为空时保持按来源IP归一的原有行为)
    /// </summary>
    public class TcpServerChannel : IChannelTransport, IDisposable
    {
        /// <summary>
        /// 监听端口
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// 会话表锁
        /// </summary>
        private readonly object _netLock = new();

        /// <summary>
        /// 端点→在线会话
        /// </summary>
        private readonly Dictionary<string, NetSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 来源"IP:Port"→端点键(注册/解析成功后登记,收帧与销毁反查用)
        /// </summary>
        private readonly Dictionary<string, string> _remoteMap = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 未认证会话(来源"IP:Port"→会话与连入时刻,注册模式专用)
        /// </summary>
        private readonly Dictionary<string, (NetSession Session, DateTime ConnectTime)> _pending = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 端点→最近活跃时刻(空闲踢除判定)
        /// </summary>
        private readonly Dictionary<string, DateTime> _lastActivity = new(StringComparer.OrdinalIgnoreCase);

        private NetServer? _server;
        private CancellationTokenSource? _watchCts;

        /// <summary>
        /// 来源IP→配置端点键解析器(返回null回退"IP:Port";注册模式下忽略)
        /// </summary>
        public Func<string, string?>? EndpointResolver { get; set; }

        /// <summary>
        /// DTU注册包处理器(非空即启用注册模式:未认证连接的数据先经此解析)
        /// </summary>
        public DtuRegistrationHandler? RegistrationResolver { get; set; }

        /// <summary>
        /// 注册等待超时秒数(超时未注册踢连接,§6.6默认30秒)
        /// </summary>
        public int RegistrationTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 应用层空闲超时秒数(0=不启用;建议心跳周期×3,超时踢半开连接)
        /// </summary>
        public int IdleTimeoutSeconds { get; set; }

        /// <summary>
        /// 心跳包判定器(true=心跳包,吞掉不上抛仅刷新活跃时刻)
        /// </summary>
        public Func<byte[], bool>? HeartbeatFilter { get; set; }

        /// <summary>
        /// 会话建立回调(端点键;注册模式在注册成功后触发)
        /// </summary>
        public Action<string>? SessionOpened { get; set; }

        /// <summary>
        /// 会话断开回调(端点键,上层据此发布疑似离线,原因1=网络连接断开)
        /// </summary>
        public Action<string>? SessionClosed { get; set; }

        /// <summary>
        /// 收帧回调(端点键,原始字节;拆帧交FrameAccumulator)
        /// </summary>
        public Action<string, byte[]>? FrameReceived { get; set; }

        public TcpServerChannel(int port)
        {
            _port = port;
        }

        #region 通道生命周期

        /// <summary>
        /// 启动监听(重复调用幂等;注册模式或空闲踢除启用时拉起看护循环)
        /// </summary>
        public bool Start()
        {
            try
            {
                if (_server == null)
                {
                    _server = new NetServer(IPAddress.Any, _port);
                    _server.NewSession += OnAccept;
                    _server.Received += OnReceived;
                    _server.Error += OnError;
                }
                if (!_server.Active)
                {
                    _server.Start();
                    LogHelper.SysLogWrite("TcpServerChannel", "Start", $"TCP通道[{_port}]启动成功。", "驱动核心");
                }
                if ((RegistrationResolver != null || IdleTimeoutSeconds > 0) && _watchCts == null)
                {
                    _watchCts = new CancellationTokenSource();
                    _ = Task.Run(() => WatchLoopAsync(_watchCts.Token));
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("TcpServerChannel", "Start", ex.ToString(), "驱动核心");
                return false;
            }
        }

        /// <summary>
        /// 停止监听并清空会话表
        /// </summary>
        public void Stop()
        {
            try
            {
                _watchCts?.Cancel();
                _watchCts = null;
                if (_server != null)
                {
                    _server.Stop("通道停止");
                    _server.Dispose();
                    _server = null;
                }
                lock (_netLock)
                {
                    _sessions.Clear();
                    _remoteMap.Clear();
                    _pending.Clear();
                    _lastActivity.Clear();
                }
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("TcpServerChannel", "Stop", ex.ToString(), "驱动核心"); }
        }

        public void Dispose() => Stop();

        /// <summary>
        /// 看护循环:踢注册超时的未认证连接与空闲超时的半开连接
        /// </summary>
        private async Task WatchLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(5_000, token);
                    var now = DateTime.Now;
                    var kicks = new List<NetSession>();
                    lock (_netLock)
                    {
                        foreach (var kv in _pending.Where(t =>
                            (now - t.Value.ConnectTime).TotalSeconds > RegistrationTimeoutSeconds).ToList())
                        {
                            _pending.Remove(kv.Key);
                            kicks.Add(kv.Value.Session);
                            LogHelper.SysLogWrite("TcpServerChannel", "WatchLoopAsync", $"TCP通道[{_port}]：{kv.Key}注册超时踢除。", "驱动核心");
                        }
                        if (IdleTimeoutSeconds > 0)
                        {
                            foreach (var kv in _lastActivity.Where(t =>
                                (now - t.Value).TotalSeconds > IdleTimeoutSeconds).ToList())
                            {
                                if (_sessions.TryGetValue(kv.Key, out var session))
                                {
                                    kicks.Add(session);
                                    LogHelper.SysLogWrite("TcpServerChannel", "WatchLoopAsync", $"TCP通道[{_port}]：{kv.Key}空闲超时踢除。", "驱动核心");
                                }
                                _lastActivity.Remove(kv.Key);
                            }
                        }
                    }
                    foreach (var session in kicks)
                    {
                        try { session.Dispose(); } catch { }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 通道停止，正常退出
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("TcpServerChannel", "WatchLoopAsync", ex.ToString(), "驱动核心"); }
        }

        #endregion

        #region 会话管理

        /// <summary>
        /// IPv4-mapped IPv6地址还原为IPv4字符串
        /// </summary>
        private static string NormalizeIp(IPAddress address) =>
            address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();

        /// <summary>
        /// 来源远端键("IP:Port")
        /// </summary>
        private static string RemoteKey(IPAddress address, int port) => $"{NormalizeIp(address)}:{port}";

        /// <summary>
        /// 新连接建立:注册模式进未认证态等注册包;IP解析模式直接登记会话表(旧会话替换销毁)
        /// </summary>
        private void OnAccept(object? sender, NetSessionEventArgs e)
        {
            try
            {
                var session = (NetSession)e.Session;
                session.OnDisposed += OnSessionDisposed;
                string remotekey = RemoteKey(session.Remote.Address, session.Remote.Port);

                if (RegistrationResolver != null)
                {
                    lock (_netLock) { _pending[remotekey] = (session, DateTime.Now); }
                    LogHelper.SysLogWrite("TcpServerChannel", "OnAccept", $"TCP通道[{_port}]：{remotekey}连入，等待注册包(限时{RegistrationTimeoutSeconds}秒)。", "驱动核心");
                    return;
                }

                string ip = NormalizeIp(session.Remote.Address);
                string key = EndpointResolver?.Invoke(ip) ?? remotekey;
                BindSession(key, remotekey, session);
                LogHelper.SysLogWrite("TcpServerChannel", "OnAccept", $"TCP通道[{_port}]：{key}连接建立(来源{remotekey})。", "驱动核心");
                SessionOpened?.Invoke(key);
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("TcpServerChannel", "OnAccept", ex.ToString(), "驱动核心"); }
        }

        /// <summary>
        /// 会话绑定到端点键(同端点旧会话销毁替换,§6.6 DTU掉线重连常残留旧连接)
        /// </summary>
        private void BindSession(string key, string remotekey, NetSession session)
        {
            NetSession? old;
            lock (_netLock)
            {
                _sessions.TryGetValue(key, out old);
                _sessions[key] = session;
                _remoteMap[remotekey] = key;
                _lastActivity[key] = DateTime.Now;
            }
            if (old != null && !ReferenceEquals(old, session))
            {
                try { old.Dispose(); } catch { }
            }
        }

        /// <summary>
        /// 会话销毁:仅当会话表仍指向本会话才移除并通知(替换旧会话时旧会话销毁不得误删新会话)
        /// </summary>
        private void OnSessionDisposed(object? sender, EventArgs e)
        {
            try
            {
                if (sender is not NetSession session) return;
                string remotekey = RemoteKey(session.Remote.Address, session.Remote.Port);
                string? closedkey = null;
                lock (_netLock)
                {
                    _pending.Remove(remotekey);
                    if (_remoteMap.TryGetValue(remotekey, out var key))
                    {
                        _remoteMap.Remove(remotekey);
                        if (_sessions.TryGetValue(key, out var current) && ReferenceEquals(current, session))
                        {
                            _sessions.Remove(key);
                            _lastActivity.Remove(key);
                            closedkey = key;
                        }
                    }
                }
                if (closedkey == null) return;
                LogHelper.SysLogWrite("TcpServerChannel", "OnSessionDisposed", $"TCP通道[{_port}]：{closedkey}连接断开。", "驱动核心");
                SessionClosed?.Invoke(closedkey);
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("TcpServerChannel", "OnSessionDisposed", ex.ToString(), "驱动核心"); }
        }

        /// <summary>
        /// 会话异常:主动释放底层资源,移除与通知逻辑同OnSessionDisposed
        /// </summary>
        private void OnError(object? sender, ExceptionEventArgs e)
        {
            try
            {
                if (sender is not NetSession session) return;
                try { session.Dispose(); } catch { }
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("TcpServerChannel", "OnError", ex.ToString(), "驱动核心"); }
        }

        /// <summary>
        /// 收到数据包:未认证连接先走注册解析(粘连的业务字节回灌);
        /// 已认证连接经心跳过滤后原样上抛,拆帧/校验由驱动完成
        /// </summary>
        private void OnReceived(object? sender, ReceivedEventArgs e)
        {
            try
            {
                string remotekey = RemoteKey(e.Remote.Address, e.Remote.Port);
                var buffer = e.Packet.ReadBytes();

                if (RegistrationResolver != null)
                {
                    (NetSession Session, DateTime ConnectTime) pend;
                    bool ispending;
                    lock (_netLock) { ispending = _pending.TryGetValue(remotekey, out pend); }
                    if (ispending)
                    {
                        var key = RegistrationResolver(buffer, out int consumed);
                        if (key.IsZxxNullOrEmpty())
                        {
                            LogHelper.SysLogWrite("TcpServerChannel", "OnReceived", $"TCP通道[{_port}]：{remotekey}未认证数据丢弃，{buffer.ToHex()}", "驱动核心");
                            return;
                        }
                        lock (_netLock) { _pending.Remove(remotekey); }
                        BindSession(key!, remotekey, pend.Session);
                        LogHelper.SysLogWrite("TcpServerChannel", "OnReceived", $"TCP通道[{_port}]：{remotekey}注册成功→{key}。", "驱动核心");
                        SessionOpened?.Invoke(key!);
                        // 注册包与首帧业务数据粘连:剩余字节回灌
                        if (consumed >= 0 && consumed < buffer.Length)
                        {
                            var leftover = new byte[buffer.Length - consumed];
                            Array.Copy(buffer, consumed, leftover, 0, leftover.Length);
                            FrameReceived?.Invoke(key!, leftover);
                        }
                        return;
                    }
                    string? endpoint;
                    lock (_netLock) { _remoteMap.TryGetValue(remotekey, out endpoint); }
                    if (endpoint == null) return;
                    Deliver(endpoint, buffer);
                    return;
                }

                string ip = NormalizeIp(e.Remote.Address);
                string resolvedkey = EndpointResolver?.Invoke(ip) ?? remotekey;
                Deliver(resolvedkey, buffer);
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("TcpServerChannel", "OnReceived", ex.ToString(), "驱动核心"); }
        }

        /// <summary>
        /// 上抛数据(刷新活跃时刻,心跳包吞掉不上抛)
        /// </summary>
        private void Deliver(string endpoint, byte[] buffer)
        {
            lock (_netLock) { _lastActivity[endpoint] = DateTime.Now; }
            if (HeartbeatFilter != null && HeartbeatFilter(buffer)) return;
            FrameReceived?.Invoke(endpoint, buffer);
        }

        /// <summary>
        /// 端点是否在线
        /// </summary>
        public bool IsOnline(string endpoint)
        {
            lock (_netLock) { return _sessions.ContainsKey(endpoint); }
        }

        #endregion

        #region IChannelTransport

        /// <summary>
        /// 向指定端点会话发送原始字节(端点不在线返回false)
        /// </summary>
        public bool Send(string endpoint, byte[] payload)
        {
            try
            {
                NetSession? session;
                lock (_netLock) { _sessions.TryGetValue(endpoint, out session); }
                if (session == null) return false;
                session.Send(new Packet(payload));
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("TcpServerChannel", "Send", ex.ToString(), "驱动核心");
                return false;
            }
        }

        #endregion
    }
}
