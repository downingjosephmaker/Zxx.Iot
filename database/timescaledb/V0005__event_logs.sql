-- =============================================================
-- V0005 日志表：device_event_log / raw_frame_log（hypertable，30天）
-- 依据：docs/IoT平台功能方案.md §8.3（日志表同策略）
-- 设计要点：
--   1. 主键沿用 SnowModel 雪花ID；hypertable 主键必须包含分区键，
--      故为复合主键 (snow_id, ts)（§8.3 已确认该例外规则）
--   2. raw_frame_log.frame 为 bytea + lz4 行级压缩，块级列存压缩再叠加
--   3. 现有 SqlSugar SplitTable 分表（EventHistory 按日、EventRun/
--      EventSignal 按月）后续迁移并入本体系（§12 迁移行）
-- =============================================================

-- 设备事件日志：上下线/操作/指令审计
CREATE TABLE IF NOT EXISTS iot_ts.device_event_log (
  snow_id    bigint       NOT NULL,              -- 雪花主键(SnowModel.Instance.NewId())
  ts         timestamptz  NOT NULL,              -- 事件时间
  device_id  bigint       NOT NULL DEFAULT 0,    -- 设备主键
  unit_id    int          NOT NULL DEFAULT 0,    -- 单位ID(单位级隔离)
  event_type varchar(30)  NOT NULL DEFAULT '',   -- 事件类型(上线/离线/操作/指令)
  event_reason varchar(50) NOT NULL DEFAULT '',  -- 事件原因(§7.5 原因枚举)
  content    text,                               -- 事件内容(json)
  PRIMARY KEY (snow_id, ts)
);

COMMENT ON TABLE iot_ts.device_event_log IS '设备事件日志(上下线/操作/指令审计,hypertable,30天滚动)';

SELECT create_hypertable('iot_ts.device_event_log', 'ts',
  chunk_time_interval => INTERVAL '1 day',
  if_not_exists       => TRUE);

CREATE INDEX IF NOT EXISTS ix_device_event_log_dt
  ON iot_ts.device_event_log (device_id, ts DESC);

ALTER TABLE iot_ts.device_event_log SET (
  timescaledb.compress,
  timescaledb.compress_segmentby = 'device_id',
  timescaledb.compress_orderby   = 'ts DESC');

SELECT add_compression_policy('iot_ts.device_event_log', INTERVAL '7 days',  if_not_exists => TRUE);
SELECT add_retention_policy  ('iot_ts.device_event_log', INTERVAL '30 days', if_not_exists => TRUE);

-- 原始报文日志：协议排障用
CREATE TABLE IF NOT EXISTS iot_ts.raw_frame_log (
  snow_id    bigint      NOT NULL,               -- 雪花主键
  ts         timestamptz NOT NULL,               -- 收发时间
  channel_id bigint      NOT NULL DEFAULT 0,     -- 通信链路ID(§6.1 channel 表,M2 落地)
  device_id  bigint      NOT NULL DEFAULT 0,     -- 设备主键(可为0=未解析出设备)
  direction  smallint    NOT NULL DEFAULT 0,     -- 方向(0=上行,1=下行)
  frame      bytea,                              -- 原始报文
  PRIMARY KEY (snow_id, ts)
);

COMMENT ON TABLE iot_ts.raw_frame_log IS '原始报文日志(排障用,bytea+lz4,hypertable,30天滚动)';

-- 行级 lz4 压缩(PG14+)，与 TimescaleDB 块级列存压缩叠加
ALTER TABLE iot_ts.raw_frame_log ALTER COLUMN frame SET COMPRESSION lz4;

SELECT create_hypertable('iot_ts.raw_frame_log', 'ts',
  chunk_time_interval => INTERVAL '1 day',
  if_not_exists       => TRUE);

CREATE INDEX IF NOT EXISTS ix_raw_frame_log_dt
  ON iot_ts.raw_frame_log (device_id, ts DESC);

ALTER TABLE iot_ts.raw_frame_log SET (
  timescaledb.compress,
  timescaledb.compress_segmentby = 'device_id',
  timescaledb.compress_orderby   = 'ts DESC');

SELECT add_compression_policy('iot_ts.raw_frame_log', INTERVAL '7 days',  if_not_exists => TRUE);
SELECT add_retention_policy  ('iot_ts.raw_frame_log', INTERVAL '30 days', if_not_exists => TRUE);
