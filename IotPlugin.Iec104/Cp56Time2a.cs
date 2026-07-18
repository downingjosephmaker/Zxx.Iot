namespace IotPlugin.Iec104
{
    /// <summary>
    /// CP56Time2a七字节时标编解码(IEC 60870-5系列专属概念,刻意留在插件内不进公共库——方案§5;
    /// 字节序:毫秒2B小端|分(bit0-5,bit7=IV)|时(bit0-4)|日(bit0-4,bit5-7=星期)|月(bit0-3)|年(bit0-6,+2000);
    /// 第一版按子站本地时间原样解析,不做时区换算——方案§7)
    /// </summary>
    internal static class Cp56Time2a
    {
        /// <summary>
        /// 编码时间为七字节CP56Time2a
        /// </summary>
        public static byte[] Encode(DateTime time)
        {
            int ms = time.Second * 1000 + time.Millisecond;
            return new byte[]
            {
                (byte)(ms & 0xFF),
                (byte)(ms >> 8),
                (byte)(time.Minute & 0x3F),
                (byte)(time.Hour & 0x1F),
                (byte)((time.Day & 0x1F) | (((int)time.DayOfWeek == 0 ? 7 : (int)time.DayOfWeek) << 5)),
                (byte)(time.Month & 0x0F),
                (byte)((time.Year - 2000) & 0x7F)
            };
        }

        /// <summary>
        /// 解码七字节CP56Time2a(IV无效位/字段越界返回null由调用方回落本机时间)
        /// </summary>
        public static DateTime? TryDecode(byte[] data, int offset)
        {
            if (data.Length - offset < 7) return null;
            if ((data[offset + 2] & 0x80) != 0) return null;  //IV=1时标无效
            int ms = data[offset] | (data[offset + 1] << 8);
            int minute = data[offset + 2] & 0x3F;
            int hour = data[offset + 3] & 0x1F;
            int day = data[offset + 4] & 0x1F;
            int month = data[offset + 5] & 0x0F;
            int year = 2000 + (data[offset + 6] & 0x7F);
            if (ms > 59999 || minute > 59 || hour > 23 || day < 1 || month < 1 || month > 12) return null;
            try
            {
                return new DateTime(year, month, day, hour, minute, ms / 1000).AddMilliseconds(ms % 1000);
            }
            catch
            {
                return null;
            }
        }
    }
}
