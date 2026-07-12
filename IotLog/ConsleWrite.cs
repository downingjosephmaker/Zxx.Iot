namespace IotLog
{
    /// <summary>
    /// 日志输出（兼容旧 IotLog.ConsleWrite）。
    /// <para>对外签名与旧版完全一致，遍布各服务项目的 <c>ConsleWrite.ConsleWriteLine(...)</c>
    /// 调用零改动。内部仅转发给 <see cref="LogHelper"/>（走 Serilog）。</para>
    /// <para>不再手写 Console.Write：控制台/文件输出统一由 <see cref="LogBootstrap"/> 的
    /// Serilog sink 负责，避免重复输出且格式与 <see cref="LogBootstrap.OutputTemplate"/> 一致。</para>
    /// </summary>
    public class ConsleWrite
    {
        /// <summary>
        /// 日志输出（纯转发，控制台/文件输出统一由 LogBootstrap 的 Serilog sink 负责）
        /// </summary>
        /// <param name="className">类名</param>
        /// <param name="methodName">方法名</param>
        /// <param name="msg">文本</param>
        /// <param name="datatype">业务分类标签</param>
        /// <param name="logtype">日志类型</param>
        public static void ConsleWriteLine(string className, string methodName, string msg,
            string datatype = "接收", LOG_TYPE logtype = LOG_TYPE.SysLog)
        {
            // 统一转发给 LogHelper（内部走 Serilog），由 LogBootstrap 的 Console/文件 sink 输出
            if (logtype == LOG_TYPE.ErrorLog)
            {
                LogHelper.ErrorLogWrite(className, methodName, msg, datatype);
            }
            else
            {
                LogHelper.SysLogWrite(className, methodName, msg, datatype);
            }
        }
    }

    /// <summary>
    /// 日志类型枚举（兼容旧 IotLog.LOG_TYPE）
    /// </summary>
    public enum LOG_TYPE
    {
        SysLog,
        ErrorLog
    }
}
