-- =============================================================
-- V0002 遥测窄表 telemetry（hypertable，按日分块）
-- 依据：docs/IoT平台功能方案.md §8.3
-- 设计要点：
--   1. 窄表优先：测点动态增减不需 DDL；列存压缩对窄表 value 列效果最好
--   2. 不设单列代理主键：每多一个二级索引，写入吞吐降 20~40%，
--      直接用 (device_id, point_id, ts) 复合寻址
--   3. point_id 为整型点位ID，由 iot_ts.point_map 映射（见 V0003）
-- =============================================================

CREATE TABLE IF NOT EXISTS iot_ts.telemetry (
  device_id  bigint      NOT NULL,             -- 设备主键(对应 device_info.device_id)
  point_id   int         NOT NULL,             -- 点位ID(iot_ts.point_map.point_id)
  ts         timestamptz NOT NULL,             -- 采集时间
  value      float8,                           -- 数值型点位值
  value_str  text,                             -- 状态/字符串型点位值
  quality    smallint    NOT NULL DEFAULT 0    -- 质量戳(0=正常)
);

COMMENT ON TABLE iot_ts.telemetry IS '遥测明细窄表(hypertable,按日分块,30天滚动)';

-- 按日分块的 hypertable（1万行/秒 → 8.64亿行/天，一天一块）
SELECT create_hypertable('iot_ts.telemetry', 'ts',
  chunk_time_interval => INTERVAL '1 day',
  if_not_exists       => TRUE);

-- 唯一的二级索引：设备+点位+时间倒序（曲线查询主路径）
CREATE INDEX IF NOT EXISTS ix_telemetry_dpt
  ON iot_ts.telemetry (device_id, point_id, ts DESC);

-- 列存压缩：按设备分段、时间倒序（压缩比 8~20x）
ALTER TABLE iot_ts.telemetry SET (
  timescaledb.compress,
  timescaledb.compress_segmentby = 'device_id',
  timescaledb.compress_orderby   = 'ts DESC');

-- ---------------------------------------------------------------
-- 策略参数链硬约束（调整任一参数必须保持该次序，否则丢聚合）：
--   cagg end_offset(1h) < cagg start_offset(3d) < 压缩策略(7d) < retention(30d)
-- ---------------------------------------------------------------
SELECT add_compression_policy('iot_ts.telemetry', INTERVAL '7 days',  if_not_exists => TRUE);
SELECT add_retention_policy  ('iot_ts.telemetry', INTERVAL '30 days', if_not_exists => TRUE);
