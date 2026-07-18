-- ============================================================================
-- 4.x 外网 MQTT 安全 · 存量库迁移脚本 (PostgreSQL)
-- ----------------------------------------------------------------------------
-- 背景:本项目 CodeFirst 对【已存在表】不自动加列(admin_mqttparam),
--       【新表】靠 DAO 首次访问自动建。存量部署启用内嵌 broker(MqttServerJob) 前
--       必须先跑本脚本,否则 broker 初始化读 admin_mqttparam 直接崩:
--         Npgsql 42703: column "lan_bind_address" does not exist
--       连锁导致 device_mqtt_credential 表也无从建、每设备凭据认证全线不可用。
-- 幂等:全部 IF NOT EXISTS,可重复执行。
-- 运行:docker exec <pg> psql -U postgres -d <业务库> -f 本文件
-- ============================================================================

-- 1. admin_mqttparam 补 Task 4.1 新增的内网绑定地址列
--    (存量表 CodeFirst 不自动加列,必须手工补,否则 broker 起不来)
ALTER TABLE admin_mqttparam ADD COLUMN IF NOT EXISTS lan_bind_address varchar(64) DEFAULT '0.0.0.0';

-- 2. device_mqtt_credential 每设备凭据表
--    CodeFirst 首次访问 DeviceMqttCredentialDAO 时会自动建;此处显式建以免依赖"首次连接时机"。
--    ⚠ is_enable 必须是 boolean(不能是 smallint):
--      实体属性是 C# bool,SqlSugar 查询生成  WHERE is_enable = true ,
--      若列为 smallint 会报  42883: operator does not exist: smallint = boolean ,
--      使每设备凭据认证在 PostgreSQL 上 100% 失败(连正确凭据也连不上)。
--      实体侧已把 ColumnDataType 由 "tinyint" 改为 "bit"(→PG bool),与全项目 bool IsEnable 对齐。
CREATE TABLE IF NOT EXISTS device_mqtt_credential (
    id             serial       PRIMARY KEY,
    mqtt_user      varchar(64)  DEFAULT '',
    pass_hash      varchar(200) DEFAULT '',      -- PBKDF2 哈希(base64)
    salt           varchar(64)  DEFAULT '',      -- 每设备盐(base64)
    device_gateway varchar(30)  DEFAULT '',      -- 绑定 deviceKey,Topic ACL 末段校验用
    is_enable      boolean      DEFAULT true NOT NULL,
    tenant_id      integer      DEFAULT 0    NOT NULL
);

-- 3. ⚠ 硬前置:启用每设备凭据前 mqtt_user 必须全局唯一
--    (broker 回调线程走租户豁免查询,同名跨租户会 FirstOrDefault 进错误 gateway)
CREATE UNIQUE INDEX IF NOT EXISTS ux_device_mqtt_credential_mqtt_user
    ON device_mqtt_credential (mqtt_user);

-- 4. 启用内嵌 broker(默认 job_status=0 停用;存量库若已是 1 可跳过)
UPDATE schedule_job SET job_status = 1
 WHERE job_class_name = 'MqttServerJob' AND job_group_name = 'System';
