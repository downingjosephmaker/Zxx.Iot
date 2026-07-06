using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using Microsoft.AspNetCore.SignalR.Client;

namespace IotWebApi.Services.Jobs
{
    public class ChatClient : IHostedService
    {
        private static HubConnection ChatHubClient = null;
        //private TimeSpan[] ReconnectTimeSpans = new TimeSpan[360];
        public Task StartAsync(CancellationToken cancellationToken)
        {
            ////每2分钟重连一次 12小时=12*30=360
            //for (int i = 0; i < 360; i++)
            //{
            //    ReconnectTimeSpans[i] = TimeSpan.FromSeconds(120);
            //}
            var SignalRUrl = AppSetting.GetConfig("SignalRConfig:ConnecString");

            ChatHubClient = new HubConnectionBuilder()
                      .AddJsonProtocol()
                      .WithUrl(SignalRUrl, options =>
                      {
                          options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                          options.UseDefaultCredentials = true;
                      })
                      .Build();
            ChatHubClient.KeepAliveInterval = TimeSpan.FromSeconds(60);
            ChatHubClient.ServerTimeout = TimeSpan.FromSeconds(130);
            ChatHubClient.Closed += DisConnectedAsync;

            // 注册接收服务器消息的回调方法
            ChatHubClient.On<string>("ReceiveMessage", ReceivedAsync);

            Task.Run(() =>
            {
                Task.Delay(1000).Wait();
                try
                {
                    ChatHubClient.StartAsync().Wait();
                }
                catch (Exception ex)
                {
                    DisConnectedAsync(ex);
                }
            });

            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"客户端[{ChatHubClient.ConnectionId}]同服务端开始连接", "SignalR客户端");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            ChatHubClient?.StopAsync().Wait();
            ChatHubClient?.DisposeAsync();
            return Task.CompletedTask;
        }
        /// <summary>
        /// 断开重新连接
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private Task DisConnectedAsync(Exception error)
        {
            Task.Delay(60 * 1000).Wait();
            try
            {
                ChatHubClient.StartAsync().Wait();
            }
            catch (Exception ex)
            {
                DisConnectedAsync(ex);
            }
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"客户端[{ChatHubClient.ConnectionId}]同服务端重新连接", "SignalR客户端");

            return Task.CompletedTask;
        }

        /// <summary>
        /// 收到消息事件
        /// </summary>
        /// <param name="message">消息</param>
        /// <returns></returns>
        private Task ReceivedAsync(string message)
        {
            if (message.IsZxxNullOrEmpty == null) return Task.CompletedTask;

            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"接收数据：{message}", "SignalR客户端");

            try
            {
                var model = message.ToObject<ChatInfo>();
                if (model != null && model.SourceType == "API")
                {
                    object? instance = Activator.CreateInstance(model.ClassType);
                    var methodInfo = model.ClassType.GetMethod(model.MethodName);
                    if (methodInfo != null)
                    {
                        methodInfo.Invoke(instance, null);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "SignalR客户端");
            }

            return Task.CompletedTask;
        }

        public static bool Publish(ChatInfo mdl)
        {
            bool isresult = false;
            try
            {
                if (ChatHubClient != null && ChatHubClient.State == HubConnectionState.Connected)
                {
                    ChatHubClient?.SendAsync("SendMessage", mdl.ToJson()).Wait();
                    isresult = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "SignalR客户端");
            }
            return isresult;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="methodName"></param>
        //public static void RefreshAdminCache(string methodName)
        //{
        //    RefreshCache(typeof(AdminService), methodName);
        //}
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="methodName"></param>
        //public static void RefreshBasicCache(string methodName)
        //{
        //    RefreshCache(typeof(BasicService), methodName);
        //}
        private static bool RefreshCache(Type ClassType, string MethodName)
        {
            bool isresult = false;
            try
            {
                if (ChatHubClient != null && ChatHubClient.State == HubConnectionState.Connected)
                {
                    var mdl = new ChatInfo { SourceType = "API", ClassName = ClassType.Name, ClassType = ClassType, MethodName = MethodName };
                    ChatHubClient?.SendAsync("SendMessage", mdl.ToJson()).Wait();
                    isresult = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "SignalR客户端");
            }
            return isresult;
        }

    }
}
