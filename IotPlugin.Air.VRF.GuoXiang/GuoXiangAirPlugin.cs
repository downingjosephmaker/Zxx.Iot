using IotLog;
using CenboEventBus;
using NewLife;
using NewLife.Data;
using NewLife.Net;
using NewLife.Threading;
using SqlSugar;
using System.Data;
using System.Globalization;
using System.Net;
using IotModel;
using CenBoCommon.Zxx;

namespace IotPlugin.Air.VRF.GuoXiang
{
    public class GuoXiangAirPlugin : ICenBoPlugin
    {
        private const string DeviceTypeCode = "vrvwj_guoxiang";

        private readonly object _netLock = new();
        private readonly object _cmdLock = new();
        private readonly object _bindingLock = new();

        private IEventBus<PluginEvent>? _eventBus;
        private NetServer? _netServer;
        private TimerX? _heartTimer;
        private TimerX? _strategyTimer; // 策略执行定时器
        private CancellationTokenSource? _sendCancellation;

        private readonly object _sendWorkerLock = new();
        private readonly Dictionary<string, Task> _endpointSendTasks = new(StringComparer.OrdinalIgnoreCase);

        private GuoXiangAirPluginConfig? _config;

        private readonly Dictionary<string, NetSession> DicNet = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<CmdInfo>> DicCmd = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, GuoXiangGatewayBinding> _gatewayMap = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<int, (string Endpoint, GuoXiangGatewayBinding Outdoor, GuoXiangIndoorBinding Indoor)> _deviceIndoorMap = new();

        // 舒适度配置全局缓存：启动/刷新时一次性加载，按 UnitId 分组，避免每设备单独查库
        private List<DeviceComfort> _comfortList = new();
        private readonly DataTable _comfortComputer = new DataTable();

        public void PluginInit(IEventBus<PluginEvent> eventBus) => _eventBus = eventBus;

        public string PluginName => "GuoXiangAirPlugin";
        public string PluginType => "国祥VRF空调解析插件";
        public string PluginDesc => "国祥VRF空调Modbus-TCP采集与控制";
        public string PluginVersion => "1.0.0";
        public string PluginGuid => "a7b3c9d2e845f16a3bc74d5e8f901a23";
        public string PluginModelPath => "";

        #region 启动/停止

        /// <summary>
        /// 启动插件。执行顺序：
        /// <list type="number">
        ///   <item>加载并校验 <see cref="GuoXiangAirPluginConfig"/> 配置（端口/采集间隔/发送间隔/超时等不得为非正数）；</item>
        ///   <item>调用 <see cref="RefreshBindingsAndCommands"/> 从数据库初始化设备绑定缓存与采集指令队列；</item>
        ///   <item>创建并启动 Modbus-TCP 服务器（监听 <c>0.0.0.0:NetPort</c>）；</item>
        ///   <item>启动心跳定时器（初始延迟5秒，之后每隔 <c>HeartSecond</c> 秒触发一次）；</item>
        ///   <item>启动每个端点独立的指令发送循环（见 <see cref="RunSendLoopAsync"/>）。</item>
        /// </list>
        /// </summary>
        /// <param name="_PluginConfig">框架传入的配置字符串（本插件忽略，配置由 <see cref="GuoXiangAirPluginConfig.Current"/> 加载）。</param>
        /// <returns>所有步骤均成功返回 <c>true</c>；任一步骤失败则记录日志并返回 <c>false</c>。</returns>
        public async Task<bool> PluginStart(string _PluginConfig)
        {
            try
            {
                _config = GuoXiangAirPluginConfig.Current;
                if (_config == null)
                {
                    LogHelper.Info($"{PluginName}：配置加载失败，插件启动失败。");
                    return false;
                }
                if (_config.NetPort <= 0 || _config.CollectSleepSecond <= 0 || _config.SendSecond <= 0 || _config.CmdTimeOut <= 0)
                {
                    LogHelper.Info($"{PluginName}：配置参数存在非正数，插件启动失败。");
                    return false;
                }
                if (!RefreshBindingsAndCommands(out var err))
                {
                    LogHelper.Info($"{PluginName}：初始化设备失败，{err}");
                    return false;
                }
                if (_netServer == null)
                {
                    _netServer = new NetServer(IPAddress.Any, _config.NetPort);
                    _netServer.NewSession += ProcessAccept;
                    _netServer.Received += ProcessOnReceived;
                    _netServer.Error += ProcessError;
                }
                if (!_netServer.Active)
                {
                    _netServer.Start();
                    LogHelper.Info($"{PluginName}：TCP 服务[{_config.NetPort}]启动成功。");
                }
                _heartTimer?.Dispose();
                _heartTimer = new TimerX(HeartBeatDown, null, 5_000, _config.HeartSecond * 1_000);
                LogHelper.Info($"{PluginName}：心跳定时器启动成功。");

                await StopEndpointSendWorkersAsync();
                _sendCancellation = new CancellationTokenSource();
                SyncEndpointSendWorkers();
                LogHelper.Info($"{PluginName}：指令发送线程启动成功。");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// 停止插件。执行顺序：
        /// <list type="number">
        ///   <item>销毁心跳定时器；</item>
        ///   <item>取消 <see cref="_sendCancellation"/>，等待所有端点发送循环安全退出（<see cref="StopEndpointSendWorkersAsync"/>）；</item>
        ///   <item>调用 <c>Stop</c> 并 <c>Dispose</c> 释放 TCP 服务器；</item>
        ///   <item>清空连接字典、指令队列、设备绑定缓存。</item>
        /// </list>
        /// </summary>
        /// <returns>始终返回 <c>true</c>；内部异常由 <c>catch</c> 捕获写入日志，不向上抛出。</returns>
        public async Task<bool> PluginStop()
        {
            try
            {
                _heartTimer?.Dispose();
                _strategyTimer?.Dispose();
                await StopEndpointSendWorkersAsync();
                if (_netServer != null)
                {
                    _netServer.Stop("服务停止");
                    _netServer.Dispose();
                    _netServer = null;
                }
                lock (_netLock) { DicNet.Clear(); }
                lock (_cmdLock) { DicCmd.Clear(); }
                lock (_bindingLock)
                {
                    _gatewayMap = new Dictionary<string, GuoXiangGatewayBinding>(StringComparer.OrdinalIgnoreCase);
                    _deviceIndoorMap = new Dictionary<int, (string, GuoXiangGatewayBinding, GuoXiangIndoorBinding)>();
                }
            }
            catch (Exception ex) { LogHelper.Error(ex); }
            return true;
        }

        #endregion

        #region 心跳

        /// <summary>
        /// 心跳定时器回调。每隔 <c>HeartSecond</c> 秒触发，向主程序发布
        /// <see cref="PluginMessageEnum.心跳"/> 消息，维持插件存活信号。
        /// 使用 <c>.Wait()</c> 是因为定时器回调不支持 async（<see cref="TimerX"/> 约束）。
        /// </summary>
        private void HeartBeatDown(object? _)
        {
            SendMessageAsync(new PluginMessage
            {
                MessageType = PluginMessageEnum.心跳,
                MessageJson = new { Message = $"{PluginName}心跳", Time = DateTime.Now }.ToJson()
            }).Wait();
        }

        /// <summary>
        /// 通过事件总线向主程序发布消息。支持的消息类型：心跳、协议解析（实时数据）、控制结果、运行状态。
        /// 发布异常不向上抛出，仅写入日志，保证调用方不受事件总线异常影响。
        /// </summary>
        /// <param name="mess">待发布的插件消息，含 <see cref="PluginMessage.MessageType"/> 和序列化的 <see cref="PluginMessage.MessageJson"/>。</param>
        public async Task SendMessageAsync(PluginMessage mess)
        {
            try { _eventBus?.Publish(new PluginEvent(PluginGuid, mess)); }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        #endregion

        #region 接收主程序消息

        /// <summary>
        /// 接收来自主程序消息的入口。根据 <see cref="PluginMessage.MessageType"/> 路由处理：
        /// <list type="bullet">
        ///   <item><see cref="PluginMessageEnum.心跳"/> — 记录收到心跳日志。</item>
        ///   <item><see cref="PluginMessageEnum.设备控制"/> — 转发至 <see cref="HandleDeviceControlAsync"/> 解析并分发控制指令。</item>
        /// </list>
        /// </summary>
        public async Task ReceiveMessageAsync(PluginMessage mess)
        {
            switch (mess.MessageType)
            {
                case PluginMessageEnum.心跳:
                    LogHelper.Info($"{PluginName}：收到心跳。");
                    break;
                case PluginMessageEnum.设备控制:
                    await HandleDeviceControlAsync(mess.MessageJson);
                    break;
            }
        }

        /// <summary>
        /// 解析设备控制消息 JSON 并按 <see cref="AirConControlCommand.ClassName"/> 分发到对应处理器：
        /// <list type="bullet">
        ///   <item><c>netairconrun</c>    — 运行控制（开关机/模式/温度/风速），见 <see cref="HandleRunControlAsync"/>。</item>
        ///   <item><c>netairparam</c>     — 触发系统参数读取 FC03，见 <see cref="HandleSysParamReadAsync"/>。</item>
        ///   <item><c>netairuser</c>      — 加速一次状态采集，见 <see cref="HandleUserParamAsync"/>。</item>
        ///   <item><c>netaircommonset</c> — 通用指令（按 DatasetId 读特定数据集），见 <see cref="HandleCommonSetAsync"/>。</item>
        /// </list>
        /// </summary>
        private async Task HandleDeviceControlAsync(string? messageJson)
        {
            if (messageJson.IsZxxNullOrEmpty())
            {
                LogHelper.Info($"{PluginName}：设备控制消息为空。");
                return;
            }
            AirConControlCommand? command;
            try { command = messageJson.ToObject<AirConControlCommand>(); }
            catch (Exception ex) { LogHelper.Error(ex); return; }

            if (command == null || command.ConContent.IsZxxNullOrEmpty())
            {
                LogHelper.Info($"{PluginName}：设备控制消息格式无效。");
                return;
            }
            switch (command.ClassName?.Trim().ToLowerInvariant())
            {
                case "netairconrun": await HandleRunControlAsync(command); break;
                case "netairparam": await HandleSysParamReadAsync(command); break;
                case "netairuser": await HandleUserParamAsync(command); break;
                case "netaircommonset": await HandleCommonSetAsync(command); break;
                default:
                    LogHelper.Info($"{PluginName}：不支持的控制类型[{command.ClassName}]，CommandId={command.CommandId}。");
                    break;
            }
        }

        /// <summary>
        /// 处理运行控制指令（<see cref="NetAirConRun"/>）。
        /// 调用 <see cref="GuoXiangAirHard.BuildRunControlCmds"/> 为每台目标设备生成 FC06 写寄存器帧：
        /// <list type="bullet">
        ///   <item>Reg10：工作模式（bit0~2）+ 风速（bit8~10），值 255 表示不修改该字段；</item>
        ///   <item>Reg11：设定温度（× 10，有效范围 16~30℃）；</item>
        ///   <item>Reg12：开关机（bit8；0=关，1=开），值 255 表示不修改。</item>
        /// </list>
        /// 多条指令依序入队，仅最后一条携带 <c>CommandId</c>，确保只发送一次控制结果回执。
        /// </summary>
        private async Task HandleRunControlAsync(AirConControlCommand command)
        {
            NetAirConRun? model;
            try { model = command.ConContent.ToObject<NetAirConRun>(); }
            catch { LogHelper.Info($"{PluginName}：NetAirConRun 解析失败。"); return; }
            if (model == null) return;

            var deviceIds = (command.DeviceIds?.Any() == true)
                ? command.DeviceIds
                : model.DeviceInfoList.Select(t => t.DeviceId).ToList();

            var devparams = DeviceParamDAO.Instance.GetListBy(t => deviceIds.Contains(t.DeviceId));

            foreach (var deviceId in deviceIds.Distinct())
            {
                if (!TryGetIndoorByDeviceId(deviceId, out var endpoint, out var outdoor, out var indoor))
                {
                    LogHelper.Info($"{PluginName}：运行控制设备[{deviceId}]未找到绑定。");
                    await PublishControlResultAsync(command.CommandId, deviceId, "", false, "未找到设备绑定信息");
                    continue;
                }
                var devparam = devparams?.Find(p => p.DeviceId == deviceId);
                var resolved = ResolveRunModel(model, devparam, indoor.Device.UnitId);
                var (cmdStr, conReturn) = GuoXiangAirHard.BuildRunControlCmds(outdoor.GatewayDevice.DeviceAdr, indoor.Device.DeviceAdr, resolved);
                EnqueueEndpointCommand(endpoint, new CmdInfo
                {
                    CmdType = 20,
                    CmdMode = 1,
                    IpPort = endpoint,
                    DeviceId = indoor.Device.DeviceId,
                    DeviceAddr = outdoor.GatewayDevice.DeviceAdr,
                    CmdStr = cmdStr,
                    ConReturnCmd = conReturn,
                    ExpectFuncCode = 0x10,
                    WaitForResponse = true,
                    OutSecond = _config?.CmdTimeOut ?? 10,
                    SleepSecond = _config?.ControlSuccess ?? 10,
                    IsStartLimit = true,
                    TimeOutLimitCount = _config?.TimeOutLimitCount ?? 1,
                    ClassName = "NetAirConRun",
                    CommandId = command.CommandId
                });
                LogHelper.Info($"{PluginName}：运行控制入队，设备[{deviceId}]，模式={resolved.AirModel},温度={resolved.AirModelTemp},风速={resolved.AirSpeed},开关={resolved.AirSwitch}。");
            }
        }

        /// <summary>
        /// 处理系统参数读取请求。向目标设备发送 FC03 读保持寄存器 100~116（17个寄存器，系统级配置）。
        /// 该指令只读取不修改，设备回执后通过 <see cref="PublishControlResultAsync"/> 发送成功通知。
        /// </summary>
        private async Task HandleSysParamReadAsync(AirConControlCommand command)
        {
            var deviceIds = command.DeviceIds ?? new List<int>();
            foreach (var deviceId in deviceIds.Distinct())
            {
                if (!TryGetIndoorByDeviceId(deviceId, out var endpoint, out var outdoor, out var indoor))
                {
                    LogHelper.Info($"{PluginName}：系统参数读取设备[{deviceId}]未找到绑定。");
                    continue;
                }
                EnqueueEndpointCommand(endpoint, new CmdInfo
                {
                    CmdType = 21,
                    CmdMode = 1,
                    IpPort = endpoint,
                    DeviceId = indoor.Device.DeviceId,
                    DeviceAddr = outdoor.GatewayDevice.DeviceAdr,
                    CmdStr = GuoXiangAirHard.GetSysParamReadCmd(outdoor.GatewayDevice.DeviceAdr),
                    ExpectFuncCode = 0x03,
                    WaitForResponse = true,
                    OutSecond = _config?.CmdTimeOut ?? 10,
                    SleepSecond = _config?.ControlSuccess ?? 10,
                    TimeOutLimitCount = _config?.TimeOutLimitCount ?? 1,
                    ClassName = "NetAirParam",
                    CommandId = command.CommandId
                });
                LogHelper.Info($"{PluginName}：系统参数读取入队，设备[{deviceId}]。");
            }
        }

        /// <summary>
        /// 处理用户参数请求：对目标设备调用 <see cref="AccelerateCollectCmd"/>，
        /// 将其 FC04 采集指令的 <c>SendNextTime</c> 重置为当前时刻，使下一发送周期立即触发一次采集，
        /// 从而快速返回最新状态。不产生额外的 Modbus 指令。
        /// </summary>
        private async Task HandleUserParamAsync(AirConControlCommand command)
        {
            var deviceIds = command.DeviceIds ?? new List<int>();
            foreach (var deviceId in deviceIds.Distinct())
            {
                if (!TryGetIndoorByDeviceId(deviceId, out var endpoint, out var outdoor, out var indoor)) continue;
                AccelerateCollectCmd(endpoint, indoor.Device.DeviceAdr);
                LogHelper.Info($"{PluginName}：用户参数加速采集，设备[{deviceId}]。");
            }
        }

        /// <summary>
        /// 处理通用指令请求，根据 <see cref="NetAirCommonSet.DatasetId"/> 发送对应读指令（一次性，不重复）：
        /// <list type="bullet">
        ///   <item>8008 — FC04 读输入寄存器 58~70（实时状态帧）；</item>
        ///   <item>8005 — FC03 读保持寄存器 100~116（系统参数）；</item>
        ///   <item>8006 — FC03 读保持寄存器 10~14（用户参数）。</item>
        /// </list>
        /// 指令设置 <c>IsStartLimit=true, LimitCount=1</c>，发送一次后由 <see cref="ResetCompleted"/> 自动删除。
        /// </summary>
        private async Task HandleCommonSetAsync(AirConControlCommand command)
        {
            NetAirCommonSet? model;
            try { model = command.ConContent.ToObject<NetAirCommonSet>(); }
            catch { LogHelper.Info($"{PluginName}：NetAirCommonSet 解析失败。"); return; }
            if (model == null) return;

            var deviceIds = (command.DeviceIds?.Any() == true)
                ? command.DeviceIds
                : model.DeviceInfoList.Select(t => t.DeviceId).ToList();

            foreach (var deviceId in deviceIds.Distinct())
            {
                if (!TryGetIndoorByDeviceId(deviceId, out var endpoint, out var outdoor, out var indoor)) continue;

                string? cmdStr = null;
                byte? expectFc = null;
                int cmdDevId = 0;
                int cmdDevAddr = 0;
                switch (model.DatasetId)
                {
                    case 8008:
                        // FC04 读外机网关状态寄存器 58~63
                        cmdStr = GuoXiangAirHard.GetRealReadCmd(outdoor.GatewayDevice.DeviceAdr);
                        expectFc = 0x04;
                        cmdDevId = outdoor.GatewayDevice.DeviceId;
                        cmdDevAddr = outdoor.GatewayDevice.DeviceAdr;
                        break;
                    case 8005:
                        // FC03 读内机系统参数保持寄存器 100~116
                        cmdStr = GuoXiangAirHard.GetSysParamReadCmd(outdoor.GatewayDevice.DeviceAdr);
                        expectFc = 0x03;
                        cmdDevId = indoor.Device.DeviceId;
                        cmdDevAddr = outdoor.GatewayDevice.DeviceAdr;
                        break;
                    case 8006:
                        // FC03 读指定内机用户参数保持寄存器 (slot-1)*5+10 ~ +14
                        cmdStr = GuoXiangAirHard.GetUserParamReadCmd(outdoor.GatewayDevice.DeviceAdr, indoor.Device.DeviceAdr);
                        expectFc = 0x03;
                        cmdDevId = indoor.Device.DeviceId;
                        cmdDevAddr = outdoor.GatewayDevice.DeviceAdr;
                        break;
                    default:
                        LogHelper.Info($"{PluginName}：DatasetId[{model.DatasetId}]暂不支持，设备[{deviceId}]。");
                        continue;
                }
                EnqueueEndpointCommand(endpoint, new CmdInfo
                {
                    CmdType = 24,
                    CmdMode = 1,
                    IpPort = endpoint,
                    DeviceId = cmdDevId,
                    DeviceAddr = cmdDevAddr,
                    CmdStr = cmdStr,
                    ExpectFuncCode = expectFc,
                    WaitForResponse = true,
                    IsStartLimit = true,
                    LimitCount = 1,
                    OutSecond = _config?.CmdTimeOut ?? 10,
                    SleepSecond = 0,
                    ClassName = "NetAirCommonSet",
                    CommandId = command.CommandId
                });
                LogHelper.Info($"{PluginName}：通用指令[{model.DatasetId}]入队，设备[{deviceId}]。");
            }
        }

        #endregion

        #region TCP 连接处理

        /// <summary>
        /// 新 TCP 连接建立时的回调。
        /// 将来源 IP（若为 IPv4-mapped IPv6 会在 <see cref="ResolveEndpointKey"/> 中还原）
        /// 与 <see cref="_bindingMap"/> 中已配置的端点 key 匹配，匹配成功则用 <c>"IP:Port"</c> 作为 key，
        /// 否则回退到 <c>"remoteIp:remotePort"</c>。
        /// Session 登记到 <see cref="DicNet"/>，后续发送指令时按 key 查找使用。
        /// </summary>
        private void ProcessAccept(object? sender, NetSessionEventArgs e)
        {
            e.Session.OnDisposed += ProcessOnDisposed;
            string ip = e.Session.Remote.Address.ToString();
            int port = e.Session.Remote.Port;
            string key = ResolveEndpointKey(ip) ?? $"{ip}:{port}";
            lock (_netLock) { DicNet[key] = (NetSession)e.Session; }
            LogHelper.Info($"{PluginName}：{key}连接建立（来源 {ip}:{port}）。");
        }

        /// <summary>
        /// TCP 连接正常关闭或被动断开时的回调。
        /// 从 <see cref="DicNet"/> 移除对应 Session，并在后台异步发布该端点下所有设备的
        /// <see cref="PluginMessageEnum.运行状态"/> 消息（state=0 表示离线）。
        /// </summary>
        protected virtual void ProcessOnDisposed(object? sender, EventArgs e)
        {
            try
            {
                if (sender is not NetSession session) return;
                string ip = session.Remote.Address.ToString();
                string key = ResolveEndpointKey(ip) ?? $"{ip}:{session.Remote.Port}";
                lock (_netLock) { DicNet.Remove(key); }
                _ = Task.Run(() => PublishRunStateByEndpointAsync(key, 0));
                LogHelper.Info($"{PluginName}：{key}连接断开。");
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        /// <summary>
        /// TCP 连接发生异常错误时的回调，处理逻辑与 <see cref="ProcessOnDisposed"/> 相同。
        /// 额外主动调用 <c>session.Dispose()</c> 以释放底层资源，
        /// 并异步发布该端点所有设备的离线运行状态消息。
        /// </summary>
        private void ProcessError(object? sender, ExceptionEventArgs e)
        {
            try
            {
                if (sender is not NetSession session) return;
                string ip = session.Remote.Address.ToString();
                string key = ResolveEndpointKey(ip) ?? $"{ip}:{session.Remote.Port}";
                lock (_netLock) { DicNet.Remove(key); }
                session.Dispose();
                _ = Task.Run(() => PublishRunStateByEndpointAsync(key, 0));
                LogHelper.Info($"{PluginName}：{key}连接错误。");
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        /// <summary>
        /// 接收到 TCP 数据包时的核心解析方法，按以下顺序处理：
        /// <list type="number">
        ///   <item>长度预检：小于 5 字节直接丢弃；</item>
        ///   <item>CRC16 校验（YJ-LH 字节序），校验失败丢弃；</item>
        ///   <item>按从站地址查找设备绑定，未命中丢弃；</item>
        ///   <item>调用 <see cref="UpdateCmdState"/> 匹配等待中的飞行指令，未命中丢弃；</item>
        ///   <item>按功能码分支处理：
        ///     <list type="bullet">
        ///       <item><b>FC04 (0x04)</b> — 解析内机状态帧，异步发布实时数据；</item>
        ///       <item><b>FC06 (0x06)</b> — 写单寄存器 echo，比对原始帧确认成功/失败，成功后加速下次采集；</item>
        ///       <item><b>FC16 (0x10)</b> — 写多寄存器 echo，处理逻辑与 FC06 相同；</item>
        ///       <item><b>FC03 (0x03)</b> — 读保持寄存器回执，发布成功通知后移除指令。</item>
        ///     </list>
        ///   </item>
        /// </list>
        /// </summary>
        private void ProcessOnReceived(object? sender, ReceivedEventArgs e)
        {
            try
            {
                string ip = e.Remote.Address.ToString();
                string key = ResolveEndpointKey(ip) ?? $"{ip}:{e.Remote.Port}";
                byte[] buffer = e.Packet.ReadBytes();
                if (buffer.Length < 5)
                {
                    LogHelper.Info($"{PluginName}：数据包太短，来自{key}，{buffer.ToHex()}");
                    return;
                }

                // CRC 校验
                var datacmd = new byte[buffer.Length - 2];
                var datacrc = new byte[2];
                Array.Copy(buffer, 0, datacmd, 0, datacmd.Length);
                Array.Copy(buffer, datacmd.Length, datacrc, 0, 2);
                if (!datacmd.YjCrc16VerifyLH(datacrc))
                {
                    LogHelper.Info($"{PluginName}：CRC 校验失败，{buffer.ToHex()}");
                    return;
                }

                int slaveAddr = buffer[0];
                byte funcCode = buffer[1];

                // 按外机网关 Modbus 从站地址查绑定（所有应答均使用网关地址）
                if (!TryGetGateway(key, slaveAddr, out var gatewayBinding))
                {
                    LogHelper.Info($"{PluginName}：未找到外机网关绑定，addr={slaveAddr}，数据:{buffer.ToHex()}");
                    return;
                }

                var waitCmd = UpdateCmdState(key, slaveAddr, buffer);
                if (waitCmd == null)
                {
                    LogHelper.Info($"{PluginName}：未匹配到等待指令，数据:{buffer.ToHex()}");
                    return;
                }

                // FC04 应答 → 解析外机网关状态，向所有内机发布实时数据
                if (funcCode == 0x04)
                {
                    int dataLen = buffer[2];
                    if (buffer.Length < dataLen + 5) return;
                    var dataBytes = buffer.Skip(3).Take(dataLen).ToList();
                    var statusInfo = GuoXiangAirHard.ParseStatusData(dataBytes, slaveAddr);
                    if (statusInfo != null)
                        _ = Task.Run(() => PublishRealtimeDataAsync(gatewayBinding, statusInfo));
                    return;
                }

                // FC06 echo → 控制回执
                if (funcCode == 0x06 && IsActionCommand(waitCmd))
                {
                    bool success = buffer.ToHex().Equals(waitCmd.ConReturnCmd, StringComparison.OrdinalIgnoreCase);
                    LogHelper.Info($"{PluginName}：FC06控制回执，内机[{ResolveDeviceName(waitCmd.DeviceId)}]，{(success ? "成功" : "失败")}。");
                    if (!waitCmd.CommandId.IsZxxNullOrEmpty())
                        _ = Task.Run(() => PublishControlResultAsync(waitCmd.CommandId, waitCmd.DeviceId,
                            ResolveDeviceName(waitCmd.DeviceId), success, success ? "控制成功" : "控制失败"));
                    if (success) AccelerateAllCollects(waitCmd.IpPort);
                    RemoveCommand(waitCmd.IpPort, waitCmd);
                    return;
                }

                // FC16 echo → 控制回执
                if (funcCode == 0x10 && IsActionCommand(waitCmd))
                {
                    bool success = buffer.ToHex().Equals(waitCmd.ConReturnCmd, StringComparison.OrdinalIgnoreCase);
                    LogHelper.Info($"{PluginName}：FC16控制回执，内机[{ResolveDeviceName(waitCmd.DeviceId)}]，{(success ? "成功" : "失败")}。");
                    if (!waitCmd.CommandId.IsZxxNullOrEmpty())
                        _ = Task.Run(() => PublishControlResultAsync(waitCmd.CommandId, waitCmd.DeviceId,
                            ResolveDeviceName(waitCmd.DeviceId), success, success ? "控制成功" : "控制失败"));
                    if (success) AccelerateAllCollects(waitCmd.IpPort);
                    RemoveCommand(waitCmd.IpPort, waitCmd);
                    return;
                }

                // FC03 应答（读保持寄存器）
                if (funcCode == 0x03)
                {
                    if (IsActionCommand(waitCmd))
                    {
                        // API 触发的一次性读取 → 回执通知并移除
                        if (!waitCmd.CommandId.IsZxxNullOrEmpty())
                            _ = Task.Run(() => PublishControlResultAsync(waitCmd.CommandId, waitCmd.DeviceId,
                                ResolveDeviceName(waitCmd.DeviceId), true, "指令已回执"));
                        RemoveCommand(waitCmd.IpPort, waitCmd);
                        return;
                    }
                    if (IsCollectCommand(waitCmd))
                    {
                        // 循环采集 FC03 → 解析内机用户参数并发布
                        int dataLen = buffer[2];
                        if (buffer.Length >= dataLen + 5)
                        {
                            var dataBytes = buffer.Skip(3).Take(dataLen).ToList();
                            var userParam = GuoXiangAirHard.ParseUserParamData(dataBytes);
                            if (userParam != null)
                            {
                                var indoor = gatewayBinding.IndoorUnits
                                    .FirstOrDefault(u => u.Device.DeviceId == waitCmd.DeviceId);
                                if (indoor != null)
                                    _ = Task.Run(() => PublishUserParamDataAsync(indoor, userParam));
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        /// <summary>
        /// 在指令队列中查找与本次应答帧匹配的"飞行中"指令（CmdResult=1），并将其标记为"已收到回执"（CmdResult=2）。
        /// 匹配条件：CmdResult==1 &amp;&amp; WaitForResponse==true &amp;&amp; DeviceAddr 一致 &amp;&amp;
        /// 功能码等于期望值（或为期望值 +0x80 的错误码）。
        /// 同时记录 <c>RevTime</c>（接收时间）和 <c>ReceiveCmdStr</c>（原始 hex）。
        /// </summary>
        /// <returns>匹配成功的 <see cref="CmdInfo"/>；未命中则返回 <c>null</c>。</returns>
        private CmdInfo? UpdateCmdState(string key, int addr, byte[] buffer)
        {
            lock (_cmdLock)
            {
                if (!DicCmd.TryGetValue(key, out var cmdlist)) return null;
                byte funcCode = buffer[1];
                string hex = buffer.ToHex();
                var cmd = cmdlist.FirstOrDefault(t =>
                    t.CmdResult == 1 && t.WaitForResponse && t.DeviceAddr == addr &&
                    (!t.ExpectFuncCode.HasValue
                     || funcCode == t.ExpectFuncCode
                     || funcCode == t.ExpectFuncCode + 0x80));
                if (cmd == null) return null;
                cmd.CmdResult = 2;
                cmd.TimeOutSendCount = 0;
                cmd.RevTime = DateTime.Now;
                cmd.ReceiveCmdStr = hex;
                return cmd;
            }
        }

        #endregion

        #region 指令调度

        /// <summary>判断指令是否为采集类指令：CmdMode=1 且 CmdType &lt; 20。</summary>
        private static bool IsCollectCommand(CmdInfo c) => c.CmdMode == 1 && c.CmdType < 20;

        /// <summary>判断指令是否为控制类指令：CmdMode=1 且 20 ≤ CmdType &lt; 60。</summary>
        private static bool IsActionCommand(CmdInfo c) => c.CmdMode == 1 && c.CmdType >= 20 && c.CmdType < 60;

        /// <summary>
        /// 返回指令的调度优先级：控制指令=2，采集指令=1，其他=0。
        /// <see cref="SelectNext"/> 按此值降序排列，确保控制指令优先于采集指令发送。
        /// </summary>
        private static int GetPriority(CmdInfo c)
        {
            if (IsActionCommand(c)) return 2;
            if (IsCollectCommand(c)) return 1;
            return 0;
        }

        /// <summary>
        /// 检查队列中是否存在仍在等待应答的指令（CmdResult=1 且 WaitForResponse=true）。
        /// <see cref="SelectNext"/> 依赖此方法实现单飞行指令约束：同一时刻只允许一条指令等待回执。
        /// </summary>
        private static bool HasInflight(List<CmdInfo> list) =>
            list.Any(t => t.CmdResult == 1 && t.WaitForResponse);

        /// <summary>
        /// 将指令标记为"已发送"，更新各时间戳字段：
        /// <list type="bullet">
        ///   <item><c>CmdResult</c>：需要应答时置 1（等待回执），否则置 2（无需等待直接完成）；</item>
        ///   <item><c>SendCount</c>：累计发送次数递增；</item>
        ///   <item><c>SendTime</c>：本次发送时刻；</item>
        ///   <item><c>SendOutTime</c>：超时截止时刻（当前时间 + OutSecond）；</item>
        ///   <item><c>SendNextTime</c>：下次允许发送时刻（当前时间 + SleepSecond）；</item>
        ///   <item><c>ReceiveCmdStr</c>：清空，准备接收新回执。</item>
        /// </list>
        /// </summary>
        private static void MarkSent(CmdInfo c, DateTime now)
        {
            c.CmdResult = c.WaitForResponse ? 1 : 2;
            c.SendCount++;
            c.SendTime = now;
            c.SendOutTime = now.AddSeconds(c.OutSecond);
            c.SendNextTime = now.AddSeconds(c.SleepSecond);
            c.ReceiveCmdStr = "";
        }

        /// <summary>
        /// 扫描队列中所有已超时指令（CmdResult=1 且 SendOutTime &lt; now），按规则处理：
        /// <list type="bullet">
        ///   <item><b>采集指令（CmdType&lt;20）</b>：超时后重新调度到下一个采集周期（SendNextTime += CollectSleepSecond），
        ///         CmdResult 保持 0，永不删除，确保设备恢复后自动恢复循环采集。</item>
        ///   <item><b>控制指令，允许重试</b>（IsStartLimit=true 且 TimeOutSendCount &lt; TimeOutLimitCount）：
        ///         将 SendNextTime 重置为当前时间 -10 秒，触发立即重发，CmdResult 回置 0。</item>
        ///   <item><b>控制指令，超过重试次数</b>：标记 CmdResult=3（由 ResetCompleted 删除），
        ///         异步发布"超时"控制结果通知主程序。</item>
        /// </list>
        /// </summary>
        private void ProcessTimeouts(List<CmdInfo> list)
        {
            DateTime now = DateTime.Now;
            foreach (var t in list.Where(c =>
                c.CmdResult == 1 && c.CmdMode == 1 && c.WaitForResponse && c.SendOutTime < now).ToList())
            {
                t.CmdResult = 0;
                t.TimeOutSendCount++;

                //// 采集指令：永不删除，重新调度到下一个采集周期
                //if (IsCollectCommand(t))
                //{
                //    t.SendNextTime = now.AddSeconds(_config?.CollectSleepSecond ?? 120);
                //    LogHelper.Info($"{PluginName}：采集超时，已重新调度，设备[{t.DeviceId}] addr[{t.DeviceAddr}]");
                //    continue;
                //}

                // 控制指令：按重试次数决定是否立即重发还是超时删除
                bool timeout = true;
                if (t.IsStartLimit && t.TimeOutLimitCount > t.TimeOutSendCount)
                {
                    timeout = false;
                    t.SendNextTime = now.AddSeconds(-10);
                }
                if (timeout)
                {
                    if (!t.CommandId.IsZxxNullOrEmpty())
                        _ = Task.Run(() => PublishControlResultAsync(t.CommandId, t.DeviceId,
                            ResolveDeviceName(t.DeviceId), false, "指令超时"));
                    t.CmdResult = 3;
                    continue;
                }
                LogHelper.Info($"{PluginName}：控制指令超时重试，设备[{t.DeviceId}] addr[{t.DeviceAddr}]");
            }
        }

        /// <summary>
        /// 整理指令队列，为下一轮发送做准备：
        /// <list type="bullet">
        ///   <item>已达发送上限（IsStartLimit=true 且 LimitCount ≤ SendCount）的指令直接移除；</item>
        ///   <item>已完成（CmdResult &gt; 1 且 ≠ 3）的循环采集指令，CmdResult 重置为 0，供下次采集；</item>
        ///   <item>超时标记（CmdResult=3）的指令全部删除。</item>
        /// </list>
        /// </summary>
        private static void ResetCompleted(List<CmdInfo> list)
        {
            foreach (var c in list.ToList())
            {
                if (c.IsStartLimit && c.LimitCount <= c.SendCount) { list.Remove(c); continue; }
                if (c.CmdResult > 1 && c.CmdResult != 3) c.CmdResult = 0;
            }
            list.RemoveAll(c => c.CmdResult == 3);
        }

        /// <summary>
        /// 从队列中选取下一条待发送的指令。
        /// 前置条件：队列中无飞行中指令（由 <see cref="HasInflight"/> 保证），
        /// 候选条件：CmdResult=0 且 SendNextTime ≤ 当前时间。
        /// 排序规则：优先级降序 → DeviceAddr 升序 → CmdType 升序。
        /// 若无满足条件的指令则返回 <c>null</c>。
        /// </summary>
        private static CmdInfo? SelectNext(List<CmdInfo> list)
        {
            if (HasInflight(list)) return null;
            DateTime now = DateTime.Now;
            return list.Where(c => c.CmdResult == 0 && c.SendNextTime <= now)
                       .OrderByDescending(GetPriority)
                       .ThenBy(c => c.DeviceAddr)
                       .ThenBy(c => c.CmdType)
                       .FirstOrDefault();
        }

        /// <summary>
        /// 将指令追加到指定端点（endpoint）的指令队列中。
        /// 若队列不存在则自动创建后再追加。
        /// 追加完成后调用 <see cref="EnsureEndpointWorker"/> 确保该端点的发送循环已启动。
        /// </summary>
        private void EnqueueEndpointCommand(string endpoint, CmdInfo cmd)
        {
            lock (_cmdLock)
            {
                if (!DicCmd.TryGetValue(endpoint, out var list))
                {
                    list = new List<CmdInfo>();
                    DicCmd[endpoint] = list;
                }
                list.Add(cmd);
            }
            EnsureEndpointWorker(endpoint);
        }

        /// <summary>
        /// 从指定端点的指令队列中移除某条已完成或已废弃的指令。
        /// <see cref="ProcessOnReceived"/> 处理完 FC06/FC16/FC03 回执后调用此方法清除一次性控制指令。
        /// </summary>
        private void RemoveCommand(string endpoint, CmdInfo cmd)
        {
            lock (_cmdLock)
            {
                if (DicCmd.TryGetValue(endpoint, out var list)) list.Remove(cmd);
            }
        }

        /// <summary>
        /// 将指定端点上所有采集指令的 SendNextTime 重置为当前时间，
        /// 当内机控制（FC06）成功后立即触发该端点下所有压缩机的状态采集。
        /// </summary>
        private void AccelerateAllCollects(string endpoint)
        {
            lock (_cmdLock)
            {
                if (!DicCmd.TryGetValue(endpoint, out var list)) return;
                DateTime now = DateTime.Now;
                foreach (var c in list.Where(IsCollectCommand))
                {
                    c.SendNextTime = now;
                    if (c.CmdResult > 1) c.CmdResult = 0;
                }
            }
        }

        /// <summary>
        /// 将指定端点下指定从站地址的所有采集指令的 <c>SendNextTime</c> 重置为当前时间，
        /// 并将已完成的指令（CmdResult &gt; 1）回置为 0，使其在下一次 <see cref="TrySend"/> 立即参与选取。
        /// 用于控制回执成功后触发即时采集，以便快速获取最新状态。
        /// </summary>
        private void AccelerateCollectCmd(string endpoint, int deviceAddr)
        {
            lock (_cmdLock)
            {
                if (!DicCmd.TryGetValue(endpoint, out var list)) return;
                DateTime now = DateTime.Now;
                foreach (var c in list.Where(t => IsCollectCommand(t) && t.DeviceAddr == deviceAddr))
                {
                    c.SendNextTime = now;
                    if (c.CmdResult > 1) c.CmdResult = 0;
                }
            }
        }

        /// <summary>
        /// 遍历 <see cref="DicCmd"/> 中的所有端点，为每个端点调用 <see cref="EnsureEndpointWorker"/>，
        /// 确保初始化完成或设备配置刷新后所有端点的发送循环均已运行。
        /// </summary>
        private void SyncEndpointSendWorkers()
        {
            List<string> eps;
            lock (_cmdLock) { eps = DicCmd.Keys.ToList(); }
            foreach (var ep in eps) EnsureEndpointWorker(ep);
        }

        /// <summary>
        /// 确保指定端点的发送循环 Task 正在运行。
        /// 若对应 Task 不存在或已完成，则启动新的 <see cref="RunSendLoopAsync"/> Task 并记录到 <c>_endpointSendTasks</c>。
        /// 若插件已停止（<c>_sendCancellation</c> 为 null 或已取消）则不启动，避免在停止过程中产生新 Task。
        /// </summary>
        private void EnsureEndpointWorker(string endpoint)
        {
            if (endpoint.IsZxxNullOrEmpty()) return;
            if (_sendCancellation == null || _sendCancellation.IsCancellationRequested) return;
            lock (_sendWorkerLock)
            {
                if (_endpointSendTasks.TryGetValue(endpoint, out var t) && !t.IsCompleted) return;
                _endpointSendTasks[endpoint] = Task.Run(() => RunSendLoopAsync(endpoint, _sendCancellation.Token));
            }
        }

        /// <summary>
        /// 优雅停止所有端点的发送循环：
        /// 先取消 <c>_sendCancellation</c>，再等待所有 Task 完成后释放 <see cref="CancellationTokenSource"/>。
        /// 在 <see cref="PluginStop"/> 中作为最后一步异步调用。
        /// </summary>
        private async Task StopEndpointSendWorkersAsync()
        {
            var cts = _sendCancellation;
            _sendCancellation = null;
            cts?.Cancel();
            Task[] tasks;
            lock (_sendWorkerLock)
            {
                tasks = _endpointSendTasks.Values.ToArray();
                _endpointSendTasks.Clear();
            }
            if (tasks.Length > 0) try { await Task.WhenAll(tasks); } catch { }
            cts?.Dispose();
        }

        /// <summary>
        /// 每个端点独立运行的发送循环 Task。
        /// 每隔 <c>SendSecond</c> 毫秒（默认 300ms）调用一次 <see cref="TrySend"/>。
        /// 收到 <see cref="OperationCanceledException"/> 时静默退出（正常停止流程）；
        /// 其他异常记录日志后退出，由 <see cref="EnsureEndpointWorker"/> 在下次触发时重新启动。
        /// </summary>
        private async Task RunSendLoopAsync(string endpoint, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                    TrySend(endpoint);
                    await Task.Delay(_config?.SendSecond ?? 300, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { LogHelper.Error(ex); }
            finally
            {
                lock (_sendWorkerLock)
                {
                    if (_endpointSendTasks.TryGetValue(endpoint, out var t) && t.IsCompleted)
                        _endpointSendTasks.Remove(endpoint);
                }
            }
        }

        /// <summary>
        /// 实际发送一条指令的核心方法，执行顺序：
        /// <list type="number">
        ///   <item>无锁查找端点对应的 Session，Session 不存在则直接返回；</item>
        ///   <item>加锁处理超时（<see cref="ProcessTimeouts"/>），选取下一条指令（<see cref="SelectNext"/>），
        ///         若无可发指令则调用 <see cref="ResetCompleted"/> 整理后返回；</item>
        ///   <item>调用 <see cref="MarkSent"/> 标记指令状态；</item>
        ///   <item>发送 TCP 数据包；发送失败则将 CmdResult 回置为 0，下次循环重试。</item>
        /// </list>
        /// </summary>
        private void TrySend(string endpoint)
        {
            try
            {
                NetSession? session;
                lock (_netLock) { DicNet.TryGetValue(endpoint, out session); }
                if (session == null) return;

                CmdInfo? cmd = null;
                lock (_cmdLock)
                {
                    if (!DicCmd.TryGetValue(endpoint, out var list) || !list.IsZxxAny()) return;
                    ProcessTimeouts(list);
                    cmd = SelectNext(list);
                    if (cmd == null) { ResetCompleted(list); return; }
                    MarkSent(cmd, DateTime.Now);
                }

                var pk = new Packet(cmd.CmdStr.ToHex());
                bool ok = false;
                lock (_netLock)
                {
                    if (DicNet.TryGetValue(endpoint, out session)) { session.Send(pk); ok = true; }
                }
                if (!ok) { lock (_cmdLock) { cmd.CmdResult = 0; } return; }
                LogHelper.Info($"{PluginName}：{(IsActionCommand(cmd) ? "控制" : "采集")}指令：{cmd.CmdStr}，设备[{cmd.DeviceId}]");
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        #endregion

        #region 设备初始化

        /// <summary>
        /// 从数据库重新加载全部设备绑定与采集指令，并原子性替换内存中的映射表和指令队列。
        /// 执行顺序：
        /// <list type="number">
        ///   <item>调用 <see cref="LoadDeviceBindings"/> 查询 DB；无设备则返回 false；</item>
        ///   <item>调用 <see cref="BuildBindingMap"/> / <see cref="BuildDeviceBindingMap"/> 构建索引；</item>
        ///   <item>调用 <see cref="BuildCommandMap"/> 生成采集指令；</item>
        ///   <item>加锁原子替换 <c>_bindingMap</c>、<c>_deviceBindingMap</c>、<see cref="DicCmd"/>；</item>
        ///   <item>调用 <see cref="SyncEndpointSendWorkers"/> 确保所有发送循环已启动。</item>
        /// </list>
        /// </summary>
        private bool RefreshBindingsAndCommands(out string error)
        {
            error = "";
            var outdoors = LoadGatewayBindings(out error);
            if (!outdoors.IsZxxAny()) return false;

            var outdoorMap = BuildGatewayMap(outdoors);
            var deviceIndoorMap = BuildDeviceIndoorMap(outdoors);
            if (!outdoorMap.Any()) { error = "未生成有效外机映射。"; return false; }

            var cmdMap = BuildCommandMap(outdoorMap);
            if (!cmdMap.Any()) { error = "未生成采集指令。"; return false; }

            // 全量加载舒适度配置（EntityCache 表，内存命中，无额外 DB 开销）
            var comfortList = DeviceComfortDAO.Instance.GetList() ?? new List<DeviceComfort>();
            lock (_bindingLock)
            {
                _gatewayMap = outdoorMap;
                _deviceIndoorMap = deviceIndoorMap;
                _comfortList = comfortList;
            }
            lock (_cmdLock)
            {
                DicCmd.Clear();
                foreach (var kv in cmdMap) DicCmd[kv.Key] = kv.Value;
            }
            SyncEndpointSendWorkers();
            int totalIndoor = outdoors.Sum(o => o.IndoorUnits.Count);
            LogHelper.Info($"{PluginName}：设备映射初始化完成，{outdoors.Count}台外机，{totalIndoor}台内机。");
            return true;
        }

        /// <summary>
        /// 加载外机绑定列表。
        /// 外机（父级，配有 IP+Port）作为 TCP 连接锚点；内机（子级，ParentId 指向外机）跟随外机连接。
        /// 一条 FC04 指令向外机 Modbus 地址发出，响应按内机 DeviceAdr 升序包含所有内机数据块。
        /// </summary>
        private List<GuoXiangGatewayBinding> LoadGatewayBindings(out string error)
        {
            error = "";
            if (_config == null) { error = "配置为空。"; return new(); }

            var allDevices = DeviceInfoDAO.Instance.GetListBy(t => t.DeviceTypeCode == DeviceTypeCode && t.IsCollection == 1);
            if (!allDevices.IsZxxAny())
            {
                error = $"未查询到[{DeviceTypeCode}]类型设备。";
                return new();
            }
            var outdoorById = allDevices.Select(t => t.DeviceId).ToList();
            // 内机：ParentId 指向外机
            var indoorDevices = DeviceInfoDAO.Instance.GetListBy(d => d.ParentId > 0 && outdoorById.Contains(d.ParentId));
            if (!indoorDevices.IsZxxAny())
            {
                error = $"[{DeviceTypeCode}]外机下未找到内机子设备。";
                return new();
            }

            // 批量加载内机参数
            var indoorIds = indoorDevices.Select(d => d.DeviceId).Distinct().ToList();
            var paramList = DeviceParamDAO.Instance.GetListBy(t => indoorIds.Contains(t.DeviceId));

            // 按外机分组
            var result = new List<GuoXiangGatewayBinding>();
            foreach (var outdoor in allDevices)
            {
                var innerUnits = indoorDevices
                    .Where(d => d.ParentId == outdoor.DeviceId)
                    .OrderBy(d => d.DeviceAdr)
                    .Select(d => new GuoXiangIndoorBinding
                    {
                        Device = d,
                        DeviceParam = paramList.Find(t => t.DeviceId == d.DeviceId)
                    })
                    .ToList();

                if (!innerUnits.Any())
                {
                    LogHelper.Info($"{PluginName}：外机[{outdoor.DeviceId}({outdoor.DeviceName})]下无内机，跳过。");
                    continue;
                }
                result.Add(new GuoXiangGatewayBinding { GatewayDevice = outdoor, IndoorUnits = innerUnits });
            }

            if (!result.Any()) error = $"[{DeviceTypeCode}]未构建任何有效外机绑定。";
            return result;
        }

        /// <summary>构建 endpoint → GuoXiangGatewayBinding 的字典。</summary>
        private static Dictionary<string, GuoXiangGatewayBinding> BuildGatewayMap(
            List<GuoXiangGatewayBinding> outdoors)
        {
            var result = new Dictionary<string, GuoXiangGatewayBinding>(StringComparer.OrdinalIgnoreCase);
            foreach (var ob in outdoors)
                result[ob.Endpoint] = ob;
            return result;
        }

        /// <summary>构建 DeviceId(内机) → (endpoint, outdoor, indoor) 的字典，供控制路径快速定位。</summary>
        private static Dictionary<int, (string Endpoint, GuoXiangGatewayBinding Outdoor, GuoXiangIndoorBinding Indoor)>
            BuildDeviceIndoorMap(List<GuoXiangGatewayBinding> outdoors)
        {
            var result = new Dictionary<int, (string, GuoXiangGatewayBinding, GuoXiangIndoorBinding)>();
            foreach (var ob in outdoors)
                foreach (var ib in ob.IndoorUnits)
                    result[ib.Device.DeviceId] = (ob.Endpoint, ob, ib);
            return result;
        }

        /// <summary>
        /// 为每台外机网关生成一条 FC04 定时采集指令（CmdType=1）。
        /// 使用外机网关自身的 Modbus 从站地址（GatewayDevice.DeviceAdr），
        /// 读寄存器 58~63（6个寄存器），取回网关状态（故障/室内温/室外温/模式）。
        /// </summary>
        private Dictionary<string, List<CmdInfo>> BuildCommandMap(
            Dictionary<string, GuoXiangGatewayBinding> outdoorMap)
        {
            var result = new Dictionary<string, List<CmdInfo>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in outdoorMap)
            {
                var gateway = kv.Value;
                var list = new List<CmdInfo>();

                //// FC04 所有内机 
                //var f4cmd = new CmdInfo
                //{
                //    CmdType = 1,
                //    CmdMode = 1,
                //    IpPort = kv.Key,
                //    DeviceId = gateway.GatewayDevice.DeviceId,
                //    DeviceAddr = gateway.GatewayDevice.DeviceAdr,
                //    CmdStr = GuoXiangAirHard.GetRealReadCmd(gateway.GatewayDevice.DeviceAdr),
                //    ExpectFuncCode = 0x04,
                //    WaitForResponse = true,
                //    SleepSecond = _config?.CollectSleepSecond ?? 120,
                //    OutSecond = _config?.CmdTimeOut ?? 10,
                //    TimeOutLimitCount = _config?.TimeOutLimitCount ?? 1
                //};
                //list.Add(f4cmd);

                // FC03 每台内机用户参数循环采集（设定模式/风速/温度/开关）
                foreach (var indoor in gateway.IndoorUnits)
                {
                    list.Add(new CmdInfo
                    {
                        CmdType = 2,
                        CmdMode = 1,
                        IpPort = kv.Key,
                        DeviceId = indoor.Device.DeviceId,
                        DeviceAddr = gateway.GatewayDevice.DeviceAdr,
                        CmdStr = GuoXiangAirHard.GetUserParamReadCmd(gateway.GatewayDevice.DeviceAdr, indoor.Device.DeviceAdr),
                        ExpectFuncCode = 0x03,
                        WaitForResponse = true,
                        SleepSecond = _config?.CollectSleepSecond ?? 120,
                        OutSecond = _config?.CmdTimeOut ?? 10,
                        TimeOutLimitCount = _config?.TimeOutLimitCount ?? 1
                    });
                }

                result[kv.Key] = list;
            }
            return result;
        }

        #endregion

        #region 数据发布

        /// <summary>
        /// 根据外机网关解析到的 <see cref="GuoXiangAirHard.AirStatusInfo"/> 为每台内机构建 <see cref="DeviceData"/> 并
        /// 发布 <see cref="PluginMessageEnum.协议解析"/> 消息到主程序事件总线。
        /// 网关采集的温度/模式/故障等数据将广播到所有内机设备。
        /// </summary>
        private async Task PublishRealtimeDataAsync(GuoXiangGatewayBinding gateway, GuoXiangAirHard.AirStatusInfo info)
        {
            try
            {
                var indoorDeviceIds = gateway.IndoorUnits.Select(u => u.Device.DeviceId).ToList();
                var relevanceTempMap = GetRelevanceDeviceTemp(indoorDeviceIds);

                var dataList = new List<DeviceData>();
                foreach (var indoor in gateway.IndoorUnits)
                {
                    decimal? envirTemp = null;
                    if (relevanceTempMap.TryGetValue(indoor.Device.DeviceId, out var temps) && temps.Count > 0)
                    {
                        envirTemp = temps.Average(t => t.Temp);
                            LogHelper.Info($"{PluginName}：[FC04] 设备[{indoor.Device.DeviceName}({indoor.Device.DeviceId})] 关联温湿度设备{temps.Count}个，平均温度={envirTemp:F1}℃（原始AC温度={info.EnvirTemp}℃）");
                    }
                    else
                    {
                        LogHelper.Info($"{PluginName}：[FC04] 设备[{indoor.Device.DeviceName}({indoor.Device.DeviceId})] 无关联温湿度设备，使用AC自带温度={info.EnvirTemp}℃");
                    }
                    var data = BuildDeviceData(indoor, info, envirTemp);
                    if (data != null) dataList.Add(data);
                }
                if (dataList.Count == 0) return;
                await SendMessageAsync(new PluginMessage
                {
                    MessageType = PluginMessageEnum.协议解析,
                    MessageJson = dataList.ToJson()
                });
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        /// <summary>
        /// 将 FC03 采集到的内机用户参数发布到主程序事件总线。
        /// </summary>
        private async Task PublishUserParamDataAsync(GuoXiangIndoorBinding indoor, GuoXiangAirHard.UserParamInfo info)
        {
            try
            {
                decimal? envirTemp = null;
                var relevanceTempMap = GetRelevanceDeviceTemp(new List<int> { indoor.Device.DeviceId });
                if (relevanceTempMap.TryGetValue(indoor.Device.DeviceId, out var temps) && temps.Count > 0)
                {
                    envirTemp = temps.Average(t => t.Temp);
                    LogHelper.Info($"{PluginName}：[FC03] 设备[{indoor.Device.DeviceName}({indoor.Device.DeviceId})] 关联温湿度设备{temps.Count}个，平均温度={envirTemp:F1}℃");
                }
                else
                {
                    LogHelper.Info($"{PluginName}：[FC03] 设备[{indoor.Device.DeviceName}({indoor.Device.DeviceId})] 无关联温湿度设备，EnvirTemp将为0");
                }

                var data = BuildUserParamDeviceData(indoor, info, envirTemp);
                if (data == null) return;
                await SendMessageAsync(new PluginMessage
                {
                    MessageType = PluginMessageEnum.协议解析,
                    MessageJson = new List<DeviceData> { data }.ToJson()
                });
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        /// <summary>
        /// 将命令中的 <see cref="NetAirConRun"/> 按以下优先级补全各 255 字段，并返回副本（不修改原对象）：
        /// <list type="number">
        ///   <item>优先取 <paramref name="devparam"/> 实时参数：AirSwitch/AirModel/AirSpeed 直接读整数，SetTemp 取整后赋给 AirModelTemp（16-30℃）；</item>
        ///   <item>仍为 255 的字段通过 <see cref="GetSeasonalDefaults"/> 按季节填入国标默认值（制冷 26℃ / 制热 20℃），AirSwitch 默认 0（不开机）。</item>
        /// </list>
        /// </summary>
        private NetAirConRun ResolveRunModel(NetAirConRun model, DeviceParamEntity? devparam, int unitId)
        {
            var r = new NetAirConRun
            {
                AirSwitch = model.AirSwitch,
                AirModel = model.AirModel,
                AirModelTemp = model.AirModelTemp,
                AirSpeed = model.AirSpeed,
            };

            // 第一步：从实时参数中补全 255 字段
            var exps = devparam?.ExpandObjects;
            if (exps?.Count > 0)
            {
                if (r.AirSwitch == 255)
                {
                    var v = exps.Find(e => string.Equals(e.ParamCode, "AirSwitch", StringComparison.OrdinalIgnoreCase));
                    if (v != null && !v.ParamValue.IsZxxNullOrEmpty()) r.AirSwitch = v.ParamValue.ToInt();
                }
                if (r.AirModel == 255)
                {
                    var v = exps.Find(e => string.Equals(e.ParamCode, "AirModel", StringComparison.OrdinalIgnoreCase));
                    if (v != null && !v.ParamValue.IsZxxNullOrEmpty()) r.AirModel = v.ParamValue.ToInt();
                }
                if (r.AirModelTemp == 255)
                {
                    var v = exps.Find(e => string.Equals(e.ParamCode, "SetTemp", StringComparison.OrdinalIgnoreCase));
                    if (v != null && !v.ParamValue.IsZxxNullOrEmpty())
                    {
                        int t = (int)Math.Round(v.ParamValue.ToDecimal());
                        if (t >= 16 && t <= 30) r.AirModelTemp = t;
                    }
                }
                if (r.AirSpeed == 255)
                {
                    var v = exps.Find(e => string.Equals(e.ParamCode, "AirSpeed", StringComparison.OrdinalIgnoreCase));
                    if (v != null && !v.ParamValue.IsZxxNullOrEmpty()) r.AirSpeed = v.ParamValue.ToInt();
                }
            }

            // 第二步：仍为 255 的字段使用季节默认值
            if (r.AirSwitch == 255 || r.AirModel == 255 || r.AirModelTemp == 255 || r.AirSpeed == 255)
            {
                GetSeasonalDefaults(unitId, out int seasonMode, out int seasonTemp);
                if (r.AirSwitch == 255) r.AirSwitch = 0;           // 默认不开机
                if (r.AirModel == 255) r.AirModel = seasonMode;
                if (r.AirModelTemp == 255) r.AirModelTemp = seasonTemp;
                if (r.AirSpeed == 255) r.AirSpeed = 0;           // 默认自动风速
            }

            return r;
        }

        /// <summary>
        /// 按当前月份匹配 <see cref="_comfortList"/> 中该单位的舒适度配置，推断季节并返回国标默认控制参数：<br/>
        /// 夏季（含"夏"/"制冷"）→ mode=1(制冷), temp=26℃；<br/>
        /// 冬季（含"冬"/"制热"）→ mode=4(制热), temp=20℃；<br/>
        /// 过渡季 / 无配置 → 按月份硬判断（6-9月夏季，1-2/11-12月冬季，其余过渡季 mode=0,temp=26）。
        /// </summary>
        private void GetSeasonalDefaults(int unitId, out int mode, out int temp)
        {
            int month = DateTime.Now.Month;
            var unitComforts = _comfortList.Where(c => c.UnitId == unitId).ToList();
            foreach (var comfort in unitComforts)
            {
                string monthExpr = comfort.MonthFormula?.Replace("M", month.ToString()) ?? "";
                if (string.IsNullOrEmpty(monthExpr)) continue;
                bool match;
                try { match = _comfortComputer.Compute(monthExpr, "true").ToBoolean(); }
                catch { continue; }
                if (!match) continue;

                string name = comfort.ComfortName ?? "";
                if (name.Contains("夏") || name.Contains("制冷") || name.Contains("summer", StringComparison.OrdinalIgnoreCase))
                { mode = 1; temp = 26; return; }
                if (name.Contains("冬") || name.Contains("制热") || name.Contains("winter", StringComparison.OrdinalIgnoreCase))
                { mode = 4; temp = 20; return; }
                // 过渡季：自动模式
                mode = 0; temp = 26; return;
            }

            // 无配置：按月份硬判断
            if (month >= 6 && month <= 9) { mode = 1; temp = 26; }
            else if (month <= 2 || month >= 11) { mode = 4; temp = 20; }
            else { mode = 0; temp = 26; }
        }

        /// <summary>
        /// 根据 UnitId 和室内温度从缓存的舒适度配置计算舒适度等级。
        /// 使用全局 <see cref="_comfortList"/> 缓存，不会对每台设备单独查询数据库。
        /// </summary>
        private int CalcEnvirComfort(int unitId, decimal envirTemp)
        {
            try
            {
                int month = DateTime.Now.Month;
                var unitComforts = _comfortList.Where(t => t.UnitId == unitId).ToList();
                foreach (var comfort in unitComforts)
                {
                    string monthExpr = comfort.MonthFormula?.Replace("M", month.ToString()) ?? "";
                    if (string.IsNullOrEmpty(monthExpr)) continue;
                    bool match;
                    try { match = _comfortComputer.Compute(monthExpr, "true").ToBoolean(); }
                    catch { continue; }
                    if (!match) continue;

                    string formula = comfort.ComfortFormula
                        ?.Replace("H", comfort.EnvirHumidity.ToString())
                        .Replace("T", envirTemp.ToString(CultureInfo.InvariantCulture)) ?? "";
                    if (string.IsNullOrEmpty(formula)) break;
                    try { return _comfortComputer.Compute(formula, null).ToInt(); }
                    catch { break; }
                }
            }
            catch { }
            return 0;
        }

        /// <summary>
        /// 将 <see cref="GuoXiangAirHard.AirStatusInfo"/> 中的物理量映射到设备参数模板，构建 <see cref="DeviceData"/>。
        /// 映射关系（ParamCode → 值）：
        /// <list type="bullet">
        ///   <item>EnvirTemp  — 室内环境温度（℃）；</item>
        ///   <item>AirModel   — 空调模式（0=自动…4=制热），由 <see cref="GuoXiangAirHard.ProtocolModeToAirModel"/> 转换；</item>
        ///   <item>AirSwitch  — 开机/关机（1/0）；</item>
        ///   <item>FanRunning — 风机运行（1/0）；</item>
        ///   <item>Fault      — 故障码原始值；</item>
        ///   <item>HasAlarm   — 是否报警（1/0）。</item>
        /// </list>
        /// 若参数配置了 ParamFormula 则应用公式计算；报警判断优先使用 ParamMinValue/ParamMaxValue，
        /// 否则使用 <c>CheckParamAlarm()</c>。无有效参数则返回 <c>null</c>。
        /// </summary>
        private DeviceData? BuildDeviceData(GuoXiangIndoorBinding binding, GuoXiangAirHard.AirStatusInfo info, decimal? effectiveEnvirTemp = null)
        {
            string timestr = DateTime.Now.ToDateTimeString();
            var dev = new DeviceInfoEntity();
            binding.Device.CopyTypeValue(dev);
            dev.ExpandObject = binding.Device.ExpandObject;
            dev.LastOnlineTime = timestr;
            dev.DeviceState = 2;

            if (binding.DeviceParam == null || !binding.DeviceParam.ExpandObjects.IsZxxAny())
            {
                return new DeviceData
                {
                    DeviceId = dev.DeviceId,
                    device = dev,
                    deviceparam = new List<Expand_DeviceParam>(),
                    paramtype = 0
                };
            }

            var paramValues = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                //["EnvirTemp"] = info.EnvirTemp,
                ["EnvirTemp"] = effectiveEnvirTemp ?? info.EnvirTemp,
                ["OutdoorTemp"] = info.OutdoorTemp,
                ["AirModel"] = GuoXiangAirHard.ProtocolModeToAirModel(info.SystemMode),
                ["Fault"] = info.FaultRaw,
                ["HasAlarm"] = info.HasAlarm ? 1m : 0m,
                ["EnvirComfort"] = CalcEnvirComfort(binding.Device.UnitId, effectiveEnvirTemp ?? info.EnvirTemp),
            };

            var realParams = new List<Expand_DeviceParam>();
            foreach (var tmpl in binding.DeviceParam.ExpandObjects)
            {
                if (!paramValues.TryGetValue(tmpl.ParamCode, out var raw)) continue;

                var p = new Expand_DeviceParam();
                tmpl.CopyTypeValue(p);
                p.StatusValues = tmpl.StatusValues;
                p.CollectTime = timestr;
                p.ParamLastValue = tmpl.ParamValue;
                p.ParamValue = raw.ToString(CultureInfo.InvariantCulture);

                if (!p.ParamFormula.IsZxxNullOrEmpty())
                {
                    string calc = ExpressoFormula.CalculateString(p.ParamFormula, p.ParamCode, (double)raw, p.DecimalDigit);
                    if (!calc.IsZxxNullOrEmpty()) p.ParamValue = calc;
                }

                p.IsAlarm = 0;
                if (p.ParamMinValue != 0 || p.ParamMaxValue != 0)
                {
                    decimal pv = p.ParamValue.ToDecimal();
                    if (pv < p.ParamMinValue || pv > p.ParamMaxValue) p.IsAlarm = 1;
                }
                else
                {
                    p.CheckParamAlarm();
                }

                if (!p.ParamValue.IsZxxNullOrEmpty()) realParams.Add(p);
            }

            if (!realParams.IsZxxAny()) return null;

            dev.DeviceAlarm = realParams.Any(p => p.IsAlarm == 1) ? 1 : 0;
            return new DeviceData
            {
                DeviceId = dev.DeviceId,
                device = dev,
                deviceparam = realParams,
                paramtype = 0
            };
        }

        /// <summary>
        /// 将 FC03 内机用户参数映射到设备参数模板，构建 <see cref="DeviceData"/>。
        /// 映射关系（ParamCode → 值）：
        /// <list type="bullet">
        ///   <item>AirModel  — 设定模式；</item>
        ///   <item>AirSpeed  — 设定风速；</item>
        ///   <item>SetTemp   — 设定温度（℃）；</item>
        ///   <item>AirSwitch — 开关状态（1=开,0=关）。</item>
        /// </list>
        /// </summary>
        private DeviceData? BuildUserParamDeviceData(GuoXiangIndoorBinding binding, GuoXiangAirHard.UserParamInfo info, decimal? effectiveEnvirTemp = null)
        {
            string timestr = DateTime.Now.ToDateTimeString();
            var dev = new DeviceInfoEntity();
            binding.Device.CopyTypeValue(dev);
            dev.ExpandObject = binding.Device.ExpandObject;
            dev.LastOnlineTime = timestr;
            dev.DeviceState = 2;

            if (binding.DeviceParam == null || !binding.DeviceParam.ExpandObjects.IsZxxAny())
            {
                return new DeviceData
                {
                    DeviceId = dev.DeviceId,
                    device = dev,
                    deviceparam = new List<Expand_DeviceParam>(),
                    paramtype = 0
                };
            }

            var paramValues = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["AirModel"] = info.AirModel,
                ["AirSpeed"] = info.AirSpeed,
                ["SetTemp"] = info.SetTemp,
                ["AirSwitch"] = info.AirSwitch,
                ["EnvirTemp"] = effectiveEnvirTemp ?? 0m,
                ["EnvirComfort"] = CalcEnvirComfort(binding.Device.UnitId, effectiveEnvirTemp ?? 0m),
            };

            var realParams = new List<Expand_DeviceParam>();
            foreach (var tmpl in binding.DeviceParam.ExpandObjects)
            {
                if (!paramValues.TryGetValue(tmpl.ParamCode, out var raw)) continue;

                var p = new Expand_DeviceParam();
                tmpl.CopyTypeValue(p);
                p.StatusValues = tmpl.StatusValues;
                p.CollectTime = timestr;
                p.ParamLastValue = tmpl.ParamValue;
                p.ParamValue = raw.ToString(CultureInfo.InvariantCulture);

                if (!p.ParamFormula.IsZxxNullOrEmpty())
                {
                    string calc = ExpressoFormula.CalculateString(p.ParamFormula, p.ParamCode, (double)raw, p.DecimalDigit);
                    if (!calc.IsZxxNullOrEmpty()) p.ParamValue = calc;
                }

                p.IsAlarm = 0;
                if (p.ParamMinValue != 0 || p.ParamMaxValue != 0)
                {
                    decimal pv = p.ParamValue.ToDecimal();
                    if (pv < p.ParamMinValue || pv > p.ParamMaxValue) p.IsAlarm = 1;
                }
                else
                {
                    p.CheckParamAlarm();
                }

                if (!p.ParamValue.IsZxxNullOrEmpty()) realParams.Add(p);
            }

            if (!realParams.IsZxxAny()) return null;

            dev.DeviceAlarm = realParams.Any(p => p.IsAlarm == 1) ? 1 : 0;
            return new DeviceData
            {
                DeviceId = dev.DeviceId,
                device = dev,
                deviceparam = realParams,
                paramtype = 0
            };
        }

        /// <summary>
        /// 封装控制结果并发布 <see cref="PluginMessageEnum.控制结果"/> 消息到主程序事件总线。
        /// 若 commandId 为空则直接返回，不发布。
        /// </summary>
        /// <param name="commandId">主程序下发的指令 ID，用于回显给主程序对账。</param>
        /// <param name="deviceId">执行操作的设备 ID。</param>
        /// <param name="deviceName">设备名称（用于人机展示）。</param>
        /// <param name="success">本次控制是否成功。</param>
        /// <param name="message">结果描述文本。</param>
        private async Task PublishControlResultAsync(string commandId, int deviceId,
            string deviceName, bool success, string message)
        {
            if (commandId.IsZxxNullOrEmpty()) return;
            var result = new PluginControlResultMessage
            {
                CommandId = commandId,
                ResultTime = DateTime.Now.ToDateTimeString(),
                DeviceResults = new List<ControlDeviceResult>
                {
                    new ControlDeviceResult
                    {
                        DeviceId   = deviceId,
                        DeviceName = deviceName,
                        Success    = success,
                        Message    = message,
                        ResultTime = DateTime.Now.ToDateTimeString()
                    }
                }
            };
            await SendMessageAsync(new PluginMessage
            {
                MessageType = PluginMessageEnum.控制结果,
                MessageJson = result.ToJson()
            });
        }

        /// <summary>
        /// 将指定端点（endpoint）下所有设备的运行状态批量发布为 <see cref="PluginMessageEnum.运行状态"/> 消息。
        /// </summary>
        /// <param name="endpoint">端点 key（"IP:Port" 格式），用于从 <see cref="_bindingMap"/> 中查找设备列表。</param>
        /// <param name="state">运行状态值：0=离线，2=在线。</param>
        private async Task PublishRunStateByEndpointAsync(string endpoint, int state)
        {
            if (endpoint.IsZxxNullOrEmpty()) return;
            var snap = _gatewayMap;
            if (!snap.TryGetValue(endpoint, out var outdoor) || !outdoor.IndoorUnits.Any()) return;

            var list = new List<DeviceData>();
            foreach (var indoor in outdoor.IndoorUnits)
            {
                var dev = new DeviceInfoEntity();
                indoor.Device.CopyTypeValue(dev);
                dev.ExpandObject = indoor.Device.ExpandObject;
                dev.DeviceState = state;
                list.Add(new DeviceData
                {
                    DeviceId = dev.DeviceId,
                    device = dev,
                    deviceparam = new List<Expand_DeviceParam>(),
                    paramtype = 0
                });
            }
            if (!list.IsZxxAny()) return;
            await SendMessageAsync(new PluginMessage
            {
                MessageType = PluginMessageEnum.运行状态,
                MessageJson = list.ToJson()
            });

        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 将远端 IP 字符串映射到 <see cref="_bindingMap"/> 中对应的端点 key（"IP:Port"）。
        /// 处理 IPv4-mapped IPv6 地址（如 <c>::ffff:192.168.1.1</c>），
        /// 自动还原为标准 IPv4 格式后按 IP 部分模糊匹配（忽略来源端口，使用配置中的目标端口）。
        /// 未匹配时返回 <c>null</c>，调用方需回退到 <c>"remoteIp:remotePort"</c>。
        /// </summary>
        private string? ResolveEndpointKey(string remoteIpStr)
        {
            if (remoteIpStr.IsZxxNullOrEmpty()) return null;
            if (IPAddress.TryParse(remoteIpStr, out var parsed) && parsed.IsIPv4MappedToIPv6)
                remoteIpStr = parsed.MapToIPv4().ToString();
            var snap = _gatewayMap;
            return snap.Keys.FirstOrDefault(k =>
            {
                int c = k.LastIndexOf(':');
                return c > 0 && k[..c].Equals(remoteIpStr, StringComparison.OrdinalIgnoreCase);
            });
        }

        /// <summary>
        /// 按端点 key + 外机网关 Modbus 地址查找网关绑定（无锁快照读）。
        /// 所有 Modbus 应答均使用外机网关的 DeviceAdr，因此只需匹配网关地址。
        /// </summary>
        private bool TryGetGateway(string key, int slaveAddr, out GuoXiangGatewayBinding outdoor)
        {
            var snap = _gatewayMap;
            if (snap.TryGetValue(key, out outdoor!) && outdoor.GatewayDevice.DeviceAdr == slaveAddr) return true;
            outdoor = null!;
            return false;
        }

        /// <summary>
        /// 按 DeviceId 查找内机绑定及所属端点、外机绑定（无锁快照读）。
        /// 用于控制路径：主程序下发控制指令时只携带 DeviceId。
        /// </summary>
        private bool TryGetIndoorByDeviceId(int deviceId, out string endpoint,
            out GuoXiangGatewayBinding outdoor, out GuoXiangIndoorBinding indoor)
        {
            var snap = _deviceIndoorMap;
            if (snap.TryGetValue(deviceId, out var item))
            {
                endpoint = item.Endpoint;
                outdoor = item.Outdoor;
                indoor = item.Indoor;
                return true;
            }
            endpoint = ""; outdoor = null!; indoor = null!;
            return false;
        }

        private string ResolveDeviceName(int deviceId) =>
            _deviceIndoorMap.TryGetValue(deviceId, out var item) ? item.Indoor.Device.DeviceName : "";

        #endregion

        #region VRV空调策略执行
        private int GetDayOfWeek(DateTime ndt)
        {
            int week = 0;
            var dt = ndt.DayOfWeek.ToString();
            switch (dt)
            {
                case "Monday":
                    week = 1;
                    break;
                case "Tuesday":
                    week = 2;
                    break;
                case "Wednesday":
                    week = 3;
                    break;
                case "Thursday":
                    week = 4;
                    break;
                case "Friday":
                    week = 5;
                    break;
                case "Saturday":
                    week = 6;
                    break;
                case "Sunday":
                    week = 7;
                    break;
            }
            return week;
        }

        /// <summary>
        /// 批量获取关联温湿度设备温度数据
        /// </summary>
        /// <param name="indoorDeviceIds">内机设备ID列表</param>
        /// <returns>字典：Key=内机设备ID, Value=关联温湿度设备温度列表</returns>
        private Dictionary<int, List<(int DeviceId, string DeviceName, decimal Temp)>> GetRelevanceDeviceTemp(List<int> indoorDeviceIds)
        {
            var result = new Dictionary<int, List<(int, string, decimal)>>();

            try
            {
                // 批量获取所有内机的关联温湿度设备
                var relevanceList = DeviceRelevanceDAO.Instance.GetListBy(it =>
                    indoorDeviceIds.Contains(it.DeviceId) &&
                    it.RelevanceTypeCode == "cenbowsd");

                if (!relevanceList.IsZxxAny())
                {
                    return result;
                }

                // 获取所有关联的温湿度设备ID
                var relevanceDeviceIds = relevanceList.Select(it => it.RelevanceId).Distinct().ToList();
                if (!relevanceDeviceIds.IsZxxAny())
                {
                    return result;
                }

                // 批量获取温湿度设备的参数
                var paramList = DeviceParamDAO.Instance.GetListBy(it => relevanceDeviceIds.Contains(it.DeviceId));
                if (!paramList.IsZxxAny())
                {
                    return result;
                }

                // 构建结果字典：内机ID -> 温度列表
                foreach (var relevance in relevanceList)
                {
                    var param = paramList.Find(it => it.DeviceId == relevance.RelevanceId);
                    if (param == null) continue;

                    var tempStr = param.ExpandObjects?.GetDeviceExpandParamValue("temp");
                    if (tempStr.IsNullOrEmpty()) continue;

                    // 温度值可能是小数字符串（如"25.6"），先按 decimal 解析再取整，避免 ToZxxInt 直接解析返回 0
                    if (!decimal.TryParse(tempStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var tempDec)) continue;
                    decimal temp = Math.Round(tempDec, 1);
                    LogHelper.Info($"【{relevance.DeviceId}-{relevance.RelevanceName}】关联温湿度设备温度：{temp}℃");

                    if (temp > -50 && temp < 100) // 合理的温度范围
                    {
                        if (!result.ContainsKey(relevance.DeviceId))
                        {
                            result[relevance.DeviceId] = new List<(int, string, decimal)>();
                        }
                        result[relevance.DeviceId].Add((relevance.RelevanceId, relevance.RelevanceName, temp));
                    }
                }

                if (result.Count > 0)
                {
                    LogHelper.Info($"获取关联温湿度设备温度：{result.Count}台内机,共{result.Sum(kv => kv.Value.Count)}个关联设备");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }

            return result;
        }

        #endregion

    }

    /// <summary>
    /// 外机绑定：包含外机设备信息及其下挂的所有内机。
    /// 外机 = TCP 连接锦点（IP:Port），一条 FC04 指令取回全部内机数据。
    /// </summary>
    /// <summary>
    /// 内机绑定：内机（有 IP：Port）为 TCP 连接锤点，下挂多台压缩机（外机）。
    /// FC06/FC03 以内机 GatewayDevice.DeviceAdr 发送；FC04 以各压缩机自身 DeviceAdr 发送。
    /// </summary>
    internal sealed class GuoXiangGatewayBinding
    {
        public DeviceInfoEntity GatewayDevice { get; set; } = null!;
        public List<GuoXiangIndoorBinding> IndoorUnits { get; set; } = new();
        public string Endpoint => $"{GatewayDevice.DeviceIp}:{GatewayDevice.DevicePort}";
    }

    /// <summary>
    /// 内机绑定：内机设备信息 + 参数模板。
    /// Device.DeviceAdr 为 1-based 槽位序号，同时作为写指令的 Modbus 从站地址。
    /// </summary>
    internal sealed class GuoXiangIndoorBinding
    {
        public DeviceInfoEntity Device { get; set; } = null!;
        public DeviceParamEntity? DeviceParam { get; set; }
    }

    internal sealed class AirConControlCommand : PluginCommandBase
    {
        public string ClassName { get; set; } = "";
        public string ConContent { get; set; } = "";
    }
}
