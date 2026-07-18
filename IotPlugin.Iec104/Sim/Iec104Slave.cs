using System.Net;
using System.Net.Sockets;
using IotDriverCore;
using IotDriverCore.Simulation;

namespace IotPlugin.Iec104.Sim
{
    /// <summary>
    /// IEC104从站模拟器(子站侧:监听2404等待主站拨入,应答STARTDT/TESTFR/总召唤,
    /// 周期突发上报COT=3,遥控45/46/50支持SBO选择后执行;
    /// ⚠铁律:本文件编解码独立实现,不引用插件主体Iec104FrameHelper——
    /// 对抗性验证要求从站与主站两套编解码互为裁判,共用代码即退化为自证)
    /// </summary>
    public sealed class Iec104Slave : IDisposable
    {
        private readonly int _port;
        private readonly List<SlavePoint> _points = new();
        private readonly List<Session> _sessions = new();
        private readonly object _sessionLock = new();
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        /// <summary>突发上报周期(秒,测试可调大避免干扰确定性断言)</summary>
        public int SpontaneousIntervalS { get; set; } = 5;

        /// <summary>日志回调</summary>
        public Action<string>? OnLog { get; set; }

        /// <summary>
        /// 从站点位运行态(Ca=设备地址,Ioa=点位Di,Ti=FuncCode)
        /// </summary>
        private sealed class SlavePoint
        {
            public int Ca;
            public int Ioa;
            public byte Ti;
            public IValueGenerator Generator = null!;
            public double Scale = 1;
            /// <summary>当前显示值(生成器产出或遥控写入)</summary>
            public double Current;
            /// <summary>遥控写死后不再随生成器刷新</summary>
            public bool Overridden;
        }

        /// <summary>
        /// 单主站会话(从站自己的收发序号与启动态)
        /// </summary>
        private sealed class Session
        {
            public TcpClient Client = null!;
            public NetworkStream Stream = null!;
            public readonly List<byte> Buffer = new();
            public readonly object SendLock = new();
            /// <summary>从站发送序号V(S)</summary>
            public int Vs;
            /// <summary>从站接收序号V(R)(=已收I帧数)</summary>
            public int Vr;
            public bool Started;
            /// <summary>SBO已选择的命令键(ti,ioa),执行须与之匹配</summary>
            public (byte Ti, int Ioa)? Selected;
        }

        public Iec104Slave(int port, IEnumerable<SimDevice> devices)
        {
            _port = port;
            foreach (var device in devices)
            {
                int ca = int.TryParse((device.Address ?? "").Trim(), out var a) ? a : 1;
                foreach (var pm in device.Points)
                {
                    byte ti = (byte)(pm.FuncCode > 0 ? pm.FuncCode : 13);
                    _points.Add(new SlavePoint
                    {
                        Ca = ca,
                        Ioa = (int)ParseUint(pm.Di),
                        Ti = ti,
                        Generator = GeneratorFactory.Create(pm.Generator),
                        Scale = pm.Scale <= 0 ? 1 : pm.Scale
                    });
                }
            }
        }

        /// <summary>
        /// 启动从站监听与突发上报循环
        /// </summary>
        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
            _ = Task.Run(() => SpontaneousLoopAsync(_cts.Token));
            OnLog?.Invoke($"IEC104从站监听启动，端口 {_port}，公共地址 [{string.Join(",", _points.Select(p => p.Ca).Distinct().OrderBy(c => c))}]，{_points.Count}个点位");
        }

        /// <summary>
        /// 停止从站并断开所有会话
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
            try { _listener?.Stop(); } catch { }
            lock (_sessionLock)
            {
                foreach (var s in _sessions)
                {
                    try { s.Client.Close(); } catch { }
                }
                _sessions.Clear();
            }
        }

        public void Dispose() => Stop();

        #region 会话与接收

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var client = await _listener!.AcceptTcpClientAsync(token);
                    var session = new Session { Client = client, Stream = client.GetStream() };
                    lock (_sessionLock) { _sessions.Add(session); }
                    OnLog?.Invoke($"主站接入:{client.Client.RemoteEndPoint}");
                    _ = Task.Run(() => ReceiveLoopAsync(session, token));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!token.IsCancellationRequested) OnLog?.Invoke($"监听异常:{ex.Message}"); }
        }

        private async Task ReceiveLoopAsync(Session session, CancellationToken token)
        {
            var buf = new byte[4096];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int count = await session.Stream.ReadAsync(buf, token);
                    if (count <= 0) break;
                    session.Buffer.AddRange(buf.Take(count));
                    ProcessBuffer(session);
                }
            }
            catch { }
            finally
            {
                lock (_sessionLock) { _sessions.Remove(session); }
                try { session.Client.Close(); } catch { }
                OnLog?.Invoke("主站会话断开");
            }
        }

        /// <summary>
        /// 缓冲区拆帧(0x68+长度域,半包留待,起始符前噪声丢弃)
        /// </summary>
        private void ProcessBuffer(Session session)
        {
            while (true)
            {
                int idx = session.Buffer.IndexOf(0x68);
                if (idx < 0) { session.Buffer.Clear(); return; }
                if (idx > 0) session.Buffer.RemoveRange(0, idx);
                if (session.Buffer.Count < 2) return;
                int len = session.Buffer[1];
                if (len < 4 || len > 253) { session.Buffer.RemoveAt(0); continue; }
                int total = len + 2;
                if (session.Buffer.Count < total) return;
                var frame = session.Buffer.Take(total).ToArray();
                session.Buffer.RemoveRange(0, total);
                HandleFrame(session, frame);
            }
        }

        /// <summary>
        /// 帧分派:控制域首字节低2位判型(bit0=0为I帧/01为S帧/11为U帧)
        /// </summary>
        private void HandleFrame(Session session, byte[] frame)
        {
            byte c1 = frame[2];
            if ((c1 & 0x01) == 0)
            {
                // I帧:推进接收序号,回S帧确认后处理ASDU
                session.Vr = ((((c1 >> 1) | (frame[3] << 7)) & 0x7FFF) + 1) % 32768;
                Send(session, BuildS(session.Vr));
                HandleAsdu(session, frame.Skip(6).ToArray());
                return;
            }
            if ((c1 & 0x03) == 0x01) return;   // S帧:模拟器不做发送窗口管理,收下即可
            switch (c1)
            {
                case 0x07:   // STARTDT_act
                    session.Started = true;
                    Send(session, BuildU(0x0B));
                    OnLog?.Invoke("STARTDT完成，数据传输已激活");
                    break;
                case 0x13:   // STOPDT_act
                    session.Started = false;
                    Send(session, BuildU(0x23));
                    break;
                case 0x43:   // TESTFR_act
                    Send(session, BuildU(0x83));
                    break;
            }
        }

        #endregion

        #region ASDU处理(总召唤/遥控)

        private void HandleAsdu(Session session, byte[] asdu)
        {
            if (asdu.Length < 9) return;
            byte ti = asdu[0];
            byte cot = (byte)(asdu[2] & 0x3F);
            int ca = asdu[4] | (asdu[5] << 8);
            int ioa = asdu[6] | (asdu[7] << 8) | (asdu[8] << 16);

            if (ti == 100 && cot == 6)
            {
                HandleInterrogation(session, ca, asdu.Length > 9 ? asdu[9] : (byte)20);
                return;
            }
            if (ti == 103 && cot == 6)
            {
                // 时钟同步:镜像激活确认
                Send(session, BuildI(session, MirrorAsdu(asdu, 7, negative: false)));
                return;
            }
            if (ti is 45 or 46 or 50 && cot == 6)
            {
                HandleCommand(session, ti, ca, ioa, asdu);
            }
        }

        /// <summary>
        /// 总召唤应答:激活确认→全点位COT=20逐帧上送→激活终止(方案§4.5时序的从站侧)
        /// </summary>
        private void HandleInterrogation(Session session, int ca, byte qoi)
        {
            var points = _points.Where(p => p.Ca == ca).ToList();
            if (!points.Any())
            {
                // 未知公共地址:否定确认
                Send(session, BuildI(session, BuildAsdu(100, 7, ca, 0, new[] { qoi }, negative: true)));
                return;
            }
            Send(session, BuildI(session, BuildAsdu(100, 7, ca, 0, new[] { qoi })));
            var now = DateTime.Now;
            foreach (var point in points)
            {
                if (!point.Overridden) point.Current = point.Generator.Next(now);
                Send(session, BuildI(session, BuildDataAsdu(point, 20)));
            }
            Send(session, BuildI(session, BuildAsdu(100, 10, ca, 0, new[] { qoi })));
            OnLog?.Invoke($"总召唤应答完成，CA={ca}，{points.Count}个点位");
        }

        /// <summary>
        /// 遥控/设定值:S/E=1选择→记忆并镜像确认;S/E=0执行→须与已选择匹配(SBO)或直接执行,
        /// 写入点位值后镜像激活确认+激活终止;未知IOA否定确认
        /// </summary>
        private void HandleCommand(Session session, byte ti, int ca, int ioa, byte[] asdu)
        {
            var point = _points.Find(p => p.Ca == ca && p.Ioa == ioa);
            if (point == null)
            {
                Send(session, BuildI(session, MirrorAsdu(asdu, 7, negative: true)));
                OnLog?.Invoke($"遥控否定:未知点位 CA={ca} IOA={ioa}");
                return;
            }
            byte qualifier = asdu[asdu.Length - 1];
            bool select = (qualifier & 0x80) != 0;
            if (select)
            {
                session.Selected = (ti, ioa);
                Send(session, BuildI(session, MirrorAsdu(asdu, 7, negative: false)));
                OnLog?.Invoke($"遥控选择确认 TI={ti} IOA={ioa}");
                return;
            }
            // 执行:曾有选择则必须匹配(SBO语义);无选择视为直接执行模式放行
            if (session.Selected != null && session.Selected != (ti, ioa))
            {
                Send(session, BuildI(session, MirrorAsdu(asdu, 7, negative: true)));
                OnLog?.Invoke($"遥控否定:执行与选择不匹配 TI={ti} IOA={ioa}");
                return;
            }
            session.Selected = null;
            double value = ti switch
            {
                45 => qualifier & 0x01,
                46 => (qualifier & 0x03) == 2 ? 1 : 0,
                _ => BitConverter.ToSingle(asdu, 9) * point.Scale
            };
            point.Current = value;
            point.Overridden = true;
            Send(session, BuildI(session, MirrorAsdu(asdu, 7, negative: false)));
            Send(session, BuildI(session, MirrorAsdu(asdu, 10, negative: false)));
            OnLog?.Invoke($"遥控执行成功 TI={ti} IOA={ioa} 值={value}");
        }

        #endregion

        #region 突发上报

        /// <summary>
        /// 周期突发上报(COT=3):生成器刷新全部点位并向所有已启动会话逐帧上送
        /// </summary>
        private async Task SpontaneousLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(SpontaneousIntervalS * 1000, token);
                    var now = DateTime.Now;
                    List<Session> sessions;
                    lock (_sessionLock) { sessions = _sessions.Where(s => s.Started).ToList(); }
                    if (!sessions.Any()) continue;
                    foreach (var point in _points)
                    {
                        if (!point.Overridden) point.Current = point.Generator.Next(now);
                        foreach (var session in sessions)
                        {
                            Send(session, BuildI(session, BuildDataAsdu(point, 3)));
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { OnLog?.Invoke($"突发上报异常:{ex.Message}"); }
        }

        #endregion

        #region 从站侧编码(独立实现,不引用Iec104FrameHelper)

        private void Send(Session session, byte[] frame)
        {
            try
            {
                lock (session.SendLock) { session.Stream.Write(frame, 0, frame.Length); }
            }
            catch { }
        }

        private static byte[] BuildU(byte ctrl) => new byte[] { 0x68, 0x04, ctrl, 0x00, 0x00, 0x00 };

        private static byte[] BuildS(int vr) => new byte[]
        {
            0x68, 0x04, 0x01, 0x00, (byte)((vr & 0x7F) << 1), (byte)(vr >> 7)
        };

        /// <summary>
        /// 包I帧并推进从站发送序号(须在session.SendLock外只被单线程构建——
        /// 本类所有BuildI调用点均在接收循环或突发循环单线程内,Vs无竞争)
        /// </summary>
        private static byte[] BuildI(Session session, byte[] asdu)
        {
            int vs;
            lock (session.SendLock) { vs = session.Vs; session.Vs = (session.Vs + 1) % 32768; }
            var frame = new byte[6 + asdu.Length];
            frame[0] = 0x68;
            frame[1] = (byte)(4 + asdu.Length);
            frame[2] = (byte)((vs & 0x7F) << 1);
            frame[3] = (byte)(vs >> 7);
            frame[4] = (byte)((session.Vr & 0x7F) << 1);
            frame[5] = (byte)(session.Vr >> 7);
            Array.Copy(asdu, 0, frame, 6, asdu.Length);
            return frame;
        }

        /// <summary>
        /// 通用单信息体ASDU(源发站地址0)
        /// </summary>
        private static byte[] BuildAsdu(byte ti, byte cot, int ca, int ioa, byte[] element, bool negative = false)
        {
            var asdu = new byte[9 + element.Length];
            asdu[0] = ti;
            asdu[1] = 0x01;
            asdu[2] = (byte)((cot & 0x3F) | (negative ? 0x40 : 0x00));
            asdu[3] = 0x00;
            asdu[4] = (byte)(ca & 0xFF);
            asdu[5] = (byte)(ca >> 8);
            asdu[6] = (byte)(ioa & 0xFF);
            asdu[7] = (byte)((ioa >> 8) & 0xFF);
            asdu[8] = (byte)((ioa >> 16) & 0xFF);
            element.CopyTo(asdu, 9);
            return asdu;
        }

        /// <summary>
        /// 镜像命令ASDU改写COT与P/N位(命令确认帧=原帧回显)
        /// </summary>
        private static byte[] MirrorAsdu(byte[] asdu, byte cot, bool negative)
        {
            var mirror = (byte[])asdu.Clone();
            mirror[2] = (byte)((cot & 0x3F) | (negative ? 0x40 : 0x00));
            return mirror;
        }

        /// <summary>
        /// 按点位TI编码监视数据元素(原始值=显示值/Scale,与主站解码语义互逆;
        /// 带时标TI追加CP56Time2a)
        /// </summary>
        private static byte[] BuildDataAsdu(SlavePoint point, byte cot)
        {
            double raw = point.Current / point.Scale;
            byte[] element = point.Ti switch
            {
                1 => new[] { (byte)(point.Current != 0 ? 1 : 0) },
                3 => new[] { (byte)(point.Current != 0 ? 2 : 1) },
                9 => EncodeNva(raw),
                11 => EncodeSva(raw),
                13 => EncodeFloat(raw),
                30 => new[] { (byte)(point.Current != 0 ? 1 : 0) }.Concat(EncodeCp56(DateTime.Now)).ToArray(),
                31 => new[] { (byte)(point.Current != 0 ? 2 : 1) }.Concat(EncodeCp56(DateTime.Now)).ToArray(),
                34 => EncodeNva(raw).Concat(EncodeCp56(DateTime.Now)).ToArray(),
                36 => EncodeFloat(raw).Concat(EncodeCp56(DateTime.Now)).ToArray(),
                _ => EncodeFloat(raw)
            };
            return BuildAsdu(point.Ti, cot, point.Ca, point.Ioa, element);
        }

        /// <summary>归一化值NVA(int16小端+QDS,显示值经Scale还原为满量程比例后放大32768)</summary>
        private static byte[] EncodeNva(double raw)
        {
            short nva = (short)Math.Clamp(Math.Round(raw * 32768), short.MinValue, short.MaxValue);
            return new[] { (byte)(nva & 0xFF), (byte)((nva >> 8) & 0xFF), (byte)0x00 };
        }

        /// <summary>标度化值SVA(int16小端+QDS)</summary>
        private static byte[] EncodeSva(double raw)
        {
            short sva = (short)Math.Clamp(Math.Round(raw), short.MinValue, short.MaxValue);
            return new[] { (byte)(sva & 0xFF), (byte)((sva >> 8) & 0xFF), (byte)0x00 };
        }

        /// <summary>短浮点(IEEE754小端4B+QDS)</summary>
        private static byte[] EncodeFloat(double raw)
        {
            var bytes = new byte[5];
            BitConverter.GetBytes((float)raw).CopyTo(bytes, 0);
            return bytes;
        }

        /// <summary>CP56Time2a七字节(独立实现:毫秒2B|分|时|日+星期|月|年)</summary>
        private static byte[] EncodeCp56(DateTime t)
        {
            int ms = t.Second * 1000 + t.Millisecond;
            int dow = (int)t.DayOfWeek == 0 ? 7 : (int)t.DayOfWeek;
            return new byte[]
            {
                (byte)(ms & 0xFF), (byte)(ms >> 8),
                (byte)(t.Minute & 0x3F), (byte)(t.Hour & 0x1F),
                (byte)((t.Day & 0x1F) | (dow << 5)),
                (byte)(t.Month & 0x0F), (byte)((t.Year - 2000) & 0x7F)
            };
        }

        private static uint ParseUint(string text)
        {
            text = (text ?? "").Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Convert.ToUInt32(text[2..], 16);
            return uint.TryParse(text, out var v) ? v : 0;
        }

        #endregion
    }
}
