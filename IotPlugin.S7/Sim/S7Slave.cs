using System.Net;
using System.Net.Sockets;
using IotDriverCore;
using IotDriverCore.Simulation;

namespace IotPlugin.S7.Sim
{
    /// <summary>
    /// S7comm服务端模拟器(手写S7.Net客户端实际使用的报文子集:
    /// COTP连接请求应答/Setup Communication PDU协商/ReadVar 0x04/WriteVar 0x05含位写;
    /// DB/M/I/Q四区内存托管,点位按生成器周期刷新(S7大端),客户端写入的区域停止刷新;
    /// 服务端编码手写而客户端为S7netplus库,两套实现天然独立,对抗性验证成立)
    /// </summary>
    public sealed class S7Slave : IDisposable
    {
        private readonly int _port;
        private readonly List<SlavePoint> _points = new();
        private readonly object _memLock = new();

        /// <summary>M/I/Q区各64KB;DB区按DB号懒分配64KB</summary>
        private readonly byte[] _m = new byte[65536];
        private readonly byte[] _i = new byte[65536];
        private readonly byte[] _q = new byte[65536];
        private readonly Dictionary<int, byte[]> _db = new();

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        /// <summary>日志回调</summary>
        public Action<string>? OnLog { get; set; }

        /// <summary>
        /// 模拟点位运行态(Area=1DB/2M/3I/4Q,Db仅DB区有效,ByteAddr=区内字节地址)
        /// </summary>
        private sealed class SlavePoint
        {
            public int Area;
            public int Db;
            public int ByteAddr;
            public int BitOffset;
            public string DataType = "uint16";
            public int ByteLen;
            public IValueGenerator Generator = null!;
            public double Scale = 1;
            /// <summary>客户端写入覆盖后不再随生成器刷新</summary>
            public bool Overridden;
        }

        public S7Slave(int port, IEnumerable<SimDevice> devices)
        {
            _port = port;
            foreach (var device in devices)
            {
                foreach (var pm in device.Points)
                {
                    int addr = (int)ParseUint(pm.Di);
                    string type = (pm.DataType ?? "uint16").Trim().ToLowerInvariant();
                    _points.Add(new SlavePoint
                    {
                        Area = pm.FuncCode is >= 1 and <= 4 ? pm.FuncCode : 1,
                        Db = addr / 1_000_000,
                        ByteAddr = addr % 1_000_000,
                        BitOffset = Math.Max(0, pm.BitOffset),
                        DataType = type,
                        ByteLen = type switch
                        {
                            "bool" or "bit" or "byte" => 1,
                            "int32" or "uint32" or "float32" => 4,
                            _ => 2
                        },
                        Generator = GeneratorFactory.Create(pm.Generator),
                        Scale = pm.Scale <= 0 ? 1 : pm.Scale
                    });
                }
            }
        }

        /// <summary>
        /// 启动监听与点位刷新循环
        /// </summary>
        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _cts = new CancellationTokenSource();
            RefreshPoints();   //先刷一轮,连接即可读到值
            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
            _ = Task.Run(() => RefreshLoopAsync(_cts.Token));
            OnLog?.Invoke($"S7从站监听启动，端口 {_port}，{_points.Count}个点位");
        }

        /// <summary>
        /// 停止从站
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
            try { _listener?.Stop(); } catch { }
        }

        public void Dispose() => Stop();

        #region 会话与TPKT收帧

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var client = await _listener!.AcceptTcpClientAsync(token);
                    OnLog?.Invoke($"客户端接入:{client.Client.RemoteEndPoint}");
                    _ = Task.Run(() => SessionLoopAsync(client, token));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!token.IsCancellationRequested) OnLog?.Invoke($"监听异常:{ex.Message}"); }
        }

        /// <summary>
        /// 单客户端会话:按TPKT长度域拆帧逐帧处理
        /// </summary>
        private async Task SessionLoopAsync(TcpClient client, CancellationToken token)
        {
            var stream = client.GetStream();
            var buffer = new List<byte>();
            var chunk = new byte[4096];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int count = await stream.ReadAsync(chunk, token);
                    if (count <= 0) break;
                    buffer.AddRange(chunk.Take(count));
                    while (buffer.Count >= 4)
                    {
                        if (buffer[0] != 0x03) { buffer.Clear(); break; }   //非TPKT,丢弃
                        int total = (buffer[2] << 8) | buffer[3];
                        if (total < 7 || total > 8192) { buffer.Clear(); break; }
                        if (buffer.Count < total) break;
                        var frame = buffer.Take(total).ToArray();
                        buffer.RemoveRange(0, total);
                        var reply = HandleFrame(frame);
                        if (reply != null) await stream.WriteAsync(reply, token);
                    }
                }
            }
            catch { }
            finally
            {
                try { client.Close(); } catch { }
                OnLog?.Invoke("客户端会话断开");
            }
        }

        #endregion

        #region 报文处理(COTP/S7)

        /// <summary>
        /// 帧分派:COTP CR→CC应答;COTP DT→S7 PDU(Setup/ReadVar/WriteVar)
        /// </summary>
        private byte[]? HandleFrame(byte[] frame)
        {
            if (frame.Length < 6) return null;
            byte cotptype = frame[5];
            if (cotptype == 0xE0) return BuildCotpConfirm(frame);
            if (cotptype != 0xF0 || frame.Length < 19 || frame[7] != 0x32) return null;
            byte rosctr = frame[8];
            if (rosctr != 0x01) return null;   //只处理Job
            byte function = frame[17];
            return function switch
            {
                0xF0 => BuildSetupAck(frame),
                0x04 => BuildReadAck(frame),
                0x05 => BuildWriteAck(frame),
                _ => null
            };
        }

        /// <summary>
        /// COTP连接确认(CC=0xD0:目的引用取请求的源引用,参数区原样回带——
        /// 客户端只校验PDU类型与引用,TSAP不做准入限制)
        /// </summary>
        private static byte[] BuildCotpConfirm(byte[] request)
        {
            // 请求COTP部分:[4]=len,[5]=0xE0,[6..7]=dstref,[8..9]=srcref,[10]=class,[11..]=params
            int cotplen = request[4];
            var paras = request.Skip(11).Take(4 + cotplen - 7).ToArray();
            var cotp = new List<byte> { (byte)(6 + paras.Length), 0xD0, request[8], request[9], 0x00, 0x01, 0x00 };
            cotp.AddRange(paras);
            var reply = new List<byte> { 0x03, 0x00, 0x00, (byte)(4 + cotp.Count) };
            reply.AddRange(cotp);
            return reply.ToArray();
        }

        /// <summary>
        /// Setup Communication应答(Ack_Data:并行任务数1/1,PDU长度回显客户端请求值)
        /// </summary>
        private static byte[] BuildSetupAck(byte[] request)
        {
            byte pduhi = request.Length > 24 ? request[23] : (byte)0x03;
            byte pdulo = request.Length > 24 ? request[24] : (byte)0xC0;
            var s7 = new byte[]
            {
                0x32, 0x03, 0x00, 0x00, request[11], request[12],
                0x00, 0x08, 0x00, 0x00, 0x00, 0x00,
                0xF0, 0x00, 0x00, 0x01, 0x00, 0x01, pduhi, pdulo
            };
            return WrapTpktDt(s7);
        }

        /// <summary>
        /// ReadVar应答(解析S7ANY项→取区内存切片→FF/0x04按位长回送;
        /// 越界或未知区回错误码0x05地址越界)
        /// </summary>
        private byte[] BuildReadAck(byte[] request)
        {
            // 项:[19]=0x12,[22]=transport,[23..24]=数量,[25..26]=DB,[27]=区,[28..30]=起始位地址
            int count = (request[23] << 8) | request[24];
            int db = (request[25] << 8) | request[26];
            byte area = request[27];
            int bitaddr = (request[28] << 16) | (request[29] << 8) | request[30];
            int byteaddr = bitaddr >> 3;

            byte[]? data = null;
            lock (_memLock)
            {
                var mem = ResolveArea(area, db);
                if (mem != null && byteaddr >= 0 && byteaddr + count <= mem.Length)
                {
                    data = new byte[count];
                    Array.Copy(mem, byteaddr, data, 0, count);
                }
            }

            List<byte> s7;
            if (data == null)
            {
                s7 = new List<byte>
                {
                    0x32, 0x03, 0x00, 0x00, request[11], request[12],
                    0x00, 0x02, 0x00, 0x04, 0x00, 0x00,
                    0x04, 0x01, 0x05, 0x00, 0x00, 0x00   //retcode 0x05=地址越界
                };
            }
            else
            {
                int dlen = 4 + data.Count();
                s7 = new List<byte>
                {
                    0x32, 0x03, 0x00, 0x00, request[11], request[12],
                    0x00, 0x02, (byte)(dlen >> 8), (byte)(dlen & 0xFF), 0x00, 0x00,
                    0x04, 0x01,
                    0xFF, 0x04, (byte)((data.Length * 8) >> 8), (byte)((data.Length * 8) & 0xFF)
                };
                s7.AddRange(data);
            }
            return WrapTpktDt(s7.ToArray());
        }

        /// <summary>
        /// WriteVar应答(解析项与数据段写入区内存:transport 0x03=位写按位置位,其余按字节拷贝;
        /// 被写覆盖的点位停止生成器刷新)
        /// </summary>
        private byte[] BuildWriteAck(byte[] request)
        {
            int count = (request[23] << 8) | request[24];
            int db = (request[25] << 8) | request[26];
            byte area = request[27];
            int bitaddr = (request[28] << 16) | (request[29] << 8) | request[30];
            int byteaddr = bitaddr >> 3;
            // 数据段:[31]=保留,[32]=transport,[33..34]=长度,[35..]=载荷
            byte transport = request.Length > 32 ? request[32] : (byte)0x04;
            byte retcode = 0x05;
            lock (_memLock)
            {
                var mem = ResolveArea(area, db);
                if (mem != null)
                {
                    if (transport == 0x03 && request.Length > 35 && byteaddr < mem.Length)
                    {
                        int bit = bitaddr & 0x07;
                        if (request[35] != 0) mem[byteaddr] |= (byte)(1 << bit);
                        else mem[byteaddr] &= (byte)~(1 << bit);
                        MarkOverridden(area, db, byteaddr, 1);
                        retcode = 0xFF;
                    }
                    else if (request.Length >= 35 + count && byteaddr >= 0 && byteaddr + count <= mem.Length)
                    {
                        Array.Copy(request, 35, mem, byteaddr, count);
                        MarkOverridden(area, db, byteaddr, count);
                        retcode = 0xFF;
                    }
                }
            }
            var s7 = new byte[]
            {
                0x32, 0x03, 0x00, 0x00, request[11], request[12],
                0x00, 0x02, 0x00, 0x01, 0x00, 0x00,
                0x05, 0x01, retcode
            };
            return WrapTpktDt(s7);
        }

        /// <summary>
        /// S7 PDU包TPKT+COTP DT头
        /// </summary>
        private static byte[] WrapTpktDt(byte[] s7)
        {
            int total = 7 + s7.Length;
            var frame = new byte[total];
            frame[0] = 0x03;
            frame[1] = 0x00;
            frame[2] = (byte)(total >> 8);
            frame[3] = (byte)(total & 0xFF);
            frame[4] = 0x02;
            frame[5] = 0xF0;
            frame[6] = 0x80;
            s7.CopyTo(frame, 7);
            return frame;
        }

        /// <summary>
        /// 区码→内存(0x84=DB/0x83=M/0x81=I/0x82=Q;DB懒分配64KB)
        /// </summary>
        private byte[]? ResolveArea(byte area, int db)
        {
            switch (area)
            {
                case 0x84:
                    if (!_db.TryGetValue(db, out var mem))
                    {
                        mem = new byte[65536];
                        _db[db] = mem;
                    }
                    return mem;
                case 0x83: return _m;
                case 0x81: return _i;
                case 0x82: return _q;
                default: return null;
            }
        }

        /// <summary>
        /// 标记写入范围覆盖的点位停止刷新(区码转点位区号:0x84→1/0x83→2/0x81→3/0x82→4)
        /// </summary>
        private void MarkOverridden(byte area, int db, int start, int length)
        {
            int pointarea = area switch { 0x84 => 1, 0x83 => 2, 0x81 => 3, 0x82 => 4, _ => 0 };
            foreach (var point in _points)
            {
                if (point.Area != pointarea) continue;
                if (pointarea == 1 && point.Db != db) continue;
                if (point.ByteAddr < start + length && start < point.ByteAddr + point.ByteLen)
                {
                    point.Overridden = true;
                }
            }
        }

        #endregion

        #region 点位刷新(生成器→区内存,S7大端)

        private async Task RefreshLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000, token);
                    RefreshPoints();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { OnLog?.Invoke($"点位刷新异常:{ex.Message}"); }
        }

        /// <summary>
        /// 把每个点位的当前生成器值按数据类型大端写入对应区内存(原始值=显示值/Scale)
        /// </summary>
        private void RefreshPoints()
        {
            var now = DateTime.Now;
            lock (_memLock)
            {
                foreach (var point in _points)
                {
                    if (point.Overridden) continue;
                    var mem = ResolveArea(point.Area switch { 1 => (byte)0x84, 2 => (byte)0x83, 3 => (byte)0x81, _ => (byte)0x82 }, point.Db);
                    if (mem == null || point.ByteAddr + point.ByteLen > mem.Length) continue;
                    double raw = point.Generator.Next(now) / point.Scale;
                    switch (point.DataType)
                    {
                        case "bool":
                        case "bit":
                            if (raw != 0) mem[point.ByteAddr] |= (byte)(1 << point.BitOffset);
                            else mem[point.ByteAddr] &= (byte)~(1 << point.BitOffset);
                            break;
                        case "byte":
                            mem[point.ByteAddr] = (byte)Math.Clamp(Math.Round(raw), byte.MinValue, byte.MaxValue);
                            break;
                        case "int16":
                            WriteU16(mem, point.ByteAddr, (ushort)(short)Math.Clamp(Math.Round(raw), short.MinValue, short.MaxValue));
                            break;
                        case "int32":
                            WriteU32(mem, point.ByteAddr, (uint)(int)Math.Clamp(Math.Round(raw), int.MinValue, int.MaxValue));
                            break;
                        case "uint32":
                            WriteU32(mem, point.ByteAddr, (uint)Math.Clamp(Math.Round(raw), uint.MinValue, uint.MaxValue));
                            break;
                        case "float32":
                            WriteU32(mem, point.ByteAddr, (uint)BitConverter.SingleToInt32Bits((float)raw));
                            break;
                        default:   // uint16
                            WriteU16(mem, point.ByteAddr, (ushort)Math.Clamp(Math.Round(raw), ushort.MinValue, ushort.MaxValue));
                            break;
                    }
                }
            }
        }

        private static void WriteU16(byte[] mem, int offset, ushort value)
        {
            mem[offset] = (byte)(value >> 8);
            mem[offset + 1] = (byte)value;
        }

        private static void WriteU32(byte[] mem, int offset, uint value)
        {
            mem[offset] = (byte)(value >> 24);
            mem[offset + 1] = (byte)(value >> 16);
            mem[offset + 2] = (byte)(value >> 8);
            mem[offset + 3] = (byte)value;
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
