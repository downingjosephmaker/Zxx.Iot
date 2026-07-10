-- =============================================================
-- V0001 回滚：tenant_id → unit_id（多租户重构 P3-A 回滚）
-- 目标库：本机 PostgreSQL 6305/zhjngkdb（分表同库）
-- 用途：V0001__rename_unit_to_tenant.sql 迁移后若需退回，执行本脚本。
--       前提是代码也一并 git revert 回 ColumnName="unit_id"。
--
-- 幂等：仅当 tenant_id 存在且 unit_id 不存在才回改。
-- 注意：C 类 basicunit_info 从未改过，不在回滚范围。
-- =============================================================

DO $$
DECLARE
    fixed_tables text[] := ARRAY[
        'device_info', 'device_param', 'device_comfort',
        'device_alarm_config', 'device_type_run', 'device_type_alarm_config',
        'scada_project', 'dash_project', 'dash_data_pool',
        'admin_mqttparam', 'admin_logoparam', 'sys_related',
        'event_alarm', 'event_control', 'event_history', 'event_run',
        'event_signal', 'event_peak_day', 'event_report_day',
        'event_report_week', 'event_report_month',
        'sys_user', 'push_strategy', 'collect_strategy'
    ];
    t text;
    rec record;
    has_unit boolean;
    has_tenant boolean;
BEGIN
    FOREACH t IN ARRAY fixed_tables LOOP
        SELECT EXISTS(SELECT 1 FROM information_schema.columns
                      WHERE table_schema = current_schema()
                        AND table_name = t AND column_name = 'unit_id') INTO has_unit;
        SELECT EXISTS(SELECT 1 FROM information_schema.columns
                      WHERE table_schema = current_schema()
                        AND table_name = t AND column_name = 'tenant_id') INTO has_tenant;
        IF has_tenant AND NOT has_unit THEN
            EXECUTE format('ALTER TABLE %I RENAME COLUMN tenant_id TO unit_id', t);
            RAISE NOTICE '[回滚] %：tenant_id→unit_id', t;
        ELSE
            RAISE NOTICE '[跳过] %', t;
        END IF;
    END LOOP;

    FOR rec IN
        SELECT c.table_name AS tbl
        FROM information_schema.columns c
        WHERE c.table_schema = current_schema()
          AND c.column_name = 'tenant_id'
          AND NOT EXISTS(SELECT 1 FROM information_schema.columns c2
                         WHERE c2.table_schema = current_schema()
                           AND c2.table_name = c.table_name AND c2.column_name = 'unit_id')
          AND (c.table_name ~ '^event_history_\d+$'
            OR c.table_name ~ '^event_run_\d+$'
            OR c.table_name ~ '^event_signal_\d+$')
    LOOP
        EXECUTE format('ALTER TABLE %I RENAME COLUMN tenant_id TO unit_id', rec.tbl);
        RAISE NOTICE '[回滚·分表] %：tenant_id→unit_id', rec.tbl;
    END LOOP;

    RAISE NOTICE '=== V0001 回滚完成 ===';
END $$;
