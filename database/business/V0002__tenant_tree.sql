-- =============================================================
-- V0002 租户树：basicunit_info 增加父子层级字段（多租户重构 P3-B）
-- 目标库：本机 PostgreSQL 6305/zhjngkdb
-- 依据：docs/P3多租户物理迁移与租户树细化方案.md §2 P3-B
-- 模板：git show 2dba029^:IotModel/MEntity/Basic/Buildinfo.cs（旧楼栋树字段）
--
-- 设计要点：
--   1. 单位表升级为租户树：新增 parent_id/tree_level/full_code/full_name/has_child。
--   2. 隔离过滤器将从 tenant_id=X 升级为 tenant_id IN(当前+所有子孙)，
--      子孙集用 full_code 祖先链（形如 |1|3|7|）计算（决策 B1：父见子孙）。
--   3. 幂等：列不存在才 ADD；存量数据初始化为"各自独立根租户"。
--   4. 方言：实体层 has_child 声明 bit（MySQL 方言），PG 下由 a0bd175 方言钩子
--      改写为 bool；本脚本直接用 PG 原生类型 boolean，与钩子改写后一致。
--
-- 根节点约定：parent_id=0、tree_level=1、full_code=|{unit_id}|、full_name=unit_name
-- 回滚：DROP COLUMN parent_id/tree_level/full_code/full_name/has_child
-- =============================================================

DO $$
DECLARE
    tbl text := 'basicunit_info';
    fn_exists boolean;
BEGIN
    -- ---- 幂等加列 ----
    IF NOT EXISTS(SELECT 1 FROM information_schema.columns
                 WHERE table_schema=current_schema() AND table_name=tbl AND column_name='parent_id') THEN
        EXECUTE format('ALTER TABLE %I ADD COLUMN parent_id int NOT NULL DEFAULT 0', tbl);
        RAISE NOTICE '[加列] %.parent_id', tbl;
    END IF;
    IF NOT EXISTS(SELECT 1 FROM information_schema.columns
                 WHERE table_schema=current_schema() AND table_name=tbl AND column_name='tree_level') THEN
        EXECUTE format('ALTER TABLE %I ADD COLUMN tree_level int NOT NULL DEFAULT 1', tbl);
        RAISE NOTICE '[加列] %.tree_level', tbl;
    END IF;
    IF NOT EXISTS(SELECT 1 FROM information_schema.columns
                 WHERE table_schema=current_schema() AND table_name=tbl AND column_name='full_code') THEN
        EXECUTE format('ALTER TABLE %I ADD COLUMN full_code varchar(200) NOT NULL DEFAULT '''''', tbl);
        RAISE NOTICE '[加列] %.full_code', tbl;
    END IF;
    IF NOT EXISTS(SELECT 1 FROM information_schema.columns
                 WHERE table_schema=current_schema() AND table_name=tbl AND column_name='full_name') THEN
        EXECUTE format('ALTER TABLE %I ADD COLUMN full_name varchar(400) NOT NULL DEFAULT '''''', tbl);
        RAISE NOTICE '[加列] %.full_name', tbl;
    END IF;
    IF NOT EXISTS(SELECT 1 FROM information_schema.columns
                 WHERE table_schema=current_schema() AND table_name=tbl AND column_name='has_child') THEN
        EXECUTE format('ALTER TABLE %I ADD COLUMN has_child boolean NOT NULL DEFAULT false', tbl);
        RAISE NOTICE '[加列] %.has_child', tbl;
    END IF;

    -- ---- 存量数据初始化：全部视为各自独立的根租户 ----
    -- 仅初始化尚未设置 full_code 的行（幂等，避免覆盖已建好的树）
    EXECUTE format($fmt$
        UPDATE %I
        SET parent_id  = 0,
            tree_level = 1,
            full_code  = '|' || unit_id || '|',
            full_name  = COALESCE(unit_name, ''),
            has_child  = false
        WHERE full_code IS NULL OR full_code = ''
    $fmt$, tbl);
    RAISE NOTICE '[初始化] % 存量数据置为根租户(parent_id=0/tree_level=1/full_code=|unit_id|)', tbl;

    RAISE NOTICE '=== V0002 租户树字段建立完成 ===';
END $$;
