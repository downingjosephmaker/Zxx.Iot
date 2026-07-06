-- =============================================================
-- V0001 初始化：TimescaleDB 扩展 + 时序专用 schema
-- 依据：docs/IoT平台功能方案.md §8.3（方案A）
-- 前置：PostgreSQL 16/17（避开 17.1，用 17.2+），
--       postgresql.conf 已配置 shared_preload_libraries = 'timescaledb'
-- =============================================================

-- TimescaleDB Community 扩展（TSL 许可，自托管免费合规）
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- 时序区独立 schema：业务表(public) 与时序表同库不同 schema，
-- 保留跨表 JOIN 与事务能力（"PG 一库两用"）
CREATE SCHEMA IF NOT EXISTS iot_ts;

COMMENT ON SCHEMA iot_ts IS '时序数据区(遥测/事件日志/原始报文),30天滚动保留';
