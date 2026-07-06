-- =============================================================
-- V0003 最新值旁路表 + 点位映射表
-- 依据：docs/IoT平台功能方案.md §8.3
-- 设计要点：
--   1. telemetry_latest 为普通表(非 hypertable)+Redis 缓存，
--      实时面板 99% 查询只打它，不扫时序表（ThingsBoard/IoTSharp 共同验证）
--   2. point_map 是 §12 表清单的必要补充：现有 device_param 为
--      "一设备一行"，点位藏在 expand_json 内仅有字符串 param_code，
--      无整型点位ID；telemetry 窄表的 point_id 由本表映射生成，
--      写入器侧做内存缓存，一次解析终身复用
-- =============================================================

-- 点位映射：设备+参数编码 → 整型点位ID
CREATE TABLE IF NOT EXISTS iot_ts.point_map (
  point_id   int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,  -- 整型点位ID
  device_id  bigint       NOT NULL,                         -- 设备主键
  param_code varchar(100) NOT NULL,                         -- 参数编码(device_param 扩展内的 ParamCode)
  param_name varchar(100) NOT NULL DEFAULT '',              -- 参数名称(冗余,便于排查)
  created_at timestamptz  NOT NULL DEFAULT now(),           -- 创建时间
  CONSTRAINT uq_point_map UNIQUE (device_id, param_code)
);

COMMENT ON TABLE iot_ts.point_map IS '点位映射表(device_id+param_code→point_id,写入器内存缓存)';

-- 最新值旁路表：主键即 (设备,点位)，UPSERT 更新
CREATE TABLE IF NOT EXISTS iot_ts.telemetry_latest (
  device_id  bigint      NOT NULL,             -- 设备主键
  point_id   int         NOT NULL,             -- 点位ID
  ts         timestamptz NOT NULL,             -- 最后采集时间
  value      float8,                           -- 数值型点位值
  value_str  text,                             -- 状态/字符串型点位值
  quality    smallint    NOT NULL DEFAULT 0,   -- 质量戳(0=正常)
  PRIMARY KEY (device_id, point_id)
);

COMMENT ON TABLE iot_ts.telemetry_latest IS '最新值旁路表(实时查询专用,不扫时序表)';
