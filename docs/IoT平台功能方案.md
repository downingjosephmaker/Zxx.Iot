# Zxx.Iot 物联网中台功能方案

> 版本：v1.0（2026-07-06）
> 技术底座：.NET 10（现有 Zxx.Iot 解决方案增量演进） + vue-pure-admin（前端） + PostgreSQL（存储）
> 调研依据：本地参考项目（IoTSharp / iotgateway / ThingsGateway）源码深读 + GitHub star top7 开源 IoT 项目调研 + PostgreSQL/协议栈/前端专题论证（附录 A）

---

## 目录

1. [目标与范围](#1-目标与范围)
2. [现状盘点（Zxx.Iot 基座）](#2-现状盘点zxxiot-基座)
3. [开源调研结论](#3-开源调研结论)
4. [总体架构](#4-总体架构)
5. [产品与物模型](#5-产品与物模型)
6. [协议接入层](#6-协议接入层)
7. [采集与防抖动设计（核心）](#7-采集与防抖动设计核心)
8. [数据管道与存储（PostgreSQL 可行性专章）](#8-数据管道与存储postgresql-可行性专章)
9. [告警引擎与告警屏蔽（AlarmConfig）](#9-告警引擎与告警屏蔽alarmconfig)
10. [规则联动与北向转发](#10-规则联动与北向转发)
11. [前端方案（vue-pure-admin）](#11-前端方案vue-pure-admin)
12. [数据库表设计变更清单](#12-数据库表设计变更清单)
13. [实施路线图](#13-实施路线图)
14. [风险与决策点](#14-风险与决策点)
15. [组态大屏与 AI 能力融合](#15-组态大屏与-ai-能力融合)
16. [附录 A：调研来源](#附录-a调研来源)

---

## 1. 目标与范围

建设一个通用物联网中台，核心能力：

| # | 能力 | 说明 |
|---|------|------|
| 1 | 多协议接入 | **Modbus RTU/TCP**、**DL/T 645-2007 电表**、**CJ/T 188 水表**、**OPC UA**、**西门子 S7** 为内置 C# 驱动插件（插件优先）；**JS 脚本解析自定义协议**为长尾兜底扩展点 |
| 2 | 接入形态 | DTU/串口服务器 TCP 透传（注册包+心跳）、MQTT 直连、驱动插件主动轮询 |
| 3 | 防抖动 | 采集频率设置（设备级+点位级）、数据推送间隔（变化上报+死区+节流+强制周期）、告警延时防抖 |
| 4 | 告警体系 | 告警产生/恢复/确认闭环、去重、**告警屏蔽（AlarmConfig）**、屏蔽窗口、通知升级 |
| 5 | 海量存储 | PostgreSQL 承载遥测+日志，**默认保留 1 个月**，按天分区整块删除 |
| 6 | 前端 | vue-pure-admin（Vue3 + Element-Plus），SignalR 实时推送 |

**原则**：在现有 Zxx.Iot 代码基座上增量演进，不推倒重来；已有的 RBAC、设备 CRUD、AlarmConfig 配置字段全部沿用。

---

## 2. 现状盘点（Zxx.Iot 基座）

### 2.1 已有能力（沿用）

| 模块 | 现状 | 结论 |
|------|------|------|
| Web 框架 | ASP.NET Core WebAPI（net10.0）+ Swagger 多分组 + Token 过滤器 | 沿用 |
| ORM | **SqlSugarCore 5.1.4.216**（CodeFirst + SplitTable 分表：EventHistory 按日、EventRun/EventSignal 按月），当前连 MySQL/TiDB | 沿用 SqlSugar，**库切 PostgreSQL**（SqlSugar 一级支持 PG） |
| RBAC | 用户/角色/菜单/按钮权限（SysroleMenuBtn）完整 | 沿用，对接 pure-admin 动态菜单 |
| 插件机制 | `ICenBoPlugin` + collectible AssemblyLoadContext 热更新 + `sys_plugin` 表管理 | 沿用骨架，接口按 §6.1 扩展为驱动模型 |
| 事件总线 | CenboEventBus（进程内发布/订阅） | 沿用（单机阶段），预留 MQ 适配 |
| MQTT | MQTTnet 5.2 内嵌 Broker + 客户端 Job | 沿用，规模化后可切 EMQX（§4.3） |
| 调度 | Quartz 3.18 + `schedule_job` 表 | 沿用 |
| 告警配置 | `alarm_config` 已有完整防抖字段（IsDebounce/DebounceType/DebounceSeconds/DebounceMode/DebounceCount/DebounceAction/AlarmConfirmSeconds）；DeviceAlarmConfig/DeviceTypeAlarmConfig 公式配置 | **字段设计沿用，补运行时引擎**（§9） |
| 表达式 | DynamicExpresso（`ExpressoFormula`：告警公式判定、点位修正公式） | 沿用（告警/修正公式），JS 协议解析另用 Jint（§6.3） |
| 日志 | Serilog（IotLog 封装，文件/控制台/Loki） | 沿用 |
| 缓存 | Redis（Tendis）实体缓存 | 沿用 |

### 2.2 关键缺口（本方案要补的洞）

1. **数据上行链路断裂（最严重）**：`PluginEvent` 无消费者（`EventBusSetup.cs` 中处理器已被移除）、`MqttClientJob.MessageReceivedHandler` 虽已订阅但核心解析逻辑被注释——采集数据不入库、不触发告警。→ §8.1 数据管道重建。
2. **告警防抖/屏蔽只有配置字段无引擎**：全仓无 Debounce 运行时实现。→ §9。
3. **无平台级采集频率/推送间隔配置**：仅插件本地 config 文件（如国祥 VRF 的 CollectSleepSecond=60s），无法按设备/点位在库中配置下发。→ §7。
4. **无产品/物模型抽象**：DeviceTypeParam 只是点表，无 TSL 三元组、无设备影子。→ §5。
5. **无时序存储设计**：EventHistory 等按日分表但无保留策略自动化、无批量写入通道。→ §8。
6. `JobInitializer` 未接线（Program.cs 注释掉）、Control 区无控制器、MqttFwd/MqttClient 两套 HostedService 死代码。→ Phase 0 清理。
7. ~~无 SignalR/WebSocket 实时推送~~ 基础 Hub 已补全（`Services/SignalR/ChatServer`），待扩展设备/告警分组订阅。→ §11.3。
8. 安全隐患：appsettings 明文 RSA 私钥、DbSetting.cs 硬编码密码。→ Phase 0 整改。
9. 无单元测试、无 CI。→ 路线图内逐步补齐。
10. 通知渠道单一（仅外部 EmailUrl）。→ §9.5。

---

## 3. 开源调研结论

### 3.1 GitHub star top7（star 数为 2026-07-06 GitHub REST API 实测值，已二次复核）

| # | 项目 | Stars | 语言 | 定位 | 对本方案的价值 |
|---|------|-------|------|------|----------------|
| 1 | home-assistant/core | 88.2k | Python | 家庭自动化平台 | 全链路防抖分层、alert 三态告警模型、分层保留策略（原始短保留+聚合长保留） |
| 2 | node-red/node-red | 23.4k | JS | 低代码流引擎 | **rbe/deadband 六模式死区**、delay 限流双策略、trigger 看门狗（离线检测） |
| 3 | thingsboard/thingsboard | 22k | Java | **物联网平台事实标准** | 告警去重四态模型、Duration/Repeating 告警条件、Schedule 时段屏蔽、Report Strategy 四种上报策略、ts_kv/ts_kv_latest 分表 |
| 4 | emqx/emqx | 16.5k | Erlang | MQTT 消息基础设施 | 高低水位滞回告警、flapping 抖动封禁、Connector/Action 转发框架、规则试运行 |
| 5 | Koenkk/zigbee2mqtt | 15.3k | TS | Zigbee→MQTT 网关 | **min/max_interval+reportable_change 防抖三件套**、debounce_ignore、可用性监测（指数退避 ping） |
| 6 | eclipse-mosquitto/mosquitto | 11k | C | 轻量 MQTT Broker | 双层流控配额、Decorrelated Jitter 退避重连、LWT+Will Delay 离线防抖 |
| 7 | kubeedge/kubeedge | 7.5k | Go | K8s 边缘底座 | **collectCycle/reportCycle 双周期解耦**、DeviceModel/Device 两级建模+JSONB 协议配置 |

补充参考（.NET / 国产）：jetlinks-community 6.5k（Java 中台标杆）、ThingsGateway 1.4k（.NET，**变量报警延时状态机最完整参考**）、IoTSharp 1.4k（.NET 平台级实现）、iioter/iotgateway 1.1k（.NET 网关）、FastBee 2.2k、ThingsPanel 等。

### 3.2 关键借鉴决策（已消化进各章节）

| 借鉴点 | 来源 | 落点 |
|--------|------|------|
| 告警规则挂"产品"而非单设备 + Dynamic 阈值 | ThingsBoard Device Profile | §5 产品层承载默认告警/采集策略 |
| 告警去重：originator+type 唯一活动告警，重复只更新 | ThingsBoard / IoTSharp | §9.2 |
| 告警条件三型 Simple/Duration(持续)/Repeating(重复次数) | ThingsBoard | §9.3（与现有 DebounceType 字段一一对应） |
| 报警延时双向状态机（触发/恢复都要求持续超时） | ThingsGateway AlarmTask | §9.3 |
| 三级异常值过滤（范围/幅度/连续容错） | ThingsGateway Variable | §7.2 |
| 上报策略 ON_CHANGE / ON_REPORT_PERIOD / 组合 | ThingsBoard Gateway Report Strategy | §7.3 |
| 死区六模式（deadband 绝对/百分比、narrowband 剔毛刺） | Node-RED rbe 节点 | §7.3 |
| debounce_ignore：关键属性变化立即冲刷不合并 | Zigbee2MQTT | §7.3 |
| 双周期：采集频率与上报频率解耦 | KubeEdge collectCycle/reportCycle | §7.1 |
| 内存批量队列聚合写库 | ThingsBoard TbSqlBlockingQueue | §8.1 |
| 最新值旁路表 + 历史表分离 | ThingsBoard ts_kv_latest / IoTSharp TelemetryLatest | §8.2 |
| 时序保留=整块 DROP 而非 DELETE | EMQX generation / TimescaleDB drop_chunks | §8.3 |
| MQTT 认证三级缓存防设备风暴 | IoTSharp MQTTService | §6.5 |
| 内存队列→SQLite 落盘→分批重传（断线续传） | ThingsGateway BusinessBaseWithCache | §10.2 |
| Jint 沙箱四重限制（内存/超时/语句数/取消） | IoTSharp Interpreter + Jint 调研 | §6.3 |
| 驱动 `[ConfigParameter]` 自描述生成配置 UI | iotgateway PluginInterface | §6.1 |
| 避坑：BPMN 自研规则链（性能黑洞） | IoTSharp FlowRuleEngine | §10.1 用轻量责任链替代 |
| 避坑：无保留策略导致表无限膨胀 | IoTSharp | §8.3 强制 retention |
| 避坑：进程外 gRPC 解析被弃用的教训 | EMQX ExProto | §6.3 选内嵌沙箱脚本 |

---

## 4. 总体架构

### 4.1 架构图

```
┌────────────────────────────── 设备侧 ──────────────────────────────┐
│  电表(DL/T645) 水表(CJ/T188) PLC(Modbus RTU/TCP) 私有协议设备       │
│        │RS485        │RS485        │RS485/以太网      │            │
│   ┌────┴─────────────┴────┐   ┌────┴────┐      ┌─────┴─────┐      │
│   │  DTU / 串口服务器透传   │   │ Modbus  │      │ MQTT 直连  │      │
│   └──────────┬────────────┘   │ TCP     │      │ 设备/网关  │      │
└──────────────┼────────────────┴────┬────┴───────────┬─────────────┘
               │ TCP(注册包+心跳)     │                │
┌──────────────▼────────────────────▼────────────────▼─────────────┐
│                     接入层（IotWebApi 宿主 / 独立采集进程）          │
│  TcpAccessServer(总线会话) │ 驱动插件宿主(ALC热插拔) │ MQTTnet Broker │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ 协议驱动框架 IDeviceDriver：Modbus │ DLT645 │ CJT188 │ JS脚本驱动 │ │
│  │ （Jint 沙箱：splitFrames / decode / encode）                    │ │
│  └───────────────────────────┬──────────────────────────────────┘ │
│         采集调度器（双周期 collectCycle/reportCycle + 异常值过滤）    │
└───────────────────────────────┬───────────────────────────────────┘
                                │ 标准化数据点 DataPoint{deviceId,key,value,ts,quality}
┌───────────────────────────────▼───────────────────────────────────┐
│                  数据管道（CenboEventBus + Channel<T>）              │
│   推送策略引擎(变化上报/死区/节流/强制周期)  →  分发到订阅者            │
│   ├─→ 批量写入器(Npgsql Binary COPY, 1~5s/批) → PostgreSQL 遥测表    │
│   ├─→ 最新值缓存(内存+Redis) → telemetry_latest                     │
│   ├─→ 告警引擎(公式判定→防抖状态机→屏蔽过滤→入库→通知)                │
│   ├─→ 规则联动引擎(条件+动作责任链)                                  │
│   ├─→ SignalR Hub(前端实时推送, 按设备分组)                          │
│   └─→ 北向转发器(MQTT/HTTP/Kafka, 带断线续传缓存)                    │
└───────────────────────────────┬───────────────────────────────────┘
┌───────────────────────────────▼───────────────────────────────────┐
│  PostgreSQL 16/17 + TimescaleDB                                    │
│  业务库(设备/产品/告警配置/RBAC, 普通表) + 时序区(遥测/日志, 30天滚动)  │
└───────────────────────────────┬───────────────────────────────────┘
                        vue-pure-admin 前端（REST + SignalR）
```

### 4.2 技术选型确认

| 层 | 选型 | 理由 |
|----|------|------|
| 后端框架 | ASP.NET Core（.NET 10） | 现状 |
| ORM | SqlSugar（业务 CRUD）+ **Npgsql Binary COPY（遥测写入通道）** | SqlSugar 一级支持 PG；大流量写入绕过 ORM（逐行 INSERT 仅 ~1K 行/秒，COPY 可达 ~316K 行/秒） |
| 数据库 | PostgreSQL 16/17 + TimescaleDB Community（自托管） | §8 专章论证；无法装扩展时降级原生分区+pg_partman |
| JS 引擎 | **Jint 4.x** | 纯 .NET 零原生依赖；每次执行级内存/超时/语句数/取消四重沙箱内建；协议解析是"短脚本高频调用+字节数组交互"负载，正中 Jint 强项（V8 封送开销反而更慢，且超限会杀进程） |
| Modbus 库 | **NewLife.Modbus**（主）+ FluentModbus（测试台/模拟器） | 项目已用 NewLife.Net 做 TCP 服务，传输层/日志一致；RTU/TCP/RTUOverTcp 全覆盖 |
| 表达式引擎 | DynamicExpresso（沿用，告警公式/点位修正） | 现状已封装 `ExpressoFormula`；注意预编译 `Parse()` 一次复用，避免每次 Eval |
| MQTT | MQTTnet 内嵌 Broker（≤1 万连接）；预留 EMQX 外置选项 | 现状沿用；EMQX 单集群亿级连接，规模化时平台改做 EMQX 的后端消费者 |
| 实时推送 | SignalR（`@microsoft/signalr`） | .NET 原生，权限收口统一 |
| 调度 | Quartz（系统任务）+ 自研轻量采集调度器（毫秒级轮询，Quartz 不适合高频采集） | |
| 前端 | pure-admin-thin（i18n 分支）v7 | MIT、20.4k star、活跃；精简版起步按需拷组件 |

### 4.3 部署形态

- **单体起步**：接入层、管道、告警、API 同进程（现 IotWebApi），事件总线为进程内 CenboEventBus。
- **可拆分预留**：接入层（驱动宿主）与平台核心之间的契约是"标准化数据点 + 指令下行"两个消息类型；未来拆为独立采集网关进程时，把 CenboEventBus 的实现替换为 MQTT/Redis Stream 即可，业务代码不动。
- **借鉴 ThingsBoard 分层教训**：Modbus/645/188 等现场协议驱动**不要**塞进 Web 主进程的关键路径——驱动崩溃通过 ALC 隔离 + 独立线程域兜住，严重故障时可整体降级为独立进程。

---

## 5. 产品与物模型

### 5.1 建模（借鉴 KubeEdge 两级模型 + ThingsBoard Device Profile）

```
Product（产品/设备型号模板）
 ├─ 基本：product_code, name, category(电表/水表/空调/...), protocol_type(modbus/dlt645/cjt188/jsscript/mqtt)
 ├─ protocol_config JSONB     —— 协议级默认参数（波特率/超时/重试），新协议零表结构变更
 ├─ script_id                 —— 自定义协议时挂 JS 脚本
 ├─ 默认采集策略/推送策略/告警规则（设备可覆盖，见 §7/§9）
 └─ ThingPoint[]（物模型点表，升级现有 DeviceTypeParam）
      ├─ point_code, name, addr(协议地址), value_type(数值/状态/字符串)
      ├─ rw(R/W/RW), unit, decimal_digit, status_value(枚举映射JSON)
      ├─ formula(修正公式 a*0.1, DynamicExpresso)
      ├─ 三级异常值过滤字段（与 §7.2 一一对应）：
      │    range_filter_enable + min_value / max_value（合理范围）
      │    amplitude_filter_enable + max_amplitude / max_amplitude_percent（跳变幅度, 升级现有 param_change_value）
      │    continuous_filter_enable + max_continuous_count（连续异常容错, 默认 3）
      ├─ 采集/推送策略不在点表冗余：经 collect_strategy/push_strategy 表以 scope=点位 挂靠覆盖（§7）
      └─ is_report, is_peak（沿用现有字段）

DeviceInfo（沿用现有表）
 ├─ 新增：product_id, session_key(注册包标识/MQTT ClientId), protocol_override JSONB
 └─ 现有 device_gateway/ip/port/com/adr/is_collection/device_state 全部沿用
```

**要点**：
- 现有 `DeviceType`/`DeviceTypeParam` 平滑升级为 `Product`/`ThingPoint`（可保留表名，仅加列），避免数据迁移断层。
- 属性(遥测) / 事件(告警) / 服务(指令) 三元组中，指令面单独建 `ProductCommand` 表（name、params JSON schema、下行编码方式），**命令白名单**（借鉴 KubeEdge DeviceMethod：只能调用声明过的命令，天然审计边界）。
- 设备影子：`telemetry_latest`（§8.2）+ `device_desired`（期望值，下发未确认时暂存），支持离线设备上线后补下发。

---

## 6. 协议接入层

### 6.1 驱动插件框架（现有 ICenBoPlugin 的演进）

现有 `ICenBoPlugin` 已具备初始化/启停/双向消息语义（PluginInit/PluginStart/PluginStop/SendMessageAsync/ReceiveMessageAsync），但缺少设备/点位粒度契约。新增 `IDeviceDriver` 层（借鉴 iotgateway `IDriver` + ThingsGateway CollectBase）：

```csharp
public interface IDeviceDriver : IDisposable
{
    // 元数据自描述：[DriverInfo("Modbus主站","1.0")]，连接参数用 [ConfigParameter("IP地址")] 标注
    // 平台反射生成前端配置表单，加驱动零 UI 代码（借鉴 iotgateway）
    Task<bool> ConnectAsync(CancellationToken ct);
    bool IsConnected { get; }
    Task CloseAsync();

    // 批量读：框架把点表按地址连续性自动合包后调用（借鉴 ThingsGateway LoadSourceRead）
    Task<DriverReadResult> ReadBatchAsync(PointBatch batch, CancellationToken ct);
    // 下行写/服务调用（写优先：写入时暂停本链路轮询，借鉴 iotgateway ManualResetEventSlim）
    Task<DriverWriteResult> WriteAsync(DeviceCommand cmd, CancellationToken ct);
}
```

- **以 IotPlugin.Air.VRF.GuoXiang 为参照模板（已确认）**：该插件已验证 NetServer TCP 接入、指令队列、超时重发、采集轮询、心跳的完整闭环，新驱动沿用其工程形态（独立 csproj、ICenBoPlugin 入口、独立配置类、sys_plugin 管理）。但需一次**框架化改造**：把 GuoXiang 1773 行中"连接管理/采集循环/超时重发/会话表"等与协议无关的部分上提为驱动框架基类（本节契约），协议插件只保留编解码与点位映射——否则 645/188/Modbus/OPC/S7 每个插件都要重复实现一遍调度逻辑。
- **加载**：沿用现有 `PluginService` 的 collectible AssemblyLoadContext + `sys_plugin` 表 + `SysPluginJob` 热更新链路（这套已是正确设计，iotgateway 的 `Assembly.LoadFrom` 反而不可卸载）。
- **通道模型**：新增 `Channel`（通信链路）概念——串口/TCP 连接参数挂通道，设备挂通道下（借鉴 ThingsGateway Channel-Device-Variable）。**同一 RS-485 总线严格一问一答**：每通道一个串行化工作队列。
- **断线自愈**：连续失败超时（默认 60s）强制 Close+重连；重连用 **Decorrelated Jitter 退避**（base/cap/stable 三参数，借鉴 Mosquitto bridge），防大面积断电恢复后的重连风暴。

### 6.2 内置驱动一：Modbus RTU/TCP

- 库：NewLife.Modbus（RTU/TCP/RTUOverTcp）。
- **自动合包**：按 从站号→寄存器区(FC01/02/03/04)→地址排序，合并连续/近邻地址为 ≤125 寄存器（0x7D）的区段；空洞容忍阈值可配（8~16 寄存器，读回丢弃间隙比多发一帧便宜），提供"禁止跨洞"开关（部分设备对未定义地址整段回异常）。**不学 iotgateway 的人工配置 ReadMultiple/ReadFromCache（易配错）**。
- 点表字段：从站号、功能码、地址、数据类型（int16/32/64、uint、float32/64、BCD、string）、**字节序四选一（ABCD/CDAB/BADC/DCBA，必须点表可配）**、位偏移。
- 并发：TCP 每连接在飞请求 1~4；RTU over TCP 网关后是共享总线，回落到按网关串行。超时按波特率配置（2400bps 下 ≥500ms）+ 重试次数可配。

### 6.3 内置驱动二/三：DL/T 645-2007 与 CJ/T 188

两协议共享同一状态机骨架（**帧定界器 + 字段布局描述** 两级抽象）：

| 项 | DL/T 645-2007 | CJ/T 188-2004/2018 |
|----|---------------|--------------------|
| 帧结构 | `FE×n, 68, 地址[6], 68, C, L, DATA(+33H), CS, 16` | `68, T(表型), 地址[7], C, L, DI[2], SER, DATA, CS, 16` |
| 地址 | 6 字节 BCD **低位在前**，99×6 广播，AA 缩位通配 | 7 字节 BCD 低位在前（含厂商代码，各厂编排有差异） |
| 数据域 | **每字节 +33H 偏移**（收后 -33H） | 明文 BCD，无偏移 |
| 数据标识 | 4 字节 DI（00010000=正向有功总电能；02 01 xx 00 电压等） | 2 字节 DI（901F=读计量数据）+ 1 字节序列号 SER |
| 校验 | 从第一个 68 起模 256 和（不含前导 FE） | 从 68 起模 256 和 |
| 应答 | C\|0x80 正常，C\|0xC0 异常（ERR 字节） | C\|0x80 |
| 物理层 | RS-485 默认 2400bps **8E1**；解析器跳过任意个前导 FE | 同 2400bps 8E1 |
| 兼容 | **必须兼容 1997 版**（2 字节 DI、控制码 01/81 体系），按产品配置版本 | 2018 版帧骨架兼容 2004，DI 表做成可配置 |

实现要点：
- DI→点位映射**做成数据库点表配置，勿硬编码**（厂家扩展 DI 普遍存在）；平台预置国标常用 DI 模板（电能量/电压/电流/功率/功率因数/频率/费率），产品创建时一键导入。
- 状态机防伪起始符：CS 校验失败从"上次 68 后一字节"重新扫描。
- 读表任务由采集调度器驱动：一表一 DI 一问一答，总线串行；广播校时（C=08H，无应答）做成平台定时任务。
- 645 帧内 6 字节表地址 / 188 帧内 7 字节地址即**子设备路由键**——一条 DTU 链路挂多表时按帧内地址分发（§6.6）。
- 阀控（188 写数据 C=04H, DI=A017H）走命令白名单 + 二次确认。

### 6.4 JS 自定义协议解析引擎（Jint）

**定位（已确认：插件优先，脚本兜底）**：DL/T 645、CJ/T 188、Modbus、OPC UA、S7 等常见协议一律以 C# 驱动插件完整实现（§6.1~§6.3、§6.7）；JS 脚本引擎仅作为**长尾私有/杂牌协议的兜底手段**（临时接一款小众设备、不值得发插件版本时使用），实施优先级后置（§13 P4 调整为可选）。参考 ThingsBoard converter / ChirpStack codec，结合 TCP 透传场景补"拆帧"层，**三段式 API**：

```javascript
// ① 帧定界（粘包/拆包）——优先用平台声明式定界器（定长/分隔符/长度域/起止符68..16/静默超时），
//    脚本 splitFrames 仅兜底，减少脚本 bug 面
function splitFrames(buffer, context) {
  return { frames: [[start, end)], consumed: n };  // consumed 之前的字节从环形缓冲移除
}

// ② 上行解码——一帧一调用，frame 为 Uint8Array
function decode(frame, context) {
  // context: { deviceKey, sessionId, regPacket, variables, now, lastState(≤4KB可写) }
  return {
    deviceKey: "...",              // 可选覆盖：一链路多表时按帧内地址路由
    telemetry: [{ key:"Ua", value:230.1, ts:..., quality:0 }],  // 数组支持一帧多时标(645冻结/曲线)
    attributes: { fw:"1.2" },
    events: [],
    reply: [0x06]                  // ACK 类协议需要立即回设备的字节
  };  // 返回 null = 丢弃该帧
}

// ③ 下行编码——平台命令对象 → 字节
function encode(command, context) {
  return { bytes:[...], expectReply:true, timeoutMs:1500, matcher:{ type:"addr+di" } };
  // 一问一答由平台完成"发送→matcher 命中→超时重试"，脚本不做 IO
}
```

**沙箱与工程化**（Jint 内建能力）：
- 每次调用：`TimeoutInterval` 100~500ms、`LimitMemory` 8~32MB、`MaxStatements` 上限、`CancellationToken`；CLR 互操作白名单。
- `Engine` 非线程安全：按脚本版本 `Engine.PrepareScript()` 预编译共享 AST + 每工作线程/对象池持有 Engine 实例；脚本升级用版本号热切换。
- **调试设计**（解决"字符串脚本无法调试"痛点，借鉴 ThingsGateway ScriptDebug + EMQX /rule_test）：
  - 前端 monaco-editor 编辑 + **试运行接口**：输入 hex 帧 + 模拟 context → 返回 decode 结果 + console 日志 + 耗时 + 语句数（干跑不产生副作用）；
  - 在线设备 Debug 开关：抓最近 N 条入站原始 hex/出站数据点/异常栈，**限时自动关闭**防刷盘；
  - 脚本表带版本历史 + diff 对比（monaco diff editor）。
- **脚本异常降级策略**：decode/encode 抛异常、超时或返回非法结构时——丢弃该帧，原始帧与异常栈记入 raw_frame_log（打 script_error 标记）；按设备累计脚本错误计数，滑动窗口超阈值（如 5 分钟 100 次）自动禁用该脚本实例并产生平台级告警，支持一键回滚至上一版本脚本。
- 安全教训（Zigbee2MQTT 2.11 起默认禁用远程注入代码）：脚本功能默认关闭，需管理员按钮级权限开启。

### 6.5 MQTT 接入

- 沿用 MQTTnet 内嵌 Broker；topic 约定 `devices/{deviceKey}/telemetry|attributes|event`，JSON payload 直接进管道；非 JSON payload 挂产品的 JS 脚本解码。
- **认证三级缓存**（照搬 IoTSharp 成熟做法）：SHA256 结果缓存（成功 5min/失败 30s）+ 内存倒排索引 + DB 查询 SemaphoreSlim 限流——防设备重连风暴打穿数据库。
- **连接抖动封禁**（借鉴 EMQX flapping_detect）：时间窗（1min）内断连超 N 次（15）自动封禁 ClientId 一段时间（5min）。
- 设备在线状态以 **LWT + 延迟离线判定**为权威信号（Will Delay 思想：短暂重连不触发离线事件），不轮询。

### 6.6 TCP/DTU 透传接入

- `TcpAccessServer`（NewLife NetServer，现有国祥插件已验证此路线）监听端口，连接后进入"未认证"状态，**限时 30s 等注册包**；注册包格式按通道配置（前缀魔数/定长/正则/JS `identify(buffer)`）解析出设备标识 → 绑定会话。注册包可能与首帧业务数据粘连——识别后剩余字节回灌拆帧缓冲。
- **总线会话模型**：一个 DTU 下挂一条 485 总线多块表——会话持有该链路的串行化命令队列，帧内地址（645/188/Modbus 从站号）再路由到子设备。
- 判活：DTU 心跳包 + TCP KeepAlive + 应用层空闲超时（心跳周期×3）踢半开连接；**同一设备新连接到来时替换旧会话**（DTU 掉线重连常残留旧连接）。
- 无注册包兜底：645 单表直连主动下发 13H 读通信地址探测；Modbus 轮询标识寄存器；或端口=通道静态映射。

### 6.7 内置驱动四：OPC UA / 西门子 S7（扩展清单）

按同一驱动契约以 C# 插件实现（参考 iotgateway 的 OPC.UaClient / PLC.SiemensS7 与 ThingsGateway 对应插件）：

- **OPC UA**：OPCFoundation.NetStandard.Opc.Ua 官方库；匿名/用户名/证书三种认证；NodeId 点表映射；**订阅模式**（Subscription+MonitoredItem，服务端推送天然自带变化上报语义）与批量读双模式。
- **S7**：S7netplus（支持 S7-200Smart/300/400/1200/1500）；DB 块/M/I/Q 区地址点表；批量读按地址连续性合包（同 §6.2 Modbus 策略）。
- 两者均为"平台直连型"驱动（不经 DTU），挂 Channel(TCP) 下复用框架的重连退避与采集调度。

---

## 7. 采集与防抖动设计（核心）

防抖动不是一个开关，而是**采集侧 → 推送侧 → 告警侧**三层各自的机制。配置粒度统一为：**产品默认 → 设备覆盖 → 点位覆盖**（借鉴 ThingsBoard Report Strategy 的分层覆盖模型）。**实际运行以产品级为主**：绝大多数设备直接继承产品策略，设备/点位覆盖仅作个别例外——UI 上产品策略是主配置入口，设备/点位覆盖以"例外清单"呈现（可一键清除、回落到产品级）。

**落库形态（统一收敛）**：三级覆盖统一存储于独立策略表 `collect_strategy` / `push_strategy`（§12），两表均含 `scope_type`(1=产品/2=设备/3=点位) + `scope_id` 两列实现挂靠，运行时按 **点位 > 设备 > 产品** 优先级逐字段合并生效；点表（ThingPoint）不冗余策略列。§7.1 / §7.3 的字段清单即这两张表的表体字段。

### 7.1 第一层：采集频率（双周期解耦，借鉴 KubeEdge）

| 字段 | 层级 | 说明 |
|------|------|------|
| `collect_cycle_ms` | 产品/设备/点位 | 从物理设备**采集**的周期（Modbus 轮询/645 读表间隔）。高频点(电流)与低频点(电能量)可不同周期——点位级覆盖设备级 |
| `collect_cron` | 设备 | 低频场景支持 cron（如水表每小时抄一次），毫秒/cron 双模调度（借鉴 ThingsGateway） |
| `cmd_interval_ms` | 通道 | 同总线相邻两次请求的最小间隔（RS-485 从站喘息时间） |
| `report_cycle_ms` | 点位 | 向平台管道**上报**的最大周期（与采集解耦：采集可快、上报可慢） |

调度实现：每通道一个采集循环（长任务 + CancellationToken），点位按周期分组打包；同周期点位合包读取（§6.2）。**不用 Quartz 跑毫秒级采集**（InMemoryStore 并发 30 上限，且粒度是秒级）。

### 7.2 第二层：采集侧异常值过滤（照搬 ThingsGateway 三级过滤链，字段直接可抄）

仅在设备在线且值变化时按序执行：

1. **合理范围**：`range_filter_enable` + `min_value/max_value`——越界丢弃（quality 标记 Bad）；
2. **变化幅度**：`amplitude_filter_enable` + `max_amplitude`（绝对差）+ `max_amplitude_percent`（百分比）——单次跳变超限视为毛刺丢弃（升级现有 `param_change_value` 字段）；
3. **连续异常容错**：`continuous_filter_enable` + `max_continuous_count`（默认 3）——连续 N 次"异常"则认定为真实阶跃，**接受该值**（防止真实突变被永久过滤，这是最容易漏的细节）。

补充 Node-RED narrowband 思想：过滤是双向的——deadband 拦"变化太小"，幅度过滤拦"变化太大（毛刺）"。

### 7.3 第三层：数据推送间隔（推送策略引擎，管道入口统一执行）

**推送策略字段组**（`push_strategy`，产品默认/设备/点位三级覆盖）：

| 字段 | 取值 | 语义 |
|------|------|------|
| `report_mode` | 1=收到即报 / 2=变化上报 / 3=定时上报 / **4=变化上报+最大静默周期兜底（默认）** | 照搬 ThingsBoard ON_RECEIVED / ON_CHANGE / ON_REPORT_PERIOD / ON_CHANGE_OR_REPORT_PERIOD 四策略 |
| `deadband_type` | 0=严格不等 / 1=绝对死区 / 2=百分比死区 | "变化"的判定标准（TB 只有严格不等，死区是我们的差异化增强，语义抄 Node-RED rbe：百分比按前值动态算 gap） |
| `deadband_value` | 数值 | 绝对值或百分比 |
| `min_push_interval_ms` | 节流窗口 | 窗口内多次变化只推最新一条（**丢弃中间值保最新**，遥测语义；借鉴 Node-RED delay drop-intermediate） |
| `max_silent_ms` | 强制上报周期 | 即使值不变，超过此时长也推一条（心跳/曲线连续性兜底） |
| `debounce_ignore_keys` | 点位列表 | **关键属性（开关量/故障码/事件）变化立即冲刷，不参与合并节流**——解决"防抖与事件不丢"的矛盾（照搬 Zigbee2MQTT debounce_ignore） |

**一致性关键细节**（照搬 Zigbee2MQTT）：节流期间**最新值缓存永远立即更新**，只有对外发布（入库/转发/SignalR）被延迟——任何读方（API 查实时值）看到的都是最新值。

**典型运行模式（默认配置样例）**：推送间隔 15 分钟（report_mode=3 定时上报），期间采集照常多轮进行——每轮采集只刷新最新值缓存（内存 + telemetry_latest），到达推送时刻才批量对外发布/转发。

**数据流与事件流分离（硬规则）**：推送间隔只约束"遥测数据流"；**告警事件、设备上下线事件走独立事件通道，判定成立即刻推送**，不受 15 分钟节流影响。告警判定发生在平台侧（告警引擎基于最新值缓存评估公式，§9），**协议插件不需要为"告警立即推送"解析任何额外的协议参数**——设备自带的告警状态位按普通点位采集即可（§9.6）。

数据点携带 `CollectTime`（每次采集都更新）与 `ChangeTime`（值变化才更新）两个时间戳（借鉴 ThingsGateway），供离线判定与曲线绘制区分语义。

### 7.4 防抖动配置下发链路

前端策略配置页（§11）写库 → 发布 `StrategyChangedEvent` → 采集调度器/推送策略引擎热重载对应设备的运行时参数（无需重启插件）。

### 7.5 设备上下线事件：单独推送 + 判定级防抖

上下线属于事件流（§7.3），**推送不节流、判定要防抖**——防抖用在"要不要认定离线"上，而不是"推送要不要延迟"上：

- **离线判定防抖**：心跳/采集超时后进入"疑似离线"中间态，持续超过确认时长（复用 AlarmConfirmSeconds 语义，默认=心跳周期×3）才正式判离线；期间恢复则静默取消——短暂网络抖动不产生任何事件（Will Delay/看门狗语义）。
- **上线判定即时生效**（连接建立+首帧数据即在线），但同一设备的"上线通知"做最小间隔限制（如 60s），配合 §6.5 flapping 封禁，防止抖动设备上下线刷屏。
- **离线原因必填**：事件携带 reason 枚举——1=网络连接断开（TCP 断开/MQTT LWT）、2=无数据采集（链路在但设备超时无应答）、3=网关/DTU 掉线（其下子设备批量离线合并为一条网关事件+子设备清单，不逐台风暴推送）、4=插件停止/平台禁用、5=心跳超时。device_state 沿用现有 2在线/1掉电/0离线。
- 事件写 device_event_log，并即刻走 SignalR + 北向推送；离线告警与恢复由告警引擎按时长型防抖处理（§9.6），与上下线事件**共用同一判定结果**，不重复判定。

---

## 8. 数据管道与存储（PostgreSQL 可行性专章）

### 8.1 数据管道（修复现有断裂链路）

```
驱动/脚本 decode → DataPoint 标准化 → 推送策略引擎(§7.3)
   → System.Threading.Channels.Channel<DataPoint>（有界，背压）
       ├─ 批量写入器：攒批 5000 行或 2s（先到为准）→ Npgsql BeginBinaryImport COPY → telemetry
       ├─ 最新值更新器：内存 ConcurrentDictionary → 异步刷 telemetry_latest + Redis
       ├─ 告警引擎（§9）
       ├─ SignalR 广播（按设备分组，前端节流渲染）
       └─ 北向转发（§10.2，独立有界队列，互不拖累）
```

- 重建 `PluginEvent`/`MqttClientJob` 的消费者：统一收敛到一个 `DataPointIngestService`（IHostedService），替换现有被注释/移除的处理器。
- **严禁逐行 INSERT**（实测仅 ~1K 行/秒）；COPY 批量通道单机 ~316K 行/秒（8 核参考值）。
- 队列有界 + 双维度配额思想（条数+字节，借鉴 Mosquitto）：遥测队列满丢最旧（可丢），指令/告警队列满拒绝并告警（必达）。

### 8.2 PostgreSQL 可行性结论（专题调研，来源见附录 A）

**结论：可行，且是该场景的优选。**

- **写入水位**：以 5 万测点 × 5 秒采集 = **1 万行/秒**为参考，仅为 TimescaleDB 单机持续写入能力（111K~1M 行/秒，官方 10 亿行基准全程稳定 ~111K 行/秒）的 **1%~10%**；原生 PG + COPY 也只用到 ~3%。瓶颈不在吞吐而在数据管理。
- **数据量**：1 万行/秒 = 8.64 亿行/天 ≈ **259 亿行/30 天**——单表必死（原生 PG 单表写到 10 亿行衰减到 ~5K 行/秒），**必须分区/分块**；而"保留 1 个月"恰好被"按天分块 + 整块 DROP"完美解决（drop_chunks 是删物理文件的元数据操作，无死元组无 vacuum 风暴，比 DELETE 快几个数量级）。
- **容量估算公式**：

```
每秒行数 R = 测点数 ÷ 采集周期(秒)
30天总行数 N = R × 86400 × 30
原始存储 ≈ N × 行宽(~60-80B) × (1+索引系数0.2~0.4)
压缩后 ≈ 原始 ÷ (8~20)          ← TimescaleDB 列存压缩
建议磁盘 = 压缩后 × 2
代入：R=10,000 → N≈259亿行 → 原始≈1.8~2.5TB → 压缩后约 150~300GB，单机 NVMe 轻松容纳
```

- **量级拐点**：持续写入 >10 万行/秒、测点基数 >百万、或保留期拉到年级并需全量高速分析时，才值得换 TDengine/IoTDB/ClickHouse（XCode/SqlSugar 均支持 TDengine，留有后路）。当前场景专用时序库属于过度设计。

### 8.3 存储方案（主推方案 A，降级方案 B）

**方案 A：PostgreSQL 16/17 + TimescaleDB Community（TSL 许可，自托管免费合规；避开 PG 17.1，用 17.2+）**

```sql
-- 遥测窄表（窄表优先：测点动态增减不需 DDL；列存压缩对窄表 value 列效果最好）
CREATE TABLE telemetry (
  device_id  bigint      NOT NULL,
  point_id   int         NOT NULL,
  ts         timestamptz NOT NULL,
  value      float8,
  value_str  text,           -- 状态/字符串型点位
  quality    smallint    NOT NULL DEFAULT 0
);
SELECT create_hypertable('telemetry','ts', chunk_time_interval => INTERVAL '1 day');
CREATE INDEX ix_telemetry_dpt ON telemetry (device_id, point_id, ts DESC);

ALTER TABLE telemetry SET (timescaledb.compress,
  timescaledb.compress_segmentby='device_id',
  timescaledb.compress_orderby='ts DESC');
SELECT add_compression_policy('telemetry', INTERVAL '7 days');   -- 7天后列存压缩(8~20x)
SELECT add_retention_policy('telemetry', INTERVAL '30 days');    -- 30天自动 drop_chunks

-- 连续聚合：原始数据30天丢弃，小时/日聚合长期保留（报表不受retention影响）
CREATE MATERIALIZED VIEW telemetry_1h WITH (timescaledb.continuous) AS
SELECT device_id, point_id, time_bucket('1 hour', ts) AS bucket,
       avg(value) AS avg_v, min(value) AS min_v, max(value) AS max_v,
       last(value, ts) AS last_v, count(*) AS cnt
FROM telemetry GROUP BY device_id, point_id, bucket;

SELECT add_continuous_aggregate_policy('telemetry_1h',
  start_offset      => INTERVAL '3 days',   -- 每次回看 3 天：远早于 30 天 retention，数据被 drop 前必已完成聚合
  end_offset        => INTERVAL '1 hour',   -- 最近 1 小时不刷新，避免与实时写入争抢
  schedule_interval => INTERVAL '1 hour');
-- 参数链硬约束：end_offset(1h) < start_offset(3d) < 压缩策略(7d) < retention(30d)
-- 即聚合刷新永远先于压缩与删除完成；调整任一参数须保持该次序，否则刷新窗口触及已删数据会丢聚合
```

- **最新值旁路表** `telemetry_latest(device_id, point_id, ts, value, value_str, quality)` 普通表 + Redis 缓存——实时面板 99% 查询只打它，不扫时序表（ThingsBoard/IoTSharp 共同验证的设计）。
- **雪花 ID（SnowId）与时序存储（已确认沿用）**：SnowId 高位是时间戳——作主键天然按时间近似有序（B-Tree 追加写友好），且可反解时间做分区/分表路由，现有 SqlSugar SplitTable 按雪花定位时间片即同一原理。**事件/告警/日志/业务表继续用 `SnowModel.Instance.NewId()` 主键完全满足要求**。唯一例外是高频遥测窄表 `telemetry`：建议不设单列代理主键（每多一个二级索引，写入吞吐降 20~40%），直接用 `(device_id, point_id, ts)` 复合寻址；若走方案 B 原生分区且坚持保留 SnowId 列，主键必须包含分区键（如 `(snow_id, ts)`）。
- **日志表同策略**：`device_event_log`（上下线/操作/指令审计）、`raw_frame_log`（原始报文，bytea + `COMPRESSION lz4`）、系统运行日志——全部按天 hypertable + 30 天 retention。现有 SqlSugar SplitTable 分表（EventHistory 按日、EventRun/EventSignal 按月）迁移到该体系。
- 业务表（设备/产品/告警配置/RBAC）：普通表，与时序表**同库不同 schema**——这正是"PG 一库两用"的价值，保留跨表 JOIN 与事务能力。

**方案 B（无法安装扩展时降级）**：原生声明式 RANGE 按天分区 + **pg_partman**（自动预建/DROP 旧分区，PG14+ 自带 background worker）+ ts 列 BRIN 索引（体积仅 B-Tree 的 ~0.1%）+ `fillfactor=100`。功能等价，代价是无列存压缩，存储按 ~2.5TB+ 规划（约为方案 A 的 8~20 倍）。

**报表长期数据**：沿用现有日/周/月报表体系，数据源改为 continuous aggregate（方案 A）或 Quartz 定时聚合任务（方案 B）——聚合表永久保留，明细 30 天滚动。

---

## 9. 告警引擎与告警屏蔽（AlarmConfig）

### 9.1 总体设计

现有 `alarm_config` 的防抖字段设计**完备且优于多数开源实现**（次数型/时长型/屏蔽三类 + 连续/累计模式 + 取值动作），缺的是运行时引擎。本章给出与现有字段一一对应的引擎设计，并补齐屏蔽窗口与通知升级。

**触发方式：事件驱动 + 定时兜底**（吸取 ThingsGateway AlarmTask 10ms 全量轮询空转的教训）：数据点进管道即触发所属设备的告警评估；另有低频兜底任务（10s）处理"无数据也要判定"的场景（离线、时长型确认到期）。

### 9.2 告警生命周期（借鉴 ThingsBoard，字段映射现有 EventAlarm）

- **去重键**：`(device_id, alarm_config_id)` 同时只允许一个活动告警——重复触发**原地更新**（刷新最后时间/次数/当前值），不新建记录；级别升级（如 H→HH）更新 severity 并重新通知，不另建。天然免告警刷屏。
- **四态**：`Active_UnAck / Active_Ack / Cleared_UnAck / Cleared_Ack`（激活×确认两维）。现有 EventAlarm 的 check_result/check_user/restore_time 字段对应 Ack/Clear 维度，平滑映射。
- **恢复也是事件**：告警恢复产生恢复通知（done_message 思想），并写 `alarm_time_range`（现有字段）。

### 9.3 告警防抖引擎（实现现有 AlarmConfig 字段）

数据点 → DynamicExpresso 公式判定（`DeviceAlarmConfig.JisuanFormula`，**预编译 Parse 一次复用**）→ 命中后进入防抖状态机：

```
                     ┌─ DebounceType=1 次数型 ─────────────────────────────┐
                     │ 窗口 DebounceSeconds 内计数：                        │
                     │  DebounceMode=1 连续：中断即清零（对应 TB Repeating）  │
                     │  DebounceMode=2 累计：窗口内累计满 DebounceCount 触发  │
                     │ 触发时按 DebounceAction 取第一次/最后一次的值与时间戳    │
 公式命中 ──IsDebounce?──┤                                                  │
       │否               ├─ DebounceType=2 时长型 ─────────────────────────┤
       ▼                 │ Normal → PrepareAlarm(记起始时间)                │
    立即告警              │   条件持续 ≥ AlarmConfirmSeconds → Alarm         │
                     │   期间任一次不满足 → 回 Normal（对应 TB Duration、    │
                     │   ThingsGateway AlarmDelay）                        │
                     │ 恢复方向同样走 PrepareFinish → 持续 → Finish          │
                     │ （双向延时：短脉冲既不产生告警也不产生误恢复）           │
                     └─ DebounceType=3 屏蔽 → 直接吞掉（见 §9.4 扩展）       ┘
```

- 状态机上下文存内存（ConcurrentDictionary） + 定期快照 Redis，重启不丢 PrepareAlarm 中间态。
- **高低水位滞回**（借鉴 EMQX，增强项）：数值型告警支持触发阈值/恢复阈值成对配置（80% 触发 / 60% 恢复），解决临界值震荡——在 DeviceAlarmConfig 增加 `recover_formula`（恢复公式，空则用触发公式取反）。
- **约束表达式**（借鉴 ThingsGateway RestrainExpressions）：告警命中后再评估 `restrain_formula`（可引用同设备其他点位，如"设备运行中才允许低温告警"），false 则抑制——联锁抑制。
- 离线告警：`Expand_DeviceType.OfflineMinute`（现有）+ 时长型确认（AlarmConfirmSeconds 的典型用例），看门狗语义：每条心跳重置计时，超时才发离线告警（Node-RED trigger 模式）。

### 9.4 告警屏蔽（AlarmConfig 扩展 —— 用户点名需求）

现有 DebounceType=3 只是"一刀切屏蔽"，将其标记为 **deprecated**：引擎按"等价于一条 scope=该告警类型、mask_mode=永久、mask_action=完全屏蔽 的 alarm_mask 规则"兼容执行旧配置，管理页提示迁移，新配置一律使用 alarm_mask。扩展为独立的**屏蔽规则表 `alarm_mask`**（运行时在"告警产生之后、入库通知之前"过滤，被屏蔽的告警仍记 `masked` 标记入库供审计，但不通知不推送）：

| 字段 | 说明 |
|------|------|
| `mask_scope_type` / `scope_id` | 屏蔽对象：全局 / 单位/建筑 / 设备类型 / 单设备 / 告警类型(alarm_config_id) / 告警级别≤N |
| `mask_mode` | 1=永久（维护模式/免打扰） 2=一次性时间段 3=周期性时间窗 |
| `time_ranges` | 周期窗：星期几 + 起止时刻（JSON，借鉴 ThingsBoard Schedule：如工作日 9:00-18:00 屏蔽）；一次性：起止时间戳 |
| `mask_action` | 1=完全屏蔽（不入库） 2=静默（入库打标不通知，**默认**） 3=降级（降为低级别） |
| `reason` / `operator` / `expire_at` | 屏蔽原因、操作人、自动失效时间（防止"忘了解除"——到期自动恢复并通知） |

配套机制：
- **维护模式**：设备/建筑一键进入维护，等价创建一条 scope 屏蔽规则（带 expire_at）；前端设备详情页显眼展示"维护中"角标。
- **Ack 即静默**（借鉴 Home Assistant alert）：活动告警被确认后，重复通知停止，但告警保持 Active 状态直至条件恢复。
- 屏蔽规则命中预览：前端保存规则前调用干跑接口，返回"当前将命中的活动告警清单"。

### 9.5 通知与升级

- 通知渠道抽象 `INotifyChannel`：邮件（现有 EmailUrl 外发保留）、Webhook、钉钉/企微机器人、短信（预留）。
- **升级链**（借鉴 ThingsBoard escalation + HA repeat 数组）：告警产生 → 立即通知第一梯队 → 未 Ack/未恢复按 `[15,30,60]` 分钟渐进重复/升级到下一梯队 → Ack 或恢复自动中断后续梯队。
- 通知模板沿用现有 `TextTemplate`（RazorEngine/字符串模板均可），带告警值/设备/时间占位符。

### 9.6 两类告警来源的收敛（内部处理边界）

- **离线告警与恢复（平台内部主场景）**：由 §7.5 的离线判定驱动，走时长型防抖（AlarmConfirmSeconds），产生/恢复都经 §9.2 生命周期与 §9.4 屏蔽过滤——即**内部告警同样执行防抖动与屏蔽规则**，且是这两套机制的第一个落地用例；数据推送侧防抖（§7.3）照常独立生效。
- **设备自报告警（协议解析出的告警状态位/事件帧）**：协议插件不做告警语义处理，只把告警位当**普通点位**解码上报；点表将该点位标记 `is_alarm_source=1` 并关联 AlarmConfig，由平台告警引擎统一裁决（去重/防抖/屏蔽/通知）。原则：**平台告警引擎是唯一裁决者，不在协议层另建第二套告警逻辑**——避免两处维护、两处屏蔽配置。

---

## 10. 规则联动与北向转发

### 10.1 规则联动（轻量，明确不做 BPMN）

吸取 IoTSharp FlowRuleEngine 教训（800+ 行 switch 解释器、每节点写审计入库、高频遥测下是性能黑洞），采用**扁平"触发-条件-动作"模型**（Home Assistant automation 形态）：

- 触发：点位变化 / 告警产生或恢复 / 定时 cron / 设备上下线；
- 条件：DynamicExpresso 表达式（可引用多设备点位最新值）+ 时间窗；
- 动作：下发设备命令（走命令白名单）、写虚拟点位、发通知、调用 Webhook；
- 现有 `DeviceStrategy` 策略雏形升级承载；执行审计只记一条汇总日志（不逐节点入库）。
- 工程化三件套（借鉴 EMQX）：规则**试运行接口**（干跑无副作用）+ 每规则漏斗指标（matched/passed/failed/action 成功失败计数）+ 规则内异常捕获隔离不影响数据主流程。

### 10.2 北向转发（第三方系统对接）

统一 **Connector（连接）/ Sink（目的地）** 抽象（借鉴 EMQX 数据集成 v2 + ThingsGateway Business 插件）：

- 目的地：MQTT（外部平台）、HTTP Webhook、Kafka（预留）、数据库直写（预留）；
- **三段式断线续传**（照搬 ThingsGateway）：内存 ConcurrentQueue（上限 10 万条）→ 失败/溢出落盘每目的地独立 SQLite 缓存文件（BulkCopy）→ 后台按批（2000 条）重传，成功才删；缓存文件上限（1GB）滚动删除；失败日志只在状态翻转时记录（防刷盘）；
- 转发内容策略：原始数据点 / 聚合后 / 仅告警——按 Sink 配置；推送节流复用 §7.3 策略引擎（转发面与存储面可用不同 push_strategy）。
- **推送目标配置（UI 必须傻瓜化，用户只需"填地址"）**：向导式表单——①选类型（MQTT / HTTP Webhook / Kafka 预留）→ ②填**推送地址**与认证（MQTT：broker 地址/端口/ClientId/账号 + 数据/事件 topic 模板；HTTP：URL/Method/Header/签名）→ ③选推送范围（全部/按产品/按设备勾选树）与推送间隔（复用 push_strategy，默认继承全局）→ ④**"测试连接"+"发送样例报文"按钮**（干跑并展示将发出的 JSON 预览）→ ⑤保存后可查看最近 N 条推送日志与失败重传队列水位。

---

## 11. 前端方案（vue-pure-admin）

### 11.1 底座与对接

- 起步：**pure-admin-thin（i18n 分支）**，v7 系（Vue 3.5 / Vite / TS / Element-Plus / Pinia / Tailwind，MIT）；需要的 demo（图表/可编辑表格/monaco）从完整版按目录拷贝。
- 后端契约（.NET 侧按此实现，可零改动接入）：
  - `/login` 返回 `{success, data:{avatar, username, nickname, roles[], permissions[], accessToken, refreshToken, expires}}`；
  - `/refresh-token` 静默刷新（前端已内建并发排队重放）；
  - `/get-async-routes` 返回动态菜单树（`component` 为 `src/views/` 相对路径；按钮权限填 `meta.auths`）——现有 Sysmenu/Sysbutton/SysroleMenuBtn 表结构可直接生成该树；
  - 统一响应包 `{success, data}`。

### 11.2 页面规划

| 路由 | 页面 | 要点 |
|------|------|------|
| `/dashboard` | 总览大屏 | 设备在线率环图、消息量折线、告警 TOP；echarts6 + `useECharts`（暗色联动）；SignalR 推送+定时兜底 |
| `/product` | 产品管理 | 产品 CRUD、物模型点表（vxe-table 行内编辑）、DI 模板一键导入（645/188 国标预置） |
| `/device` | 设备管理 | pure-table 列表 + 左侧建筑/分组树；设备详情 tabs：基本信息/实时数据（SignalR，值变化高亮）/历史曲线（echarts `dataZoom`+`sampling:'lttb'`，聚合粒度切换）/事件日志/指令下发（命令白名单表单） |
| `/protocol` | 协议与脚本 | 通道管理、驱动插件管理（上传/启停/热更新，复用 sys_plugin）、**JS 脚本编辑调试**：monaco-editor（worker 显式导入集成），左编辑右试运行（hex 输入→decode 结果+日志+耗时），版本列表+diff |
| `/strategy` | 采集与推送策略 | 分步表单（el-steps）：选对象（产品/设备/点位穿梭框）→ 采集周期（毫秒/cron）→ 推送策略（report_mode/死区/节流/静默兜底）→ 预览生效范围 |
| `/alarm` | 告警中心 | 实时告警（SignalR + ElNotification + 声音开关）、历史告警（多条件+导出）、告警配置（沿用 AlarmConfig/DeviceAlarmConfig 管理页）、**告警屏蔽**（alarm_mask 规则 CRUD + 时间窗选择器 + 命中预览 + 维护模式一键开关） |
| `/scada` | 组态/大屏 | 沿用现有 Scada 区（DashDataPool），逐步迁移 |
| `/system` | 系统管理 | 直接复用完整版 system/monitor 目录（用户/角色/菜单/部门/在线用户/操作日志），mock 换 .NET 接口 |

### 11.3 实时推送（SignalR，服务端基础已就绪）

- 服务端：IotWebApi 已注册 SignalR 并实现 `Services/SignalR/ChatServer`（Hub：UserLogin 按 Token 绑定连接 / Ping / 广播），在其上扩展设备分组（`JoinGroup("device:{id}")`）与告警分组即可，无需另起炉灶。**注意**：全仓尚无 `MapHub` 调用——需在 Program.cs 补 `app.MapHub<ChatServer>("/signalr/chatHub")` 一行完成端点映射，前端才能实际连接（组态前端 .env 中的 `_WIRHURL` 即指向该路径）；
- `src/utils/signalr.ts` 单例封装（HubConnectionBuilder），`accessTokenFactory` 复用 PureHttp token；
- `withAutomaticReconnect([0,2000,10000,30000])`，**重连后重放分组订阅**（订阅状态 Pinia 记账）；
- 设备详情进入 `JoinGroup("device:{id}")`、离开退组；组件卸载自动 off 防泄漏；
- 高频遥测前端 100~500ms 节流合并渲染；连接建立时服务端先推全量快照再增量（借鉴 Zigbee2MQTT retained 回放模式）。

---

## 12. 数据库表设计变更清单

| 动作 | 表 | 说明 |
|------|-----|------|
| 改造 | `device_type` → 补产品语义列（protocol_type、protocol_config jsonb、script_id、默认策略引用） | §5 |
| 改造 | `device_type_param` → 补物模型/过滤列（rw、三组过滤开关与阈值：range_filter_enable+min/max_value、amplitude_filter_enable+max_amplitude(_percent)、continuous_filter_enable+max_continuous_count、modbus 字节序等；策略列不落点表） | §5/§7 |
| 新增 | `channel`（通信链路：类型/串口参数/监听端口/注册包配置/cmd_interval_ms） | §6.1 |
| 新增 | `protocol_script`（JS 脚本：内容/版本/状态/试运行样例） + `protocol_script_history` | §6.4 |
| 新增 | `product_command`（命令白名单：名称/参数 schema/编码方式） | §5 |
| 新增 | `collect_strategy` / `push_strategy`（独立策略表：scope_type/scope_id 三级挂靠，表体字段=§7.1/§7.3 清单；点表不冗余策略列） | §7 |
| 新增 | `alarm_mask`（告警屏蔽规则，字段见 §9.4） | §9.4 |
| 改造 | `device_alarm_config` → 补 recover_formula、restrain_formula | §9.3 |
| 新增 | `notify_channel` / `notify_escalation`（通知渠道与升级链） | §9.5 |
| 新增 | `telemetry`（hypertable，30 天）/ `telemetry_latest` / `telemetry_1h`（cagg，长期） | §8.3 |
| 新增 | `device_event_log` / `raw_frame_log`（hypertable，30 天） | §8.3 |
| 新增 | `northbound_sink`（北向目的地配置） | §10.2 |
| 迁移 | 现有 EventHistory（按日分表）与 EventRun/EventSignal（按月分表）→ 并入 telemetry/device_event_log 体系 | §8.3 |
| 保留 | alarm_config（字段不动，引擎实现语义；DebounceType=3 标 deprecated，由 alarm_mask 等价兼容并引导迁移，见 §9.4）、EventAlarm（映射四态）、RBAC 全家、sys_plugin、schedule_job | |

> 迁移策略：数据库从 MySQL 切 PostgreSQL——SqlSugar `DbType` 切换 + CodeFirst 重建 + 存量数据一次性迁移（业务表量小）；时序区全新建（历史遥测按保留策略本就只留 30 天，可不迁）。

---

## 13. 实施路线图

| 阶段 | 内容 | 交付判据 |
|------|------|----------|
| **P0 修复基座**（1~2 周） | 重建数据上行链路（DataPointIngestService 消费 PluginEvent）；接线 JobInitializer；清理死代码（MqttFwd/MqttClient HostedService——**注意：内嵌 MQTTnet Broker 的启动逻辑物理上位于 MqttFwdHostService 内，须先迁出到新宿主再删**）；删除多余实体类与控制器（csproj 已排除的 DeviceLifecycle/Report/YxeThird 及未引用实体，删前全仓引用检查）；密钥出配置文件（环境变量/密管）；PG 切库跑通 CodeFirst | 国祥 VRF 插件数据能入库、能查询 |
| **P1 存储与管道**（2~3 周） | TimescaleDB 遥测表/日志表 + retention + 压缩 + cagg；Channel 批量 COPY 写入器；telemetry_latest + Redis；SignalR Hub | 模拟 1 万行/秒压测写入稳定；30 天 retention 演练（缩短为 1 小时验证 drop） |
| **P2 采集与防抖**（3~4 周） | Channel/采集调度器（双周期+cron）；三级异常值过滤；推送策略引擎（四模式+死区+节流+debounce_ignore）；策略配置 API+热重载 | 同一设备高低频点位混采；死区/节流用例通过 |
| **P3 协议驱动**（4~6 周） | IDeviceDriver 框架（自 GuoXiang 插件抽取基类，§6.1）+ Modbus（自动合包）→ DL/T 645（含 1997 兼容）→ CJ/T 188 → OPC UA / S7（§6.7）；TCP/DTU 透传接入（注册包/总线会话）；FluentModbus 模拟器测试台 | 真实电表/水表/Modbus 设备各接入一台走通采集+下发 |
| **P4 JS 脚本引擎（可选，可后置）**（2~3 周） | 常见协议已全部插件化（P3），本阶段仅为长尾私有协议兜底：Jint 沙箱宿主 + 三段式 API + 声明式定界器；试运行接口 + Debug 抓包；脚本版本管理 | 用 JS 脚本接入一个私有协议设备（可拿国祥 VRF 协议做验证样例） |
| **P5 告警体系**（3~4 周） | 防抖状态机（次数/时长/双向延时）；四态生命周期+去重；alarm_mask 屏蔽引擎+维护模式；通知渠道+升级链 | AlarmConfig 全部字段语义落地；屏蔽窗口/Ack 静默/到期自动恢复用例通过 |
| **P6 前端**（与 P2~P5 并行） | pure-admin-thin 骨架+登录+动态菜单对接 → 设备/产品/策略/告警/脚本调试页 → 大屏；**并入组态编辑器与运行时页**（§15.2 迁移清单与欠账修复，含补 MapHub 端点映射） | 全流程可在 UI 完成：建产品→配点表→接设备→看曲线→配告警→屏蔽告警；组态画面在统一前端可编辑/发布/预览 |
| **P7 增强**（持续） | 规则联动、北向转发（断线续传）、EMQX 外置选项、测试与 CI、OTA（远期） | |
| **P8 AI 能力融合**（2~3 周，可与 P6 并行） | 第一阶段（§15.3）：AiNet.WebApi 独立部署 + 统一认证（DES 密钥统一或 token 换发）+ nginx/YARP `/AiApi/*` 反代（SSE 关缓冲透传）+ 统一前端迁入 AI对话/Agent/知识库视图 + Agent 注册 IoT 查询工具 | 在中台 UI 内完成一次「查询设备状态 → AI 分析告警」的对话闭环 |

---

## 14. 风险与决策点

| # | 风险/决策 | 建议 |
|---|-----------|------|
| 1 | **TimescaleDB 可用性**：国内信创库/云 RDS 多数不带此扩展 | 自建 PG 实例（Docker/裸机）用方案 A；受限环境自动降级方案 B（代码层只差 DDL 脚本，写入/查询路径同构） |
| 2 | SqlSugar 对 PG 分区表：CodeFirst 不管理 hypertable DDL | 时序区 DDL 用独立 SQL 迁移脚本维护（版本化入库）；SqlSugar 只管业务表 |
| 3 | CJ/T 188 各厂差异大（7 字节地址编排、902x 历史数据） | DI 表配置化 + 按厂家建 Product 模板；建议采购 188-2018 标准原文核对 |
| 4 | 645 存量 1997 版表 | 驱动内置双版本，产品级配置版本号 |
| 5 | 内嵌 MQTTnet Broker 上限（单机万级连接） | 架构预留：MQTT 接入抽象为 IMqttFrontend，可切 EMQX（平台变为 EMQX 后端消费者 + Webhook 认证） |
| 6 | 进程内事件总线无持久化，横向扩展受限 | 单机阶段接受；P7 引入 MQ 时仅替换 CenboEventBus 实现 |
| 7 | JS 脚本安全 | 默认禁用 + 按钮级权限 + Jint 四重配额 + CLR 白名单；绝不学 Zigbee2MQTT 早期的裸 require |
| 8 | 高频采集下 DynamicExpresso 性能 | 所有公式 Parse 预编译缓存（按公式文本 hash），禁止每次 Eval 字符串拼接（iotgateway 的坑） |
| 9 | 大屏/曲线查询压力 | 查询强制走 telemetry_latest / cagg；明细查询限时间窗 + LTTB 降采样 |
| 10 | 团队单人维护 | 每阶段交付判据可独立验证；测试台（FluentModbus 模拟器 + 645/188 帧回放工具）优先建设 |
| 11 | 数据备份：retention 整块 DROP 不可逆，误删/磁盘故障无兜底 | pg_basebackup 周全量 + WAL 归档连续备份（压缩后 150~300GB 量级可承受）；恢复演练纳入上线清单 |
| 12 | 磁盘水位：异常测点暴增可能打满磁盘，导致写入全线阻塞 | 磁盘使用率 85% 告警、95% 写入熔断（丢可丢遥测、保指令与告警）；每设备/每租户入库速率配额（借鉴 Mosquitto 双维度配额） |
| 13 | 多租户：是否需要完整多租户设计（已决策） | **现阶段不做完整多租户**（私有化/项目制部署，每客户一套环境）。但既有 UnitId 单位级数据隔离必须做严做统一：从各控制器手写 sconlist 条件收敛为 SqlSugar 全局 QueryFilter（DbContext 对含 UnitId 实体统一注入查询过滤与插入赋值），杜绝再漏（本次修复的两处 GetListByPage 越权即手写遗漏所致）；组态迁入后自动纳入同一机制。未来若 SaaS 化，在 BaseEntity 预留 TenantId + 复用同一过滤器机制升级，当前不建（YAGNI） |

---

## 15. 组态大屏与 AI 能力融合

### 15.1 重大事实：三个前端同一血脉

| 项目 | 底座 | 版本 |
|------|------|------|
| 目标前端（本方案 §11） | pure-admin-thin | v7 系 |
| 组态大屏 `Energy/vuefrontend` | **vue-pure-admin 完整版本体**（package.json 未改名） | v6.0.0（Vue 3.5.13 / Vite 6.2.5 / EP 2.9.7 / Tailwind 4） |
| AiNet 前端 `AiNet.Web` | vue-pure-admin 基座 | v6.2.0（Vue 3.5 / EP 2.11 / Pinia 3） |

**结论：统一前端 = 目录级视图迁移，不存在跨框架改造成本。** 最终形态是一个 pure-admin 工程承载 IoT 管理 + 组态编辑/运行时 + AI 三块菜单。

### 15.2 组态大屏融合（直接并入统一前端，不用 iframe）

> **状态（2026-07-06）：迁移已完成落地。** 组态前端已整体迁入 `vuefrontend/`，本节所列前端欠账（编辑器路由、runtime 页复活、GetInfoByPk 清理、DashPublish 方法与参数对齐、重复注册、bigScreen 遗留清理）已全部修复并通过 vite 构建验证。**服务端地址已改为发布后运行时可配置**：`public/platform-config.json` 新增 `ApiUrl / WapianUrl / SignalRUrl` 三键（优先级：platform-config.json > 打包时 .env > 同源默认值 `/Api`），部署后直接编辑 `dist/platform-config.json` 即可切换环境、无需重新打包。后端待补项（发布快照、匿名分享、数据池收敛）见下文与《IoT实施计划》。

**迁移清单**（从 Energy/vuefrontend 拷入）：`src/views/scada`（编辑器，自研 FUXA 风格 SVG 组态，index.vue 约 4139 行）+ `src/views/project`、`src/api/scada|mqtt.ts`、`src/plugins/mqtt.ts`、`src/utils/mqtt.ts`、对应 router modules；依赖补 `mqtt`、`echarts`（+liquidfill）；maotu/logicflow/vue-flow 等闲置依赖不带。

**迁移时必须补的前端欠账**（现存断裂，源码核实）：
1. `ScadaFuxaEditor` 路由不存在——项目列表打开编辑器的链路是断的；
2. `runtime.vue` 是死代码：未注册路由、引用的 `@/api/scada`（getProjectMeta/loadProjectFromFile）不存在——需注册 `/scada/runtime/:id` 全屏路由并对齐后端 `GetDataInfo`（它已设计 postMessage + 自动缩放，接通后天然支持 iframe 无边框嵌入）；
3. 前端 `GetInfoByPk` 后端无此 action；`DashPublish` 未传后端必需的 `runtimeUrl`；
4. `/scada` 在 modules 与 remaining.ts 重复注册（name 冲突）；清理无引用的 bigScreen 遗留 API。

**后端 Scada 区必须补齐**（对照调研缺陷清单）：
1. **发布快照**：草稿与发布共用同一份 ContentData，发布后继续编辑会直接影响运行画面——发布时复制快照，运行时读快照，支持回滚；
2. **匿名分享**：发布后大屏仍需登录 token——增加 share_token 机制 + `[AllowAnonymous]` 运行时只读端点（大屏电视墙场景免登录）；
3. **数据池收敛（二选一）**：DashDataPool 无服务端执行引擎（前端直连第三方 URL 会被 CORS 拦）——要么补服务端代理取数端点（消费 ResponseMapping/RefreshInterval），要么废弃 DashDataPool、统一走设备点位绑定（ScadaProjectData.GetListByPage）+ SignalR 推送；
4. **修 5 个已知 bug**：ScadaProjectController.DashSetDefault 更新错列（置 ProjectDefault 却更新 ProjectStatus）、DashProjectDataController.DemoChart 永远返回空 source、DeleteById 不级联删 ScadaProjectData（孤儿数据）、ScadaProject/DashProject 两处 GetListByPage 缺 UnitId 过滤（**多租户越权风险**）。

**实时通道收敛**：组态运行时现有"MQTT over WebSocket 直连 + HTTP 5s 轮询"两通道，随 §11.3 统一收敛到平台 SignalR（MQTT 直连保留为大屏高频场景可选项）。前提：补 `app.MapHub<ChatServer>()` 端点映射（§11.3 注意事项）。

### 15.3 AI 能力融合（AiNet：AI对话 / Agent应用 / 知识库）

**事实基础（源码核实，融合成本极低）**：
- AiNet.WebApi 为 **net10.0 + SqlSugarCore 5.1.4.216**——与 IotWebApi 完全同版本，共有 NuGet 包（Newtonsoft/Serilog/Swashbuckle/Quartz）版本全一致，零依赖冲突；
- 生产配置 **PostgreSQL + pgvector**（PgVectorStore，HNSW 索引）——与本方案 §8 的 PG 选型天然契合，可共用同一 PG 实例（iot 库与 ainet 库分 database，pgvector 扩展同实例安装）；
- 认证与 IotWebApi **同源同构**：同一套 TokenAuthorizationFilter 代码谱系、同为 DES 加密 token 放 `token` 请求头，唯一分叉是 DES 密钥（"AiNetWebApi" vs "IotWebApi"）；
- LLM 层完全自研（AiClientFactory 支持 25+ 厂商，配置存 ai_provider 表）、对话 SSE 流式、Agent 为 function calling 循环（ToolExecutor 支持 HTTP Function/MCP/代码执行，自带 ToolGuardrails+ToolApprovalService 审批链）、知识库为完整 RAG 管线（解析→分块→向量化→混合检索→rerank，GraphRAG 可选）。

**推荐两阶段路线**：

| 阶段 | 做法 | 说明 |
|------|------|------|
| **第一阶段（推荐立即执行）：独立部署 + 统一入口** | ① AiNet.WebApi 独立进程部署；② 统一认证：统一 DES 密钥，或 IotWebApi 登录成功后向 AiNet 换发 token（改动只在密钥/OperatorModel 对齐，成本极低）；③ nginx/YARP 以 `/AiApi/*` 前缀反代到 AiNet（**SSE 需关闭反代缓冲**）；④ 统一前端新增 AI 菜单，从 AiNet.Web（同为 pure-admin v6）目录级迁移 对话/Agent/知识库 视图；⑤ 数据库同一 PG 实例、独立 ainet database | 双进程、双 database，但登录态/前端/域名全部统一；AiNet 中 Drama/视频/音乐等无关域不迁前端菜单即自然隐藏 |
| **第二阶段（可选，仅当双进程运维成为痛点）：子集并入单体** | 只搬 Chat/Agent/Knowledge 三域控制器 + AiCore 精简子集 + 十几张 Ai* 表进 IotWebApi | AiCore 与 Drama/Video/Music/Writing 等 50+ 服务目录耦合，剥离面大；前置条件（PG+pgvector）本方案已满足，但不建议短期做 |

**IoT × AI 化学反应（融合后的差异化价值）**：
1. **对话式运维**：把 IoT 中台 API 注册为 Agent 的 HTTP Function 工具（查设备状态/活动告警/历史曲线/下发指令），高危指令走 AiNet 已有的 ToolApprovalService 审批链——"帮我看看 3 号楼哪些电表离线了"直接对话完成；
2. **知识库 × 告警诊断**：把设备手册、DL/T 645 / CJ/T 188 协议文档、故障处理 SOP 摄取进知识库，告警详情页一键"AI 分析"（RAG 检索 SOP + 告警上下文生成处理建议）；
3. **告警 Copilot**：§9.5 通知渠道扩展 AiAnalysis 类型——告警触发时自动生成分析摘要随通知下发。

**风险**：AiNet 依赖 `UglyToad.PdfPig 1.7.0-custom-5` 私有定制包（本地 feed，CI/迁移环境需带上）；双进程日志监控收敛到同一 Serilog/Loki 栈。

---

## 附录 A：调研来源

- 本地源码深读：`D:/MyWork/Project/My/Zxx.Iot`（现状盘点）、`Zxx.IotCanKao/IoTSharp`、`Zxx.IotCanKao/iotgateway`、`Zxx.IotCanKao/ThingsGateway`
- GitHub top7 深读（2026-07-06）：home-assistant/core、node-red/node-red、thingsboard/thingsboard、emqx/emqx、Koenkk/zigbee2mqtt、eclipse-mosquitto/mosquitto、kubeedge/kubeedge；补充 jetlinks-community、IoTSharp、ThingsGateway、iioter/iotgateway、FastBee、dgiot、ThingsPanel、SagooIOT
- PostgreSQL 基准：Timescale 官方 1B 行基准（~111K 行/秒 vs 原生 PG 衰减至 ~5K）、Tiger Data ingest 测试（COPY ~316K 行/秒）、Cloudflare 生产（~100K 行/秒）、TSBS/QuestDB 对比、TSL 许可条款、pg_partman、BRIN 索引（Crunchy Data/Cybertec）、Npgsql COPY 文档
- 协议：DL/T 645-2007 标准原文 PDF、EMQX NeuronX DL/T645 文档、CJ/T 188 协议详解（tubring/CSDN）、Modbus Application Protocol V1.1b3、NModbus/FluentModbus/NewLife.Modbus 仓库
- JS 引擎：Jint 4.11 / ClearScript 7.5.1 / Jurassic 仓库与文档、JS Engine Switcher 实测
- 前端：pure-admin 官方文档仓库、GitHub API（v7.0.0，20.4k star）、DeepWiki、SignalR-Vue 集成实践
