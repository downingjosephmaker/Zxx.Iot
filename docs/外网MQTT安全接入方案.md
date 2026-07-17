# 外网 MQTT 安全接入方案

> 目标：让设备可以安全地从**外网**接入平台 MQTT，代价是补齐现在完全没有的三样东西：**链路加密、每设备凭据、Topic ACL**。
> 设计原则：**设备端零证书**。加密只用"服务端自签证书 + 客户端跳过证书链校验"，不建 CA 体系、不预置根证书、不做客户端 X.509。
> 前置事实：**当前 broker 一旦开到公网即等于裸奔**，本方案落地前不得暴露任何 MQTT 端口到外网。

## 一、现状：三样全缺（带行号）

平台**自己内嵌**了一个 MQTT Broker（`IotWebApi/Services/Jobs/MqttServerJob.cs:119`，MQTTnet Server），设备连的就是它。

### 1.1 认证 = 全局单账号，明文比对

```csharp
// MqttServerJob.cs:184
if ((arg.UserName ?? "") != MqttParam.MqttUser || (arg.Password ?? "") != MqttParam.MqttPass)
```

所有设备**共用同一套用户名密码**，从 `admin_mqttparam` 表读出，明文比较。

### 1.2 TLS = 完全没有

```csharp
// MqttServerJob.cs:109-117 —— 服务端选项的全部内容
optionsBuilder.WithDefaultEndpointBoundIPAddress(IPAddress.Any);
optionsBuilder.WithDefaultEndpointBoundIPV6Address(IPAddress.IPv6Any);
optionsBuilder.WithDefaultEndpointPort(MqttParam.MqttServerPort);
optionsBuilder.WithConnectionBacklog(1000);
```

无 `WithEncryptedEndpoint`，无证书配置。**纯明文**，且 `IPAddress.Any` **监听所有网卡**。

### 1.3 Topic 权限 = 不存在

`InterceptingPublishAsync`（`MqttServerJob.cs:199-206`）**只写日志，不做任何校验**。

### 1.4 原作者留的欠条

```
// MqttFlappingGuard.cs:10-11
注:§6.5认证三级缓存暂不适用——当前Broker为内存单一全局账号比对无每连接DB查询,
待每设备凭据模型落地时再引入
```

**这不是疏忽，是一张明确的欠条。** 本方案就是来还它的。

## 二、攻击链条（为什么必须先修再开外网）

MQTT 上行的设备身份 = **topic 末段**（`MqttClientJob.cs:198-199`）：

```csharp
var segments = args.ApplicationMessage.Topic.Split('/', ...);
string devicekey = segments.Length > 0 ? segments[^1] : "";
```

把 1.1 + 1.2 + 1.3 串起来：

> 抓一次明文包拿到那**唯一一个**全局密码
> → 用**任意 ClientId** 连上（ClientId 与设备无绑定）
> → 往**任意 topic** publish（无 ACL）
> → **冒充平台上任何一台设备**灌数据

`MqttFlappingGuard` 挡不住：它防的是**猜密码的人**，而攻击者已经持有正确密码。

### 2.1 对标：主流平台把身份建在凭据上，不建在 topic 上

| 平台 | 设备身份从哪来 | topic |
|---|---|---|
| ThingsBoard（22k★） | access token 当 MQTT username，连接时认证 | `v1/devices/me/telemetry`——**"me"，不写设备 ID** |
| IoTSharp（同栈 .NET+MQTTnet） | 每设备凭据，连接时认证 | 同理，身份由认证决定 |
| emqx（16k★，最高分纯 broker） | per-client 认证 + ACL | 认证与 ACL 就是它的产品核心 |
| **本平台（现状）** | **topic 末段那串字符** | `{prefix}/{deviceKey}`——**谁填谁就是谁** |

**本方案的方向就是把身份从 topic 挪回凭据。**

## 三、设计：内外网分层

沿用"局域网靠网络边界、外网靠密码学"的分层：

| | 端口 | 绑定 | 认证 | 加密 | 适用 |
|---|---|---|---|---|---|
| **内网** | 1883 | **改为绑内网网卡**（不再 `Any`） | 维持全局账号 | 明文 | 局域网设备、存量设备 |
| **外网** | **8883** | `Any` | **每设备凭据（哈希）+ Topic ACL** | **强制 TLS（自签，仅加密）** | 外网设备 |

内网口收窄绑定是纯收益，与外网改造无关，可独立先行。**外网设备只准走 8883，禁止走 1883 明文口。**

### 3.1 TLS 端点（仅加密，不验证服务端身份）

TLS 有两半职责，本方案**只要"加密链路"这一半，砍掉"验证服务端身份"那一半**——后者才是证书体系麻烦的来源。

**服务端**追加加密端点（MQTTnet Server）：

```csharp
optionsBuilder.WithEncryptedEndpoint();
optionsBuilder.WithEncryptedEndpointPort(8883);
optionsBuilder.WithEncryptionCertificate(selfSignedCert);   // 自签，见 3.2
optionsBuilder.WithEncryptionSslProtocol(SslProtocols.Tls12 | SslProtocols.Tls13);
```

**设备端**（MQTTnet Client）只需把现在写死的开关翻过来，再加一句"接受自签证书"：

```csharp
// 现状 MqttClientJob.cs:93-96 是 UseTls = false
.WithTlsOptions(o => o
    .UseTls(true)
    .WithCertificateValidationHandler(_ => true))   // 跳过证书链校验，接受自签
```

**设备端零证书文件**，只多这一个 flag。8883 是 MQTT over TLS 的标准端口。

### 3.2 证书：服务端自签，不建 CA 体系

**决策：服务端一张自签证书即可，不建 CA、不签发设备证书、不预置根证书。**

- 服务端用 `.NET` 内置 `CertificateRequest.CreateSelfSigned` 或一行 `openssl` 生成——**零申请、零 CA 体系、零公网域名依赖**。
- 客户端跳过证书链校验（3.1），所以**不需要**信任任何 CA，也就不需要预置根证书。

**这样做防住什么、防不住什么（诚实标注）：**

| | 防被动抓包/嗅探（最常见威胁） | 防主动中间人（MITM） |
|---|---|---|
| 现状（明文） | ❌ | ❌ |
| **本方案（自签 + 跳过链校验）** | ✅ | ❌ |
| 严格档（验证服务端证书） | ✅ | ✅ |

**唯一代价**：攻击者若能在网络路径上做中间人（DNS 劫持 / ARP 欺骗 / BGP 劫持），可假冒服务器骗取凭据。但这门槛远高于"随便抓个包"，对"自家设备连自家平台"通常可接受。

**将来若要防中间人**：走**证书指纹 pinning**（设备固件写死服务端证书的公钥指纹，只信那一张）——**不碰 CA、不碰文件**，只是固件里一个常量，服务端换证书时同步更新。列为远期可选，非本期范围。

### 3.3 每设备凭据

新表 **`device_mqtt_credential`**：

| 列 | 说明 |
|---|---|
| `DeviceId` | 关联设备 |
| `MqttClientId` | 设备的 MQTT ClientId / username，**唯一**，可明文（它是标识不是秘密） |
| `PassHash` | 设备密码的 PBKDF2 哈希（密码才是秘密） |
| `Salt` | 每设备独立盐 |
| `IsEnable` | 吊销开关 |
| `CreateTime` / `LastAuthTime` | 审计 |

**为什么独立建表而不是给 `DeviceInfo` 加列**：

- 凭据是安全资产。加在设备表上，意味着每次普通设备查询都把哈希带进内存，白白扩大泄露面。
- 独立表支持**吊销**（`IsEnable`）与轮换，不惊动设备表。

**哈希算法**：PBKDF2（.NET 内置 `Rfc2898DeriveBytes`），**绝不沿用现在的明文比对**。凭据经 3.1 的 TLS 通道传输，不会明文过网。

### 3.4 认证缓存（还原作者那张欠条）

PBKDF2 是**慢哈希**（这正是它的价值），但 MQTT 每次连接都验一遍会拖垮连接风暴下的表现。

**两级缓存**：

1. **L1 内存**：`ClientId → (校验通过, 有效期)`，命中即放行
2. **L2 数据库**：未命中查 `device_mqtt_credential`，验过写回 L1

**不引入 Redis**（原注释提到的"三级缓存"的第三级）：当前是单实例部署，L1+L2 足够。等真出现多实例再补第三级——YAGNI。

凭据变更/吊销时清 L1（与 `ProtocolScriptService.Reload()` 同构的做法）。

### 3.5 Topic ACL —— 从根上堵死冒充（整个方案的关键）

约定设备上行 topic：`{MqttSubTopicWebApi}/{deviceKey}`（与现有约定一致，不变）。

在 `InterceptingPublishAsync` 中校验：

```
该连接的 ClientId 绑定的 deviceKey  ==  topic 末段的 deviceKey ？
    是 → 放行
    否 → arg.ProcessPublish = false，记日志
```

连接通过 `ValidatingConnectionAsync` 时，把 `ClientId → deviceKey` 写入会话映射；断开时清除。

**这一条是整个方案的关键**：即使某台设备的凭据泄露（含 MITM 骗到），攻击者也**只能冒充那一台**，无法横向冒充其他设备。爆炸半径从"全平台"缩小到"一台"。

### 3.6 内网口收窄

```csharp
// 改前：监听所有网卡，含公网
optionsBuilder.WithDefaultEndpointBoundIPAddress(IPAddress.Any);

// 改后：只绑内网网卡（可配置）
optionsBuilder.WithDefaultEndpointBoundIPAddress(IPAddress.Parse(MqttParam.LanBindAddress));
```

## 四、迁移路径（存量设备不能断）

1. **第一步（可独立发布）**：内网口收窄绑定。存量设备无感。
2. **第二步**：加 8883 TLS 端点（自签证书）+ `device_mqtt_credential` 表 + 认证缓存 + ACL。**1883 明文口保持不变**，存量设备照常跑。
3. **第三步**：新设备/外网设备走 8883；逐台为设备签发凭据（一套 ClientId + 密码），固件开 TLS 开关。**无需预置任何证书文件。**
4. **第四步（远期）**：当所有设备迁完，1883 口可只对内网开放，或彻底关闭。

**任何一步都不破坏存量链路。**

## 五、部署前提（非代码，但必须落实）

- 8883 需要**真实的公网可达**。cloudflared 隧道**不适用**（它主要面向 HTTP，TCP 隧道要求设备端也装客户端），需要路由器/防火墙端口映射，或云服务器直接监听。
- 服务端自签证书由平台生成并保管；**过期只换服务端证书**，因客户端跳过链校验，换证书对设备无感（连指纹 pinning 都没启用时）。
- 1883 内网口**不得**映射到公网。
- `MqttParam` 新增 `LanBindAddress`，改 `admin_mqttparam` 表须重启才生效（既有行为），需在部署文档注明。

## 六、风险

| 风险 | 缓解 |
|---|---|
| 主动中间人（自签 + 跳过链校验的已知代价） | 门槛远高于被动抓包；将来可加证书指纹 pinning（3.2）；Topic ACL 把单点泄露的爆炸半径限制在一台 |
| 设备凭据泄露 | Topic ACL 把爆炸半径限制在单台设备；`IsEnable` 可即时吊销 |
| 连接风暴下 PBKDF2 拖慢认证 | L1 内存缓存；`MqttFlappingGuard` 已有的封禁机制继续生效 |
| 自签服务端证书过期 | 客户端跳过链校验，过期换证书设备无感（未启用 pinning 时） |

## 七、验证

1. **单测**：PBKDF2 哈希/校验往返；ACL 判定（ClientId 与 topic 末段匹配/不匹配两条路径）；L1 缓存命中与失效。
2. **集成**：
   - 用正确凭据连 8883（TLS，自签证书）→ 成功，且链路已加密（抓包看不到明文）
   - 用错误凭据连 8883 → 拒绝，且 `MqttFlappingGuard` 计数
   - 设备 A 的凭据往设备 B 的 topic publish → **被 ACL 拒绝**（本方案的核心断言）
   - 1883 明文口存量链路 → 行为不变
3. **回归**：现有 MQTT 上行端到端（记忆中已跑通的那条链路）必须仍然全绿。

## 八、不做什么（YAGNI）

- **不做客户端 X.509 / mTLS**。设备端一张证书都不碰——太不实用，已明确排除。
- **不建 CA 体系、不签发设备证书、不预置根证书**。服务端一张自签证书即可，客户端跳过链校验。
- **不验证服务端证书链**。本方案只求"加密链路"，接受自签证书；防中间人是远期可选（证书指纹 pinning），非本期。
- **不引入 Redis 做第三级认证缓存**。单实例部署下 L1+L2 足够，多实例出现时再补。
- **不改现有 topic 约定**。`{prefix}/{deviceKey}` 保持不变，ACL 建在既有约定之上。
- **不动 `MqttFlappingGuard`**。它防爆破的职责不变，与本方案正交且互补。
