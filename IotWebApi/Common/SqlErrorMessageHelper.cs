using System;
using System.Text.RegularExpressions;

namespace IotWebApi
{
    /// <summary>
    /// SQL 异常信息转中文提示助手。
    /// <para>识别常见 MySQL 错误（主键冲突、字段过长、字段不存在、非空约束、重复键等），
    /// 转成用户可读的中文提示，避免把原始 SQL/堆栈暴露给前端。</para>
    /// <para>未识别的错误返回 null，由调用方走通用提示。</para>
    /// </summary>
    public static class SqlErrorMessageHelper
    {
        /// <summary>
        /// 尝试将 SQL 异常转为中文提示。
        /// </summary>
        /// <param name="ex">异常（含 InnerException 链）</param>
        /// <returns>中文提示；无法识别返回 null</returns>
        public static string TryTranslate(Exception ex)
        {
            if (ex == null) return null;

            // 遍历异常链找数据库错误信息（SqlSugarException → InnerException → MySqlException）
            string message = null;
            for (Exception e = ex; e != null; e = e.InnerException)
            {
                message = e.Message;
                if (!string.IsNullOrEmpty(message)) break;
            }
            if (string.IsNullOrEmpty(message)) return null;

            return TranslateMessage(message);
        }

        /// <summary>解析 MySQL 错误消息，返回中文提示</summary>
        private static string TranslateMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return null;
            string lower = msg.ToLower();

            // 1062: Duplicate entry 'xxx' for key 'yyy'（主键/唯一键冲突）
            var dupMatch = Regex.Match(msg, @"Duplicate entry\s+'([^']+)'\s+for key\s+'([^']+)'", RegexOptions.IgnoreCase);
            if (dupMatch.Success)
            {
                return $"数据重复：值\"{dupMatch.Groups[1].Value}\"已存在（{dupMatch.Groups[2].Value}），请勿重复添加";
            }

            // 1406: Data too long for column 'xxx' at row N（字段值过长）
            var longMatch = Regex.Match(msg, @"Data too long for column\s+'([^']+)'", RegexOptions.IgnoreCase);
            if (longMatch.Success)
            {
                return $"字段\"{longMatch.Groups[1].Value}\"的值长度超过限制，请缩短内容";
            }

            // 1048: Column 'xxx' cannot be null（非空约束）
            var nullMatch = Regex.Match(msg, @"Column\s+'([^']+)'\s+cannot be null", RegexOptions.IgnoreCase);
            if (nullMatch.Success)
            {
                return $"字段\"{nullMatch.Groups[1].Value}\"不能为空";
            }

            // 1054: Unknown column 'xxx' in 'field list'（字段不存在）
            var unknownColMatch = Regex.Match(msg, @"Unknown column\s+'([^']+)'", RegexOptions.IgnoreCase);
            if (unknownColMatch.Success)
            {
                return $"字段\"{unknownColMatch.Groups[1].Value}\"不存在，请联系管理员检查";
            }

            // 1052: Column 'xxx' in field list is ambiguous（字段歧义，多表join未指定表名）
            var ambiguousMatch = Regex.Match(msg, @"Column\s+'([^']+)'\s+in\s+field list is ambiguous", RegexOptions.IgnoreCase);
            if (ambiguousMatch.Success)
            {
                return $"查询异常：字段\"{ambiguousMatch.Groups[1].Value}\"存在歧义（多表查询未指明来源表），请联系管理员";
            }

            // 1146: Table 'xxx' doesn't exist（表不存在）
            var tableMatch = Regex.Match(msg, @"Table\s+'([^']+)'\s+doesn't exist", RegexOptions.IgnoreCase);
            if (tableMatch.Success)
            {
                return $"数据表不存在（{tableMatch.Groups[1].Value}），请联系管理员检查";
            }

            // 1452: Cannot add or update a child row: a foreign key constraint fails（外键约束）
            if (lower.Contains("foreign key constraint fails") || lower.Contains("cannot add or update a child row"))
            {
                return $"操作失败：关联数据不存在（外键约束），请检查关联项是否正确";
            }

            // 1364: Field 'xxx' doesn't have a default value（必填字段无默认值）
            var noDefaultMatch = Regex.Match(msg, @"Field\s+'([^']+)'\s+doesn't have a default value", RegexOptions.IgnoreCase);
            if (noDefaultMatch.Success)
            {
                return $"字段\"{noDefaultMatch.Groups[1].Value}\"为必填项，请提供该字段值";
            }

            // 1366: Incorrect string value: 'xxx' for column 'yyy'（编码错误/非法字符）
            var encodingMatch = Regex.Match(msg, @"Incorrect.*?value.*?for column\s+'([^']+)'", RegexOptions.IgnoreCase);
            if (encodingMatch.Success)
            {
                return $"字段\"{encodingMatch.Groups[1].Value}\"包含非法字符，请检查输入内容";
            }

            // 连接类错误
            if (lower.Contains("connection") && (lower.Contains("refused") || lower.Contains("closed") || lower.Contains("lost")))
            {
                return "数据库连接异常，请稍后重试或联系管理员";
            }
            if (lower.Contains("timeout") && lower.Contains("expired"))
            {
                return "操作超时，请稍后重试";
            }

            return null;  // 未识别，返回 null 让调用方走通用提示
        }
    }
}
