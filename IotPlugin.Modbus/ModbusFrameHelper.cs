using CenBoCommon.Zxx;

namespace IotPlugin.Modbus
{
    /// <summary>
    /// Modbus帧构建与解析(RTU:CRC16低字节在前;TCP:MBAP七字节头;
    /// 异常应答功能码=请求功能码|0x80,数据域首字节为异常码)
    /// </summary>
    internal static class ModbusFrameHelper
    {
        #region RTU帧

        /// <summary>
        /// 构建RTU读帧(FC01/02/03/04)
        /// </summary>
        public static byte[] BuildReadRtu(byte slave, byte func, ushort addr, ushort count) =>
            AppendCrc(new byte[]
            {
                slave, func, (byte)(addr >> 8), (byte)addr, (byte)(count >> 8), (byte)count
            });

        /// <summary>
        /// 构建RTU写单寄存器帧(FC06,应答为原帧回显)
        /// </summary>
        public static byte[] BuildWriteSingleRtu(byte slave, ushort addr, ushort value) =>
            AppendCrc(new byte[]
            {
                slave, 0x06, (byte)(addr >> 8), (byte)addr, (byte)(value >> 8), (byte)value
            });

        /// <summary>
        /// 构建RTU写多寄存器帧(FC16,registerbytes为大端线序寄存器字节)
        /// </summary>
        public static byte[] BuildWriteMultiRtu(byte slave, ushort addr, byte[] registerbytes)
        {
            int regcount = registerbytes.Length / 2;
            var pdu = new byte[7 + registerbytes.Length];
            pdu[0] = slave;
            pdu[1] = 0x10;
            pdu[2] = (byte)(addr >> 8);
            pdu[3] = (byte)addr;
            pdu[4] = (byte)(regcount >> 8);
            pdu[5] = (byte)regcount;
            pdu[6] = (byte)registerbytes.Length;
            Array.Copy(registerbytes, 0, pdu, 7, registerbytes.Length);
            return AppendCrc(pdu);
        }

        /// <summary>
        /// 解析RTU应答帧(CRC校验通过返回true,data为从站地址/功能码之后、CRC之前的数据域)
        /// </summary>
        public static bool TryParseRtu(byte[] frame, out byte slave, out byte func, out byte[] data)
        {
            slave = 0;
            func = 0;
            data = Array.Empty<byte>();
            if (frame.Length < 5) return false;
            var body = new byte[frame.Length - 2];
            var crc = new byte[2];
            Array.Copy(frame, body, body.Length);
            Array.Copy(frame, body.Length, crc, 0, 2);
            if (!body.YjCrc16VerifyLH(crc)) return false;
            slave = frame[0];
            func = frame[1];
            data = new byte[body.Length - 2];
            Array.Copy(frame, 2, data, 0, data.Length);
            return true;
        }

        /// <summary>
        /// PDU追加CRC16(低字节在前,Modbus线序)
        /// </summary>
        private static byte[] AppendCrc(byte[] pdu)
        {
            var crc = pdu.YjCrc16LHBtye();
            var frame = new byte[pdu.Length + 2];
            Array.Copy(pdu, frame, pdu.Length);
            Array.Copy(crc, 0, frame, pdu.Length, 2);
            return frame;
        }

        #endregion

        #region TCP帧(MBAP)

        /// <summary>
        /// 构建TCP读帧(MBAP+FC01/02/03/04)
        /// </summary>
        public static byte[] BuildReadTcp(ushort tid, byte unit, byte func, ushort addr, ushort count) =>
            new byte[]
            {
                (byte)(tid >> 8), (byte)tid, 0, 0, 0, 6, unit, func,
                (byte)(addr >> 8), (byte)addr, (byte)(count >> 8), (byte)count
            };

        /// <summary>
        /// 构建TCP写单寄存器帧(FC06)
        /// </summary>
        public static byte[] BuildWriteSingleTcp(ushort tid, byte unit, ushort addr, ushort value) =>
            new byte[]
            {
                (byte)(tid >> 8), (byte)tid, 0, 0, 0, 6, unit, 0x06,
                (byte)(addr >> 8), (byte)addr, (byte)(value >> 8), (byte)value
            };

        /// <summary>
        /// 构建TCP写多寄存器帧(FC16)
        /// </summary>
        public static byte[] BuildWriteMultiTcp(ushort tid, byte unit, ushort addr, byte[] registerbytes)
        {
            int regcount = registerbytes.Length / 2;
            int len = 7 + registerbytes.Length;
            var frame = new byte[6 + len];
            frame[0] = (byte)(tid >> 8);
            frame[1] = (byte)tid;
            frame[4] = (byte)(len >> 8);
            frame[5] = (byte)len;
            frame[6] = unit;
            frame[7] = 0x10;
            frame[8] = (byte)(addr >> 8);
            frame[9] = (byte)addr;
            frame[10] = (byte)(regcount >> 8);
            frame[11] = (byte)regcount;
            frame[12] = (byte)registerbytes.Length;
            Array.Copy(registerbytes, 0, frame, 13, registerbytes.Length);
            return frame;
        }

        /// <summary>
        /// 解析TCP应答帧(校验MBAP协议标识与长度域,data为单元标识/功能码之后的数据域)
        /// </summary>
        public static bool TryParseTcp(byte[] frame, out ushort tid, out byte unit, out byte func, out byte[] data)
        {
            tid = 0;
            unit = 0;
            func = 0;
            data = Array.Empty<byte>();
            if (frame.Length < 9) return false;
            if (frame[2] != 0 || frame[3] != 0) return false;
            int len = (frame[4] << 8) | frame[5];
            if (len < 2 || frame.Length < 6 + len) return false;
            tid = (ushort)((frame[0] << 8) | frame[1]);
            unit = frame[6];
            func = frame[7];
            data = new byte[len - 2];
            Array.Copy(frame, 8, data, 0, data.Length);
            return true;
        }

        #endregion
    }
}
