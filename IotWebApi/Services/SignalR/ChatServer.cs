using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using Microsoft.AspNetCore.SignalR;

namespace IotWebApi.Services.Jobs
{
    /// <summary>
    /// SignalR服务端
    /// </summary>
    public class ChatServer : Hub
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task UserLogin(string token)
        {
            await Task.Delay(50); //模拟异步操作
            var useropt = OperatorCommon.UserList.Find(t => t.Token == token);
            if (useropt != null)
            {
                useropt.UserSignalR = Context.ConnectionId;
                LogHelper.SysLogWrite("ChatServer", "UserLogin", $"用户登录：{useropt.UserId}-{useropt.UserSignalR}", "SignalR服务端");
            }
        }

        /// <summary>
        /// ping
        /// </summary>
        /// <returns></returns>
        public async Task Ping()
        {
            await Task.Delay(50); //模拟异步操作
        }

        /// <summary>
        /// 可通过已连接客户端调用 SendMessage，以向所有客户端发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessage(string message)
        {
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"用户名称{Context.ConnectionId},收到消息{message}", "SignalR服务端");
            // 广播消息给所有连接的客户端
            // ReceiveMessage 是客户端监听的方法
            await Clients.All.SendAsync("ReceiveMessage", message);

            /*
                // 常用方法
                // 给所有人发送消息
                await Clients.All.SendAsync("ReceiveMessage", data);

                // 给组里所有人发消息
                await Clients.Group("SignalRUsers").SendAsync("ReceiveMessage", data);

                // 给调用方法的那个人发消息
                await Clients.Caller.SendAsync("ReceiveMessage", data);

                // 给除了调用方法的以外所有人发消息
                await Clients.Others.SendAsync("ReceiveMessage", data);

                // 给指定connectionId的人发消息
                await Clients.User(connectionId).SendAsync("ReceiveMessage", data);

                // 给指定connectionId的人发消息
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", data);

                // 给指定connectionId的人发消息，同时指定多个connectionId
                await Clients.Clients(IReadOnlyList<> connectionIds).SendAsync("ReceiveMessage", data);
            */

        }
        public async Task SendMessageToCaller(string user, string message)
            => await Clients.Caller.SendAsync("ReceiveMessage", user, message); //将消息发送回调用方

        public async Task SendMessageToGroup(string user, string message)
            => await Clients.Group("SignalRUsers").SendAsync("ReceiveMessage", user, message); //将消息发送给 SignalR Users 组中的所有客户端

        /// <summary>
        /// 加入设备分组(设备详情页订阅实时数据,服务端推送经ReceiveDeviceData/ReceiveDeviceState下发)
        /// </summary>
        public async Task JoinDeviceGroup(long deviceId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, $"device:{deviceId}");

        /// <summary>
        /// 离开设备分组(页面离开时调用,连接断开时SignalR自动清理)
        /// </summary>
        public async Task LeaveDeviceGroup(long deviceId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device:{deviceId}");

        /// <summary>
        /// 加入告警分组(告警中心页按单位订阅实时告警,告警引擎落地后经ReceiveAlarm下发)
        /// </summary>
        public async Task JoinAlarmGroup(int unitId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, $"alarm:{unitId}");

        /// <summary>
        /// 离开告警分组
        /// </summary>
        public async Task LeaveAlarmGroup(int unitId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"alarm:{unitId}");

        /// <summary>
        /// 在客户端连接到中心时执行操作
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            //await Groups.AddToGroupAsync(Context.ConnectionId, "SignalRUsers");
            //LogHelper.SysLogWrite("ChatServer", "OnConnectedAsync", $"{Context.ConnectionId}连接成功", "SignalR服务端");

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 在客户端断开连接时执行操作
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            var useropt = OperatorCommon.UserList.Find(t => t.UserSignalR == Context.ConnectionId);
            if (useropt != null)
            {
                var user = SysUserDAO.Instance.GetOneBy(t => t.UserId == useropt.UserId);
                if (user != null && user.OnlineState == 1)
                {
                    if (LoginPublicMode.LoginOut(user, useropt.OperatorModel.SourceType, useropt.ClientIp))
                    {
                        OperatorCommon.UserList.Remove(useropt);
                        LogHelper.SysLogWrite("ChatServer", "OnDisconnectedAsync", $"用户登出：{useropt.UserId}-{useropt.UserSignalR}", "SignalR服务端");
                    }
                }
            }

            await base.OnDisconnectedAsync(ex);
        }

    }
}
