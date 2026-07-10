-- =============================================================
-- V0002 回滚：移除 basicunit_info 租户树字段（P3-B 回滚）
-- 目标库：本机 PostgreSQL 6305/zhjngkdb
-- 用途：V0002__tenant_tree.sql 后需退回时执行（配合代码 git revert）。
-- 幂等：列存在才 DROP。
-- =============================================================

DO $$
DECLARE
    tbl text := 'basicunit_info';
    col text;
    cols text[] := ARRAY['parent_id','tree_level','full_code','full_name','has_child'];
BEGIN
    FOREACH col IN ARRAY cols LOOP
        IF EXISTS(SELECT 1 FROM information_schema.columns
                 WHERE table_schema=current_schema() AND table_name=tbl AND column_name=col) THEN
            EXECUTE format('ALTER TABLE %I DROP COLUMN %I', tbl, col);
            RAISE NOTICE '[回滚·删列] %.%', tbl, col;
        END IF;
    END LOOP;
    RAISE NOTICE '=== V0002 回滚完成 ===';
END $$;
