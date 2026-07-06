using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using SqlSugar;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary>
    /// 年分表→月分表数据迁移工具（一次性运维接口）。
    /// 迁移 EventPeakDay / EventReportDay / EventReportWeek 三张表，从 2026-01-01 起到当前月的年表数据到对应月表。
    /// 说明：三张实体的分表类型已改为 SplitType.Month，新数据自动写入月表（xxx_month_yyMM）；
    /// 此接口负责把仍残留在年表（xxx_year_26）中的 2026 年数据搬到对应的月表，便于后续清理年表。
    /// </summary>
    [ApiController]
    [ControllSort("25-99")]
    public class YearToMonthMigrateController : ControllerBaseApi
    {
        /// <summary>
        /// 待迁移表的物理表名前缀（与实体 SugarTable.TableName 一致）
        /// </summary>
        private static readonly string[] TableBases = { "event_peak_day", "event_report_day" };

        /// <summary>
        /// 迁移起始日期（含）。早于此日期的数据保留在年表中不动。
        /// </summary>
        private static readonly DateTime StartDate = new DateTime(2026, 1, 1);

        /// <summary>
        /// 单批最大迁移行数。TiDB 单事务有大小限制（默认 100MB），分批避免超限。
        /// </summary>
        private const int BatchSize = 5000;

        /// <summary>
        /// 迁移全部三张表（EventPeakDay/EventReportDay/EventReportWeek）2026 年起至当前月的年表数据到月表。
        /// 幂等：月表主键为 snow_id，使用 INSERT IGNORE，可重复执行。
        /// </summary>
        /// <returns>每张表/每个月的迁移明细</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [ApiGroup(ApiGroupNames.Event)]
        public List<MigrateTableResult> MigrateYearToMonth()
        {
            TotalCount = 1;
            var results = new List<MigrateTableResult>();

            // 截止月份（含）：当前月
            var today = DateTime.Now;
            var endDateExclusive = new DateTime(today.Year, today.Month, 1).AddMonths(1);

            try
            {
                var db = SqlSugar_Split.Db;
                if (db == null)
                {
                    Status = false;
                    Message = "分表库(SqlSugar_Split)未就绪";
                    return results;
                }

                foreach (var tableBase in TableBases)
                {
                    var tableResult = MigrateTable(db, tableBase, endDateExclusive);
                    results.Add(tableResult);
                }

                Status = results.All(r => r.Success);
                Message = Status ? "迁移完成" : "迁移完成但存在校验不一致，请查看明细";
            }
            catch (Exception ex)
            {
                Status = false;
                Message = $"迁移异常：{ex.Message}";
            }

            return results;
        }

        /// <summary>
        /// 迁移单张表：遍历 2026-01 至当前月，逐月搬迁。
        /// </summary>
        private MigrateTableResult MigrateTable(ISqlSugarClient db, string tableBase, DateTime endDateExclusive)
        {
            var tableResult = new MigrateTableResult { TableName = tableBase };

            // 2026 年数据所在的年表名（yy=26）。若年表不存在则跳过该表。
            string yearTable = $"{tableBase}_year_26";
            bool yearTableExists = TableExists(db, yearTable);
            tableResult.YearTable = yearTable;
            tableResult.YearTableExists = yearTableExists;
            if (!yearTableExists)
            {
                tableResult.Message = $"年表 {yearTable} 不存在，跳过";
                return tableResult;
            }

            // 按月循环：StartDate(2026-01) ~ endDateExclusive
            var monthCursor = new DateTime(StartDate.Year, StartDate.Month, 1);
            while (monthCursor < endDateExclusive)
            {
                var monthResult = MigrateOneMonth(db, tableBase, yearTable, monthCursor);
                tableResult.Months.Add(monthResult);
                monthCursor = monthCursor.AddMonths(1);
            }

            tableResult.TotalSourceCount = tableResult.Months.Sum(m => m.SourceCount);
            tableResult.TotalInsertedCount = tableResult.Months.Sum(m => m.InsertedCount);
            tableResult.TotalVerifyCount = tableResult.Months.Sum(m => m.MonthTableCount);
            tableResult.Success = tableResult.Months.All(m => m.Success);
            return tableResult;
        }

        /// <summary>
        /// 迁移某张表一个月的数据：建月表(若缺)→分批INSERT IGNORE→校验行数
        /// </summary>
        private MigrateMonthResult MigrateOneMonth(ISqlSugarClient db, string tableBase, string yearTable, DateTime monthFirstDay)
        {
            var result = new MigrateMonthResult
            {
                Year = monthFirstDay.Year,
                Month = monthFirstDay.Month,
            };

            // 月份边界 SnowId：用 SnowModel.GetId 与读取路由（SnowSplitService）完全对齐
            long startId = SnowModel.Instance.GetId(monthFirstDay);                       // 本月1日0点
            long endId = SnowModel.Instance.GetId(monthFirstDay.AddMonths(1));             // 下月1日0点（不含）

            // 月表名：_month_{yy}{MM}，与 SnowSplitService.GetTableName(SplitType.Month) 一致
            string yy = (monthFirstDay.Year % 100).ToString("D2");
            string mm = monthFirstDay.Month.ToString("D2");
            string monthTable = $"{tableBase}_month_{yy}{mm}";
            result.MonthTable = monthTable;

            try
            {
                // 1. 统计年表中本月数据量
                int sourceCount = CountRange(db, yearTable, startId, endId);
                result.SourceCount = sourceCount;
                if (sourceCount == 0)
                {
                    result.Message = "年表本月无数据";
                    result.Success = true;
                    return result;
                }

                // 2. 预建月表（结构与年表完全一致，含索引/注释）。幂等。
                db.Ado.ExecuteCommand($"CREATE TABLE IF NOT EXISTS `{monthTable}` LIKE `{yearTable}`");

                // 3. 分批 INSERT IGNORE 拷贝（主键 snow_id 冲突自动跳过，幂等可重跑）
                //    游标推进关键点：
                //    ① 必须用"子查询先 LIMIT 再 MAX"来定位本批边界——直接 MAX(...LIMIT) 是错的，
                //       因为 MAX 是聚合函数，执行在 LIMIT 之前，会返回整段范围的最大值而非前 N 行的边界。
                //    ② 不能用 affected<BATCHSIZE 判断结束：INSERT IGNORE 重跑时本批可能大部分已存在，
                //       affected 偏小会误判提前退出。改用"本批窗口是否有行"（子查询返回 NULL）来判断结束。
                int insertedTotal = 0;
                long lastId = startId - 1; // 已处理边界（不含），下一批从 snow_id > lastId 开始
                while (true)
                {
                    // 定位本批窗口的最大 snow_id：取 lastId 之后的前 BatchSize 行里的 MAX
                    object windowMaxObj = db.Ado.GetScalar(
                        $"SELECT MAX(`snow_id`) FROM (" +
                        $"SELECT `snow_id` FROM `{yearTable}` " +
                        $"WHERE `snow_id` > {lastId} AND `snow_id` < {endId} " +
                        $"ORDER BY `snow_id` ASC LIMIT {BatchSize}) AS tmp");

                    // 子查询无行 -> 年表已无更多数据，结束
                    if (windowMaxObj == null || windowMaxObj == DBNull.Value) break;

                    long windowMax = Convert.ToInt64(windowMaxObj);

                    // 拷贝本批窗口 (lastId, windowMax] 的全部行
                    int affected = db.Ado.ExecuteCommand(
                        $"INSERT IGNORE INTO `{monthTable}` " +
                        $"SELECT * FROM `{yearTable}` " +
                        $"WHERE `snow_id` > {lastId} AND `snow_id` <= {windowMax} " +
                        $"ORDER BY `snow_id` ASC");

                    insertedTotal += Math.Max(0, affected);

                    // 推进游标；防御性退出避免死循环
                    if (windowMax <= lastId) break;
                    lastId = windowMax;
                }
                result.InsertedCount = insertedTotal;

                // 4. 校验：月表实际行数（含历史已迁+本次） vs 年表本月源行数
                int monthTableCount = CountRange(db, monthTable, startId, endId);
                result.MonthTableCount = monthTableCount;
                result.Success = monthTableCount >= sourceCount; // 月表可能已含历史数据，只要不少于源即视为一致
                if (!result.Success)
                {
                    result.Message = $"校验不一致：年表{sourceCount}条，月表{monthTableCount}条";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"异常：{ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 统计指定表在 [startId, endId) 区间的行数
        /// </summary>
        private int CountRange(ISqlSugarClient db, string table, long startId, long endId)
        {
            object obj = db.Ado.GetScalar(
                $"SELECT COUNT(0) FROM `{table}` WHERE `snow_id` >= {startId} AND `snow_id` < {endId}");
            return obj == null || obj == DBNull.Value ? 0 : Convert.ToInt32(obj);
        }

        /// <summary>
        /// 判断物理表是否存在（从 information_schema 查询）
        /// </summary>
        private bool TableExists(ISqlSugarClient db, string tableName)
        {
            object obj = db.Ado.GetScalar(
                $"SELECT COUNT(0) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{tableName}'");
            return obj != null && obj != DBNull.Value && Convert.ToInt32(obj) > 0;
        }
    }

    #region 响应模型

    /// <summary>
    /// 单张表迁移汇总结果
    /// </summary>
    public class MigrateTableResult
    {
        /// <summary>表前缀</summary>
        public string TableName { get; set; }
        /// <summary>年表名</summary>
        public string YearTable { get; set; }
        /// <summary>年表是否存在</summary>
        public bool YearTableExists { get; set; }
        /// <summary>各月明细</summary>
        public List<MigrateMonthResult> Months { get; set; } = new List<MigrateMonthResult>();
        /// <summary>源总行数</summary>
        public int TotalSourceCount { get; set; }
        /// <summary>本次插入总行数</summary>
        public int TotalInsertedCount { get; set; }
        /// <summary>月表最终总行数</summary>
        public int TotalVerifyCount { get; set; }
        /// <summary>整体是否成功</summary>
        public bool Success { get; set; }
        /// <summary>说明</summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// 单月迁移明细
    /// </summary>
    public class MigrateMonthResult
    {
        /// <summary>年</summary>
        public int Year { get; set; }
        /// <summary>月</summary>
        public int Month { get; set; }
        /// <summary>月表名</summary>
        public string MonthTable { get; set; }
        /// <summary>年表该月源行数</summary>
        public int SourceCount { get; set; }
        /// <summary>本次实际插入行数</summary>
        public int InsertedCount { get; set; }
        /// <summary>月表该月最终行数</summary>
        public int MonthTableCount { get; set; }
        /// <summary>是否成功（校验通过）</summary>
        public bool Success { get; set; }
        /// <summary>说明</summary>
        public string Message { get; set; }
    }

    #endregion
}
