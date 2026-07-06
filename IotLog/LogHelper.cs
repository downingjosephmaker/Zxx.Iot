using Serilog;
using System;

namespace IotLog
{
    /// <summary>
    /// 日志助手（兼容旧 CenboNew.ServiceLog.LogHelper）。
    /// <para>对外签名与旧版完全一致，调用点零改动：
    ///   <c>LogHelper.SysLogWrite(className, methodName, message, datatype)</c>
    ///   <c>LogHelper.ErrorLogWrite(className, methodName, message, datatype)</c>
    /// </para>
    /// <para>内部不再写 SQLite/XTrace，统一转发给 <see cref="LogBootstrap.Logger"/>（Serilog）。
    /// 日志产物：&lt;应用目录&gt;/Logs/app-{date}.log（全量）、error-{date}.log（错误分流）。</para>
    /// </summary>
    public class LogHelper
    {
        /// <summary>
        /// 写系统日志（Information 级别）。
        /// 对应旧版 SysLogWrite，用于记录业务流程、接口请求/响应、任务执行等。
        /// </summary>
        /// <param name="className">类名（如 ClassHelper.ClassName 或硬编码字符串）</param>
        /// <param name="methodName">方法名</param>
        /// <param name="logMessage">日志内容</param>
        /// <param name="datatype">业务分类标签（如 "任务调度"、"数据库备份"）</param>
        public static void SysLogWrite(string className, string methodName, string logMessage, string datatype)
        {
            try
            {
                var logger = LogBootstrap.Logger;
                if (logger == null) return;

                // SourceContext 形如 "EquipControlController.GetOptContent"，便于按类/方法过滤
                logger.ForContext("SourceContext", BuildSourceContext(className, methodName))
                      .ForContext("category", datatype ?? "")
                      .Information("{Message}", logMessage);
            }
            catch
            {
                // 桥接层绝不向上抛，避免影响业务主流程
            }
        }

        /// <summary>
        /// 写错误日志（Error 级别）。
        /// 对应旧版 ErrorLogWrite，用于记录异常、业务错误。
        /// </summary>
        public static void ErrorLogWrite(string className, string methodName, string logMessage, string datatype)
        {
            try
            {
                var logger = LogBootstrap.Logger;
                if (logger == null) return;

                logger.ForContext("SourceContext", BuildSourceContext(className, methodName))
                      .ForContext("category", datatype ?? "")
                      .Error("{Message}", logMessage);
            }
            catch
            {
                // 同上，绝不向上抛
            }
        }

        /// <summary>
        /// 构造 SourceContext 标签：优先 "类名.方法名"，两者皆空则返回 "Unknown"。
        /// </summary>
        private static string BuildSourceContext(string className, string methodName)
        {
            if (string.IsNullOrEmpty(className) && string.IsNullOrEmpty(methodName))
                return "Unknown";
            if (string.IsNullOrEmpty(methodName))
                return className;
            if (string.IsNullOrEmpty(className))
                return methodName;
            return $"{className}.{methodName}";
        }

        // ===== 单参数重载：方便从 XTrace.WriteLine/WriteException 快速迁移 =====
        // 自动用调用方的类名做 SourceContext（通过 [CallerFilePath] 推导文件名作为上下文）
        // 用法：原 XTrace.WriteLine(msg) → LogHelper.Info(msg)；原 XTrace.WriteException(ex) → LogHelper.Error(ex)

        /// <summary>写信息日志（单参数，方便迁移 XTrace.WriteLine）</summary>
        public static void Info(string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            try
            {
                LogBootstrap.Logger.ForContext("SourceContext", memberName)
                              .Information("{Message}", message);
            }
            catch { }
        }

        /// <summary>写错误日志（单参数，方便迁移 XTrace.WriteException）</summary>
        public static void Error(Exception exception, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            try
            {
                LogBootstrap.Logger.ForContext("SourceContext", memberName)
                              .Error(exception, "{Message}", exception.Message);
            }
            catch { }
        }
    }
}
