-- =============================================================
-- V0001 业务库列改名：unit_id → tenant_id（多租户重构 P3-A）
-- 目标库：本机 PostgreSQL 6305/zhjngkdb（分表同库）
-- 依据：docs/P3多租户物理迁移与租户树细化方案.md §2 P3-A
-- 方言：PostgreSQL（ALTER TABLE ... RENAME COLUMN，双引号标识符）
--
-- 设计要点：
--   1. 铁律——本脚本必须在 CodeFirst 触发之前、停服窗口内执行。
--      CodeFirst(DbContext.cs:868) 若先跑，会给已改 ColumnName 的实体加一个
--      空 tenant_id 列并保留孤儿 unit_id 列，导致数据丢失。
--   2. RENAME COLUMN 是元数据操作，数据原地保留，零搬运零丢失。
--   3. 全程幂等：可反复执行。每张表先判定 unit_id 在、tenant_id 不在才改名；
--      若 CodeFirst 抢跑误加了空 tenant_id 列，先 DROP 空列再 RENAME（双保险）。
--   4. 分表：event_history(按日 _YYYYMMDD)、event_run/event_signal(按月 _YYYYMM)
--      在同库，用 information_schema 动态枚举所有实例逐个改名。
--
-- 迁移范围（决策 A1：全量 A+B+D 26 张）：
--   A 类·隔离歧视列(12 固定 + 9 事件基表/含分表)  → 改名
--   B 类·保留 UnitId 属性(sys_user/push_strategy/collect_strategy) → 改名
--   D 类·hypertable(iot_ts.device_event_log)        → 见 V0003(TS 兼容单独处理)
--   C 类·basicunit_info 主键 unit_id                → 【不改】保留作 PK
--
-- 回滚：对称执行 RENAME COLUMN tenant_id TO unit_id（或恢复 database/backup 备份）
-- =============================================================

DO $$
DECLARE
    -- A 类固定表 + B 类表（非分表，表名确定）。C 类 basicunit_info 刻意不含在内。
    fixed_tables text[] := ARRAY[
        -- A 类·隔离歧视列（直接映射）
        'device_info', 'device_param', 'device_comfort',
        'device_alarm_config', 'device_type_run', 'device_type_alarm_config',
        'scada_project', 'dash_project', 'dash_data_pool',
        'admin_mqttparam', 'admin_logoparam', 'sys_related',
        -- A 类·事件基表（event_alarm/control/history/run/signal/peak_day/report_*）
        -- 其中 history/run/signal 是分表基名，真实数据在 *_YYYYMMDD/_YYYYMM 实例，
        -- 基表本身若存在也一并处理；分表实例由下方动态段覆盖。
        'event_alarm', 'event_control', 'event_history', 'event_run',
        'event_signal', 'event_peak_day', 'event_report_day',
        'event_report_week', 'event_report_month',
        -- B 类·保留 UnitId 属性（sys_user 牵登录链，push/collect 平台级共享）
        'sys_user', 'push_strategy', 'collect_strategy'
    ];
    t text;
    rec record;
    has_unit boolean;
    has_tenant boolean;
BEGIN
    -- ---- 第一段：固定表改名（幂等 + CodeFirst 误加兜底）----
    FOREACH t IN ARRAY fixed_tables LOOP
        SELECT EXISTS(SELECT 1 FROM information_schema.columns
                      WHERE table_schema = current_schema()
                        AND table_name = t AND column_name = 'unit_id')
          INTO has_unit;
        SELECT EXISTS(SELECT 1 FROM information_schema.columns
                      WHERE table_schema = current_schema()
                        AND table_name = t AND column_name = 'tenant_id')
          INTO has_tenant;

        IF has_unit AND has_tenant THEN
            -- CodeFirst 抢跑误加了空 tenant_id：先删空列，再让 unit_id 改名归位
            EXECUTE format('ALTER TABLE %I DROP COLUMN tenant_id', t);
            EXECUTE format('ALTER TABLE %I RENAME COLUMN unit_id TO tenant_id', t);
            RAISE NOTICE '[修复] %：删除误加空 tenant_id 后改名 unit_id→tenant_id', t;
        ELSIF has_unit AND NOT has_tenant THEN
            EXECUTE format('ALTER TABLE %I RENAME COLUMN unit_id TO tenant_id', t);
            RAISE NOTICE '[改名] %：unit_id→tenant_id', t;
        ELSIF NOT has_unit AND has_tenant THEN
            RAISE NOTICE '[跳过] %：已是 tenant_id', t;
        ELSE
            RAISE NOTICE '[无列] %：无 unit_id/tenant_id 列(或表不存在)', t;
        END IF;
    END LOOP;

    -- ---- 第二段：分表实例动态改名 ----
    -- event_history_YYYYMMDD / event_run_YYYYMM / event_signal_YYYYMM
    -- 凡表名匹配分表前缀且仍持有 unit_id 列的，逐个改名。
    FOR rec IN
        SELECT c.table_name AS tbl,
               EXISTS(SELECT 1 FROM information_schema.columns c2
                      WHERE c2.table_schema = current_schema()
                        AND c2.table_name = c.table_name
                        AND c2.column_name = 'tenant_id') AS t_has_tenant
        FROM information_schema.columns c
        WHERE c.table_schema = current_schema()
          AND c.column_name = 'unit_id'
          AND (c.table_name ~ '^event_history_\d+$'
            OR c.table_name ~ '^event_run_\d+$'
            OR c.table_name ~ '^event_signal_\d+$')
    LOOP
        IF rec.t_has_tenant THEN
            EXECUTE format('ALTER TABLE %I DROP COLUMN tenant_id', rec.tbl);
            EXECUTE format('ALTER TABLE %I RENAME COLUMN unit_id TO tenant_id', rec.tbl);
            RAISE NOTICE '[修复·分表] %：删除误加空 tenant_id 后改名', rec.tbl;
        ELSE
            EXECUTE format('ALTER TABLE %I RENAME COLUMN unit_id TO tenant_id', rec.tbl);
            RAISE NOTICE '[改名·分表] %：unit_id→tenant_id', rec.tbl;
        END IF;
    END LOOP;

    RAISE NOTICE '=== V0001 unit_id→tenant_id 改名完成 ===';
END $$;
