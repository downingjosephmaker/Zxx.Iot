namespace IotPlugin.Iec104
{
    /// <summary>
    /// SBO选择后执行的挂起控制(方案§4.6:先选择等激活确认再执行,电力遥控普遍强制)
    /// </summary>
    internal sealed class PendingControl
    {
        /// <summary>平台命令ID(控制结果回执路由)</summary>
        public string CommandId = "";

        /// <summary>目标设备ID</summary>
        public int DeviceId;

        /// <summary>目标设备名(回执展示)</summary>
        public string DeviceName = "";

        /// <summary>命令类型标识(45/46/50,用于匹配确认帧)</summary>
        public byte Ti;

        /// <summary>信息体地址IOA(用于匹配确认帧)</summary>
        public int Ioa;

        /// <summary>是否处于选择阶段(收到选择确认后转执行阶段)</summary>
        public bool SelectPhase;

        /// <summary>执行帧ASDU(选择确认后下发;直接执行模式下即首发帧)</summary>
        public byte[] ExecuteAsdu = Array.Empty<byte>();

        /// <summary>发起时刻(超时判定)</summary>
        public DateTime StartTime = DateTime.Now;
    }

    /// <summary>
    /// 单链路状态机(方案§2/§4.4:STARTDT启动态+收发序号窗口k/w+t1~t3定时基准;
    /// t0连接建立超时由TcpClientChannelPool的连接与退避承担;
    /// 本类不含线程同步,由插件对每端点state加锁访问)
    /// </summary>
    internal sealed class Iec104StateMachine
    {
        /// <summary>STARTDT已确认,数据传输已启动</summary>
        public bool Started;

        /// <summary>下一发送序号V(S)</summary>
        public int Vs;

        /// <summary>下一期望接收序号V(R)</summary>
        public int Vr;

        /// <summary>已收未确认的I帧数(达w须发S帧——方案§4.4)</summary>
        public int RecvSinceAck;

        /// <summary>首个未确认接收帧的到达时刻(t2超时发S帧基准,null=无欠账)</summary>
        public DateTime? FirstUnackedRx;

        /// <summary>最近收到任何帧的时刻(t3空闲基准)</summary>
        public DateTime LastRx = DateTime.Now;

        /// <summary>最近发出任何帧的时刻(t3空闲基准)</summary>
        public DateTime LastTx = DateTime.Now;

        /// <summary>STARTDT_act发出时刻(等待con,t1超时断链重连)</summary>
        public DateTime? StartDtSent;

        /// <summary>TESTFR_act发出时刻(等待con,t1超时断链重连)</summary>
        public DateTime? TestFrSent;

        /// <summary>最近一次总召唤发出时刻(周期重召基准)</summary>
        public DateTime LastGi = DateTime.MinValue;

        /// <summary>已发未被对端确认的I帧(序号+发出时刻,t1超时与k窗口判定)</summary>
        public readonly Queue<(int Seq, DateTime Sent)> PendingI = new();

        /// <summary>挂起的遥控/设定值命令(单链路同时最多一条)</summary>
        public PendingControl? Control;

        /// <summary>
        /// 取下一发送序号并推进V(S)
        /// </summary>
        public int NextSendSeq()
        {
            int seq = Vs;
            Vs = (Vs + 1) % Iec104FrameHelper.SeqModulo;
            return seq;
        }

        /// <summary>
        /// 发送窗口是否允许再发I帧(未确认帧数达k即停发——方案§4.4)
        /// </summary>
        public bool CanSendI(int k) => PendingI.Count < k;

        /// <summary>
        /// 处理对端接收序号N(R)确认(确认所有序号小于nr的已发帧;
        /// 模32768回绕比较,距离超4096视为陈旧序号不消费)
        /// </summary>
        public void OnAck(int nr)
        {
            while (PendingI.Count > 0)
            {
                int dist = (nr - PendingI.Peek().Seq + Iec104FrameHelper.SeqModulo) % Iec104FrameHelper.SeqModulo;
                if (dist >= 1 && dist <= 4096) PendingI.Dequeue();
                else break;
            }
        }

        /// <summary>
        /// 复位为初始态(连接建立/断开时调用;LastGi不复位由重连后STARTDT_con触发总召唤)
        /// </summary>
        public void Reset()
        {
            Started = false;
            Vs = 0;
            Vr = 0;
            RecvSinceAck = 0;
            FirstUnackedRx = null;
            LastRx = DateTime.Now;
            LastTx = DateTime.Now;
            StartDtSent = null;
            TestFrSent = null;
            PendingI.Clear();
            Control = null;
        }
    }
}
