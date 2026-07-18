#if PLUGIN_INTERNALS
using IotPlugin.Iec104;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// IEC104编解码与状态机单测(方案§8:APCI三种帧格式往返/ASDU各TI解码/
    /// SQ=0与SQ=1两条路径/序号窗口k到顶停发与OnAck消费/CP56Time2a往返/104拆帧器)
    /// </summary>
    public class Iec104FrameHelperTests
    {
        // ============ APCI ============

        [Fact]
        public void U帧_STARTDT往返()
        {
            var frame = Iec104FrameHelper.BuildUFrame(Iec104FrameHelper.StartDtAct);
            Assert.Equal(new byte[] { 0x68, 0x04, 0x07, 0x00, 0x00, 0x00 }, frame);
            Assert.True(Iec104FrameHelper.TryParseApci(frame, out var apci));
            Assert.Equal('U', apci.Kind);
            Assert.Equal(Iec104FrameHelper.StartDtAct, apci.UCtrl);
        }

        [Fact]
        public void S帧_接收序号往返()
        {
            var frame = Iec104FrameHelper.BuildSFrame(200);
            Assert.True(Iec104FrameHelper.TryParseApci(frame, out var apci));
            Assert.Equal('S', apci.Kind);
            Assert.Equal(200, apci.Nr);
        }

        [Fact]
        public void I帧_十五位序号往返_跨字节边界()
        {
            var asdu = Iec104FrameHelper.BuildInterrogation(1);
            var frame = Iec104FrameHelper.BuildIFrame(300, 32767, asdu);
            Assert.True(Iec104FrameHelper.TryParseApci(frame, out var apci));
            Assert.Equal('I', apci.Kind);
            Assert.Equal(300, apci.Ns);
            Assert.Equal(32767, apci.Nr);
            Assert.Equal(asdu, apci.Asdu);
        }

        [Fact]
        public void APCI_长度域与实际不符_返回false()
        {
            var frame = Iec104FrameHelper.BuildUFrame(Iec104FrameHelper.TestFrAct);
            frame[1] = 0x08;
            Assert.False(Iec104FrameHelper.TryParseApci(frame, out _));
        }

        // ============ 拆帧器 ============

        [Fact]
        public void 拆帧_半包等待_粘包拆两帧()
        {
            var f1 = Iec104FrameHelper.BuildUFrame(Iec104FrameHelper.StartDtCon);
            var f2 = Iec104FrameHelper.BuildSFrame(5);
            var sticky = f1.Concat(f2).ToArray();

            // 半包:只给前4字节须等待
            var (start, len) = Iec104FrameHelper.Extract104(f1.Take(4).ToArray());
            Assert.Equal(-1, start);

            // 粘包:一次给两帧,首次提取第一帧
            (start, len) = Iec104FrameHelper.Extract104(sticky);
            Assert.Equal(0, start);
            Assert.Equal(6, len);
        }

        [Fact]
        public void 拆帧_起始符前噪声_跳过定位()
        {
            var frame = Iec104FrameHelper.BuildUFrame(Iec104FrameHelper.TestFrCon);
            var noisy = new byte[] { 0x00, 0xFF }.Concat(frame).ToArray();
            var (start, len) = Iec104FrameHelper.Extract104(noisy);
            Assert.Equal(2, start);
            Assert.Equal(6, len);
        }

        // ============ ASDU:SQ=0 / SQ=1(方案§7首要风险) ============

        [Fact]
        public void ASDU_SQ0_每元素各带IOA()
        {
            // TI=1单点,2个信息体:IOA=100值1,IOA=200值0
            var asdu = new byte[]
            {
                1, 0x02, 20, 0, 0x01, 0x00,
                100, 0, 0, 0x01,
                200, 0, 0, 0x00
            };
            Assert.True(Iec104FrameHelper.TryParseAsdu(asdu, out var result));
            Assert.Equal(1, result.Ca);
            Assert.Equal(20, result.Cot);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(100, result.Items[0].Ioa);
            Assert.Equal("1", result.Items[0].Value);
            Assert.Equal(200, result.Items[1].Ioa);
            Assert.Equal("0", result.Items[1].Value);
        }

        [Fact]
        public void ASDU_SQ1_仅首IOA后续连续递增()
        {
            // TI=1单点,SQ=1,3个信息体:首IOA=500,后续501/502
            var asdu = new byte[]
            {
                1, 0x83, 20, 0, 0x01, 0x00,
                0xF4, 0x01, 0,
                0x01, 0x00, 0x01
            };
            Assert.True(Iec104FrameHelper.TryParseAsdu(asdu, out var result));
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(500, result.Items[0].Ioa);
            Assert.Equal("1", result.Items[0].Value);
            Assert.Equal(501, result.Items[1].Ioa);
            Assert.Equal("0", result.Items[1].Value);
            Assert.Equal(502, result.Items[2].Ioa);
            Assert.Equal("1", result.Items[2].Value);
        }

        // ============ ASDU:各TI解码 ============

        [Fact]
        public void ASDU_TI13短浮点_IEEE754小端()
        {
            // 25.5f 小端 = 00 00 CC 41,QDS=0
            var asdu = new byte[]
            {
                13, 0x01, 3, 0, 0x02, 0x00,
                10, 0, 0,
                0x00, 0x00, 0xCC, 0x41, 0x00
            };
            Assert.True(Iec104FrameHelper.TryParseAsdu(asdu, out var result));
            Assert.Equal(13, result.Ti);
            Assert.Equal(2, result.Ca);
            Assert.Equal(Iec104FrameHelper.CotSpontaneous, result.Cot);
            Assert.Equal("25.5", result.Items[0].Value);
            Assert.Equal(0, result.Items[0].Quality);
        }

        [Fact]
        public void ASDU_TI9归一化_满量程比例()
        {
            // NVA=16384 → 16384/32768=0.5
            var asdu = new byte[]
            {
                9, 0x01, 20, 0, 0x01, 0x00,
                1, 0, 0,
                0x00, 0x40, 0x00
            };
            Assert.True(Iec104FrameHelper.TryParseAsdu(asdu, out var result));
            Assert.Equal("0.5", result.Items[0].Value);
        }

        [Fact]
        public void ASDU_TI11标度化_有符号int16()
        {
            // SVA=-123 = 0x85 0xFF
            var asdu = new byte[]
            {
                11, 0x01, 3, 0, 0x01, 0x00,
                1, 0, 0,
                0x85, 0xFF, 0x00
            };
            Assert.True(Iec104FrameHelper.TryParseAsdu(asdu, out var result));
            Assert.Equal("-123", result.Items[0].Value);
        }

        [Fact]
        public void ASDU_QDS品质位_IV上浮()
        {
            // QDS=0x80(IV无效)
            var asdu = new byte[]
            {
                13, 0x01, 3, 0, 0x01, 0x00,
                10, 0, 0,
                0x00, 0x00, 0xCC, 0x41, 0x80
            };
            Assert.True(Iec104FrameHelper.TryParseAsdu(asdu, out var result));
            Assert.Equal(0x80, result.Items[0].Quality);
        }

        [Fact]
        public void ASDU_TI30带时标单点_时标解码()
        {
            var ts = new DateTime(2026, 7, 19, 10, 30, 15, 500);
            var asdu = new byte[] { 30, 0x01, 3, 0, 0x01, 0x00, 66, 0, 0, 0x01 }
                .Concat(Cp56Time2a.Encode(ts)).ToArray();
            Assert.True(Iec104FrameHelper.TryParseAsdu(asdu, out var result));
            Assert.Equal("1", result.Items[0].Value);
            Assert.Equal(ts, result.Items[0].Timestamp);
        }

        [Fact]
        public void ASDU_否定确认PN位()
        {
            // COT字节0x47 = P/N(0x40)|激活确认7
            var asdu = new byte[] { 45, 0x01, 0x47, 0, 0x01, 0x00, 10, 0, 0, 0x01 };
            Assert.True(Iec104FrameHelper.TryParseAsdu(asdu, out var result));
            Assert.True(result.Negative);
            Assert.Equal(Iec104FrameHelper.CotActivationCon, result.Cot);
        }

        [Fact]
        public void ASDU_不支持TI_返回true但无信息体()
        {
            var asdu = new byte[] { 120, 0x01, 3, 0, 0x01, 0x00, 1, 0, 0, 0x00 };
            Assert.True(Iec104FrameHelper.TryParseAsdu(asdu, out var result));
            Assert.Empty(result.Items);
        }

        [Fact]
        public void ASDU_元素截断_返回false()
        {
            // TI=13声明1个信息体但数据域不足5字节
            var asdu = new byte[] { 13, 0x01, 3, 0, 0x01, 0x00, 1, 0, 0, 0x00, 0x00 };
            Assert.False(Iec104FrameHelper.TryParseAsdu(asdu, out _));
        }

        // ============ 命令构建 ============

        [Fact]
        public void 总召唤帧_COT6_IOA0_QOI20()
        {
            var asdu = Iec104FrameHelper.BuildInterrogation(3);
            Assert.Equal(new byte[] { 100, 0x01, 6, 0, 0x03, 0x00, 0, 0, 0, 20 }, asdu);
        }

        [Fact]
        public void 单点命令_SBO选择位与值位()
        {
            var sel = Iec104FrameHelper.BuildSingleCommand(1, 100, true, true);
            var exe = Iec104FrameHelper.BuildSingleCommand(1, 100, true, false);
            Assert.Equal(0x81, sel[9]);   // S/E=1 + SCS=1
            Assert.Equal(0x01, exe[9]);   // S/E=0 + SCS=1
            Assert.Equal(Iec104FrameHelper.TiSingleCommand, sel[0]);
            Assert.Equal(100, sel[6] | (sel[7] << 8) | (sel[8] << 16));
        }

        [Fact]
        public void 双点命令_合1为2分0为1()
        {
            var on = Iec104FrameHelper.BuildDoubleCommand(1, 100, true, false);
            var off = Iec104FrameHelper.BuildDoubleCommand(1, 100, false, false);
            Assert.Equal(0x02, on[9]);
            Assert.Equal(0x01, off[9]);
        }

        [Fact]
        public void 短浮点设定值_往返()
        {
            var asdu = Iec104FrameHelper.BuildSetpointFloat(1, 100, 36.6f, false);
            Assert.True(Iec104FrameHelper.TryParseAsdu(asdu, out var result));
            Assert.Equal(Iec104FrameHelper.TiSetpointFloat, result.Ti);
            Assert.Equal("36.6", result.Items[0].Value);
        }

        // ============ CP56Time2a ============

        [Fact]
        public void CP56_编解码往返()
        {
            var time = new DateTime(2026, 7, 19, 23, 59, 58, 999);
            var decoded = Cp56Time2a.TryDecode(Cp56Time2a.Encode(time), 0);
            Assert.Equal(time, decoded);
        }

        [Fact]
        public void CP56_IV无效位_返回null()
        {
            var data = Cp56Time2a.Encode(DateTime.Now);
            data[2] |= 0x80;
            Assert.Null(Cp56Time2a.TryDecode(data, 0));
        }

        [Fact]
        public void CP56_字段越界_返回null()
        {
            var data = new byte[] { 0, 0, 0, 0, 0, 0, 26 };  // day=0非法
            Assert.Null(Cp56Time2a.TryDecode(data, 0));
        }

        // ============ 状态机:序号窗口(方案§7第二风险) ============

        [Fact]
        public void 状态机_k窗口到顶停发()
        {
            var state = new Iec104StateMachine();
            for (int i = 0; i < 12; i++)
            {
                Assert.True(state.CanSendI(12));
                state.PendingI.Enqueue((state.NextSendSeq(), DateTime.Now));
            }
            Assert.False(state.CanSendI(12));
        }

        [Fact]
        public void 状态机_OnAck_消费已确认帧()
        {
            var state = new Iec104StateMachine();
            for (int i = 0; i < 5; i++) state.PendingI.Enqueue((state.NextSendSeq(), DateTime.Now));
            state.OnAck(3);   // 确认序号0/1/2
            Assert.Equal(2, state.PendingI.Count);
            Assert.Equal(3, state.PendingI.Peek().Seq);
            state.OnAck(5);   // 全确认
            Assert.Empty(state.PendingI);
        }

        [Fact]
        public void 状态机_OnAck_序号回绕()
        {
            var state = new Iec104StateMachine { Vs = 32766 };
            state.PendingI.Enqueue((state.NextSendSeq(), DateTime.Now));   // 32766
            state.PendingI.Enqueue((state.NextSendSeq(), DateTime.Now));   // 32767
            state.PendingI.Enqueue((state.NextSendSeq(), DateTime.Now));   // 0(回绕)
            Assert.Equal(1, state.Vs);
            state.OnAck(1);   // 确认到回绕后的序号1
            Assert.Empty(state.PendingI);
        }

        [Fact]
        public void 状态机_陈旧确认序号_不消费()
        {
            var state = new Iec104StateMachine { Vs = 100 };
            state.PendingI.Enqueue((state.NextSendSeq(), DateTime.Now));   // 100
            state.OnAck(90);   // 陈旧nr,距离为负(模回绕后>4096)
            Assert.Single(state.PendingI);
        }

        [Fact]
        public void 状态机_Reset_清空全部运行态()
        {
            var state = new Iec104StateMachine { Started = true, Vs = 5, Vr = 9, RecvSinceAck = 3 };
            state.PendingI.Enqueue((1, DateTime.Now));
            state.Control = new PendingControl();
            state.Reset();
            Assert.False(state.Started);
            Assert.Equal(0, state.Vs);
            Assert.Equal(0, state.Vr);
            Assert.Equal(0, state.RecvSinceAck);
            Assert.Empty(state.PendingI);
            Assert.Null(state.Control);
        }
    }
}
#endif
