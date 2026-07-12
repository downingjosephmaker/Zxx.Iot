using CenBoCommon.Zxx;
using IotLog;
using Quartz;
using IotModel;

namespace IotWebApi.Services.Jobs
{
    /// <summary>
    /// 数据库备份任务
    /// </summary>
    [DisallowConcurrentExecution] // 禁止并发执行
    public class BackupDBJob : BaseJob
    {
        /// <summary>
        /// 执行任务
        /// </summary>
        protected override async Task<string> ExecuteJob(IJobExecutionContext context)
        {
            try
            {
                await Task.Delay(10);
                string backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");
                if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);
                string backupFolderdb = Path.Combine(backupFolder, "zhjngkdb");
                var isres = SysRoleDAO.Instance.BackupDataBase(backupFolderdb);

                string backupFoldersplit = Path.Combine(backupFolder, "zhjngkdb_split");
                isres = ScheduleJobLogDAO.Instance.BackupDataBase(backupFoldersplit);

                // 清理30天前的备份文件
                CleanOldBackupFiles(backupFolder, 30);

                return $"数据库备份:{(isres ? "成功" : "失败")}";
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "数据库备份");
                throw; // 将异常抛出，由基类记录错误日志
            }
        }

        /// <summary>
        /// 清理指定天数前的备份文件
        /// </summary>
        /// <param name="backupFolder">备份文件夹路径</param>
        /// <param name="daysToKeep">保留天数</param>
        private void CleanOldBackupFiles(string backupFolder, int daysToKeep)
        {
            try
            {
                if (!Directory.Exists(backupFolder)) return;

                DateTime cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                // 获取所有文件夹
                var directories = Directory.GetDirectories(backupFolder, "*", SearchOption.AllDirectories);
                foreach (string directory in directories.OrderByDescending(d => d.Length))
                {
                    var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                    // 删除过期文件
                    foreach (string file in files)
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            File.Delete(file);
                            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"删除过期备份文件: {file}", "数据库备份");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "数据库备份");
            }
        }

    }
}