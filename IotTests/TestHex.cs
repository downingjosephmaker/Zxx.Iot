namespace IotTests
{
    /// <summary>
    /// 测试用hex工具(帧样本以hex字符串书写,可读且便于比对)
    /// </summary>
    internal static class TestHex
    {
        /// <summary>hex字符串→字节(忽略空格/连字符)</summary>
        public static byte[] ToBytes(string hex)
        {
            hex = new string(hex.Where(Uri.IsHexDigit).ToArray());
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }

        /// <summary>字节→大写hex字符串</summary>
        public static string ToHex(byte[] data) => Convert.ToHexString(data);
    }
}
