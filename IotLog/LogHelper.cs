using System;

namespace IotLog
{
    /// <summary>
    /// 日志助手（兼容旧 IotLog.LogHelper）。
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
        public static void SysLogWrite(string className, string methodName, string logMessage, string datatype = "")
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
        public static void ErrorLogWrite(string className, string methodName, string logMessage, string datatype = "")
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

        /// <summary>
        /// 开启日志链路作用域（TraceId 上下文）。
        /// <para>返回的 IDisposable 在 Dispose 前，当前异步上下文内的所有 LogHelper 调用都会自动携带该 TraceId
        /// 和 Action（由 LogBootstrap 的 Enrich.FromLogContext 实现，配合输出模板的 {TraceId}/{Action} 字段）。</para>
        /// <para>典型用法（业务链路入口处）：</para>
        /// <code>
        /// using var scope = LogHelper.BeginScope($"MQA-{SnowModel.Instance.NewId()}", "AlarmDataChuLi");
        /// // 此处及下游 await/Task.Run 链路内的所有日志自动带 TraceId
        /// </code>
        /// <para>注意：Serilog LogContext 基于 AsyncLocal，<c>await</c> 异步等待可自动流转；
        /// fire-and-forget 的 <c>Task.Run</c>（非 await）若跨出 scope 生命周期，需在 Task 内重新建 scope。</para>
        /// </summary>
        /// <param name="traceId">链路标识（建议带业务前缀，如 MQA-/MQO-/RPT-/HTTP-）</param>
        /// <param name="action">可选的业务接口名（填充 {Action} 字段，便于按接口过滤）</param>
        /// <returns>IDisposable 作用域；traceId 为空时返回 null（调用方 using null 安全）</returns>
        public static IDisposable BeginScope(string traceId, string action = null)
        {
            if (string.IsNullOrEmpty(traceId)) return null;
            try
            {
                var scope1 = Serilog.Context.LogContext.PushProperty("Trace", traceId);
                if (!string.IsNullOrEmpty(action))
                {
                    var scope2 = Serilog.Context.LogContext.PushProperty("Action", action);
                    return new ScopeChain(scope1, scope2);
                }
                return scope1;
            }
            catch
            {
                // LogContext.PushProperty 在未配置 Enrich.FromLogContext 时会抛，
                // 桥接层绝不向上抛，返回 null（using null 安全）
                return null;
            }
        }

        /// <summary>协助多个 IDisposable 一起 Dispose 的小工具</summary>
        private sealed class ScopeChain : IDisposable
        {
            private readonly IDisposable _first;
            private readonly IDisposable _second;
            public ScopeChain(IDisposable first, IDisposable second) { _first = first; _second = second; }
            public void Dispose()
            {
                try { _second?.Dispose(); }
                finally { _first?.Dispose(); }
            }
        }
    }
}
