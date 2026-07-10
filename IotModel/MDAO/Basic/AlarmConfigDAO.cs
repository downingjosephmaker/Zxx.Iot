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
                // 种子数据：参数化插入(跨方言,PG/MySQL 通用)。CodeFirst 已按实体建好全部列,无需补列/迁移。
                // 离线默认开启时长型(DebounceType=时长型, AlarmConfirmSeconds=1800=30分钟)：持续无数据满30分钟才确认告警。
                // 其余默认关闭防抖(IsDebounce=false)，类型为次数型。
                string time = "2025-05-17 17:43:18";
                var list = new List<AlarmConfig>
                {
                    new AlarmConfig { Id = 1, EventType = "离线", AlarmGrade = "普通", AlarmType = "通讯状态", ExampleFormula = null, TextTemplate = null, IsLimit = false, IsNote = false, IsDebounce = true, DebounceType = DebounceTypeEnum.时长型, DebounceSeconds = 60, DebounceMode = DebounceModeEnum.累计, DebounceCount = 1, DebounceAction = DebounceActionEnum.第一次, AlarmConfirmSeconds = 1800, CreateId = 1, CreateTime = time, CreateName = "开发管理员", UpdateId = 1, UpdateTime = time, UpdateName = "开发管理员" },
                    new AlarmConfig { Id = 2, EventType = "掉电", AlarmGrade = "事故", AlarmType = "通讯状态", ExampleFormula = null, TextTemplate = null, IsLimit = false, IsNote = false, IsDebounce = false, DebounceType = DebounceTypeEnum.次数型, DebounceSeconds = 60, DebounceMode = DebounceModeEnum.累计, DebounceCount = 3, DebounceAction = DebounceActionEnum.第一次, AlarmConfirmSeconds = 0, CreateId = 1, CreateTime = time, CreateName = "开发管理员", UpdateId = 1, UpdateTime = time, UpdateName = "开发管理员" },
                    new AlarmConfig { Id = 3, EventType = "传感器故障", AlarmGrade = "严重", AlarmType = "传感器报警", ExampleFormula = null, TextTemplate = null, IsLimit = false, IsNote = false, IsDebounce = false, DebounceType = DebounceTypeEnum.次数型, DebounceSeconds = 60, DebounceMode = DebounceModeEnum.累计, DebounceCount = 3, DebounceAction = DebounceActionEnum.第一次, AlarmConfirmSeconds = 0, CreateId = 1, CreateTime = time, CreateName = "开发管理员", UpdateId = 1, UpdateTime = time, UpdateName = "开发管理员" },
                    new AlarmConfig { Id = 4, EventType = "超限告警", AlarmGrade = "严重", AlarmType = "数据异常", ExampleFormula = "la > 30", TextTemplate = "", IsLimit = true, IsNote = false, IsDebounce = false, DebounceType = DebounceTypeEnum.次数型, DebounceSeconds = 60, DebounceMode = DebounceModeEnum.累计, DebounceCount = 3, DebounceAction = DebounceActionEnum.第一次, AlarmConfirmSeconds = 0, CreateId = 1, CreateTime = time, CreateName = "开发管理员", UpdateId = 1, UpdateTime = time, UpdateName = "开发管理员" },
                };
                // id 是 IsIdentity 自增列,device 表以 alarm_config_id 引用这些固定 id(1..4),
                // 普通 InsertRange 会由 DB 生成 id(显式值被忽略)。
                // SeedOffIdentity 强制写入 1..4 并同步 PG 序列,避免运行期新增告警配置撞种子主键。
                SeedOffIdentity(list);

                // 注册防抖补发 Job 到 schedule_job（参数化,跨方言）
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
        /// 注册告警防抖补发 Job 到 schedule_job 表（参数化,跨方言）
        /// </summary>
        private void RegisterDebounceFlushJob()
        {
            try
            {
                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var job = new ScheduleJob
                {
                    SnowId = SnowModel.Instance.NewId(),
                    JobName = "告警防抖补发任务",
                    JobGroupName = "System",
                    JobClassName = "AlarmDebounceFlushJob",
                    JobDescription = "告警防抖补发任务",
                    TriggerType = 0,
                    JobCron = "0 0/1 * * * ?",
                    IntervalSeconds = 0,
                    JobLimit = 1,
                    JobStatus = 1,
                    JobLog = 0,
                    ExecuteCount = 0,
                    CreateTime = now,
                    UpdateTime = now,
                    CreateId = 1,
                    UpdateId = 1,
                    CreateName = "开发管理员",
                    UpdateName = "开发管理员",
                };
                ScheduleJobDAO.Instance.Insert(job);
            }
            catch
            {
                // schedule_job 表结构差异等情况不阻塞主流程
            }
        }

    }
}
