#!/usr/bin/env bash
# =============================================================
# 磁盘水位巡检脚本(85%告警 / 95%熔断)
# 依据: docs/IoT实施计划.md M1.5 / 方案 §8
# 用法: crontab 每5分钟执行
#   */5 * * * * /opt/iot/database/backup/disk_watermark_check.sh
# 熔断动作按部署环境二选一(默认只落熔断标记文件,由运维接管):
#   1) 标记文件: 应用/采集侧检测到标记文件后暂停遥测写入
#   2) 直接停服: systemctl stop <采集服务> (需按环境改服务名)
# =============================================================
set -euo pipefail

DATA_MOUNT="${DATA_MOUNT:-/data}"                 # PG 数据盘挂载点
WARN_PERCENT="${WARN_PERCENT:-85}"                # 告警水位
BREAK_PERCENT="${BREAK_PERCENT:-95}"              # 熔断水位
BREAKER_FILE="${BREAKER_FILE:-/var/run/iot_telemetry_breaker}"
WEBHOOK_URL="${WEBHOOK_URL:-}"                    # 告警Webhook(钉钉/企微,留空只写日志)

USED_PERCENT="$(df -P "${DATA_MOUNT}" | awk 'NR==2 {gsub("%","",$5); print $5}')"

notify() {
  local msg="$1"
  echo "[$(date '+%F %T')] ${msg}"
  if [ -n "${WEBHOOK_URL}" ]; then
    curl -fsS -m 10 -H 'Content-Type: application/json' \
      -d "{\"msgtype\":\"text\",\"text\":{\"content\":\"${msg}\"}}" "${WEBHOOK_URL}" || true
  fi
}

if [ "${USED_PERCENT}" -ge "${BREAK_PERCENT}" ]; then
  touch "${BREAKER_FILE}"
  notify "【熔断】数据盘 ${DATA_MOUNT} 使用率 ${USED_PERCENT}% ≥ ${BREAK_PERCENT}%,已落熔断标记 ${BREAKER_FILE},请立即扩容或清理"
elif [ "${USED_PERCENT}" -ge "${WARN_PERCENT}" ]; then
  notify "【告警】数据盘 ${DATA_MOUNT} 使用率 ${USED_PERCENT}% ≥ ${WARN_PERCENT}%,请关注容量趋势"
else
  # 水位回落自动解除熔断标记
  if [ -f "${BREAKER_FILE}" ]; then
    rm -f "${BREAKER_FILE}"
    notify "【恢复】数据盘 ${DATA_MOUNT} 使用率回落至 ${USED_PERCENT}%,熔断标记已解除"
  fi
fi
