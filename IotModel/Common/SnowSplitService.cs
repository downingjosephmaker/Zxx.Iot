using CenBoCommon.Zxx;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IotModel
{
    /// <summary>
    /// 自定义雪花ID分表
    /// </summary>
    public class SnowSplitService : ISplitTableService
    {
        /// <summary>
        /// 返回数据库中所有分表
        /// </summary>
        /// <param name="db"></param>
        /// <param name="EntityInfo"></param>
        /// <param name="tableInfos"></param>
        /// <returns></returns>
        public List<SplitTableInfo> GetAllTables(ISqlSugarClient db, EntityInfo EntityInfo, List<DbTableInfo> tableInfos)
        {
            List<SplitTableInfo> result = new List<SplitTableInfo>();
            foreach (var item in tableInfos)
            {
                if (item.Name.Contains(EntityInfo.DbTableName)) //区分标识如果不用正则符复杂一些，防止找错表
                {
                    SplitTableInfo data = new SplitTableInfo()
                    {
                        TableName = item.Name //要用item.name不要写错了
                    };
                    result.Add(data);
                }
            }
            return result.OrderBy(it => it.TableName).ToList();//打断点看一下有没有查出所有分表
        }

        /// <summary>
        /// 默认表名
        /// </summary>
        /// <param name="db"></param>
        /// <param name="EntityInfo"></param>
        /// <returns></returns>
        public string GetTableName(ISqlSugarClient db, EntityInfo entityInfo)
        {
            return entityInfo.DbTableName;
        }

        public string GetTableName(ISqlSugarClient db, EntityInfo entityInfo, SplitType splitType)
        {
            DateTime time = DateTime.Now;
            string tablename = entityInfo.DbTableName + "_wuxiao";
            if (splitType == SplitType.Week)
            {
                CultureInfo culture = CultureInfo.InvariantCulture; // 或指定 ISO 标准的区域
                CalendarWeekRule weekRule = CalendarWeekRule.FirstFourDayWeek;
                DayOfWeek firstDayOfWeek = DayOfWeek.Monday; // 根据习惯指定每周从星期几开始(如周一或周日)
                string weekNumber = culture.Calendar.GetWeekOfYear(time, weekRule, firstDayOfWeek).ToString().PadLeft(2, '0');
                tablename = entityInfo.DbTableName + $"_week_{time.ToString("yy")}{weekNumber}";
            }
            else if (splitType == SplitType.Day)
            {
                tablename = entityInfo.DbTableName + $"_day_{time.ToString("yyMMdd")}";
            }
            else if (splitType == SplitType.Month)
            {
                tablename = entityInfo.DbTableName + $"_month_{time.ToString("yyMM")}";
            }
            else if (splitType == SplitType.Year)
            {
                tablename = entityInfo.DbTableName + $"_year_{time.ToString("yy")}";
            }
            return tablename;
        }

        public string GetTableName(ISqlSugarClient db, EntityInfo entityInfo, SplitType splitType, object fieldValue)
        {
            string tablename = entityInfo.DbTableName + "_wuxiao";
            if (SnowModel.Instance.TryParse(fieldValue.ToZxxLong(), out DateTime time, out int wid, out int seq))
            {
                if (splitType == SplitType.Week)
                {
                    CultureInfo culture = CultureInfo.InvariantCulture; // 或指定 ISO 标准的区域
                    CalendarWeekRule weekRule = CalendarWeekRule.FirstFourDayWeek;
                    DayOfWeek firstDayOfWeek = DayOfWeek.Monday; // 根据习惯指定每周从星期几开始(如周一或周日)
                    string weekNumber = culture.Calendar.GetWeekOfYear(time, weekRule, firstDayOfWeek).ToString().PadLeft(2, '0');
                    tablename = entityInfo.DbTableName + $"_week_{time.ToString("yy")}{weekNumber}";
                }
                else if (splitType == SplitType.Day)
                {
                    tablename = entityInfo.DbTableName + $"_day_{time.ToString("yyMMdd")}";
                }
                else if (splitType == SplitType.Month)
                {
                    tablename = entityInfo.DbTableName + $"_month_{time.ToString("yyMM")}";
                }
                else if (splitType == SplitType.Year)
                {
                    tablename = entityInfo.DbTableName + $"_year_{time.ToString("yy")}";
                }
            }
            return tablename;
        }

        /// <summary>
        /// 获取分表字段的值
        /// </summary>
        /// <param name="db"></param>
        /// <param name="entityInfo"></param>
        /// <param name="splitType"></param>
        /// <param name="entityValue"></param>
        /// <returns></returns>
        public object GetFieldValue(ISqlSugarClient db, EntityInfo entityInfo, SplitType splitType, object entityValue)
        {
            var splitColumn = entityInfo.Columns.Find(t => t.IsPrimarykey);
            //var splitColumn = entityInfo.Columns.FirstOrDefault(it => it.PropertyInfo.GetCustomAttribute<SplitFieldAttribute>() != null);
            var value = splitColumn?.PropertyInfo.GetValue(entityValue, null);
            return value;
        }

    }
}
