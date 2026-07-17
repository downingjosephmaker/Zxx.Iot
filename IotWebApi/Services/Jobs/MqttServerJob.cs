using CenBoCommon.Zxx;
using IotLog;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Quartz;
using System.Buffers;
using System.Net;
using System.Text;
using IotModel;
using IotWebApi.Services.Mqtt;

namespace IotWebApi.Services.Jobs
{
    /// <summary>
    /// MQTT服务端状态检查任务
    /// </summary>
    [DisallowConcurrentExecution] // 禁止并发执行
    public class MqttServerJob : BaseJob
    {
        private AdminMqttparam? MqttParam;
        /// <summary>
        /// 执行任务
        /// </summary>
        protected override async Task<string> ExecuteJob(IJobExecutionContext context)
        {
            try
            {
                // 检查MQTT服务器状态
                bool serverStatus = await CheckMqttServerStatus();
                // 检查MQTT服务器连接数量
                var connectionCount = await GetMqttServerConnectionCount();

                return $"MQTT服务端状态: {(serverStatus ? "正常" : "异常")}  MQTT服务端连接数: {connectionCount}";
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "MQTT服务端检查任务");
                throw; // 将异常抛出，由基类记录错误日志
            }
        }

        /// <summary>
        /// 检查MQTT服务端状态
        /// </summary>
        private async Task<bool> CheckMqttServerStatus()
        {
            try
            {
                bool isActive = MqttFwdService._mqttServer != null && MqttFwdService._mqttServer.IsStarted;
                if (!isActive)
                {
                    // 尝试重新初始化服务端
                    await TryInitializeServer();
                }
                isActive = MqttFwdService._mqttServer != null && MqttFwdService._mqttServer.IsStarted;

                return isActive;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"检查MQTT服务端状态失败: {ex.Message}", "MQTT服务端检查任务");
                return false;
            }
        }

        /// <summary>
        /// 获取MQTT服务端连接数量
        /// </summary>
        private async Task<int> GetMqttServerConnectionCount()
        {
            try
            {
                if (MqttFwdService._mqttServer == null || !MqttFwdService._mqttServer.IsStarted)
                {
                    return 0;
                }

                var clients = await MqttFwdService._mqttServer.GetClientsAsync();
                int count = clients.Count;

                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"MQTT服务端当前连接数: {count}", "MQTT服务端检查任务");

                return count;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"获取MQTT服务端连接数量失败: {ex.Message}", "MQTT服务端检查任务");
                return -1;
            }
        }

        /// <summary>
        /// 尝试初始化服务端
        /// </summary>
        private async Task<bool> TryInitializeServer()
        {
            try
            {
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "尝试初始化MQTT服务端", "MQTT服务端检查任务");

                MqttParam = AdminMqttparamDAO.Instance.GetOneBy(t => t.Id > 0);
                if (MqttParam == null)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "无法初始化MQTT服务端: MqttParam为空", "MQTT服务端检查任务");
                    return false;
                }

                // 创建服务器选项构建器
                MqttServerOptionsBuilder optionsBuilder = new MqttServerOptionsBuilder();
                // 默认绑定到所有IPv4和IPv6地址（LanBindAddress为空时兜底0.0.0.0，等价IPAddress.Any，存量行为不变）
                var lanbind = string.IsNullOrWhiteSpace(MqttParam.LanBindAddress) ? "0.0.0.0" : MqttParam.LanBindAddress;
                optionsBuilder.WithDefaultEndpointBoundIPAddress(IPAddress.Parse(lanbind));
                optionsBuilder.WithDefaultEndpointBoundIPV6Address(IPAddress.IPv6Any);
                optionsBuilder.WithDefaultEndpointPort(MqttParam.MqttServerPort);// 配置端口
                optionsBuilder.WithConnectionBacklog(1000); // 最大连接数

                // 构建服务器选项
                MqttServerOptions options = optionsBuilder.Build();
                // 创建MQTT服务器实例
                MqttFwdService._mqttServer = new MqttServerFactory().CreateMqttServer(options);

                // 注册事件处理
                MqttFwdService._mqttServer.ClientConnectedAsync += _mqttServer_ClientConnectedAsync; //客户端连接事件
                MqttFwdService._mqttServer.ClientDisconnectedAsync += _mqttServer_ClientDisconnectedAsync; // 客户端关闭事件
                MqttFwdService._mqttServer.ApplicationMessageNotConsumedAsync += _mqttServer_ApplicationMessageNotConsumedAsync; // 消息接收事件

                MqttFwdService._mqttServer.ClientSubscribedTopicAsync += _mqttServer_ClientSubscribedTopicAsync; // 客户端订阅主题事件
                MqttFwdService._mqttServer.ClientUnsubscribedTopicAsync += _mqttServer_ClientUnsubscribedTopicAsync; // 客户端取消订阅事件
                MqttFwdService._mqttServer.StartedAsync += _mqttServer_StartedAsync; // 启动后事件
                MqttFwdService._mqttServer.StoppedAsync += _mqttServer_StoppedAsync; // 关闭后事件
                MqttFwdService._mqttServer.InterceptingPublishAsync += _mqttServer_InterceptingPublishAsync; // 消息接收事件
                MqttFwdService._mqttServer.InterceptingSubscriptionAsync += _mqttServer_InterceptingSubscriptionAsync; // 订阅拦截:Topic ACL
                MqttFwdService._mqttServer.ValidatingConnectionAsync += _mqttServer_ValidatingConnectionAsync; // 用户名和密码验证有关

                // 启动服务器
                await MqttFwdService._mqttServer.StartAsync();

                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "MQTT服务端初始化成功", "MQTT服务端检查任务");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"初始化MQTT服务端失败: {ex.Message}", "MQTT服务端检查任务");
                return false;
            }
        }

        #region MQTT服务器事件处理

        /// <summary>
        /// 客户端订阅主题事件
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task _mqttServer_ClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs arg)
        {
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"ClientSubscribedTopicAsync：客户端ID=【{arg.ClientId}】订阅的主题=【{arg.TopicFilter}】 ", "MQTT服务端");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 关闭后事件
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task _mqttServer_StoppedAsync(EventArgs arg)
        {
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"StoppedAsync：MQTT服务已关闭……", "MQTT服务端");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 用户名和密码验证有关
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task _mqttServer_ValidatingConnectionAsync(ValidatingConnectionEventArgs arg)
        {
            // 抖动/爆破封禁期内直接拒绝(§6.5 flapping_detect)
            if (Mqtt.MqttFlappingGuard.IsBanned(arg.ClientId))
            {
                arg.ReasonCode = MqttConnectReasonCode.Banned;
                return Task.CompletedTask;
            }
            arg.ReasonCode = MqttConnectReasonCode.Success;

            // 内网存量:仍允许全局账号(1883 明文口不断存量);否则走每设备凭据
            bool isGlobal = (arg.UserName ?? string.Empty) == MqttParam.MqttUser && (arg.Password ?? string.Empty) == MqttParam.MqttPass;
            if (!isGlobal)
            {
                var (ok, gateway) = Services.Mqtt.MqttCredentialCache.Validate(arg.UserName ?? string.Empty, arg.Password ?? string.Empty);
                if (!ok)
                {
                    arg.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                    Mqtt.MqttFlappingGuard.OnAuthFailed(arg.ClientId);
                    LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"ValidatingConnectionAsync：客户端ID=【{arg.ClientId}】用户名或密码验证错误 ", "MQTT服务端");
                    return Task.CompletedTask;
                }
                // 每设备凭据必须绑定设备;gateway 为空会让该连接不受 Topic ACL 约束(可发布/订阅任意 topic),视为配置错误直接拒绝(堵 Topic ACL 绕过)
                if (string.IsNullOrEmpty(gateway))
                {
                    arg.ReasonCode = MqttConnectReasonCode.NotAuthorized;
                    Mqtt.MqttFlappingGuard.OnAuthFailed(arg.ClientId);
                    LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"ValidatingConnectionAsync：客户端ID=【{arg.ClientId}】凭据未绑定设备(device_gateway 为空)已拒绝 ", "MQTT服务端");
                    return Task.CompletedTask;
                }
                // 认证通过:把 ClientId→deviceGateway 写入会话映射,供 Topic ACL 用(§4.4 已挂校验)
                Mqtt.MqttAclMap.Bind(arg.ClientId, gateway);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 消息接收事件
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task _mqttServer_InterceptingPublishAsync(InterceptingPublishEventArgs arg)
        {
            // 每条消息载荷日志挂开关,防高频遥测刷盘
            if (AppSetting.GetConfig("MqttConfig:LogOpen").ToLower() == "true")
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"InterceptingPublishAsync：客户端ID=【{arg.ClientId}】 Topic主题=【{arg.ApplicationMessage.Topic}】 消息=【{Encoding.UTF8.GetString(arg.ApplicationMessage.Payload.ToArray())}】 qos等级=【{arg.ApplicationMessage.QualityOfServiceLevel}】", "MQTT服务端");

            // Topic ACL:该连接若已绑定设备,只放行末段为自身deviceGateway的topic(未绑定的全局账号不受限)
            if (!Mqtt.MqttAclMap.Match(arg.ClientId, arg.ApplicationMessage.Topic))
            {
                arg.ProcessPublish = false;
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"InterceptingPublishAsync：ACL拒绝发布 客户端ID=【{arg.ClientId}】 Topic主题=【{arg.ApplicationMessage.Topic}】", "MQTT服务端");
            }
            return Task.CompletedTask;

        }

        /// <summary>
        /// 客户端订阅拦截事件(Topic ACL:该连接若已绑定设备,只放行末段为自身deviceGateway的topic)
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task _mqttServer_InterceptingSubscriptionAsync(InterceptingSubscriptionEventArgs arg)
        {
            if (!Mqtt.MqttAclMap.Match(arg.ClientId, arg.TopicFilter.Topic))
            {
                arg.ProcessSubscription = false;
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"InterceptingSubscriptionAsync：ACL拒绝订阅 客户端ID=【{arg.ClientId}】 Topic主题=【{arg.TopicFilter.Topic}】", "MQTT服务端");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 启动后事件
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task _mqttServer_StartedAsync(EventArgs arg)
        {
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"StartedAsync：MQTT服务已启动……", "MQTT服务端");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 客户端取消订阅事件
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task _mqttServer_ClientUnsubscribedTopicAsync(ClientUnsubscribedTopicEventArgs arg)
        {
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"ClientUnsubscribedTopicAsync：客户端ID=【{arg.ClientId}】已取消订阅的主题=【{arg.TopicFilter}】  ", "MQTT服务端");
            return Task.CompletedTask;
        }

        private Task _mqttServer_ApplicationMessageNotConsumedAsync(ApplicationMessageNotConsumedEventArgs arg)
        {
            // 每条消息载荷日志挂开关,防高频遥测刷盘
            if (AppSetting.GetConfig("MqttConfig:LogOpen").ToLower() == "true")
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"ApplicationMessageNotConsumedAsync：发送端ID=【{arg.SenderId}】 Topic主题=【{arg.ApplicationMessage.Topic}】 消息=【{Encoding.UTF8.GetString(arg.ApplicationMessage.Payload.ToArray())}】 qos等级=【{arg.ApplicationMessage.QualityOfServiceLevel}】", "MQTT服务端");
            return Task.CompletedTask;

        }

        /// <summary>
        /// 客户端断开时候触发
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private Task _mqttServer_ClientDisconnectedAsync(ClientDisconnectedEventArgs arg)
        {
            // 抖动计数(§6.5:1分钟窗口断连超阈值封禁,防重连风暴)
            Mqtt.MqttFlappingGuard.OnDisconnected(arg.ClientId);
            // 断开清 Topic ACL 绑定映射,防绑定映射长期驻留
            Mqtt.MqttAclMap.Unbind(arg.ClientId);
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"ClientDisconnectedAsync：客户端ID=【{arg.ClientId}】已断开, 地址=【{arg.RemoteEndPoint}】", "MQTT服务端");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 客户端连接时候触发
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task _mqttServer_ClientConnectedAsync(ClientConnectedEventArgs arg)
        {
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"ClientConnectedAsync：客户端ID=【{arg.ClientId}】已连接, 用户名=【{arg.UserName}】地址=【{arg.RemoteEndPoint}】", "MQTT服务端");
            return Task.CompletedTask;
        }

        #endregion
    }
}