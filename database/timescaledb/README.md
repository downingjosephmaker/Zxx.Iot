# TimescaleDB 时序库脚本（M1.1）

> 依据：`docs/IoT平台功能方案.md` §8.3（方案 A）、§12 表清单；任务出处：`docs/IoT实施计划.md` M1.1

## 前置条件

- PostgreSQL **16 或 17**（避开 17.1，用 17.2+）
- TimescaleDB **Community 2.x**（TSL 许可，自托管免费合规）
- `postgresql.conf` 中 `shared_preload_libraries = 'timescaledb'`（改后需重启 PG）
- Docker 快速起库（开发/演练用）：

```bash
docker run -d --name iot-tsdb -p 5432:5432 -e POSTGRES_PASSWORD=<密码> timescale/timescaledb:latest-pg17
```

## 执行顺序

按文件名版本号依次执行（`psql -d <库名> -f <文件>`）：

| 文件 | 内容 |
|------|------|
| `V0001__init.sql` | timescaledb 扩展 + `iot_ts` schema（业务表在 public，时序表在 iot_ts，同库不同 schema） |
| `V0002__telemetry.sql` | 遥测窄表 hypertable（按日分块）+ 压缩(7d) + retention(30d) |
| `V0003__telemetry_latest.sql` | 最新值旁路表 + **point_map 点位映射表**（见下） |
| `V0004__telemetry_1h_cagg.sql` | 小时级连续聚合 + 刷新策略。**不能在事务块内执行**，勿用迁移工具的事务模式包裹 |
| `V0005__event_logs.sql` | device_event_log / raw_frame_log（hypertable，30 天，frame 列 lz4） |

全部脚本幂等（`IF NOT EXISTS` / `if_not_exists => TRUE`），重复执行安全。

## point_map 为什么存在（§12 清单的补充）

现有 `device_param` 是**一设备一行**，点位在 `expand_json`（Expand_DeviceParam 列表）里只有字符串 `ParamCode`，没有整型点位 ID；而 §8.3 的 telemetry 窄表用 `point_id int`（窄行宽 + segmentby 效率）。`iot_ts.point_map` 负责 `(device_id, param_code) → point_id` 的映射，由批量写入器（M1.2）首见即建、内存缓存。

## 策略参数链（硬约束）

```
cagg end_offset(1h) < cagg start_offset(3d) < 压缩策略(7d) < retention(30d)
```

即聚合刷新永远先于压缩与删除完成。调整任一参数必须保持该次序，否则刷新窗口触及已删数据会丢聚合。

## retention 演练（M1.1 验收判据）

按 `drill_retention_1h.sql` 内注释逐段执行：缩短为 1 小时 → 插入 2 小时前数据 → `CALL run_job(<job_id>)` → `show_chunks` 确认旧 chunk 整块 DROP → **恢复 30 天**。

## 验证查询速查

```sql
-- hypertable 清单
SELECT hypertable_schema, hypertable_name FROM timescaledb_information.hypertables;
-- 策略任务清单（压缩/retention/cagg 刷新）
SELECT job_id, proc_name, hypertable_name, config FROM timescaledb_information.jobs;
-- chunk 明细
SELECT show_chunks('iot_ts.telemetry');
-- 压缩效果
SELECT * FROM hypertable_compression_stats('iot_ts.telemetry');
```
