using CenBoCommon.Zxx;
using System;
using System.Collections.Generic;

namespace IotModel
{
    public sealed partial class AlarmConfigDAO : DbContext<AlarmConfig>
    {
        private static AlarmConfigDAO instance;
        public static AlarmConfigDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AlarmConfigDAO();
                }
                return instance;
            }
        }
        public override void Init()
        {
            try
            {
                // 告警防抖屏蔽规则字段（CodeFirst.InitTables 对已存在的表不会自动新增列，需手动补列）
                AddColumnIfNotExists("alarm_config", "is_debounce", "bit DEFAULT b'0'", "是否开启防抖屏蔽(0:否1:是)");
                AddColumnIfNotExists("alarm_config", "debounce_type", "int DEFAULT 1", "防抖类型(1:次数型 2:时长型 3:屏蔽)");
                AddColumnIfNotExists("alarm_config", "debounce_seconds", "int DEFAULT 60", "防抖时间窗口(秒)");
                // debounce_mode / debounce_action 已改为枚举(int 存储)：先确保列存在，再统一规整为 int
                AddColumnIfNotExists("alarm_config", "debounce_mode", "int DEFAULT 2", "防抖模式(1:连续 2:累计)");
                AddColumnIfNotExists("alarm_config", "debounce_count", "int DEFAULT 3", "防抖次数阈值");
                AddColumnIfNotExists("alarm_config", "debounce_action", "int DEFAULT 1", "防抖动作(1:第一次 2:最后一次)");
                AddColumnIfNotExists("alarm_config", "alarm_confirm_seconds", "int DEFAULT 0", "告警确认时长(秒)");
                // 若历史库中这两列曾是 varchar，规整为 int 并把中文值映射为枚举值
                NormalizeDebounceEnumColumn("debounce_mode", "连续", 1, "累计", 2, 2);
                // 注意：旧版 debounce_action 有"屏蔽(3)"，重构后屏蔽移到 debounce_type。这里先把"屏蔽"映射到3再交给 MigrateShieldToType 迁移
                NormalizeDebounceEnumColumn("debounce_action", "第一次", 1, "最后一次", 2, 1, thirdText: "屏蔽", thirdValue: 3);
                // 把历史 debounce_action=3(屏蔽) 迁移到 debounce_type=3，并将 action 归位为默认值(第一次)
                MigrateShieldActionToType();
                // 兼容历史库：未填 debounce_type 的，按 alarm_confirm_seconds>0 反推为时长型(2)，否则次数型(1)
                BackfillDebounceType();

                // 种子数据：改用列名式插入，避免加字段后位置式 VALUES 列数不匹配；幂等(仅当 id 不存在时插入)
                // 离线默认开启时长型(DebounceType=2, AlarmConfirmSeconds=1800=30分钟)：持续无数据满30分钟才确认告警
                // 其余默认关闭防抖(IsDebounce=0)，类型为次数型(1)
                // debounce_type: 1=次数型 2=时长型 3=屏蔽；debounce_mode: 2=累计；debounce_action: 1=第一次 2=最后一次
                string seedSql = @"
INSERT INTO `alarm_config`
(`id`, `event_type`, `alarm_grade`, `alarm_type`, `example_formula`, `text_template`, `is_limit`, `is_note`, `is_debounce`, `debounce_type`, `debounce_seconds`, `debounce_mode`, `debounce_count`, `debounce_action`, `alarm_confirm_seconds`, `create_id`, `create_time`, `create_name`, `update_id`, `update_time`, `update_name`)
SELECT 1, '离线', '普通', '通讯状态', NULL, NULL, b'0', b'0', b'1', 2, 60, 2, 1, 1, 1800, 1, '2025-05-17 17:43:18', '开发管理员', 1, '2025-05-17 17:43:18', '开发管理员'
WHERE NOT EXISTS (SELECT 1 FROM `alarm_config` WHERE `id` = 1);
INSERT INTO `alarm_config`
(`id`, `event_type`, `alarm_grade`, `alarm_type`, `example_formula`, `text_template`, `is_limit`, `is_note`, `is_debounce`, `debounce_type`, `debounce_seconds`, `debounce_mode`, `debounce_count`, `debounce_action`, `alarm_confirm_seconds`, `create_id`, `create_time`, `create_name`, `update_id`, `update_time`, `update_name`)
SELECT 2, '掉电', '事故', '通讯状态', NULL, NULL, b'0', b'0', b'0', 1, 60, 2, 3, 1, 0, 1, '2025-05-17 17:43:18', '开发管理员', 1, '2025-05-17 17:43:18', '开发管理员'
WHERE NOT EXISTS (SELECT 1 FROM `alarm_config` WHERE `id` = 2);
INSERT INTO `alarm_config`
(`id`, `event_type`, `alarm_grade`, `alarm_type`, `example_formula`, `text_template`, `is_limit`, `is_note`, `is_debounce`, `debounce_type`, `debounce_seconds`, `debounce_mode`, `debounce_count`, `debounce_action`, `alarm_confirm_seconds`, `create_id`, `create_time`, `create_name`, `update_id`, `update_time`, `update_name`)
SELECT 3, '传感器故障', '严重', '传感器报警', NULL, NULL, b'0', b'0', b'0', 1, 60, 2, 3, 1, 0, 1, '2025-05-17 17:43:18', '开发管理员', 1, '2025-05-17 17:43:18', '开发管理员'
WHERE NOT EXISTS (SELECT 1 FROM `alarm_config` WHERE `id` = 3);
INSERT INTO `alarm_config`
(`id`, `event_type`, `alarm_grade`, `alarm_type`, `example_formula`, `text_template`, `is_limit`, `is_note`, `is_debounce`, `debounce_type`, `debounce_seconds`, `debounce_mode`, `debounce_count`, `debounce_action`, `alarm_confirm_seconds`, `create_id`, `create_time`, `create_name`, `update_id`, `update_time`, `update_name`)
SELECT 4, '超限告警', '严重', '数据异常', 'la > 30', '', b'1', b'0', b'0', 1, 60, 2, 3, 1, 0, 1, '2025-05-17 17:43:18', '开发管理员', 1, '2025-05-17 17:43:18', '开发管理员'
WHERE NOT EXISTS (SELECT 1 FROM `alarm_config` WHERE `id` = 4);";
                Db.Ado.ExecuteCommand(seedSql);

                // 注册防抖补发 Job 到 schedule_job（幂等：仅当不存在该任务类时插入）
                // Program.cs 中 JobInitializer.InitializeJobs 当前被注释，这里兜底注入；QuartzService.StartAsync 会自动加载调度。
                RegisterDebounceFlushJob();
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
            }
        }

        /// <summary>
        /// 幂等新增列：基于 information_schema 判断列是否存在，不存在则 ALTER TABLE ADD COLUMN
        /// </summary>
        private void AddColumnIfNotExists(string tableName, string columnName, string columnDefinition, string description)
        {
            try
            {
                string checkSql = $"SELECT COUNT(1) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}';";
                var exists = Db.Ado.SqlQuerySingle<int>(checkSql);
                if (exists == 0)
                {
                    string alterSql = $"ALTER TABLE `{tableName}` ADD COLUMN `{columnName}` {columnDefinition} COMMENT '{description}';";
                    Db.Ado.ExecuteCommand(alterSql);
                }
            }
            catch
            {
                // 单列失败不阻塞其它列/种子数据（列可能已存在等情况）
            }
        }

        /// <summary>
        /// 规整防抖枚举列为 int：把历史库中可能存在的中文值映射为枚举值，并把列类型强制改为 int。
        /// <para>历史库这两列曾是 varchar(存"连续/累计"/"第一次/最后一次/屏蔽")，需平滑迁移到 int 枚举存储。</para>
        /// <para>newValue1/firstText 主映射；thirdText/thirdValue 为可空第三项(debounce_action 有3个值)。</para>
        /// </summary>
        private void NormalizeDebounceEnumColumn(string columnName, string firstText, int firstValue, string secondText, int secondValue, int defaultValue, string thirdText = null, int thirdValue = 0)
        {
            try
            {
                string checkSql = $"SELECT DATA_TYPE FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'alarm_config' AND COLUMN_NAME = '{columnName}';";
                var dataType = Db.Ado.SqlQuerySingle<string>(checkSql);
                if (string.IsNullOrEmpty(dataType)) return;

                // 把中文/旧字符串值先翻成数字，避免 MODIFY COLUMN 时丢值
                Db.Ado.ExecuteCommand($"UPDATE `alarm_config` SET `{columnName}` = '{firstValue}' WHERE `{columnName}` = '{firstText}';");
                Db.Ado.ExecuteCommand($"UPDATE `alarm_config` SET `{columnName}` = '{secondValue}' WHERE `{columnName}` = '{secondText}';");
                if (!string.IsNullOrEmpty(thirdText))
                {
                    Db.Ado.ExecuteCommand($"UPDATE `alarm_config` SET `{columnName}` = '{thirdValue}' WHERE `{columnName}` = '{thirdText}';");
                }
                // 兜底：含非数字字符 / 空字符串 / NULL 的异常残留，统一置为默认值
                Db.Ado.ExecuteCommand($"UPDATE `alarm_config` SET `{columnName}` = '{defaultValue}' WHERE `{columnName}` IS NULL OR `{columnName}` = '' OR `{columnName}` REGEXP '[^0-9]';");

                // 强制列类型为 int
                if (dataType.ToLower() != "int")
                {
                    Db.Ado.ExecuteCommand($"ALTER TABLE `alarm_config` MODIFY COLUMN `{columnName}` int DEFAULT {defaultValue};");
                }
            }
            catch
            {
                // 规整失败不阻塞主流程(可能已是 int 且无中文残留)
            }
        }

        /// <summary>
        /// 把历史 debounce_action=3(屏蔽) 迁移到 debounce_type=3(屏蔽)，并将 action 归位为默认值(第一次)。
        /// <para>重构后"屏蔽"从动作上移到类型层，老库中 action=3 的行需平移到 type=3。</para>
        /// </summary>
        private void MigrateShieldActionToType()
        {
            try
            {
                // action=3(旧屏蔽) → type=3(屏蔽)，并把 action 归位为 1(第一次)
                Db.Ado.ExecuteCommand("UPDATE `alarm_config` SET `debounce_type` = 3, `debounce_action` = 1 WHERE `debounce_action` = 3;");
            }
            catch
            {
                // 迁移失败不阻塞主流程
            }
        }

        /// <summary>
        /// 兼容历史库：未填/非法的 debounce_type，按 alarm_confirm_seconds &gt; 0 反推为时长型(2)，否则次数型(1)。
        /// </summary>
        private void BackfillDebounceType()
        {
            try
            {
                Db.Ado.ExecuteCommand("UPDATE `alarm_config` SET `debounce_type` = 2 WHERE `debounce_type` IS NULL AND `alarm_confirm_seconds` > 0;");
                Db.Ado.ExecuteCommand("UPDATE `alarm_config` SET `debounce_type` = 1 WHERE `debounce_type` IS NULL;");
                // 越界/非法值兜底(不含合法的 1/2/3)：统一归为次数型(1)
                Db.Ado.ExecuteCommand("UPDATE `alarm_config` SET `debounce_type` = 1 WHERE `debounce_type` NOT IN (1, 2, 3);");
            }
            catch
            {
                // 反推失败不阻塞主流程
            }
        }


        /// <summary>
        /// 注册告警防抖补发 Job 到 schedule_job 表（幂等）
        /// </summary>
        private void RegisterDebounceFlushJob()
        {
            try
            {
                string checkSql = "SELECT COUNT(1) FROM `schedule_job` WHERE `job_class_name` = 'AlarmDebounceFlushJob' AND `job_group_name` = 'System';";
                var exists = Db.Ado.SqlQuerySingle<int>(checkSql);
                if (exists > 0) return;

                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string snowId = SnowModel.Instance.NewId().ToString();
                string insertSql = $@"INSERT INTO `schedule_job`
(`snow_id`, `job_name`, `job_group_name`, `job_class_name`, `job_description`, `trigger_type`, `job_cron`, `interval_seconds`, `job_limit`, `job_status`, `job_log`, `execute_count`, `prev_fire_time`, `next_fire_time`, `create_time`, `update_time`, `create_id`, `update_id`, `create_name`, `update_name`)
VALUES ({snowId}, '告警防抖补发任务', 'System', 'AlarmDebounceFlushJob', '告警防抖补发任务', 0, '0 0/1 * * * ?', 0, 1, 1, 0, 0, NULL, NULL, '{now}', '{now}', 1, 1, '开发管理员', '开发管理员');";
                Db.Ado.ExecuteCommand(insertSql);
            }
            catch
            {
                // schedule_job 表结构差异等情况不阻塞主流程
            }
        }

    }
}
