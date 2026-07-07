using System.Net;
using IotLog;
using NewLife;
using NewLife.Data;
using NewLife.Net;

namespace IotDriverCore
{
    /// <summary>
    /// TCP服务端通道(自GuoXiang插件NetServer会话管理上提:endpoint会话表/断线通知/新连接替换旧会话;
    /// 设备或DTU作为客户端拨入,端点键默认"IP:Port",可挂EndpointResolver按来源IP归一到配置键;
    /// 旧会话销毁时校验引用防止误删新会话)
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

        private NetServer? _server;

        /// <summary>
        /// 来源IP→配置端点键解析器(返回null回退"IP:Port")
        /// </summary>
        public Func<string, string?>? EndpointResolver { get; set; }

        /// <summary>
        /// 会话建立回调(端点键)
        /// </summary>
        public Action<string>? SessionOpened { get; set; }

        /// <summary>
        /// 会话断开回调(端点键,上层据此发布疑似离线,原因1=网络连接断开)
        /// </summary>
        public Action<string>? SessionClosed { get; set; }

        /// <summary>
        /// 收帧回调(端点键,原始字节)
        /// </summary>
        public Action<string, byte[]>? FrameReceived { get; set; }

        public TcpServerChannel(int port)
        {
            _port = port;
        }

        #region 通道生命周期

        /// <summary>
        /// 启动监听(重复调用幂等)
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
                    LogHelper.Info($"TCP通道[{_port}]启动成功。");
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
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
                if (_server != null)
                {
                    _server.Stop("通道停止");
                    _server.Dispose();
                    _server = null;
                }
                lock (_netLock) { _sessions.Clear(); }
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        public void Dispose() => Stop();

        #endregion

        #region 会话管理

        /// <summary>
        /// IPv4-mapped IPv6地址还原为IPv4字符串
        /// </summary>
        private static string NormalizeIp(IPAddress address) =>
            address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();

        /// <summary>
        /// 解析端点键(EndpointResolver命中配置键,否则回退"IP:Port")
        /// </summary>
        private string ResolveKey(IPAddress address, int port)
        {
            string ip = NormalizeIp(address);
            return EndpointResolver?.Invoke(ip) ?? $"{ip}:{port}";
        }

        /// <summary>
        /// 新连接建立:登记会话表;同端点已有旧会话则销毁替换(DTU掉线重连常残留旧连接)
        /// </summary>
        private void OnAccept(object? sender, NetSessionEventArgs e)
        {
            try
            {
                var session = (NetSession)e.Session;
                session.OnDisposed += OnSessionDisposed;
                string key = ResolveKey(session.Remote.Address, session.Remote.Port);
                NetSession? old;
                lock (_netLock)
                {
                    _sessions.TryGetValue(key, out old);
                    _sessions[key] = session;
                }
                if (old != null && !ReferenceEquals(old, session))
                {
                    try { old.Dispose(); } catch { }
                }
                LogHelper.Info($"TCP通道[{_port}]：{key}连接建立(来源{NormalizeIp(session.Remote.Address)}:{session.Remote.Port})。");
                SessionOpened?.Invoke(key);
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        /// <summary>
        /// 会话销毁:仅当会话表仍指向本会话才移除并通知(替换旧会话时旧会话销毁不得误删新会话)
        /// </summary>
        private void OnSessionDisposed(object? sender, EventArgs e)
        {
            try
            {
                if (sender is not NetSession session) return;
                string key = ResolveKey(session.Remote.Address, session.Remote.Port);
                bool removed = false;
                lock (_netLock)
                {
                    if (_sessions.TryGetValue(key, out var current) && ReferenceEquals(current, session))
                    {
                        _sessions.Remove(key);
                        removed = true;
                    }
                }
                if (!removed) return;
                LogHelper.Info($"TCP通道[{_port}]：{key}连接断开。");
                SessionClosed?.Invoke(key);
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        /// <summary>
        /// 会话异常:主动释放底层资源,移除与通知逻辑同OnSessionDisposed
        /// </summary>
        private void OnError(object? sender, ExceptionEventArgs e)
        {
            try
            {
                if (sender is not NetSession session) return;
                string key = ResolveKey(session.Remote.Address, session.Remote.Port);
                bool removed = false;
                lock (_netLock)
                {
                    if (_sessions.TryGetValue(key, out var current) && ReferenceEquals(current, session))
                    {
                        _sessions.Remove(key);
                        removed = true;
                    }
                }
                try { session.Dispose(); } catch { }
                if (!removed) return;
                LogHelper.Info($"TCP通道[{_port}]：{key}连接错误断开。");
                SessionClosed?.Invoke(key);
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        /// <summary>
        /// 收到数据包:解析端点键后原样上抛,帧定界/校验由驱动完成
        /// </summary>
        private void OnReceived(object? sender, ReceivedEventArgs e)
        {
            try
            {
                string key = ResolveKey(e.Remote.Address, e.Remote.Port);
                FrameReceived?.Invoke(key, e.Packet.ReadBytes());
            }
            catch (Exception ex) { LogHelper.Error(ex); }
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
                LogHelper.Error(ex);
                return false;
            }
        }

        #endregion
    }
}
