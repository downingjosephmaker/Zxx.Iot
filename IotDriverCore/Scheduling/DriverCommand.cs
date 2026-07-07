using CenBoCommon.Zxx;
using NewLife.Threading;

namespace IotDriverCore
{
    /// <summary>
    /// 通道指令(自GuoXiang插件CmdInfo泛化,协议无关的一问一答状态机载体;
    /// 采集指令常驻队列按毫秒周期/cron循环调度且超时永不删除,
    /// 控制指令优先发送、超时按RetryLimit重发、超限废弃并回调通知)
    /// </summary>
    public class DriverCommand
    {
        /// <summary>
        /// 指令种类:采集(常驻循环,设备恢复后自动恢复采集)
        /// </summary>
        public const int KindCollect = 1;

        /// <summary>
        /// 指令种类:控制(优先于采集调度)
        /// </summary>
        public const int KindControl = 2;

        /// <summary>
        /// 指令种类(KindCollect/KindControl)
        /// </summary>
        public int CmdKind { get; set; } = KindCollect;

        /// <summary>
        /// 所属通道端点键(TCP服务端为"IP:Port"或配置归一键)
        /// </summary>
        public string Endpoint { get; set; } = "";

        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 设备地址(Modbus从站号/645表地址索引等协议寻址键)
        /// </summary>
        public int DeviceAddr { get; set; }

        /// <summary>
        /// 下发报文原始字节
        /// </summary>
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 应答匹配器(入站帧→是否本指令回执;null=匹配该端点任意飞行指令)
        /// </summary>
        public Func<byte[], bool>? ResponseMatcher { get; set; }

        /// <summary>
        /// 是否等待设备应答(false=发出即完成,如广播校时)
        /// </summary>
        public bool WaitForResponse { get; set; } = true;

        /// <summary>
        /// 应答超时时长(秒)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// 控制指令超时重发上限(次)
        /// </summary>
        public int RetryLimit { get; set; } = 1;

        /// <summary>
        /// 采集周期(毫秒,>0按周期循环;CollectCron非空时以cron为准)
        /// </summary>
        public int CycleMs { get; set; }

        /// <summary>
        /// 低频采集cron表达式(如水表整点抄读,优先于CycleMs)
        /// </summary>
        public string CollectCron { get; set; } = "";

        /// <summary>
        /// 一次性指令(回执或最终超时后从队列删除,控制指令通常为true)
        /// </summary>
        public bool OneShot { get; set; }

        /// <summary>
        /// 命令唯一标识(控制结果回执路由)
        /// </summary>
        public string CommandId { get; set; } = "";

        /// <summary>
        /// 控制类名(回执路由)
        /// </summary>
        public string ClassName { get; set; } = "";

        /// <summary>
        /// 驱动自定义上下文(解析应答时取用,引擎不关心)
        /// </summary>
        public object? Tag { get; set; }

        /// <summary>
        /// 指令状态(0待发 1飞行中 2已回执 3废弃,引擎维护)
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// 累计发送次数
        /// </summary>
        public int SendCount { get; set; }

        /// <summary>
        /// 连续超时次数(收到回执后清零)
        /// </summary>
        public int TimeoutCount { get; set; }

        /// <summary>
        /// 本次发送时刻
        /// </summary>
        public DateTime SendTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 应答超时截止时刻
        /// </summary>
        public DateTime SendDeadline { get; set; } = DateTime.Now;

        /// <summary>
        /// 下次允许发送时刻
        /// </summary>
        public DateTime NextSendTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最近一次应答原始帧
        /// </summary>
        public byte[] ResponseFrame { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 应答接收时刻
        /// </summary>
        public DateTime ReceiveTime { get; set; } = DateTime.Now;

        /// <summary>
        /// cron解析缓存(首次计算时构建)
        /// </summary>
        private Cron? _cron;

        /// <summary>
        /// 计算下次发送时刻(cron优先→毫秒周期→采集指令兜底60秒防误配空转,控制指令返回当前时刻)
        /// </summary>
        public DateTime ComputeNextSendTime(DateTime now)
        {
            if (!CollectCron.IsZxxNullOrEmpty())
            {
                if (_cron == null)
                {
                    var cron = new Cron();
                    if (cron.Parse(CollectCron)) _cron = cron;
                }
                if (_cron != null) return _cron.GetNext(now);
            }
            if (CycleMs > 0) return now.AddMilliseconds(CycleMs);
            return CmdKind == KindCollect ? now.AddMilliseconds(60_000) : now;
        }
    }
}
