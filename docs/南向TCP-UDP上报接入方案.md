# 南向 TCP / UDP 上报接入方案

> 目标：让设备除 MQTT 外，还能通过 **TCP / UDP** 把数据主动报给平台，复用现有解码与入库链路。
> 定位：**局域网内**接入。外网一律走 MQTT，见 [外网MQTT安全接入方案.md](./外网MQTT安全接入方案.md)。
> 关联：TCP 流定界依赖 [协议通用工具库与脚本注入方案.md](./协议通用工具库与脚本注入方案.md) 的脚本沙箱。

## 一、范围

| 传输 | 状态 |
|---|---|
| MQTT | ✅ 已完整实现，本方案不动它，**以它为模板** |
| TCP | 🔨 本方案新增 |
| UDP | 🔨 本方案新增 |
| ~~HTTP~~ | ❌ **已撤销**，不做 |

**这是南向（数据进来）。** 与北向转发（数据出去）是反方向，见 [北向设备状态事件方案.md](./北向设备状态事件方案.md)。

## 二、模板：MQTT 上行链路

```
设备 → Broker → MqttClientJob.MessageReceivedHandler
   ├─ 契约① PluginMessage{MessageType, MessageJson}  → 原样路由
   ├─ 契约② 裸 List<DeviceData>                      → 包成「协议解析」
   └─ 契约③ 非JSON兜底 → ScriptService.DecodeMqttPayload(topic末段, buffer)
                            ↓
              bus.Publish(new PluginEvent(MqttPluginGuid, message))
                            ↓
                   DataPointIngestService（统一上行管道）
```

**关键：上行管道早已统一。** MQTT 是用一个"虚拟插件 GUID"（`MqttClientService.cs:22`）混进插件上行管道的。TCP/UDP 照此办理，各发一个自己的 GUID 进同一条总线即可。

## 三、两处名字骗人的东西（必须先提取）

### 3.1 三层契约解析是**传输无关**的

`MqttClientJob.cs:172-215` 那段解析逻辑，通读下来只用到 `(devicekey, buffer, strdata)`，**没有一行是 MQTT 特有的**。

不提取，TCP/UDP 就要各抄一遍——**三份重复**。

**新建** `IotWebApi/Services/Uplink/UplinkPayloadRouter.cs`：

```csharp
// 输入：来源标识 + 设备标识 + 原始字节
// 输出：PluginMessage?（null = 无法识别，调用方丢弃并记日志）
PluginMessage? Route(string source, string devicekey, byte[] payload)
```

`MqttClientJob.MessageReceivedHandler` 改为调用它，**行为逐字不变**（现有 MQTT 端到端链路必须仍然全绿）。

### 3.2 `DecodeMqttPayload` 与 MQTT 无关

`ProtocolScriptService.DecodeMqttPayload` 的函数体只是"用 devicekey 查设备 → 查脚本 → 解码 → 建 DeviceData"，**没有任何 MQTT 语义**。

**改名为 `DecodePayload`**，逻辑零改动。

## 四、设备身份：来源 IP

**决策：TCP 与 UDP 一律按来源 IP 匹配 `DeviceInfo.DeviceIp`。**

依据：本方案定位**局域网内**接入——**能进那个网段本身就是一道认证**，网络边界即信任边界。这与外网走 MQTT 强认证的分层是配套的。

已知代价（接受，但需在部署文档注明）：

- 设备走 NAT 或 DHCP 换 IP 即认不出来 → **要求设备静态 IP 或 DHCP 保留地址**
- 同网段内任何主机都能伪造上报 → 靠网段隔离与 IP 白名单兜底（见 §7）

## 五、UDP 接入

**新建** `IotWebApi/Services/Uplink/UdpUplinkListener.cs`（`IHostedService`）。

- **定界：不需要。** UDP 数据报天然是一帧，收到一个包就是一帧。
- 流程：收包 → 来源 IP 查设备 → `UplinkPayloadRouter.Route("udp", devicekey, payload)` → 发 `PluginEvent(UdpPluginGuid, message)`
- **不实现 `IChannelTransport`**：设备主动上报，平台不需要向设备发，`Send` 用不上。造一个永远不被调用的 `Send` 就是造空壳。

## 六、TCP 接入

**复用 `IotDriverCore/Channels/TcpServerChannel`**，只用它的 `FrameReceived` 回调。

理由：连接管理、心跳过滤、空闲超时踢连接、日志全都现成。`EndpointResolver`（`Func<string, string?>`）正好用来按来源 IP 解析设备。不启用 DTU 注册（`RegistrationResolver` 留空），走纯 IP 匹配——这条路径 Modbus 插件已在用（`ModbusPluginConfig.EnableDtuRegistration = false` 时的行为）。

**新建** `IotWebApi/Services/Uplink/TcpUplinkListener.cs`（`IHostedService`）持有它。

### 6.1 🚨 TCP 的真难点：字节流定界

MQTT/UDP 天然定界，**TCP 不是**。设备连上来一路推字节，平台怎么知道一帧到哪结束？

**决策：按设备类型是否挂脚本二选一。**

来源 IP → 查到设备 → 查到设备类型 → 查是否挂了启用的协议脚本：

| 情况 | 定界方式 |
|---|---|
| 挂了脚本，且脚本定义了 `splitFrames` | **走 `splitFrames`**，配合 `FrameAccumulator` 累积 |
| 未挂脚本（JSON 设备） | 按 **换行符 `\n`** 定界 |

设备类型既已由 IP 定位，这个分派是确定的，不需要猜。

### 6.2 🎁 `splitFrames` 在此首次通电

`ScriptSandbox.RunSplitFrames` / `splitFrames(buffer, ctx) → {frames, consumed}` 此前**没有任何生产消费者**——只有 `ProtocolScriptService.DryRun` 的试运行页面在调。

原因很简单：**MQTT 天然定界，用不上它**。

TCP 上行是它天生要解决的问题，本方案让它第一次真正跑起来。

> 注：这更新了工具库方案 §7 中"不给 `splitFrames` 补生产链路"的判断——那条结论在当时（只有 MQTT）成立，TCP 上行落地后即被本方案取代。

### 6.3 累积与保护

`FrameAccumulator` 已提供按 endpoint 累积与 `Reset`。需补两条保护：

- **累积上限**：单连接缓冲超过阈值（如 64KB）仍拆不出帧 → 判定为垃圾流，`Reset` 并断开
- **`consumed` 必须推进**：脚本返回 `consumed == 0` 且无帧时，视为脚本失效，不得原地空转

## 七、安全

局域网边界是主防线，但不能只有它：

| 措施 | 说明 |
|---|---|
| **IP 白名单** | 监听器只接受 `DeviceInfo.DeviceIp` 中登记过的来源；未登记的 TCP 连接直接断，UDP 包直接丢 |
| **端口不得映射公网** | TCP/UDP 上报端口**只对内网网卡绑定**，与 MQTT 内网口收窄同理 |
| **脚本错误降级** | 复用 `ProtocolScriptService` 已有的滑动窗口自动禁用（5 分钟 100 次错误 → 禁用 + 平台通知） |
| **日志挂开关** | 载荷日志沿用 `MqttConfig:LogOpen` 同款开关，防高频上报刷盘 |

## 八、虚拟插件 GUID

与 `MqttClientService.MqttPluginGuid`（`MqttClientService.cs:22`）同构，各分配一个常量：

- `TcpUplinkPluginGuid`
- `UdpUplinkPluginGuid`

用于上行事件溯源，让 `DataPointIngestService` 侧能区分数据从哪条路进来。

## 九、验证

1. **回归（最重要）**：现有 MQTT 上行端到端链路必须**逐字不变**地全绿。`UplinkPayloadRouter` 提取属于纯重构。
2. **单测**：
   - `UplinkPayloadRouter` 三层契约各自命中（PluginMessage / 裸 List&lt;DeviceData&gt; / 脚本解码 / 全不匹配返回 null）
   - `\n` 定界：半包、粘包、连续多帧
   - `splitFrames` 定界：正常拆帧、`consumed == 0` 的失效保护、缓冲超限保护
   - IP 白名单：未登记来源被拒
3. **集成**：UDP 单包上报入库；TCP 粘包上报正确拆分入库。

## 十、不做什么（YAGNI）

- **不做 HTTP 接入**（已撤销）。
- **不提炼 `IChannelTransport` 的完整契约**。当前 `IChannelTransport` 只声明 `Send`，而 `TcpServerChannel` 与 `TcpClientChannelPool` 各自长出了一模一样的 `SessionOpened`/`SessionClosed`/`FrameReceived`/`IsOnline` 五件套却无接口约束——这是真技术债，但它属于"驱动通道可切换"那个议题，不是本方案的目标。本方案的 UDP 监听器刻意**不**实现 `IChannelTransport`，因此不会把重复从 2 份变成 3 份。
- **不给 UDP 做可靠性重传**。UDP 就是不可靠的，需要可靠就用 TCP 或 MQTT。在 UDP 上造一层重传等于重新发明 TCP。
- **不做设备主动上报的下行控制**。本方案只管数据进来。TCP/UDP 设备的控制下发若有需求，是独立议题。
