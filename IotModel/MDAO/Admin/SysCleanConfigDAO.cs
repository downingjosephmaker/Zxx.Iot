using CenBoCommon.Zxx;
using System;
using System.Collections.Generic;

namespace IotModel
{
    /// <summary>
    /// 数据清理时间设置DAO
    /// </summary>
    public sealed partial class SysCleanConfigDAO : DbContext<SysCleanConfig>
    {
        private static SysCleanConfigDAO instance;
        public static SysCleanConfigDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SysCleanConfigDAO();
                }
                return instance;
            }
        }

        /// <summary>
        /// 初始化默认配置
        /// </summary>
        public override void Init()
        {
            try
            {
                string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                // 定义默认配置列表
                var defaultConfigs = new List<SysCleanConfig>
                {
                    new SysCleanConfig
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        CleanName = "系统状态检查记录",
                        DataCode = "SysStatusCheck",
                        RetentionDays = 30,
                        IsAutoCleanup = true,
                        CreateTime = time,
                        CreateId = 1,
                        CreateName = "开发管理员",
                        UpdateId = 1,
                        UpdateTime = time,
                        UpdateName = "开发管理员"
                    },
                    new SysCleanConfig
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        CleanName = "用户日志记录",
                        DataCode = "SysuserLog",
                        RetentionDays = 90,
                        IsAutoCleanup = true,
                        CreateTime = time,
                        CreateId = 1,
                        CreateName = "开发管理员",
                        UpdateId = 1,
                        UpdateTime = time,
                        UpdateName = "开发管理员"
                    },
                    new SysCleanConfig
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        CleanName = "操作日志记录",
                        DataCode = "SysyoptLog",
                        RetentionDays = 90,
                        IsAutoCleanup = true,
                        CreateTime = time,
                        CreateId = 1,
                        CreateName = "开发管理员",
                        UpdateId = 1,
                        UpdateTime = time,
                        UpdateName = "开发管理员"
                    },
                    new SysCleanConfig
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        CleanName = "报警记录",
                        DataCode = "EventAlarm",
                        RetentionDays = 365*3,
                        IsAutoCleanup = true,
                        CreateTime = time,
                        CreateId = 1,
                        CreateName = "开发管理员",
                        UpdateId = 1,
                        UpdateTime = time,
                        UpdateName = "开发管理员"
                    },
                    new SysCleanConfig
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        CleanName = "控制日志",
                        DataCode = "EventControl",
                        RetentionDays = 365,
                        IsAutoCleanup = true,
                        CreateTime = time,
                        CreateId = 1,
                        CreateName = "开发管理员",
                        UpdateId = 1,
                        UpdateTime = time,
                        UpdateName = "开发管理员"
                    },
                    new SysCleanConfig
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        CleanName = "运行日志",
                        DataCode = "EventRun",
                        RetentionDays = 365,
                        IsAutoCleanup = true,
                        CreateTime = time,
                        CreateId = 1,
                        CreateName = "开发管理员",
                        UpdateId = 1,
                        UpdateTime = time,
                        UpdateName = "开发管理员"
                    },
                    new SysCleanConfig
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        CleanName = "数据备份文件",
                        DataCode = "DataBackup",
                        RetentionDays = 90,
                        IsAutoCleanup = true,
                        CreateTime = time,
                        CreateId = 1,
                        CreateName = "开发管理员",
                        UpdateId = 1,
                        UpdateTime = time,
                        UpdateName = "开发管理员"
                    }
                };
                InsertRange(defaultConfigs);
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
    }
}