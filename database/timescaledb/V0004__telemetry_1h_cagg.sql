-- =============================================================
-- V0004 连续聚合 telemetry_1h（小时级聚合，长期保留）
-- 依据：docs/IoT平台功能方案.md §8.3
-- 注意：CREATE MATERIALIZED VIEW ... WITH (timescaledb.continuous)
--       不能在事务块内执行——本文件必须用 psql -f 直接执行，
--       不要包在 BEGIN/COMMIT 或迁移工具的事务模式里
-- 用途：原始数据 30 天丢弃，小时/日聚合长期保留（报表不受 retention 影响），
--       日/周/月报表数据源改为查询本视图
-- =============================================================

CREATE MATERIALIZED VIEW IF NOT EXISTS iot_ts.telemetry_1h
WITH (timescaledb.continuous) AS
SELECT device_id,
       point_id,
       time_bucket('1 hour', ts) AS bucket,   -- 小时桶
       avg(value)                AS avg_v,    -- 均值
       min(value)                AS min_v,    -- 最小值
       max(value)                AS max_v,    -- 最大值
       last(value, ts)           AS last_v,   -- 时段末值
       count(*)                  AS cnt       -- 样本数
FROM iot_ts.telemetry
GROUP BY device_id, point_id, bucket
WITH NO DATA;

-- 刷新策略（参数链硬约束：end_offset(1h) < start_offset(3d) < 压缩(7d) < retention(30d)，
-- 即聚合刷新永远先于压缩与删除完成；调整任一参数须保持该次序，
-- 否则刷新窗口触及已删数据会丢聚合）
SELECT add_continuous_aggregate_policy('iot_ts.telemetry_1h',
  start_offset      => INTERVAL '3 days',   -- 每次回看 3 天：远早于 30 天 retention
  end_offset        => INTERVAL '1 hour',   -- 最近 1 小时不刷新，避免与实时写入争抢
  schedule_interval => INTERVAL '1 hour',
  if_not_exists     => TRUE);
