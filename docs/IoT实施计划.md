# Zxx.Iot 实施计划

> 版本：v1.0（2026-07-06）
> 关联文档：《[IoT平台功能方案](IoT平台功能方案.md)》（章节引用 §x 均指方案文档）
> 约定：每项任务必须带可独立验证的验收判据；未验证不得标记完成。

---

## 一、已完成（2026-07-06）

| ✅ | 事项 | 验证方式 |
|----|------|----------|
| ✅ | 方案文档定稿（15 章，双审查闭环：需求完整性 + 源码事实核查） | 审查缺陷 17+6 处全部修订 |
| ✅ | SignalR 端点补接：`app.MapHub<ChatServer>("/signalr/chatHub")` | dotnet build 0 错误 |
| ✅ | Scada 后端五处缺陷修复（DashSetDefault 错列、DemoChart 空数据、DeleteById 孤儿、两处 GetListByPage 越权） | dotnet build 0 错误 |
| ✅ | **UnitId 单位级数据隔离统一机制**：`IUnitEntity` + `UnitScope`(AsyncLocal) + DbContext 双路过滤（SQL QueryFilter/DataExecuting + EntityCache 缓存出口过滤），Token 过滤器写入上下文；首批纳入 ScadaProject/DashProject/DashDataPool | dotnet build 0 错误；待两账号人工越权验证 |
| ✅ | **组态前端迁移**：Energy/vuefrontend → `vuefrontend/`；六处断裂修复（编辑器路由 `/scada/editor/:id`、运行时路由 `/scada/runtime/:id`、API 模块补全 getDataInfo/saveProjectData/uploadBase64Image、DashPublish GET+runtimeUrl、信封字段 Status/Message、bigScreen 清理） | pnpm install + vite build ✓（21.67MB） |
| ✅ | **服务端地址运行时配置化**：platform-config.json 新增 ApiUrl/WapianUrl/SignalRUrl，http 拦截器/登录 SignalR/瓦片三类共 5 处读点改造，发布后编辑 dist/platform-config.json 即生效 | vite build ✓ |

**部署提示**：前端发布 = `pnpm build` → 部署 `dist/` → 按环境编辑 `dist/platform-config.json` 的三个地址键（留空则回退打包时 .env 值）。

---

## 二、待办任务分解（按里程碑）

### M0 基座修复收尾（对应方案 P0，1~2 周）

| # | 任务 | 依赖 | 验收判据 |
|---|------|------|----------|
| 0.1 | 重建数据上行链路：`DataPointIngestService`(IHostedService) 消费 PluginEvent；恢复 MqttClientJob 消息解析 | — | 国祥 VRF 插件数据入库可查询 |
| 0.2 | 接线 JobInitializer（Program.cs 注释段恢复） | — | 新库首启自动创建系统任务 |
| 0.3 | 清理死代码：MqttFwd/MqttClient HostedService（**先把内嵌 Broker 启动逻辑从 MqttFwdHostService 迁出**）、csproj 已排除的 DeviceLifecycle/Report/YxeThird 及未引用实体 | — | 全仓引用检查后编译通过 |
| 0.4 | 密钥出配置文件（RSA 私钥/DB 密码 → 环境变量或密管） | — | 仓库内无明文密钥 |
| 0.5 | PG 切库：SqlSugar DbType 切 PostgreSQL，CodeFirst 重建，业务表存量迁移 | — | 全部 Areas 接口冒烟通过 |
| 0.6 | UnitId 隔离人工验收 + 逐模块扩展（DeviceInfo/DeviceAlarmConfig 等 18 个含 UnitId 实体，逐模块加 `, IUnitEntity` 并回归） | 隔离机制(已完成) | 两个单位账号互查不可见；后台任务不受影响 |

### M1 存储与管道（对应方案 P1，2~3 周）

| # | 任务 | 依赖 | 验收判据 |
|---|------|------|----------|
| 1.1 | TimescaleDB：telemetry/telemetry_latest/日志表 DDL 脚本（版本化）、压缩(7d)、retention(30d)、cagg(§8.3 参数链) | 0.5 | retention 缩短为 1 小时演练 drop_chunks 生效 |
| 1.2 | Channel<T> 批量写入器 + Npgsql Binary COPY（5000 行/2s 批） | 1.1 | 模拟 1 万行/秒压测写入稳定 |
| 1.3 | 最新值缓存（内存+Redis→telemetry_latest） | 1.2 | API 实时值查询不扫时序表 |
| 1.4 | SignalR 设备分组推送（ChatServer 扩展 JoinGroup("device:{id}")/告警组） | MapHub(已完成) | 设备详情页实时刷新 |
| 1.5 | PG 备份基线：pg_basebackup 周全量 + WAL 归档；磁盘水位告警(85%)/熔断(95%) | 1.1 | 恢复演练成功 |

### M2 采集与防抖动（对应方案 P2，3~4 周）

| # | 任务 | 依赖 | 验收判据 |
|---|------|------|----------|
| 2.1 | collect_strategy/push_strategy 表 + 三级 scope 合并引擎（产品>设备>点位，产品级为主入口） | 0.5 | 策略热重载不重启插件 |
| 2.2 | 采集调度器（毫秒/cron 双模，通道串行队列） | 2.1 | 高低频点位混采正确 |
| 2.3 | 三级异常值过滤（范围/幅度/连续容错） | 2.2 | 毛刺被滤、真实阶跃 N 次后接受 |
| 2.4 | 推送策略引擎（四模式+死区+min_push_interval+max_silent+debounce_ignore；缓存立即更新发布延迟） | 2.2 | 15 分钟定时推送样例场景通过；告警/上下线事件不受节流 |
| 2.5 | 上下线事件（§7.5：判定防抖+原因枚举+网关连带合并） | 2.2 | 拔网线场景产生一条带原因的离线事件 |

### M3 协议驱动（对应方案 P3，4~6 周）

| # | 任务 | 依赖 | 验收判据 |
|---|------|------|----------|
| 3.1 | IDeviceDriver 驱动框架：自 GuoXiang 插件抽取基类（连接管理/采集循环/超时重发/会话表上提） | 2.x | GuoXiang 插件改造后行为不变 |
| 3.2 | Modbus RTU/TCP 驱动（自动合包≤125 寄存器、字节序四选一、写优先） | 3.1 | FluentModbus 模拟器 + 真实设备各过一遍读写 |
| 3.3 | DL/T 645-2007 驱动（含 1997 兼容、DI 模板导入、广播校时） | 3.1 | 真实电表抄读电能量/电压电流 |
| 3.4 | CJ/T 188 驱动（DI 可配、厂家模板） | 3.1 | 真实水表抄读累积流量 |
| 3.5 | TCP/DTU 透传接入（注册包/心跳/总线会话/旧连接替换） | 3.1 | DTU 挂双表分别路由正确 |
| 3.6 | OPC UA / S7 驱动（§6.7） | 3.1 | 模拟服务器订阅+批量读 |

### M4 告警体系（对应方案 P5，3~4 周）

| # | 任务 | 依赖 | 验收判据 |
|---|------|------|----------|
| 4.1 | 告警防抖引擎（AlarmConfig 现有字段全语义：次数型/时长型/连续/累计/取值动作；状态机快照 Redis） | 2.4 | 每个 Debounce 字段各一条用例 |
| 4.2 | 四态生命周期+去重（(device,alarm_config) 唯一活动告警、原地升级、恢复事件） | 4.1 | 重复触发不新建记录 |
| 4.3 | alarm_mask 屏蔽引擎（scope×模式×动作、时间窗、expire_at 自动失效、命中预览接口、维护模式） | 4.2 | 屏蔽窗口内静默入库打标；到期自动恢复 |
| 4.4 | 通知渠道 + 升级链（邮件/Webhook/钉钉，[15,30,60] 梯队，Ack 中断） | 4.2 | 未确认告警按梯队重复通知 |
| 4.5 | 离线告警接线（复用 §7.5 判定，AlarmConfirmSeconds 时长型确认） | 2.5+4.1 | 短暂断网不告警、持续断网一条告警+恢复闭环 |

### M5 组态后端补齐 + 前端整合（可与 M2~M4 并行）

| # | 任务 | 依赖 | 验收判据 |
|---|------|------|----------|
| 5.1 | 发布快照：发布时复制 ContentData 快照，运行时读快照，支持回滚 | — | 发布后继续编辑不影响运行画面 |
| 5.2 | 匿名分享：share_token + [AllowAnonymous] 运行时只读端点 | 5.1 | 电视墙免登录打开运行时页 |
| 5.3 | 数据池收敛决策：服务端代理取数端点 或 废弃 DashDataPool 统一设备点位绑定 | 1.4 | 二选一落地，画面数据源可用 |
| 5.4 | 运行时页完整图元渲染（复用 SvgManager 渲染管线替换简化渲染） | 5.1 | 编辑器画面与运行时视觉一致 |
| 5.5 | IoT 管理页开发：产品/设备/物模型/策略/告警中心/告警屏蔽（§11.2 页面表） | 各后端模块 | 全流程 UI 闭环 |

### M6 增强（对应方案 P4/P7/P8，可选/后置）

- JS 脚本引擎（Jint 三段式，长尾协议兜底）——常见协议插件化完成后再评估；
- 规则联动（触发-条件-动作 + 试运行 + 漏斗指标）；北向转发（Connector/Sink + 三段式断线续传）；
- AI 能力融合第一阶段（§15.3：AiNet 独立部署 + 统一 DES 密钥/token 换发 + `/AiApi/*` 反代 + 前端迁入 AI 三视图 + Agent 注册 IoT 查询工具）；
- EMQX 外置选项、测试与 CI、OTA（远期）。

---

## 三、验证约定

1. 每次后端改动：`dotnet build` 0 错误为底线；涉及数据链路的补集成冒烟。
2. 每次前端改动：`pnpm build` 通过；涉及页面的用 `pnpm dev` + superadmin 账号人工走查。
3. 隔离/权限类改动：必须用两个不同单位账号做越权对测。
4. 时序/保留类改动：retention 用缩短窗口（1 小时）演练验证后再配置 30 天。
5. 协议驱动：先模拟器（FluentModbus/帧回放）后真实设备，双通过才算完成。
