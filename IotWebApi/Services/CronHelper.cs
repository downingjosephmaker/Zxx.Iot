using System;
using System.Text;

namespace IotWebApi.Services
{
    /// <summary>
    /// Cron表达式生成器
    /// </summary>
    public class CronHelper
    {
        /// <summary>
        /// 创建Cron表达式
        /// </summary>
        /// <param name="seconds">秒，范围：0-59</param>
        /// <param name="minutes">分钟，范围：0-59</param>
        /// <param name="hours">小时，范围：0-23</param>
        /// <param name="dayOfMonth">日期，范围：1-31</param>
        /// <param name="month">月份，范围：1-12</param>
        /// <param name="dayOfWeek">星期，范围：1-7（1=星期日）</param>
        /// <param name="year">年份，可选</param>
        /// <returns>Cron表达式</returns>
        public static string CreateCronExpression(string seconds, string minutes, string hours, string dayOfMonth, string month, string dayOfWeek, string year = "")
        {
            StringBuilder cron = new StringBuilder();
            cron.Append(seconds).Append(" ");
            cron.Append(minutes).Append(" ");
            cron.Append(hours).Append(" ");
            cron.Append(dayOfMonth).Append(" ");
            cron.Append(month).Append(" ");
            cron.Append(dayOfWeek);
            
            if (!string.IsNullOrEmpty(year))
            {
                cron.Append(" ").Append(year);
            }
            
            return cron.ToString();
        }

        /// <summary>
        /// 创建每隔N秒执行一次的Cron表达式
        /// </summary>
        /// <param name="seconds">秒，范围：1-59</param>
        /// <returns>Cron表达式</returns>
        public static string CreateEveryNSecondsExpression(int seconds)
        {
            if (seconds < 1 || seconds > 59)
            {
                throw new ArgumentException("秒数必须在1-59范围内", nameof(seconds));
            }
            
            return $"0/{seconds} * * * * ?";
        }

        /// <summary>
        /// 创建每隔N分钟执行一次的Cron表达式
        /// </summary>
        /// <param name="minutes">分钟，范围：1-59</param>
        /// <returns>Cron表达式</returns>
        public static string CreateEveryNMinutesExpression(int minutes)
        {
            if (minutes < 1 || minutes > 59)
            {
                throw new ArgumentException("分钟数必须在1-59范围内", nameof(minutes));
            }
            
            return $"0 0/{minutes} * * * ?";
        }

        /// <summary>
        /// 创建每隔N小时执行一次的Cron表达式
        /// </summary>
        /// <param name="hours">小时，范围：1-23</param>
        /// <returns>Cron表达式</returns>
        public static string CreateEveryNHoursExpression(int hours)
        {
            if (hours < 1 || hours > 23)
            {
                throw new ArgumentException("小时数必须在1-23范围内", nameof(hours));
            }
            
            return $"0 0 0/{hours} * * ?";
        }

        /// <summary>
        /// 创建每天定时执行的Cron表达式
        /// </summary>
        /// <param name="hour">小时，范围：0-23</param>
        /// <param name="minute">分钟，范围：0-59</param>
        /// <param name="second">秒，范围：0-59</param>
        /// <returns>Cron表达式</returns>
        public static string CreateDailyExpression(int hour, int minute, int second = 0)
        {
            if (hour < 0 || hour > 23)
            {
                throw new ArgumentException("小时必须在0-23范围内", nameof(hour));
            }
            
            if (minute < 0 || minute > 59)
            {
                throw new ArgumentException("分钟必须在0-59范围内", nameof(minute));
            }
            
            if (second < 0 || second > 59)
            {
                throw new ArgumentException("秒必须在0-59范围内", nameof(second));
            }
            
            return $"{second} {minute} {hour} * * ?";
        }

        /// <summary>
        /// 创建每周定时执行的Cron表达式
        /// </summary>
        /// <param name="dayOfWeek">星期几，范围：1-7（1=星期日）</param>
        /// <param name="hour">小时，范围：0-23</param>
        /// <param name="minute">分钟，范围：0-59</param>
        /// <param name="second">秒，范围：0-59</param>
        /// <returns>Cron表达式</returns>
        public static string CreateWeeklyExpression(int dayOfWeek, int hour, int minute, int second = 0)
        {
            if (dayOfWeek < 1 || dayOfWeek > 7)
            {
                throw new ArgumentException("星期几必须在1-7范围内（1=星期日）", nameof(dayOfWeek));
            }
            
            if (hour < 0 || hour > 23)
            {
                throw new ArgumentException("小时必须在0-23范围内", nameof(hour));
            }
            
            if (minute < 0 || minute > 59)
            {
                throw new ArgumentException("分钟必须在0-59范围内", nameof(minute));
            }
            
            if (second < 0 || second > 59)
            {
                throw new ArgumentException("秒必须在0-59范围内", nameof(second));
            }
            
            return $"{second} {minute} {hour} ? * {dayOfWeek}";
        }

        /// <summary>
        /// 创建每月定时执行的Cron表达式
        /// </summary>
        /// <param name="dayOfMonth">日期，范围：1-31</param>
        /// <param name="hour">小时，范围：0-23</param>
        /// <param name="minute">分钟，范围：0-59</param>
        /// <param name="second">秒，范围：0-59</param>
        /// <returns>Cron表达式</returns>
        public static string CreateMonthlyExpression(int dayOfMonth, int hour, int minute, int second = 0)
        {
            if (dayOfMonth < 1 || dayOfMonth > 31)
            {
                throw new ArgumentException("日期必须在1-31范围内", nameof(dayOfMonth));
            }
            
            if (hour < 0 || hour > 23)
            {
                throw new ArgumentException("小时必须在0-23范围内", nameof(hour));
            }
            
            if (minute < 0 || minute > 59)
            {
                throw new ArgumentException("分钟必须在0-59范围内", nameof(minute));
            }
            
            if (second < 0 || second > 59)
            {
                throw new ArgumentException("秒必须在0-59范围内", nameof(second));
            }
            
            return $"{second} {minute} {hour} {dayOfMonth} * ?";
        }

        /// <summary>
        /// 创建在指定日期时间执行一次的Cron表达式
        /// </summary>
        /// <param name="dateTime">执行时间</param>
        /// <returns>Cron表达式</returns>
        public static string CreateOnceExpression(DateTime dateTime)
        {
            return $"{dateTime.Second} {dateTime.Minute} {dateTime.Hour} {dateTime.Day} {dateTime.Month} ? {dateTime.Year}";
        }
    }
} 