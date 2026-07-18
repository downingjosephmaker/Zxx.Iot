# 外网 MQTT 安全 · 运行时验证手册（Task 4.6 + Phase 1）

> 覆盖"通讯层重构"分支唯一的运行时未验证面：内嵌 broker 的**每设备凭据认证 + Topic ACL + 8883 单向 TLS**，以及 MqttClientJob 的**重连不重复/不堆会话**。
> 代码面已全绿推送（build 0 错 / 178 测试）；本手册是把 `ValidatingConnection`（4.3）真实接线亲眼看一遍——它没有单测，集成验证是其唯一覆盖。
>
> ⚠ 前置安全约束：**本验证在内网/本机做**。8883 落地公网前，1883 明文口不得映射公网。

---

## 0. 环境前置（宅邸点火）

按 `docs/前端发布与外网访问部署文档.md` / 本地开发环境把基础设施与后端点起来：

| 服务 | 端口 | 说明 |
|---|---|---|
| PostgreSQL | 6305 | zhjngkdb |
| Redis | 8111 | |
| RabbitMQ(MQTT 插件) | 1883 | 平台**订阅端**(MqttClientJob)连它;与内嵌 broker 不是一回事 |
| 后端 IotWebApi | 13699 | Production 口启动命令有坑,见本地环境记录 |

> 术语澄清：本分支 4.x 改的是**内嵌 broker**（`MqttServerJob` → MQTTnet 自建服务端，默认 1883 明文 + 新增 8883 TLS）。它与 RabbitMQ 那个 1883 是两套东西——验证时确保**内嵌 broker 占用的端口**没被 RabbitMQ 抢（若都想用 1883，需错开：把 `admin_mqttparam.MqttServerPort` 改成如 11883，8883 恒定不变）。

---

## 1. 启用内嵌 broker（默认是关的）

`JobInitializer.cs:25` 种子把 `MqttServerJob` 的 `JobStatus` 设为 `0`（停止），且种子**只在行不存在时插入**。启用二选一：

- **全新库**：首启前把 `JobInitializer.cs:25` 的 `JobStatus = 0` 改为 `1`，重编译启动。
- **已有库**（推荐，不改码）：改库里那行再重启后端让 Quartz 重读：
  ```sql
  UPDATE schedule_job SET job_status = 1
   WHERE job_class_name = 'MqttServerJob' AND job_group_name = 'System';
  ```
  （或用后端的任务管理页把"MQTT服务端状态检查任务"置为运行。）

启动后看日志出现 `尝试初始化MQTT服务端` + `MQTT服务端状态: 正常`，且端口就绪：
```powershell
Get-NetTCPConnection -State Listen -LocalPort 8883   # 应 LISTENING
```

---

## 2. 造一条测试凭据（无管理 UI，直接写库）

口令已用与 `EncryptsHelper.Pbkdf2Hash` 完全一致的参数（SHA256 / 100k 迭代 / 16 字节盐 / 32 字节 / base64）预烤：

- **MqttUser** = `dev-test`
- **Password** = `Test@8883`（明文，仅测试用）
- **绑定网关(DeviceGateway)** = `dev-test`（ACL 校验 topic 末段用）

```sql
INSERT INTO device_mqtt_credential (mqtt_user, pass_hash, salt, device_gateway, is_enable, tenant_id)
VALUES (
  'dev-test',
  'k8Z4+4Vnz/JX7TvNxBLc3InnrhZoZZckFogAJtrdfAE=',
  'KcwqKuK1bi2wTA62upNVqg==',
  'dev-test',
  1,
  0
);
```
> `id` 自增可省。若你的 PG 列名带引号大小写敏感，以 `device_mqtt_credential` 表实际列为准（实体见 `IotModel/MEntity/Basic/DeviceMqttCredential.cs`）。
> ⚠ **mqtt_user 必须全局唯一**——broker 回调线程走租户豁免查询，同名会认证进错误 gateway（这也是启用每设备凭据的硬前置）。

---

## 3. 验证用例

### 测试 A — 1883 存量全局账号不断（不得回归）
用 `admin_mqttparam` 表里的 `MqttUser`/`MqttPass`（全局账号）连**默认明文端口**（`MqttServerPort`）：
```bash
mosquitto_pub -h 127.0.0.1 -p 1883 -u <全局MqttUser> -P <全局MqttPass> -t "any/topic" -m "hi"
```
**期望**：连接成功、发布成功（全局账号不 Bind、ACL 恒放行）。这是"存量并存"的底线。

### 测试 B — 8883 单向 TLS + 每设备凭据（客户端跳链校验）
设备端语义 = 只加密、跳过证书链校验。`mosquitto` 对自签跳链不便，用 Python `paho-mqtt`（`ssl.CERT_NONE` 精确对应设备侧 `CertificateValidationHandler => true`）：

```python
# pip install paho-mqtt   （若未装）
import ssl, paho.mqtt.client as mqtt

def run(user, pwd, tag):
    c = mqtt.Client(client_id=f"verify-{tag}")
    c.username_pw_set(user, pwd)
    ctx = ssl.create_default_context()
    ctx.check_hostname = False
    ctx.verify_mode = ssl.CERT_NONE          # 跳过证书链校验 = 只加密
    c.tls_set_context(ctx)
    rc = {"code": None}
    c.on_connect = lambda cl, u, f, reason, props=None: rc.update(code=int(reason))
    c.connect("127.0.0.1", 8883, 60); c.loop_start()
    import time; time.sleep(2); c.loop_stop()
    print(f"[{tag}] CONNACK rc={rc['code']}  (0=接受, 非0=拒绝)")

run("dev-test", "Test@8883", "right")   # 期望 rc=0 接受
run("dev-test", "WRONG",     "wrong")   # 期望 rc!=0 拒绝(BadUserNameOrPassword)
run("ghost",    "whatever",  "unknown") # 期望 rc!=0 拒绝(未知用户,不负缓存)
```
**期望**：正确口令接受、错误口令拒、未知用户拒。抓包（Wireshark 看 8883）应**全程 TLS 密文**，看不到明文 topic/payload。

### 测试 C — Topic ACL 爆炸半径锁单台
`dev-test` 凭据绑定 gateway=`dev-test`。用测试 B 的连接（跳链校验）发布：

```python
# 复用上面的 ctx / username_pw_set("dev-test","Test@8883")
c.publish("zhjngk/receive/webapi/dev-test", "own device")   # 自己的 topic → 放行
c.publish("zhjngk/receive/webapi/victim",   "impersonate")  # 冒充他人 → 被 ACL 拒
```
**观测点**（broker 静默丢弃被拒发布，客户端侧看不出，**看服务端 SysLog**）：
- 后端系统日志出现：`ACL 拒绝发布 client=verify-... topic=zhjngk/receive/webapi/victim`
- `.../dev-test` 那条**无**拒绝日志（放行）。
- 订阅同理：`c.subscribe("zhjngk/receive/webapi/victim")` 应触发 `ACL` 订阅拒绝路径（`ProcessSubscription=false`）。

**期望**：即便 `dev-test` 凭据泄露，攻击者也只能发/订 `.../dev-test`，碰不到别的设备——爆炸半径锁死单台。

---

## 4. Phase 1 — MqttClientJob 重连（不重复上行 / 不堆僵尸会话）

平台订阅端连 RabbitMQ(1883)。制造断线看收敛：

1. 后端稳定运行、有设备在上行（观察上行入库正常）。
2. 制造断线：停 RabbitMQ 容器几秒再拉起（或防火墙短暂阻断 1883）。
3. 看后端日志：
   - 重连应**由唯一入口 `TryInitializeClient` 串行发起**（`SemaphoreSlim` 保证），**不出现两路重连打架**。
   - 建新 client 前旧的被 `DisconnectAsync`/`Dispose`——**不堆僵尸会话**。
   - 恢复后同一条上行**只入库一次**（旧 client 未释放会导致重复入库，这正是本次修复点）。
4. 对照 RabbitMQ 管理台连接数：恢复后应回到 **1 条**平台订阅连接，不残留。

**期望**：一次断线 → 一次干净重连 → 无重复上行 → 无僵尸会话。

---

## 5. 验收清单

- [ ] A：1883 全局账号连通、发布成功（存量不断）。
- [ ] B：8883 正确凭据接受、错误口令拒、未知用户拒；抓包全密文。
- [ ] C：`.../dev-test` 放行、`.../victim` 被 ACL 拒（服务端日志为证）；订阅同理。
- [ ] Phase 1：断线一次 → 干净重连一次 → 无重复上行 → RabbitMQ 侧连接数回 1。

全绿即 Task 4.6 + Phase 1 收口。清理：`DELETE FROM device_mqtt_credential WHERE mqtt_user='dev-test';` 并按需把 broker `job_status` 复位。

---

## 附：已知边界（非本次回归，供验证时心里有数）
- **载荷内 DeviceId 未受 ACL 约束**：ACL 只锁 topic 末段，绑定 `dev-test` 的客户端发到 `.../dev-test` 但载荷 `DeviceId` 写成别的设备，仍会入库到那个设备。属设计边界（载荷 DeviceId 一直被信任），后续在 ingest 侧增强。
- **凭据吊销/改密最长 10min 生效**：`MqttCredentialCache` L1 TTL=10min 且 `Invalidate` 暂无调用者，交凭据管理任务接线。
- **8883 自签证书每次 broker reinit 重生成**：跳链校验下功能无害；Windows 上若 8883 转常态运行，建议自签路径加 `X509KeyStorageFlags.EphemeralKeySet` 避免磁盘密钥文件遗留。
