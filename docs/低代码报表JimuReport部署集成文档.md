# 低代码报表 JimuReport 部署集成文档（D-7 第四批）

> 口径来源：`docs/协议模拟器与插件体系完善方案.md` §7 D-7 与 Q7。
> 路线：旁挂 **JimuReport**（Java/Spring Boot 独立服务）；数据源**不直连业务库**，经 WebApi 数据集端点（带租户过滤）；平台侧代码已随第四批落码，**docker 部署与示例报表制作为运维侧动作**。
> 许可合规确认项已于 2026-07-12 经用户拍板取消，不作为部署前置。

## 1. 架构

```
浏览器
 ├─ pure-admin「报表中心/低代码报表」页（/report/lowcode）
 │    └─ iframe src = ReportUrl?token=<平台accessToken>
 │
 └─ JimuReport 服务（Java，独立部署，8085）
      ├─ 鉴权：JmReportTokenServiceI 从请求取 token
      │    └─ 回调 WebApi  GET /Api/ReportDataset/VerifyToken   （匿名端点，只验真；token 走请求头，查询参数仅回落）
      └─ 取数：API 数据源（请求头带 token，[Token] 鉴权+租户过滤自动生效）
           ├─ GET /Api/ReportDataset/GetDailyEnergy    日用量数据集
           └─ GET /Api/ReportDataset/GetAlarmDaily     告警日统计数据集
```

平台业务库/时序库对 JimuReport **零暴露**：报表元数据存 JimuReport 自己的库；业务数据全部经 WebApi 端点，租户隔离由 `[Token]` + `TenantScope` 全局过滤兜底。

## 2. 平台侧已交付面（本批落码）

### 2.1 后端端点（`IotWebApi/Areas/Event/Controllers/ReportDatasetController.cs`）

| 端点 | 鉴权 | 说明 |
|---|---|---|
| `GET /Api/ReportDataset/GetDailyEnergy?paramcode=&starttime=&endtime=&deviceid=0` | `[Token]` | 按日设备用量。基于 `iot_ts.telemetry_1h` 长期聚合的日末表码差分（`TelemetryQueryService.QueryDailyUsageAsync`），适用电能等累积点位；`deviceid=0` 查当前租户全部设备；设备集经 `DeviceInfoDAO` 取得（租户过滤自动生效，时序库无租户列不能直查）；表码回退（换表/清零）当日记 null |
| `GET /Api/ReportDataset/GetAlarmDaily?starttime=&endtime=` | `[Token]` | 按日×等级告警计数，零值补齐；等级全集 = `AlarmConfig` 配置 ∪ 窗口内实际出现；排除"离线"事件（与现有统计口径一致）；时间过滤走 SnowId 区间跨月分表 |
| `GET /Api/ReportDataset/VerifyToken` | 匿名 | Token 校验回调，供 JimuReport 服务端验真。token 优先走请求头 `token`（查询参数 `?token=` 仅作回落，避免有效 token 进反代访问日志）；校验口径与 `TokenAuthorizationFilter` 同源（DES 解密 + LoginTime 时限），另加用户在库且启用；失败文案统一不区分原因（防 token 状态探测）；返回 `{Valid, UserId, UserName, TenantId}` |

返回外层为平台统一 `MetaData` 包装（`Status/Message/Result/TotalCount`），数据集行在 `Result` 数组内。

### 2.2 前端

| 文件 | 内容 |
|---|---|
| `src/router/modules/report.ts` | 「报表中心 → 低代码报表」菜单（rank 4，静态路由） |
| `src/views/report/lowcode.vue` | iframe 页：`ReportUrl` 未配置时显示提示；已配置则拼 `?token=<accessToken>` 加载 |
| `public/platform-config.json` | 新增 `"ReportUrl": ""`（发布后免打包修改，同 ApiUrl 三级回退模式） |
| `src/config/index.ts` | 新增 `getReportUrl()` |

## 3. JimuReport 部署（运维侧）

### 3.1 阶段一：spike 验证（内网，无鉴权）

用官方 docker 镜像快速起服务验证「PG 数据集 + iframe 嵌入」两件事（Q7 的 0.5~1 天 spike）：

```yaml
# docker-compose.yml（骨架，镜像名/环境变量以官方文档为准：https://help.jeecg.com/jimureport/）
services:
  jimureport-mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: <改我>
      MYSQL_DATABASE: jimureport
    volumes:
      - ./mysql-data:/var/lib/mysql
  jimureport:
    image: jeecgdocker/jimureport   # 以官方最新文档核对镜像名与tag
    ports:
      - "8085:8085"
    depends_on:
      - jimureport-mysql
    # 数据库连接环境变量按镜像文档填写（指向 jimureport-mysql）
```

- JimuReport 元数据库用自带 MySQL sidecar，**不共用平台 PG**（职责隔离，升级互不牵连）。
- spike 阶段设计器地址 `http://<host>:8085/jmreport/list`，先手工在浏览器另开页签验证，再回填 `platform-config.json.ReportUrl` 验证 iframe。
- 验证 API 数据源：数据集 URL 填平台端点（见 §4），token 先用登录后从浏览器请求头拷出的真实值。

### 3.2 阶段二：生产（token 打通）

官方镜像不含自定义鉴权，生产需自建 Spring Boot 工程引入 `jimureport-spring-boot-starter`，实现 `JmReportTokenServiceI`（接口签名以所用 starter 版本为准）：

```java
@Component
public class IotTokenService implements JmReportTokenServiceI {
    @Value("${iot.webapi-base}")           // 如 http://webapi-host:13699
    private String webapiBase;
    private final RestTemplate rest = new RestTemplate();

    @Override
    public String getToken(HttpServletRequest request) {
        // 前端 iframe 以查询参数带入，starter 会在后续请求中透传
        String token = request.getParameter("token");
        return token != null ? token : request.getHeader("X-Access-Token");
    }

    @Override
    public Boolean verifyToken(String token) {
        try {
            // 回调平台匿名校验端点；token 走请求头（避免进反代访问日志）；外层是 MetaData 包装，Result 里带 Valid
            HttpHeaders headers = new HttpHeaders();
            headers.set("token", token);
            ResponseEntity<Map> resp = rest.exchange(
                webapiBase + "/Api/ReportDataset/VerifyToken", HttpMethod.GET,
                new HttpEntity<Void>(headers), Map.class);
            Object result = resp.getBody() == null ? null : resp.getBody().get("Result");
            return result instanceof Map && Boolean.TRUE.equals(((Map<?, ?>) result).get("Valid"));
        } catch (Exception e) {
            return false;
        }
    }

    @Override
    public String getUsername(String token) {
        // 同上回调取 Result.UserName（可加本地短时缓存避免每次往返）
        ...
    }

    @Override
    public Map<String, Object> getUserInfo(String token) {
        // 把 Result.TenantId 放入用户信息，报表数据集参数可引用 ${tenantId} 做冗余过滤
        ...
    }
}
```

要点：
- **数据集请求头带 token**：JimuReport API 数据源支持自定义请求头，把用户 token 配进 `token` 头 → WebApi `[Token]` 鉴权 + 租户过滤自动生效，这是租户隔离的**主防线**（VerifyToken 只管报表服务自身登录态）。
- `iot.webapi-base` 指向 WebApi 内网地址；VerifyToken 是匿名端点，无需额外凭据。

### 3.3 反代与外网（沿用现有隧道体系）

与前端发布同轨（见 `docs/前端发布与外网访问部署文档.md`）：WSL nginx 加一条 location（如 `/jimu/ → http://127.0.0.1:8085/`），cloudflared 命名隧道无需新增主机名；`ReportUrl` 填反代后的公网路径。注意 JimuReport 页面内静态资源路径是否兼容子路径反代，若不兼容则为其单独分配主机名。

**日志加固（必做）**：iframe URL 以查询参数携带用户 accessToken（JimuReport 官方集成的标准形态，泄露=token 有效期内全 API 权限），须为该 location 关闭 access_log 或改用去 query 的 log_format，Cloudflare 侧同理确认边缘日志不留 query。

## 4. 示例报表制作步骤（设计器内，2 张）

**报表一：设备日用量报表**
1. 设计器「数据集管理」→ 新增 **API 数据集**：
   - URL：`http://<webapi>/Api/ReportDataset/GetDailyEnergy?paramcode=${paramcode}&starttime=${starttime}&endtime=${endtime}&deviceid=0`
   - 请求头：`token = ${token}`（动态参数，由 JmReportTokenServiceI 透传的用户 token）
   - 数据路径：`Result`（行字段 `Day/DeviceId/DeviceName/Value`）
2. 报表参数：`paramcode`（默认填现网电能点位编码）、`starttime`/`endtime`（日期控件）。
3. 画布：日期为行、设备为列的交叉表或明细表 + 按 `Day` 汇总的柱状图。

**报表二：告警日统计报表**
1. API 数据集：`.../GetAlarmDaily?starttime=${starttime}&endtime=${endtime}`，行字段 `Day/AlarmGrade/AlarmCount`（零值已补齐，直接透视）。
2. 画布：按 `AlarmGrade` 分系列的堆叠柱状图 + 等级×日期交叉汇总表。

## 5. 遗留与可选清理

- **同族遗留已删（2026-07-12 拍板）**：`EventReportWeek/Month/PeakDay` 三族（实体+DAO+FullEntity+Expand+Controller 共 15 文件）与前端死文件 `api/report/electricReport.ts`（零导入方，所引 `DataReport`/`HtkRecordDataPeakDay` 控制器后端不存在）已删除。
- **已删实体的物理表**（CodeFirst 不删表，确认无回滚需要后由运维执行）：

```sql
-- event_report_day/event_report_week 按年分表、event_peak_day 按月分表，先查出全部物理分表再删：
SELECT tablename FROM pg_tables WHERE schemaname='public'
  AND (tablename LIKE 'event_report_day%' OR tablename LIKE 'event_report_week%' OR tablename LIKE 'event_peak_day%');
-- 逐表：DROP TABLE IF EXISTS public.<tablename>;
-- event_report_month 为单表：
DROP TABLE IF EXISTS public.event_report_month;
```

- ~~数据库 `sys_menu` 菜单残行清理~~ **已核验作废（2026-07-12）**：本地 PG 实例（6305）全部五库逐库核验均不存在 `sys_menu` 表（CodeFirst 惰性建表，菜单 DAO 从未触达），目标环境亦确认无此表，无残行可清。若日后某环境建出该表并存有指向已删页面（能耗分析/dataAnalysis/reportForms 系）的菜单行，按 `menu_url` 匹配删行并清 Redis 键 `SysMenu`（Db=1，实体挂 [EntityCache]）即可。
- **中期加固项（有生产多租户诉求时做）**：新增 `[Token]` 端点签发"报表专用短时 token"（复用 EncryptsHelper，载荷加 marker 字段+独立短时限，VerifyToken 识别 marker），前端 iframe 改拼该 token——把泄露半径从全 API 缩小到报表数据集两个端点。
- ~~已知预存问题~~ **已修复（2026-07-12）**：`DbContext` 缓存回填污染——`EnsureCacheLoaded` 回填、`ValidateCacheConsistency` 计数与刷新共三处查询加 `ClearFilter<ITenantEntity>()` 取无租户过滤的真全表（后者原是更主要的污染引擎：普通租户请求触发 10 分钟一致性校验时，子集计数与全表缓存必然"不一致"，反复清掉正确缓存再用子集污染回填）；租户隔离仍由缓存出口 `FilterTenantScope` 负责，全部缓存读路径已核验无未过滤出口。残余小项：插入恰逢缓存过期窗口会短暂种下少量行快照，由修正后的一致性校验在 ≤10 分钟内自愈。
