using Serilog.Events;
using Serilog.Formatting;
using System;
using System.IO;
using System.Text;

namespace IotLog
{
    /// <summary>
    /// 把 Serilog LogEvent 序列化成与 Vector 处理 Docker 采集日志后等价的 JSON 结构。
    /// <para>背景：Docker 部署的 WebApi 经 Vector 解析 OutputTemplate 文本 → JSON 编码 → 推 Loki，
    /// Loki-Ui 前端按此 JSON 结构显示。独立进程（采集服务等）不走 Docker/Vector，由 Loki sink 直推，
    /// 需要本 formatter 直接输出等价 JSON，使其日志与 Docker 采集的应用在 Loki 里完全同构。</para>
    /// <para>对齐 Vector 的字段（见 infra/vector/vector.toml parse transform）：
    /// ts / level / app / category / trace / api / caller / message / source_type</para>
    /// </summary>
    public sealed class LokiVectorJsonFormatter : ITextFormatter
    {
        /// <summary>
        /// 把单条 LogEvent 格式化成一行 JSON（末尾带 \n，供 Loki sink 批处理按行切分）。
        /// </summary>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            var sb = new StringBuilder(256);
            sb.Append('{');

            // ts：本地时间，格式与 Vector C# 解析的 ts 一致（yyyy-MM-dd HH:mm:ss.fff）
            WriteField(sb, "ts", logEvent.Timestamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            // level 归一化：Information→INFO, Warning→WARN, Error→ERROR, Debug→DEBUG
            // 与 Vector 的 starts_with 逻辑等价
            WriteField(sb, "level", NormalizeLevel(logEvent.Level));

            // 属性字段（小写键名，对齐 Vector JSON；属性缺失时输出空字符串）
            WriteProp(sb, "app", logEvent, "App");
            WriteProp(sb, "category", logEvent, "category");
            WriteProp(sb, "trace", logEvent, "Trace");
            WriteProp(sb, "api", logEvent, "Action");
            WriteProp(sb, "caller", logEvent, "SourceContext");

            // message：渲染消息模板 + 异常堆栈（异常追加在消息后，前端能完整显示）
            // LogHelper 统一用 .Information("{Message}", logMessage) 记录，RenderMessage 会给字符串值
            // 包外层引号（Serilog 对属性占位符的默认渲染）。此处特殊处理：模板恰为 "{Message}" 时
            // 直接取属性原始字符串，避免双引号，与 Vector 解析 OutputTemplate "消息:" 后的纯文本一致。
            string message = RenderMessageRaw(logEvent);
            if (logEvent.Exception != null)
            {
                message = string.IsNullOrEmpty(message)
                    ? logEvent.Exception.ToString()
                    : message + "\n" + logEvent.Exception.ToString();
            }
            WriteField(sb, "message", message);

            // source_type 固定 csharp，对齐 Vector 的 .source_type = "csharp"
            WriteField(sb, "source_type", "csharp", isLast: true);

            sb.Append('}');
            sb.Append('\n');
            output.Write(sb.ToString());
        }

        /// <summary>
        /// 渲染消息为纯文本，避免 Serilog 对字符串属性值包裹外层引号。
        /// <para>LogHelper 统一用模板 "{Message}" 记录，RenderMessage 会输出 "带引号的值"。
        /// 本方法在该特定模板下直接取属性原始值；其他模板回退到 RenderMessage。</para>
        /// </summary>
        private static string RenderMessageRaw(LogEvent logEvent)
        {
            const string messageProp = "Message";
            // 模板恰为 "{Message}" 且存在同名字符串属性 → 直接取原始值，无引号包裹
            if (logEvent.MessageTemplate.Text == "{" + messageProp + "}"
                && logEvent.Properties.TryGetValue(messageProp, out var mv)
                && mv is ScalarValue sv
                && sv.Value is string raw)
            {
                return raw;
            }
            return logEvent.RenderMessage();
        }

        /// <summary>Serilog 等级 → Vector/Loki-Ui 前端用的大写等级</summary>
        private static string NormalizeLevel(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                case LogEventLevel.Debug:
                    return "DEBUG";
                case LogEventLevel.Information:
                    return "INFO";
                case LogEventLevel.Warning:
                    return "WARN";
                case LogEventLevel.Error:
                    return "ERROR";
                case LogEventLevel.Fatal:
                    return "ERROR";   // Fatal 归入 ERROR（前端 LEVEL_CLASS 只认 ERROR/WARN/INFO/DEBUG）
                default:
                    return level.ToString().ToUpperInvariant();
            }
        }

        /// <summary>写一个 JSON 字符串字段：`"key":"escaped value"`（非末尾字段后跟逗号）</summary>
        private static void WriteField(StringBuilder sb, string key, string value, bool isLast = false)
        {
            sb.Append('"').Append(key).Append("\":\"");
            EscapeString(sb, value);
            sb.Append('"');
            if (!isLast) sb.Append(',');
        }

        /// <summary>从 LogEvent 属性取值写字段；属性不存在或非字符串时输出空字符串</summary>
        private static void WriteProp(StringBuilder sb, string jsonKey, LogEvent logEvent, string propName)
        {
            string value = "";
            if (logEvent.Properties != null
                && logEvent.Properties.TryGetValue(propName, out var sv)
                && sv is ScalarValue scalar
                && scalar.Value != null)
            {
                value = scalar.Value.ToString();
            }
            WriteField(sb, jsonKey, value);
        }

        /// <summary>JSON 字符串转义（RFC 8259）："、\、控制字符</summary>
        private static void EscapeString(StringBuilder sb, string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                        {
                            // 其他控制字符转 \uXXXX
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
        }
    }
}
