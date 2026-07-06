using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace IotLog
{
    /// <summary>
    /// 控制台+日志输出（兼容旧 CenboNew.ServiceLog.ConsleWrite）。
    /// <para>对外签名与旧版完全一致，Service.4G_ZT 的 273 处 <c>ConsleWrite.ConsleWriteLine(...)</c>
    /// 调用零改动。内部直接转发给 <see cref="LogHelper"/>（走 Serilog）。</para>
    /// <para>相比旧版去掉了 SQLite 写入逻辑，控制台着色仅在 Windows 下保留（调试便利）。</para>
    /// </summary>
    public class ConsleWrite
    {
        #region 控制台和日志输出

        private static ConcurrentDictionary<Int32, ConsoleColor> dic = new ConcurrentDictionary<Int32, ConsoleColor>();
        private static ConsoleColor[] colors = new ConsoleColor[] {
            ConsoleColor.Green, ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.White, ConsoleColor.Yellow,
            ConsoleColor.DarkGreen, ConsoleColor.DarkCyan, ConsoleColor.DarkMagenta, ConsoleColor.DarkRed, ConsoleColor.DarkYellow };

        /// <summary>
        /// 控制台和日志输出
        /// </summary>
        /// <param name="className">类名</param>
        /// <param name="methodName">方法名</param>
        /// <param name="msg">文本</param>
        /// <param name="datatype">业务分类标签</param>
        /// <param name="logtype">日志类型</param>
        public static void ConsleWriteLine(string className, string methodName, string msg,
            string datatype = "接收", LOG_TYPE logtype = LOG_TYPE.SysLog)
        {
            // 统一转发给 LogHelper（内部走 Serilog），保留旧版的方法签名和行为契约
            if (logtype == LOG_TYPE.ErrorLog)
            {
                LogHelper.ErrorLogWrite(className, methodName, msg, datatype);
            }
            else
            {
                LogHelper.SysLogWrite(className, methodName, msg, datatype);
            }

            // Windows 下保留控制台着色输出（开发调试便利），Linux/容器下跳过（避免额外开销）
            OperatingSystem os = Environment.OSVersion;
            if (os.Platform == PlatformID.Win32NT)
            {
                Task.Run(() =>
                {
                    try
                    {
                        int threadId = Thread.CurrentThread.ManagedThreadId;
                        string strmsg = $"{DateTime.Now.ToString("HH:mm:ss.fff")}  {datatype}[{methodName}]：{msg}";
                        Console.ForegroundColor = logtype == LOG_TYPE.ErrorLog
                            ? ConsoleColor.Red
                            : GetColor(threadId);
                        Console.Write(strmsg + "\n");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    catch
                    {
                        // 控制台输出失败不影响日志落盘（LogHelper 已写入）
                    }
                });
            }
        }

        public static ConsoleColor GetColor(Int32 threadid)
        {
            if (threadid == 1) return ConsoleColor.White;
            try
            {
                return dic.GetOrAdd(threadid, k => colors[dic.Count % colors.Length]);
            }
            catch
            {
            }
            return ConsoleColor.White;
        }

        #endregion
    }

    /// <summary>
    /// 日志类型枚举（兼容旧 CenboNew.ServiceLog.LOG_TYPE）
    /// </summary>
    public enum LOG_TYPE
    {
        SysLog,
        ErrorLog
    }
}
