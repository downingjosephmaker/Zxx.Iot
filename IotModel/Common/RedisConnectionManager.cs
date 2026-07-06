using CenBoCommon.Zxx;
using IotLog;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IotModel
{
    public class RedisConnectionManager
    {
        private static int _defaultDatabase = 0;
        private static readonly object _lock = new object();
        private static readonly int _maxRetryCount = 3;
        private static readonly int _retryDelay = 1000; // 1秒
        private static ConnectionMultiplexer _connection;
        private static bool _isReconnecting = false;
        private static bool _isClusterDown = false;
        private static DateTime _lastReconnectAttempt = DateTime.MinValue;
        private static readonly TimeSpan _reconnectCooldown = TimeSpan.FromSeconds(10); // 重连冷却时间改为10秒
        private static int _connectionVersion = 0; // 连接版本号

        private static void InitializeConnection()
        {
            lock (_lock)
            {
                if (_connection != null && _connection.IsConnected)
                {
                    return;
                }

                try
                {
                    // 只有在连接为 null 或完全失效时才创建新连接
                    bool needNewConnection = false;

                    if (_connection == null)
                    {
                        needNewConnection = true;
                        LogHelper.SysLogWrite("RedisConnectionManager", "Execute", "初始化Redis连接（首次）", "Redis");;
                    }
                    else if (!_connection.IsConnected)
                    {
                        // 检查是否所有端点都断开
                        var endpoints = _connection.GetEndPoints();
                        bool allDisconnected = endpoints.All(ep =>
                        {
                            var server = _connection.GetServer(ep);
                            return !server.IsConnected;
                        });

                        if (allDisconnected)
                        {
                            needNewConnection = true;
                            LogHelper.SysLogWrite("RedisConnectionManager", "Execute", "所有端点已断开，需要重新创建连接", "Redis");;
                        }
                        else
                        {
                            LogHelper.SysLogWrite("RedisConnectionManager", "Execute", "部分端点仍连接，等待自动恢复", "Redis");;
                            return; // 让 StackExchange.Redis 自动重连
                        }
                    }

                    if (needNewConnection)
                    {
                        // 清理旧连接
                        if (_connection != null)
                        {
                            try
                            {
                                _connection.ConnectionFailed -= OnConnectionFailed;
                                _connection.ConnectionRestored -= OnConnectionRestored;
                                _connection.ErrorMessage -= OnErrorMessage;
                                _connection.InternalError -= OnInternalError;

                                // 给一个短暂时间让正在进行的操作完成
                                Thread.Sleep(500);

                                _connection.Close(false); // allowCommandsToComplete = false
                                _connection.Dispose();

                                LogHelper.SysLogWrite("RedisConnectionManager", "Execute", "已清理旧连接", "Redis");;
                            }
                            catch (Exception ex)
                            {
                                LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"清理旧连接时出错: {ex.Message}", "Redis");;
                            }
                            _connection = null;
                        }

                        var config = GetConfigurationOptions();
                        _connection = ConnectionMultiplexer.Connect(config);

                        // 注册事件处理器
                        _connection.ConnectionFailed += OnConnectionFailed;
                        _connection.ConnectionRestored += OnConnectionRestored;
                        _connection.ErrorMessage += OnErrorMessage;
                        _connection.InternalError += OnInternalError;

                        _connectionVersion++; // 增加版本号
                        LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"Redis连接已建立（版本: {_connectionVersion}）", "Redis");;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite("RedisConnectionManager", "Execute", ex.ToString(), "Redis");
                    _connection = null;
                    throw;
                }
            }
        }

        private static void OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"Redis连接失败: {e.Exception?.Message}, EndPoint: {e.EndPoint}, ConnectionType: {e.ConnectionType}, FailureType: {e.FailureType}", "Redis");;

            // 只有在致命错误时才触发重连
            if (e.FailureType == ConnectionFailureType.UnableToConnect ||
                e.FailureType == ConnectionFailureType.InternalFailure)
            {
                // 检查是否需要重连
                if (_isReconnecting)
                {
                    LogHelper.SysLogWrite("RedisConnectionManager", "Execute", "正在重连中，跳过本次重连请求", "Redis");;
                    return;
                }

                var timeSinceLastAttempt = DateTime.Now - _lastReconnectAttempt;
                if (timeSinceLastAttempt < _reconnectCooldown)
                {
                    LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"距离上次重连仅 {timeSinceLastAttempt.TotalSeconds:F1} 秒，跳过本次重连", "Redis");;
                    return;
                }

                _isReconnecting = true;
                _lastReconnectAttempt = DateTime.Now;

                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(5000); // 等待5秒再重连

                        LogHelper.SysLogWrite("RedisConnectionManager", "Execute", "开始重新创建Redis连接...", "Redis");;
                        InitializeConnection();
                        LogHelper.SysLogWrite("RedisConnectionManager", "Execute", "Redis重连成功", "Redis");;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"Redis重连失败: {ex.Message}", "Redis");;
                    }
                    finally
                    {
                        _isReconnecting = false;
                    }
                });
            }
            else
            {
                // 其他类型的失败，让 StackExchange.Redis 自动处理
                LogHelper.SysLogWrite("RedisConnectionManager", "Execute", "非致命错误，由StackExchange.Redis自动处理", "Redis");;
            }
        }

        private static void OnConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"Redis连接已恢复: EndPoint: {e.EndPoint}, ConnectionType: {e.ConnectionType}", "Redis");;
            _isClusterDown = false;
        }

        private static void OnErrorMessage(object sender, RedisErrorEventArgs e)
        {
            LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"Redis错误消息: {e.Message}", "Redis");;
            if (e.Message.Contains("CLUSTERDOWN"))
            {
                _isClusterDown = true;
                LogHelper.SysLogWrite("RedisConnectionManager", "Execute", "Redis集群已关闭，等待恢复...", "Redis");;
            }
        }

        private static void OnInternalError(object sender, InternalErrorEventArgs e)
        {
            LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"Redis内部错误: {e.Exception?.Message}, Origin: {e.Origin}", "Redis");;
        }

        public static ConnectionMultiplexer Connection
        {
            get
            {
                if (_connection == null)
                {
                    try
                    {
                        InitializeConnection();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"获取Redis连接失败: {ex.Message}", "Redis");;
                        return null;
                    }
                }
                return _connection;
            }
        }

        public static IDatabase GetDatabase(int? db = null)
        {
            try
            {
                // 尝试获取数据库连接
                for (int i = 0; i < _maxRetryCount; i++)
                {
                    try
                    {
                        var connection = Connection;
                        if (connection == null)
                        {
                            LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"Redis连接不可用，等待重试 {i + 1}/{_maxRetryCount}", "Redis");;
                            if (i < _maxRetryCount - 1)
                            {
                                Thread.Sleep(_retryDelay);
                            }
                            continue;
                        }

                        if (_isClusterDown)
                        {
                            LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"Redis集群已关闭，等待恢复 {i + 1}/{_maxRetryCount}", "Redis");;
                            if (i < _maxRetryCount - 1)
                            {
                                Thread.Sleep(_retryDelay);
                            }
                            continue;
                        }

                        // 如果正在重连中，等待一段时间
                        if (_isReconnecting)
                        {
                            LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"检测到正在重连，等待完成 {i + 1}/{_maxRetryCount}", "Redis");;
                            if (i < _maxRetryCount - 1)
                            {
                                Thread.Sleep(_retryDelay * 3); // 等待3秒
                                continue;
                            }
                        }

                        // 获取数据库实例
                        var database = connection.GetDatabase(db ?? _defaultDatabase);

                        // 快速测试连接可用性
                        try
                        {
                            // 使用 ConfigureAwait(false) 避免死锁
                            var pingTask = database.PingAsync();
                            if (pingTask.Wait(2000)) // 等待2秒
                            {
                                var pingResult = pingTask.Result;
                                if (pingResult.TotalMilliseconds >= 0)
                                {
                                    return database;
                                }
                            }
                            else
                            {
                                LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"Ping超时 {i + 1}/{_maxRetryCount}", "Redis");;
                            }
                        }
                        catch (AggregateException ae)
                        {
                            var innerEx = ae.InnerException ?? ae;
                            if (innerEx is ObjectDisposedException)
                            {
                                LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"连接对象已释放 {i + 1}/{_maxRetryCount}", "Redis");;
                                // 等待更长时间，让重连完成
                                if (i < _maxRetryCount - 1)
                                {
                                    Thread.Sleep(_retryDelay * 3);
                                    continue;
                                }
                            }
                            else if (innerEx is RedisConnectionException)
                            {
                                LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"Redis连接异常: {innerEx.Message}", "Redis");;
                            }
                            else
                            {
                                LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"Ping测试失败: {innerEx.GetType().Name} - {innerEx.Message}", "Redis");
                            }
                        }

                        // 等待后重试
                        if (i < _maxRetryCount - 1)
                        {
                            Thread.Sleep(_retryDelay);
                        }
                    }
                    catch (ObjectDisposedException ex)
                    {
                        // 连接对象已被释放
                        LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"连接已释放，等待重连完成 {i + 1}/{_maxRetryCount}: {ex.Message}", "Redis");;

                        if (i < _maxRetryCount - 1)
                        {
                            // 等待重连完成
                            Thread.Sleep(_retryDelay * 3);
                        }
                    }
                    catch (RedisConnectionException ex)
                    {
                        LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"Redis连接异常 {i + 1}/{_maxRetryCount}: {ex.Message}", "Redis");;

                        if (i < _maxRetryCount - 1)
                        {
                            Thread.Sleep(_retryDelay);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"获取数据库失败 {i + 1}/{_maxRetryCount}: {ex.GetType().Name} - {ex.Message}", "Redis");

                        if (i < _maxRetryCount - 1)
                        {
                            Thread.Sleep(_retryDelay);
                        }
                    }
                }

                LogHelper.SysLogWrite("RedisConnectionManager", "Execute", "无法获取Redis数据库连接，已达到最大重试次数", "Redis");;
                return null;
            }
            catch (Exception ex)
            {
                LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"GetDatabase发生未预期的异常: {ex.GetType().Name} - {ex.Message}", "Redis");
                LogHelper.ErrorLogWrite("RedisConnectionManager", "Execute", ex.ToString(), "Redis");
                return null;
            }
        }

        /// <summary>
        /// 获取Redis配置选项
        /// </summary>
        private static ConfigurationOptions GetConfigurationOptions()
        {
            var config = new ConfigurationOptions
            {
                ConnectTimeout = 5000,
                SyncTimeout = 10000,
                AsyncTimeout = 10000,
                KeepAlive = 60,
                ConnectRetry = 3,
                ReconnectRetryPolicy = new ExponentialRetry(1000, 10000), // 增加最大间隔
                ClientName = "ZhjngkWebApi",
                AbortOnConnectFail = false, // 重要：不要在连接失败时中止
                AllowAdmin = true,
                Ssl = false,
                DefaultDatabase = 0,
                Password = "Cenbo88211111"
            };

            // 解析连接字符串
            var connectionString = DbSetting.Current.TendisConString;
            if (DbSetting.Current.IsTendisCluster) connectionString = DbSetting.Current.TendisConStringCluster;

            if (string.IsNullOrEmpty(connectionString))
            {
                config.EndPoints.Add("localhost:6379");
                return config;
            }

            try
            {
                // 解析连接字符串（格式：Server=host:port,host:port;Pwd=password;Database=db）
                var parts = connectionString.Split(';');

                foreach (var part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part)) continue;

                    var keyValue = part.Split('=');
                    if (keyValue.Length != 2) continue;

                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();

                    switch (key.ToLower())
                    {
                        case "server":
                            // 解析服务器地址（支持多个，逗号分隔）
                            var endpoints = value.Split(',');
                            foreach (var endpoint in endpoints)
                            {
                                var trimmedEndpoint = endpoint.Trim();
                                if (string.IsNullOrEmpty(trimmedEndpoint)) continue;

                                var hostAndPort = trimmedEndpoint.Split(':');
                                var host = hostAndPort[0].Trim();
                                var port = hostAndPort.Length > 1 ? hostAndPort[1].Trim().ToZxxInt() : 6379;

                                if (!string.IsNullOrEmpty(host))
                                {
                                    config.EndPoints.Add(host, port);
                                }
                            }
                            break;

                        case "pwd":
                            config.Password = value;
                            break;

                        case "db":
                            _defaultDatabase = value.ToZxxInt();
                            config.DefaultDatabase = _defaultDatabase;
                            break;

                        case "connecttimeout":
                            config.ConnectTimeout = value.ToZxxInt();
                            break;

                        case "synctimeout":
                            config.SyncTimeout = value.ToZxxInt();
                            break;

                        case "asynctimeout":
                            config.AsyncTimeout = value.ToZxxInt();
                            break;

                        case "keepalive":
                            config.KeepAlive = value.ToZxxInt();
                            break;

                        case "connectretry":
                            config.ConnectRetry = value.ToZxxInt();
                            break;

                        case "clientname":
                            config.ClientName = value;
                            break;

                        case "abortconnect":
                        case "abortonconnectfail":
                            config.AbortOnConnectFail = value.ToZxxBoolean();
                            break;

                        case "allowadmin":
                            config.AllowAdmin = value.ToZxxBoolean();
                            break;

                        case "ssl":
                            config.Ssl = value.ToZxxBoolean();
                            break;
                    }
                }

                // 如果没有解析到任何端点，使用默认配置
                if (config.EndPoints.Count == 0)
                {
                    LogHelper.SysLogWrite("RedisConnectionManager", "Execute", "警告: 未能从连接字符串中解析到任何端点，使用默认配置", "Redis");;
                    config.EndPoints.Add("localhost:6379");
                }
                // 如果是集群模式（多个端点），添加集群配置
                else if (config.EndPoints.Count > 1)
                {
                    config.CommandMap = CommandMap.Default;
                    config.TieBreaker = "";
                    config.DefaultVersion = new Version(3, 0, 0);
                    LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"检测到集群模式，端点数量: {config.EndPoints.Count}", "Redis");;
                }
                else
                {
                    LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"检测到单机模式，端点: {config.EndPoints[0]}", "Redis");;
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("RedisConnectionManager", "Execute", ex.ToString(), "Redis");
                LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"解析连接字符串失败: {connectionString}", "Redis");;
                // 如果解析失败，使用默认配置
                config.EndPoints.Clear();
                config.EndPoints.Add("localhost:6379");
            }

            return config;
        }

        /// <summary>
        /// 手动关闭连接（应用程序退出时调用）
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                if (_connection != null)
                {
                    try
                    {
                        _connection.ConnectionFailed -= OnConnectionFailed;
                        _connection.ConnectionRestored -= OnConnectionRestored;
                        _connection.ErrorMessage -= OnErrorMessage;
                        _connection.InternalError -= OnInternalError;
                        _connection.Close();
                        _connection.Dispose();
                        _connection = null;
                        LogHelper.SysLogWrite("RedisConnectionManager", "Execute", "Redis连接已关闭", "Redis");;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.SysLogWrite("RedisConnectionManager", "Execute", $"关闭Redis连接时出错: {ex.Message}", "Redis");;
                    }
                }
            }
        }
    }
}