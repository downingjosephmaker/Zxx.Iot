-- =============================================================
-- retention 演练脚本（M1.1 验收判据：retention 缩短为 1 小时演练
-- drop_chunks 生效；docs/IoT实施计划.md 任务 1.1）
-- 用法：psql 连接目标库后逐段手工执行（含需要人工代入 job_id 的步骤），
--       演练完成后务必执行最后的"恢复 30 天"段
-- 注意：仅在演练/测试库执行，不要在已有生产数据的库上跑
-- =============================================================

-- ① 将 telemetry 的 retention 临时缩短为 1 小时
SELECT remove_retention_policy('iot_ts.telemetry', if_exists => TRUE);
SELECT add_retention_policy   ('iot_ts.telemetry', INTERVAL '1 hour');

-- ② 插入一条 2 小时前的测试数据（落在"应删除"的旧 chunk 内）
INSERT INTO iot_ts.telemetry (device_id, point_id, ts, value, quality)
VALUES (999999999, 1, now() - INTERVAL '2 hours', 1.23, 0);

-- ③ 记录当前 chunk 清单（应能看到旧时间片的 chunk）
SELECT show_chunks('iot_ts.telemetry');

-- ④ 查出 retention 后台任务的 job_id
SELECT j.job_id, j.proc_name, j.config
FROM timescaledb_information.jobs j
WHERE j.proc_name = 'policy_retention'
  AND j.hypertable_schema = 'iot_ts'
  AND j.hypertable_name   = 'telemetry';

-- ⑤ 手动触发该任务（把上一步查到的 job_id 代入）
-- CALL run_job(<job_id>);

-- ⑥ 复查 chunk 清单：旧 chunk 应已被整块 DROP（元数据操作，
--    无死元组、无 vacuum 风暴——这正是选 drop_chunks 的原因）
SELECT show_chunks('iot_ts.telemetry');

-- ⑦ 【必须执行】恢复 30 天 retention
SELECT remove_retention_policy('iot_ts.telemetry', if_exists => TRUE);
SELECT add_retention_policy   ('iot_ts.telemetry', INTERVAL '30 days');

-- ⑧ 复核策略已恢复
SELECT j.job_id, j.proc_name, j.config
FROM timescaledb_information.jobs j
WHERE j.proc_name = 'policy_retention'
  AND j.hypertable_schema = 'iot_ts'
  AND j.hypertable_name   = 'telemetry';
