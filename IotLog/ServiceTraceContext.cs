using Serilog.Context;
using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace IotLog
{
    /// <summary>
    /// 采集服务的请求级追踪上下文（供采集服务/插件等非 Web 进程复用）。
    /// <para>同时操作两套 AsyncLocal，确保 Trace 贯穿"接收设备数据→解析处理→上报WebApi"全流程：</para>
    /// <para>1. Serilog LogContext.PushProperty —— 让 LogHelper 的日志自动带上 Trace（Enrich.FromLogContext 读取）</para>
    /// <para>2. 内部 AsyncLocal —— 让 HTTP 上报侧能读到 Trace 注入到 trace-id header</para>
    /// <para>两者都是 AsyncLocal，自动跨 await / Task.Run 传递。</para>
    /// </summary>
    public static class ServiceTraceContext
    {
        private static readonly AsyncLocal<string> _traceId = new AsyncLocal<string>();

        /// <summary>当前逻辑调用链的 Trace（供 HTTP 上报侧读取注入 trace-id header）</summary>
        public static string CurrentTraceId
        {
            get => _traceId.Value;
            set => _traceId.Value = value;
        }

        /// <summary>
        /// 建立 Trace 作用域。同时注入 Serilog LogContext 和内部 AsyncLocal，
        /// using 块内所有日志（LogHelper）和 HTTP 上报调用都自动携带 Trace。
        /// </summary>
        /// <param name="traceId">要设置的 Trace</param>
        /// <returns>用 using 释放的 IDisposable</returns>
        public static System.IDisposable BeginScope(string traceId)
        {
            _traceId.Value = traceId;
            // Serilog LogContext：让 LogHelper（Enrich.FromLogContext）自动读到 Trace
            var logScope = LogContext.PushProperty("Trace", traceId);
            return new ScopeReverter(logScope);
        }

        /// <summary>作用域结束时恢复原 Trace（LogContext 和 AsyncLocal 都恢复）</summary>
        private class ScopeReverter : System.IDisposable
        {
            private readonly System.IDisposable _logScope;
            private bool _disposed;

            public ScopeReverter(System.IDisposable logScope)
            {
                _logScope = logScope;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _logScope?.Dispose();   // 弹出 Serilog LogContext 的 Trace
                    _traceId.Value = null;  // 清除 AsyncLocal
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// 三种 Trace 格式都内嵌起始时间戳 yyyyMMddHHmmssfff：
        ///   模式1/2：yyyyMMddHHmmssfff-设备类型-设备ID
        ///   模式3  ：Controller-yyyyMMddHHmmssfff-设备类型-设备ID
        /// 此处用正则提取第一段 17 位数字，反解为链路起始时间。
        /// </summary>
        private static readonly Regex _startTimeRegex = new Regex(@"(\d{17})", RegexOptions.Compiled);

        /// <summary>从 Trace 中解析链路起始时间；解析失败返回 null。</summary>
        public static DateTime? ParseStartTime(string traceId)
        {
            if (string.IsNullOrEmpty(traceId)) return null;
            var m = _startTimeRegex.Match(traceId);
            if (!m.Success) return null;
            // yyyyMMddHHmmssfff 共17位
            if (DateTime.TryParseExact(m.Groups[1].Value, "yyyyMMddHHmmssfff",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt))
            {
                return dt;
            }
            return null;
        }

        /// <summary>
        /// 计算从 Trace 起始时间到现在的累计耗时（毫秒）。
        /// 供关键节点日志打印"链路耗时=Xms"。Trace 缺失或解析失败返回 null。
        /// </summary>
        public static long? ElapsedMs(string traceId)
        {
            var start = ParseStartTime(traceId);
            if (!start.HasValue) return null;
            return (long)(DateTime.Now - start.Value).TotalMilliseconds;
        }

        /// <summary>
        /// 用设备信息补全当前上下文 Trace（模式1专用）。
        /// 入口仅建立"{时间}"基础 scope；解析出设备后调用本方法，
        /// 追加"-设备类型-设备ID"，使后续日志/HTTP头都带完整 Trace。
        /// 已含设备段的 Trace 不再追加（幂等）。
        /// </summary>
        public static void EnrichWithDevice(int devType, int deviceId)
        {
            string current = CurrentTraceId;
            if (string.IsNullOrEmpty(current)) return;
            // 已含设备段（时间戳之后还有内容）则不重复追加
            var m = _startTimeRegex.Match(current);
            if (!m.Success) return;
            if (current.Length > m.Index + m.Length) return;
            CurrentTraceId = $"{current}-{devType}-{deviceId}";
            // 同步刷新 Serilog LogContext，让后续日志带新 Trace
            LogContext.PushProperty("Trace", CurrentTraceId);
        }

        /// <summary>
        /// 模式2专用：为主动下发/定时采集/轮询的指令生成 Trace。
        /// 格式："yyyyMMddHHmmssfff-设备类型-设备ID"（下发时刻产生）。
        /// </summary>
        public static string BuildForPolling(int devType, int deviceId)
        {
            return $"{DateTime.Now:yyyyMMddHHmmssfff}-{devType}-{deviceId}";
        }

        /// <summary>
        /// 模式3专用：为控制链路拆出的单设备指令生成 Trace。
        /// 格式："Controller-yyyyMMddHHmmssfff-设备类型-设备ID"，
        /// 前缀（Controller-时间）来自上游 traceId，设备类型+ID 在拆单设备时补上。
        /// </summary>
        public static string BuildForControl(string upstreamTraceId, int devType, int deviceId)
        {
            string prefix = string.IsNullOrEmpty(upstreamTraceId)
                ? DateTime.Now.ToString("yyyyMMddHHmmssfff")
                : upstreamTraceId;
            return $"{prefix}-{devType}-{deviceId}";
        }

        /// <summary>
        /// 生成"链路耗时=Xms"片段，供关键节点日志追加。
        /// 从当前上下文 Trace 反解起始时间；解析失败返回空串（不影响原日志）。
        /// </summary>
        public static string ElapsedSegment()
        {
            var ms = ElapsedMs(CurrentTraceId);
            return ms.HasValue ? $"链路耗时={ms.Value}ms" : "";
        }
    }
}
