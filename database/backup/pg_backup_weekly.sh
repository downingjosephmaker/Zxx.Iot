#!/usr/bin/env bash
# =============================================================
# PG 周全量备份脚本(pg_basebackup,tar+gzip,流式带WAL)
# 依据: docs/IoT实施计划.md M1.5 / 方案 §8
# 用法: crontab 每周日 02:00 执行
#   0 2 * * 0 /opt/iot/database/backup/pg_backup_weekly.sh >> /var/log/pg_backup.log 2>&1
# 认证: 建议 ~/.pgpass 存放复制用户口令(600 权限),不要写进本脚本
# =============================================================
set -euo pipefail

PGHOST="${PGHOST:-127.0.0.1}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-replicator}"          # 具 REPLICATION 权限的备份专用用户
BACKUP_ROOT="${BACKUP_ROOT:-/data/pg_backup}"
KEEP_WEEKS="${KEEP_WEEKS:-4}"           # 全量备份保留份数

STAMP="$(date +%Y%m%d_%H%M%S)"
TARGET="${BACKUP_ROOT}/base_${STAMP}"

mkdir -p "${TARGET}"

# -Ft tar格式 -z gzip压缩 -Xs 流式接收WAL(备份自身可独立恢复) -P 进度
pg_basebackup -h "${PGHOST}" -p "${PGPORT}" -U "${PGUSER}" \
  -D "${TARGET}" -Ft -z -Xs -P --checkpoint=fast

echo "[$(date '+%F %T')] 全量备份完成: ${TARGET}"

# 轮转: 只保留最近 KEEP_WEEKS 份
ls -1dt "${BACKUP_ROOT}"/base_* 2>/dev/null | tail -n +$((KEEP_WEEKS + 1)) | while read -r old; do
  rm -rf "${old}"
  echo "[$(date '+%F %T')] 轮转删除旧备份: ${old}"
done
