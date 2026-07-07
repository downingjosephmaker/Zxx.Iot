using System.Diagnostics;
using Jint;
using Jint.Native;
using Jint.Native.Json;

namespace IotDriverCore
{
    /// <summary>
    /// JS协议脚本沙箱(§6.4:Jint 4.x宿主,三段式API=splitFrames/decode/encode;
    /// 四重限制=超时/内存/语句数/CLR互操作禁用(不开AllowClr);
    /// Engine非线程安全,实例内锁串行——长尾私有协议兜底场景低频调用可接受;
    /// 脚本编译一次共享,升级用新实例热切换)
    /// </summary>
    public class ScriptSandbox
    {
        /// <summary>单次调用超时(毫秒)</summary>
        public const int TimeoutMs = 500;

        /// <summary>单引擎内存上限(字节)</summary>
        public const long MemoryLimit = 16 * 1024 * 1024;

        /// <summary>单次调用语句数上限</summary>
        public const int MaxStatements = 100_000;

        /// <summary>
        /// 内部前奏(hex→Uint8Array/JSON解析走纯JS,规避CLR数组封送歧义;__zxx前缀防与用户脚本冲突)
        /// </summary>
        private const string Prelude = @"
function __zxxHexToU8(hex){var n=hex.length>>1;var u=new Uint8Array(n);for(var i=0;i<n;i++){u[i]=parseInt(hex.substr(i*2,2),16);}return u;}
function __zxxParse(j){if(!j||j.length===0)return {};return JSON.parse(j);}";

        private readonly Engine _engine;
        private readonly JsonSerializer _json;
        private readonly List<string> _consoleLogs = new();
        private readonly object _lock = new();

        /// <summary>脚本编译或初始化失败原因(成功为空)</summary>
        public string InitError { get; } = "";

        /// <summary>脚本是否可用</summary>
        public bool Ready => InitError.Length == 0;

        /// <summary>
        /// 构建沙箱并编译脚本(定义三段式函数;编译失败不抛异常,置InitError)
        /// </summary>
        public ScriptSandbox(string script)
        {
            try
            {
                _engine = new Engine(options =>
                {
                    options.TimeoutInterval(TimeSpan.FromMilliseconds(TimeoutMs));
                    options.LimitMemory(MemoryLimit);
                    options.MaxStatements(MaxStatements);
                    options.Strict(false);
                });
                _json = new JsonSerializer(_engine);
                var logs = _consoleLogs;
                _engine.SetValue("console", new ScriptConsole(logs));
                _engine.Execute(Prelude);
                _engine.Execute(script ?? "");
            }
            catch (Exception ex)
            {
                InitError = $"脚本编译失败：{ex.Message}";
            }
        }

        /// <summary>
        /// 脚本是否定义了指定函数
        /// </summary>
        public bool HasFunction(string name)
        {
            if (!Ready) return false;
            lock (_lock)
            {
                try { return !_engine.GetValue(name).IsUndefined(); }
                catch { return false; }
            }
        }

        /// <summary>
        /// 上行解码干跑/执行:decode(Uint8Array帧, context对象)→结果JSON
        /// </summary>
        public ScriptRunResult RunDecode(string framehex, string contextjson)
        {
            return RunFunction("decode", engine =>
            {
                var frame = engine.Invoke("__zxxHexToU8", framehex ?? "");
                var context = engine.Invoke("__zxxParse", contextjson ?? "");
                return engine.Invoke("decode", frame, context);
            });
        }

        /// <summary>
        /// 下行编码干跑/执行:encode(command对象, context对象)→结果JSON
        /// </summary>
        public ScriptRunResult RunEncode(string commandjson, string contextjson)
        {
            return RunFunction("encode", engine =>
            {
                var command = engine.Invoke("__zxxParse", commandjson ?? "");
                var context = engine.Invoke("__zxxParse", contextjson ?? "");
                return engine.Invoke("encode", command, context);
            });
        }

        /// <summary>
        /// 帧定界干跑/执行:splitFrames(Uint8Array缓冲, context对象)→{frames,consumed}JSON
        /// </summary>
        public ScriptRunResult RunSplitFrames(string bufferhex, string contextjson)
        {
            return RunFunction("splitFrames", engine =>
            {
                var buffer = engine.Invoke("__zxxHexToU8", bufferhex ?? "");
                var context = engine.Invoke("__zxxParse", contextjson ?? "");
                return engine.Invoke("splitFrames", buffer, context);
            });
        }

        /// <summary>
        /// 通用函数执行(锁内串行;异常/超时/超限不外抛,收敛为失败结果由调用方降级)
        /// </summary>
        private ScriptRunResult RunFunction(string funcname, Func<Engine, JsValue> invoker)
        {
            var result = new ScriptRunResult { FuncName = funcname };
            if (!Ready)
            {
                result.Error = InitError;
                return result;
            }
            lock (_lock)
            {
                _consoleLogs.Clear();
                var sw = Stopwatch.StartNew();
                try
                {
                    if (_engine.GetValue(funcname).IsUndefined())
                    {
                        result.Error = $"脚本未定义{funcname}函数";
                        return result;
                    }
                    var value = invoker(_engine);
                    result.ResultJson = value.IsUndefined() || value.IsNull()
                        ? ""
                        : _json.Serialize(value, JsValue.Undefined, JsValue.Undefined).ToString();
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.Error = $"脚本执行失败：{ex.Message}";
                }
                finally
                {
                    sw.Stop();
                    result.ElapsedMs = sw.Elapsed.TotalMilliseconds;
                    result.ConsoleLogs = new List<string>(_consoleLogs);
                }
            }
            return result;
        }

        /// <summary>
        /// 注入脚本的console对象(捕获log/warn/error输出供试运行回显)
        /// </summary>
        private class ScriptConsole
        {
            private readonly List<string> _logs;
            public ScriptConsole(List<string> logs) => _logs = logs;

            public void log(object message) => Append("log", message);
            public void warn(object message) => Append("warn", message);
            public void error(object message) => Append("error", message);

            private void Append(string level, object message)
            {
                if (_logs.Count >= 200) return;  // 防脚本刷日志撑爆内存
                _logs.Add($"[{level}] {message}");
            }
        }
    }

    /// <summary>
    /// 脚本执行结果(试运行接口直接回传:结果JSON+console日志+耗时)
    /// </summary>
    public class ScriptRunResult
    {
        /// <summary>执行的函数名</summary>
        public string FuncName { get; set; } = "";
        /// <summary>是否成功</summary>
        public bool Success { get; set; }
        /// <summary>失败原因(编译失败/未定义函数/执行异常/超时超限)</summary>
        public string Error { get; set; } = "";
        /// <summary>返回值JSON(null/undefined为空串)</summary>
        public string ResultJson { get; set; } = "";
        /// <summary>console输出(最多200条)</summary>
        public List<string> ConsoleLogs { get; set; } = new();
        /// <summary>耗时(毫秒)</summary>
        public double ElapsedMs { get; set; }
    }
}
