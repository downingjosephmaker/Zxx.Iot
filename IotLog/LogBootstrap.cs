using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace IotLog
{
    /// <summary>
    /// Serilog 全局初始化器。
    /// <para>设计目标：替代各项目 Program.cs 里手写的 Serilog 配置，
    /// 让 ZhjngkWebApi、Service.4G_ZT、各插件共用同一套日志输出。</para>
    /// <para>用法：各进程入口调用一次 <see cref="Init(string, LogOptions)"/>，
    /// 之后 <see cref="LogHelper"/> 的静态调用、ASP.NET 的 ILogger&lt;T&gt;、
    /// 插件里的 LogHelper 全部走同一个全局 Logger（<see cref="Serilog.Log.Logger"/>）。</para>
    /// </summary>
    public static class LogBootstrap
    {
        /// <summary>默认输出模板（中文前缀，贴近原 id4logs 风格；含 category/TraceId/Action 上下文字段）</summary>
        public const string OutputTemplate =
            "[时间:{Timestamp:yyyy-MM-dd HH:mm:ss.fff} 等级:{Level} 分类:{category} 追踪:{TraceId} 接口:{Action}](来源:{SourceContext}){NewLine}消息:{Message:lj}{NewLine}{Exception}";

        /// <summary>控制台输出模板（精简，无日期，容器友好）</summary>
        public const string ConsoleTemplate =
            "[{Timestamp:HH:mm:ss} 等级:{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}";

        private static int _initialized; // 0=未初始化, 1=已初始化（Interlocked 用）
        private static ILogger _logger;

        /// <summary>
        /// 全局 Logger 实例。
        /// 未调用 <see cref="Init"/> 前访问，返回一个只写 Console 的兜底 Logger，绝不抛异常。
        /// </summary>
        public static ILogger Logger
        {
            get
            {
                if (_logger != null) return _logger;
                lock (typeof(LogBootstrap))
                {
                    if (_logger == null)
                    {
                        _logger = new LoggerConfiguration()
                            .MinimumLevel.Debug()
                            .WriteTo.Console(outputTemplate: ConsoleTemplate)
                            .CreateLogger();
                    }
                    return _logger;
                }
            }
        }

        /// <summary>
        /// 检测当前是否运行在 Docker 容器中。
        /// <para>Docker 运行时会在容器根目录创建 /.dockerenv（Linux）；
        /// Microsoft 官方 .NET 镜像会设置 DOTNET_RUNNING_IN_CONTAINER=true 环境变量。
        /// 两者满足其一即判定为容器环境。</para>
        /// </summary>
        public static bool IsRunningInContainer =>
            File.Exists("/.dockerenv")
            || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        /// <summary>
        /// 初始化全局 Logger（自定义配置）。
        /// </summary>
        /// <param name="appName">应用标识</param>
        /// <param name="options">日志选项（文件大小/数量/控制台/Loki 开关等），传 null 用默认值</param>
        public static void Init(string appName, LogOptions options = null)
        {
            // Interlocked 原子标记，避免重复初始化
            if (System.Threading.Interlocked.Exchange(ref _initialized, 1) == 1) return;

            try
            {
                options ??= new LogOptions();
                _logger = Build(appName, options);
                Log.Logger = _logger;   // 同步设置静态门面
            }
            catch
            {
                // 初始化失败时 _logger 保持兜底 Logger，不向上抛
            }
        }

        /// <summary>构建 Logger 配置</summary>
        private static ILogger Build(string appName, LogOptions opt)
        {
            if (string.IsNullOrEmpty(appName)) appName = "app";

            // 决定 Sink 启用策略：未显式配置时，按运行环境自动判断
            // 容器内：默认只输出 stdout（供 docker logs / Alloy 采集），不写本地文件（容器销毁即丢失）
            // 非容器：默认写文件 + 控制台
            bool enableFile = opt.EnableFile ?? !IsRunningInContainer;
            bool enableConsole = opt.EnableConsole ?? true;   // 容器和非容器都默认开控制台
            // Loki 与 stdout 采集互斥原则：若启用了 Loki 直推，应由调用方确保 Alloy 不再重复采集 stdout
            // 这里不做强制互斥，交给部署方决策（文档说明）

            // 解析日志目录：相对路径拼到应用基目录下，绝对路径原样使用
            string logDir = opt.LogDir;
            if (string.IsNullOrEmpty(logDir))
            {
                logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            }
            else if (!Path.IsPathRooted(logDir))
            {
                // 相对路径（如 "id4logs"）→ 拼到应用基目录下，确保位置确定（不依赖工作目录）
                logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logDir);
            }

            if (enableFile && !Directory.Exists(logDir))
            {
                try { Directory.CreateDirectory(logDir); } catch { }
            }

            var cfg = new LoggerConfiguration()
                .MinimumLevel.Is(opt.MinimumLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Internal", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("App", appName)
                .Enrich.WithProperty("TraceId", "")   // 兜底：非请求线程的日志显示空
                .Enrich.WithProperty("Action", "");    // 兜底：非请求线程（如Job）无接口名

            // ===== Sink 1：全量日志文件（按天 + 按大小滚动）=====
            if (enableFile)
            {
                cfg.WriteTo.Async(a => a.File(
                    path: Path.Combine(logDir, "app-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: opt.RetainedFileCount,
                    encoding: System.Text.Encoding.UTF8,
                    outputTemplate: OutputTemplate,
                    fileSizeLimitBytes: opt.FileSizeLimitMB * 1024L * 1024L,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1)));
            }

            // ===== Sink 2：错误日志单独分流（保留更久）=====
            if (enableFile && opt.ErrorRetainedFileCount > 0)
            {
                cfg.WriteTo.Async(a => a.File(
                    path: Path.Combine(logDir, "error-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: opt.ErrorRetainedFileCount,
                    restrictedToMinimumLevel: LogEventLevel.Error,
                    encoding: System.Text.Encoding.UTF8,
                    outputTemplate: OutputTemplate,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1)));
            }

            // ===== Sink 3：控制台（容器 stdout / 开发调试）=====
            if (enableConsole)
            {
                cfg.WriteTo.Async(a => a.Console(outputTemplate: ConsoleTemplate));
            }

            // ===== Sink 4：可选 Loki（地址非空才启用）=====
            if (!string.IsNullOrEmpty(opt.LokiUrl))
            {
                cfg.WriteTo.GrafanaLoki(
                    uri: opt.LokiUrl,
                    labels: new[] { new LokiLabel { Key = "app", Value = appName } },
                    propertiesAsLabels: new[] { "App", "category" });
            }

            return cfg.CreateLogger();
        }

        /// <summary>关闭并刷新 Logger（进程退出时调用，确保缓冲日志落盘）</summary>
        public static void Shutdown()
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// 日志配置选项。所有字段均有默认值，按需覆盖。
    /// <para>未显式设置 EnableFile/EnableConsole 时，按运行环境自动判断：
    /// Docker 容器内默认只输出 stdout；非容器环境默认写文件 + 控制台。</para>
    /// </summary>
    public class LogOptions
    {
        /// <summary>日志目录（默认 "&lt;应用目录&gt;/Logs"）。仅文件 sink 启用时生效</summary>
        public string LogDir { get; set; }

        /// <summary>单个日志文件大小上限（MB），超过则滚动切新文件。默认 20</summary>
        public int FileSizeLimitMB { get; set; } = 20;

        /// <summary>全量日志文件保留数量（按天滚动时的天数）。默认 31</summary>
        public int RetainedFileCount { get; set; } = 101;

        /// <summary>错误日志文件保留数量，设为 0 则不单独分流错误日志（默认关闭，错误与全量合并在 app-*.log，按"等级:Error"筛选即可）。默认 0</summary>
        public int ErrorRetainedFileCount { get; set; } = 0;

        /// <summary>
        /// 是否启用文件 sink。null=自动判断（容器内不写文件，非容器写文件）。
        /// 显式设 true/false 可强制覆盖自动判断。
        /// </summary>
        public bool? EnableFile { get; set; }

        /// <summary>
        /// 是否启用控制台 sink。null=自动判断（默认启用）。
        /// </summary>
        public bool? EnableConsole { get; set; }

        /// <summary>最低日志级别。默认 Information（生产级）</summary>
        public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

        /// <summary>Loki 地址，留空则不推 Loki（如 "http://loki:3100"）</summary>
        public string LokiUrl { get; set; }
    }
}
