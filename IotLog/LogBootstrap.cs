using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using System;
using System.IO;

namespace IotLog
{
    /// <summary>
    /// Serilog 全局初始化器。
    /// <para>设计目标：替代各项目 Program.cs 里手写的 Serilog 配置，
    /// 让 IotWebApi、采集服务、各插件共用同一套日志输出。</para>
    /// <para>用法：各进程入口调用一次 <see cref="Init(LogOptions)"/>，
    /// 之后 <see cref="LogHelper"/> 的静态调用、ASP.NET 的 ILogger&lt;T&gt;、
    /// 插件里的 LogHelper 全部走同一个全局 Logger（<see cref="Serilog.Log.Logger"/>）。</para>
    /// </summary>
    public static class LogBootstrap
    {
        /// <summary>默认输出模板（中文前缀，贴近原 id4logs 风格；含应用名/category/Trace/Action 上下文字段）</summary>
        /// <para>注意：Serilog 中 TraceId 是保留属性名（被 Activity.TraceId 独占），LogContext/WithProperty 无法覆盖，
        /// 故改用自定义属性名 Trace。WebApi/采集服务全链路统一用 Trace。</para>
        public const string OutputTemplate =
            "[时间:{Timestamp:yyyy-MM-dd HH:mm:ss.fff} 等级:{Level} 应用:{App} 分类:{category} 追踪:{Trace} 接口:{Action}](来源:{SourceContext}){NewLine}消息:{Message:lj}{NewLine}{Exception}";

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
                            .WriteTo.Console(outputTemplate: OutputTemplate)
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
        /// <param name="options">日志选项（AppName/文件大小/数量/控制台/Loki 开关等），传 null 用默认值</param>
        public static void Init(LogOptions options = null)
        {
            // Interlocked 原子标记，避免重复初始化
            if (System.Threading.Interlocked.Exchange(ref _initialized, 1) == 1) return;

            try
            {
                options ??= new LogOptions();
                _logger = Build(options);
                Log.Logger = _logger;   // 同步设置静态门面
            }
            catch
            {
                // 初始化失败时 _logger 保持兜底 Logger，不向上抛
            }
        }

        /// <summary>构建 Logger 配置</summary>
        private static ILogger Build(LogOptions opt)
        {
            string appName = opt.AppName;
            if (string.IsNullOrEmpty(appName)) appName = "app";

            // 本地文件始终生成，控制台始终输出；Loki 根据 LokiUrl 是否配置决定是否推送

            // 解析日志目录：相对路径拼到应用基目录下，绝对路径原样使用
            string logDir = opt.LogDir;
            if (string.IsNullOrEmpty(logDir))
            {
                logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            }
            else if (!Path.IsPathRooted(logDir))
            {
                logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logDir);
            }

            if (!Directory.Exists(logDir))
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
                .Enrich.WithProperty("Trace", "")   // 兜底：非请求线程的日志显示空
                .Enrich.WithProperty("Action", "");    // 兜底：非请求线程（如Job）无接口名

            // ===== Sink 1：全量日志文件（按天 + 按大小滚动）=====
            cfg.WriteTo.Async(a => a.File(
                path: Path.Combine(logDir, $"{appName}-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: opt.RetainedFileCount,
                encoding: System.Text.Encoding.UTF8,
                outputTemplate: OutputTemplate,
                fileSizeLimitBytes: opt.FileSizeLimitMB * 1024L * 1024L,
                rollOnFileSizeLimit: true,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1)));

            // ===== Sink 2：错误日志单独分流（保留更久）=====
            if (opt.ErrorRetainedFileCount > 0)
            {
                cfg.WriteTo.Async(a => a.File(
                    path: Path.Combine(logDir, $"{appName}-error-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: opt.ErrorRetainedFileCount,
                    restrictedToMinimumLevel: LogEventLevel.Error,
                    encoding: System.Text.Encoding.UTF8,
                    outputTemplate: OutputTemplate,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1)));
            }

            // ===== Sink 3：控制台（容器 stdout / 开发调试）=====
            cfg.WriteTo.Async(a => a.Console(outputTemplate: OutputTemplate));

            // ===== Sink 4：可选 Loki（地址非空才启用）=====
            // 用对齐 Vector JSON 的自定义 formatter（LokiVectorJsonFormatter），让独立进程直推的
            // 日志与 Docker+Vector 采集的 WebApi 在 Loki 里完全同构（同字段名、同等级归一化），
            // Loki-Ui 无需区分来源即可统一解析显示。Console/File 仍走 OutputTemplate 中文文本，互不影响。
            if (!string.IsNullOrEmpty(opt.LokiUrl))
            {
                cfg.WriteTo.Async(a => a.GrafanaLoki(
                    uri: opt.LokiUrl,
                    labels: new[] {
                        new LokiLabel { Key = "service", Value = appName }    // Loki-Ui 前端默认选择器 {service="..."} 必需
                    },
                    textFormatter: new LokiVectorJsonFormatter()));
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
    /// <para>文件与控制台 sink 始终启用；Loki 由 LokiUrl 是否配置决定。</para>
    /// </summary>
    public class LogOptions
    {
        /// <summary>应用标识，用作日志文件名前缀和 Loki 标签（如 "iot-webapi"）</summary>
        public string AppName { get; set; } = "app";

        /// <summary>日志目录（默认 "&lt;应用目录&gt;/Logs"）。仅文件 sink 启用时生效</summary>
        public string LogDir { get; set; } = "Logs";

        /// <summary>单个日志文件大小上限（MB），超过则滚动切新文件。默认 20</summary>
        public int FileSizeLimitMB { get; set; } = 20;

        /// <summary>全量日志文件保留个数（按天+按大小滚动，超出则删最旧）。默认 31</summary>
        public int RetainedFileCount { get; set; } = 31;

        /// <summary>错误日志文件保留个数，设为 0 则不单独分流错误日志（错误与全量合并，按"等级:Error"筛选即可）。默认 0</summary>
        public int ErrorRetainedFileCount { get; set; } = 0;

        /// <summary>最低日志级别。默认 Information（生产级）</summary>
        public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

        /// <summary>Loki 地址，留空则不推 Loki（如 "http://loki:3100"）</summary>
        public string LokiUrl { get; set; }
    }
}
