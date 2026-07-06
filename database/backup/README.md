# PG 备份基线（M1.5）

> 依据：`docs/IoT实施计划.md` M1.5 / 方案 §8；目标：**周全量 + WAL 归档 + 磁盘水位告警(85%)/熔断(95%)**

## 1. WAL 归档（postgresql.conf）

```conf
wal_level = replica
archive_mode = on
archive_command = 'test ! -f /data/pg_wal_archive/%f && cp %p /data/pg_wal_archive/%f'
archive_timeout = 300          # 最长5分钟强制切段,限制RPO
```

- 归档目录 `/data/pg_wal_archive` 建议独立磁盘/挂载点，权限归 postgres
- 全量备份成功后可安全清理早于该备份起点的归档段（`pg_archivecleanup`）

## 2. 周全量备份

`pg_backup_weekly.sh`：`pg_basebackup -Ft -z -Xs`（tar+gzip、流式带 WAL，可独立恢复），默认保留 4 份轮转。

```cron
0 2 * * 0 /opt/iot/database/backup/pg_backup_weekly.sh >> /var/log/pg_backup.log 2>&1
```

前置：创建备份专用用户并配置 `~/.pgpass`（600 权限）：

```sql
CREATE ROLE replicator WITH REPLICATION LOGIN PASSWORD '<口令>';
```

## 3. 磁盘水位巡检

`disk_watermark_check.sh`：每 5 分钟检查数据盘使用率——

| 水位 | 动作 |
|------|------|
| ≥85% | 告警（日志 + 可选 Webhook 通知钉钉/企微） |
| ≥95% | **熔断**：落标记文件 `/var/run/iot_telemetry_breaker`（采集/写入侧检测到即暂停遥测写入；或按环境改成直接停采集服务） |
| 回落 | 自动解除熔断标记并通知 |

```cron
*/5 * * * * /opt/iot/database/backup/disk_watermark_check.sh
```

## 4. 恢复演练（M1.5 验收判据，需 PG 实例）

1. 准备一台空白恢复机（同大版本 PG + TimescaleDB）
2. 解开最近一份全量：`mkdir -p /data/pg_restore && tar -xzf base_<时间戳>/base.tar.gz -C /data/pg_restore`（`pg_wal.tar.gz` 解到 `pg_wal/`）
3. 如需恢复到故障时刻（PITR）：`touch /data/pg_restore/recovery.signal`，并在 postgresql.conf 配置
   `restore_command = 'cp /data/pg_wal_archive/%f %p'`（可选 `recovery_target_time`）
4. 以该目录为 PGDATA 启动，确认业务表与 `iot_ts` 时序表均可查询
5. 记录演练时长（恢复时间目标 RTO 基线）

## 5. Windows 部署备注

生产若在 Windows：`pg_basebackup.exe` 参数一致，改用任务计划程序（Task Scheduler）替代 cron；磁盘巡检可用 PowerShell `Get-PSDrive` 改写同等逻辑。
