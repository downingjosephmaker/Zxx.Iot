using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using IotWebApi.Services.Jobs;
using Microsoft.AspNetCore.SignalR.Client;
using Quartz;
using System.Text.Json;

namespace IotWebApi.Services.Jobs
{
    /// <summary>
    /// SignalR客户端处理任务
    /// </summary>
    [DisallowConcurrentExecution] // 禁止并发执行
    public class ChatClientJob : BaseJob
    {
        private static HubConnection? _chatHubClient;

        /// <summary>
        /// 执行任务
        /// </summary>
        protected override async Task<string> ExecuteJob(IJobExecutionContext context)
        {
            try
            {
                // 获取SignalR相关配置
                string signalRUrl = AppSetting.GetConfig("SignalRConfig:ConnecString");

                // 检查当前连接状态
                string statusMessage = CheckConnectionStatus();
                string messs = "";

                // 如果连接不存在或已断开，则重新连接
                if (_chatHubClient == null || _chatHubClient.State != HubConnectionState.Connected)
                {
                    LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"SignalR客户端连接状态: {statusMessage}，准备重新建立连接", "SignalR重连任务");

                    // 创建SignalR连接
                    await CreateConnection(signalRUrl);

                    // 重新检查连接状态
                    statusMessage = CheckConnectionStatus();

                    if (_chatHubClient?.State == HubConnectionState.Connected)
                    {
                        messs = $"SignalR客户端重连成功，连接ID: {_chatHubClient.ConnectionId}";
                        LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, messs, "SignalR重连任务");
                        return messs;
                    }
                    else
                    {
                        messs = $"SignalR客户端重连失败，当前状态: {statusMessage}";
                        LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, messs, "SignalR重连任务");
                        return messs;
                    }
                }

                // 如果连接正常，执行心跳检测
                var pingResult = await SendPing();
                messs = $"SignalR客户端连接状态: {statusMessage}，心跳检测: {pingResult}";
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, messs, "SignalR重连任务");

                return messs;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"SignalR客户端重连任务执行异常: {ex}", "SignalR重连任务");
                throw; // 将异常抛出，由基类记录错误日志
            }
        }

        /// <summary>
        /// 检查SignalR连接状态
        /// </summary>
        private string CheckConnectionStatus()
        {
            if (_chatHubClient == null)
                return "未初始化";

            return _chatHubClient.State switch
            {
                HubConnectionState.Disconnected => "已断开",
                HubConnectionState.Connected => "已连接",
                HubConnectionState.Connecting => "连接中",
                HubConnectionState.Reconnecting => "重连中",
                _ => "未知状态"
            };
        }

        /// <summary>
        /// 创建SignalR连接
        /// </summary>
        private async Task CreateConnection(string signalRUrl)
        {
            try
            {
                // 如果已有连接对象但状态不是Connected，先尝试关闭
                if (_chatHubClient != null)
                {
                    try
                    {
                        await _chatHubClient.StopAsync();
                        await _chatHubClient.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                            $"关闭旧连接时出错: {ex.Message}", "SignalR重连任务");
                    }
                }

                // 创建新的连接
                _chatHubClient = new HubConnectionBuilder()
                    .AddJsonProtocol()
                    .WithUrl(signalRUrl, options =>
                    {
                        options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                        options.UseDefaultCredentials = true;
                    })
                    .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2) })
                    .Build();

                // 设置参数
                _chatHubClient.KeepAliveInterval = TimeSpan.FromSeconds(60);
                _chatHubClient.ServerTimeout = TimeSpan.FromSeconds(130);

                // 注册事件处理
                _chatHubClient.Closed += HandleConnectionClosed;
                _chatHubClient.Reconnecting += HandleReconnecting;
                _chatHubClient.Reconnected += HandleReconnected;

                // 注册接收服务器消息的回调方法
                _chatHubClient.On<string>("ReceiveMessage", HandleReceivedMessage);

                // 启动连接
                await _chatHubClient.StartAsync();

                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"SignalR连接已创建，连接ID: {_chatHubClient.ConnectionId}", "SignalR重连任务");
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"创建SignalR连接失败: {ex}", "SignalR重连任务");
            }
        }

        /// <summary>
        /// 处理连接关闭事件
        /// </summary>
        private Task HandleConnectionClosed(Exception? ex)
        {
            if (ex != null)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"SignalR连接已关闭，错误: {ex.Message}", "SignalR重连任务");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 处理正在重连事件
        /// </summary>
        private Task HandleReconnecting(Exception? ex)
        {
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                $"SignalR正在尝试重新连接，原因: {ex?.Message}", "SignalR重连任务");

            return Task.CompletedTask;
        }

        /// <summary>
        /// 处理重连成功事件
        /// </summary>
        private Task HandleReconnected(string? connectionId)
        {
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                $"SignalR重连成功，新连接ID: {connectionId}", "SignalR重连任务");

            return Task.CompletedTask;
        }

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private Task HandleReceivedMessage(string message)
        {
            if (message.IsZxxNullOrEmpty())
                return Task.CompletedTask;

            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                $"接收到SignalR消息: {message}", "SignalR重连任务");

            try
            {
                // 解析消息
                var chatInfo = JsonSerializer.Deserialize<ChatInfo>(message);
                // 处理消息
                if (chatInfo != null && chatInfo.SourceType == "API" &&
                    !string.IsNullOrEmpty(chatInfo.ClassName) &&
                    !string.IsNullOrEmpty(chatInfo.MethodName) &&
                    chatInfo.ClassType != null)
                {
                    // 创建实例并调用方法
                    object? instance = Activator.CreateInstance(chatInfo.ClassType);
                    if (instance != null)
                    {
                        var methodInfo = chatInfo.ClassType.GetMethod(chatInfo.MethodName);
                        if (methodInfo != null)
                        {
                            methodInfo.Invoke(instance, null);
                            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                                $"成功调用方法: {chatInfo.ClassName}.{chatInfo.MethodName}", "SignalR重连任务");
                        }
                        else
                        {
                            LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                                $"找不到方法: {chatInfo.ClassName}.{chatInfo.MethodName}", "SignalR重连任务");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"处理SignalR消息时出错: {ex}", "SignalR重连任务");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 发送心跳检测
        /// </summary>
        private async Task<string> SendPing()
        {
            try
            {
                if (_chatHubClient != null && _chatHubClient.State == HubConnectionState.Connected)
                {
                    // 调用心跳方法
                    await _chatHubClient.InvokeAsync("Ping");
                    return "成功";
                }
                return "失败：连接不可用";
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"心跳检测失败: {ex.Message}", "SignalR重连任务");
                return $"失败：{ex.Message}";
            }
        }

        ///// <summary>
        ///// 刷新管理员缓存
        ///// </summary>
        //public static bool RefreshAdminCache(string methodName)
        //{
        //    return RefreshCache(typeof(AdminService), methodName);
        //}

        ///// <summary>
        ///// 刷新基础服务缓存
        ///// </summary>
        //public static bool RefreshBasicCache(string methodName)
        //{
        //    return RefreshCache(typeof(BasicService), methodName);
        //}

        ///// <summary>
        ///// 刷新缓存通用方法
        ///// </summary>
        //private static bool RefreshCache(Type classType, string methodName)
        //{
        //    bool result = false;
        //    try
        //    {
        //        if (_chatHubClient != null && _chatHubClient.State == HubConnectionState.Connected)
        //        {
        //            // 创建缓存刷新消息
        //            var chatInfo = new ChatInfo
        //            {
        //                SourceType = "API",
        //                ClassName = classType.Name,
        //                ClassType = classType,
        //                MethodName = methodName
        //            };
        //            _chatHubClient?.SendAsync("SendMessage", chatInfo.ToJson()).Wait();

        //            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
        //                $"已发送缓存刷新消息: {classType.Name}.{methodName}", "SignalR重连任务");

        //            result = true;
        //        }
        //        else
        //        {
        //            LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
        //                "无法刷新缓存，SignalR连接不可用", "SignalR重连任务");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
        //            $"刷新缓存失败: {ex.Message}", "SignalR重连任务");
        //    }

        //    return result;
        //}

        /// <summary>
        /// 发布消息到SignalR
        /// </summary>
        public static bool PublishMessage(ChatInfo message)
        {
            bool result = false;
            try
            {
                if (_chatHubClient != null && _chatHubClient.State == HubConnectionState.Connected)
                {
                    _chatHubClient?.SendAsync("SendMessage", message.ToJson()).Wait();

                    LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"已发布消息: {message.ClassName}.{message.MethodName}", "SignalR重连任务");

                    result = true;
                }
                else
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                        "无法发布消息，SignalR连接不可用", "SignalR重连任务");
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"发布消息失败: {ex.Message}", "SignalR重连任务");
            }

            return result;
        }
    }
}