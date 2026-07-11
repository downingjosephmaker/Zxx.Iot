#if PLUGIN_INTERNALS
using IotPlugin.Modbus;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// ModbusValueCodec单测(需插件加InternalsVisibleTo("IotTests")+定义PLUGIN_INTERNALS;
    /// 字节序四选一×数据类型编解码对称性——最易出bug处;含往返/位偏移/BCD/string)
    /// </summary>
    public class ModbusValueCodecTests
    {
        // ============ 寄存器数推导 ============

        [Fact]
        public void InferRegLength_显式配置优先()
        {
            Assert.Equal(3, ModbusValueCodec.InferRegLength("uint16", 3));
        }

        [Theory]
        [InlineData("int32", 2)]
        [InlineData("uint32", 2)]
        [InlineData("float32", 2)]
        [InlineData("int64", 4)]
        [InlineData("float64", 4)]
        [InlineData("uint16", 1)]
        [InlineData("int16", 1)]
        public void InferRegLength_按类型推导(string type, int expected)
        {
            Assert.Equal(expected, ModbusValueCodec.InferRegLength(type, 0));
        }

        // ============ 解码:基本类型 ============

        [Fact]
        public void Decode_uint16_ABCD大端()
        {
            var raw = new byte[] { 0x01, 0x00 };  // 256
            Assert.Equal("256", ModbusValueCodec.Decode(raw, "uint16", "ABCD", -1));
        }

        [Fact]
        public void Decode_int16_负数()
        {
            var raw = new byte[] { 0xFF, 0xFF };  // -1
            Assert.Equal("-1", ModbusValueCodec.Decode(raw, "int16", "ABCD", -1));
        }

        [Fact]
        public void Decode_int32_大端()
        {
            var raw = new byte[] { 0x00, 0x01, 0x00, 0x00 };  // 65536
            Assert.Equal("65536", ModbusValueCodec.Decode(raw, "int32", "ABCD", -1));
        }

        [Fact]
        public void Decode_float32_圆周率()
        {
            // 3.14159f的IEEE754大端 = 40 49 0F D0
            var raw = new byte[] { 0x40, 0x49, 0x0F, 0xD0 };
            var result = ModbusValueCodec.Decode(raw, "float32", "ABCD", -1);
            Assert.StartsWith("3.14159", result);
        }

        // ============ 字节序四选一 ============

        [Fact]
        public void Decode_CDAB字交换()
        {
            // 线序CDAB要还原为ABCD:大端65536=00 01 00 00,CDAB线序=00 00 00 01
            var raw = new byte[] { 0x00, 0x00, 0x00, 0x01 };
            Assert.Equal("65536", ModbusValueCodec.Decode(raw, "int32", "CDAB", -1));
        }

        [Fact]
        public void Decode_BADC字内交换()
        {
            // 大端65536=00 01 00 00,BADC(字内高低换)=01 00 00 00
            var raw = new byte[] { 0x01, 0x00, 0x00, 0x00 };
            Assert.Equal("65536", ModbusValueCodec.Decode(raw, "int32", "BADC", -1));
        }

        [Fact]
        public void Decode_DCBA全反转()
        {
            // 大端65536=00 01 00 00,DCBA全反=00 00 01 00
            var raw = new byte[] { 0x00, 0x00, 0x01, 0x00 };
            Assert.Equal("65536", ModbusValueCodec.Decode(raw, "int32", "DCBA", -1));
        }

        [Fact]
        public void Decode_单寄存器BADC等价字节交换()
        {
            var raw = new byte[] { 0x00, 0x01 };  // BADC后=01 00=256
            Assert.Equal("256", ModbusValueCodec.Decode(raw, "uint16", "BADC", -1));
        }

        // ============ 位偏移 ============

        [Fact]
        public void Decode_bool位偏移()
        {
            var raw = new byte[] { 0x00, 0x04 };  // bit2=1
            Assert.Equal("1", ModbusValueCodec.Decode(raw, "bool", "ABCD", 2));
        }

        [Fact]
        public void Decode_uint16位偏移取布尔()
        {
            var raw = new byte[] { 0x00, 0x01 };  // bit0=1
            Assert.Equal("1", ModbusValueCodec.Decode(raw, "uint16", "ABCD", 0));
        }

        // ============ BCD/string ============

        [Fact]
        public void Decode_bcd()
        {
            var raw = new byte[] { 0x12, 0x34 };  // BCD=1234
            Assert.Equal("1234", ModbusValueCodec.Decode(raw, "bcd", "ABCD", -1));
        }

        [Fact]
        public void Decode_string_去尾空()
        {
            var raw = new byte[] { 0x41, 0x42, 0x00, 0x00 };  // "AB\0\0"
            Assert.Equal("AB", ModbusValueCodec.Decode(raw, "string", "ABCD", -1));
        }

        // ============ 编解码对称性(核心:编码后再解码应还原) ============

        [Theory]
        [InlineData("uint16", "ABCD")]
        [InlineData("uint16", "BADC")]
        [InlineData("int16", "ABCD")]
        [InlineData("int32", "ABCD")]
        [InlineData("int32", "CDAB")]
        [InlineData("int32", "BADC")]
        [InlineData("int32", "DCBA")]
        [InlineData("uint32", "ABCD")]
        public void 编解码对称_整数往返(string type, string order)
        {
            string original = type.StartsWith("int") ? "12345" : "54321";
            int reglen = ModbusValueCodec.InferRegLength(type, 0);
            var encoded = ModbusValueCodec.Encode(original, type, order, reglen);
            Assert.NotNull(encoded);
            var decoded = ModbusValueCodec.Decode(encoded!, type, order, -1);
            Assert.Equal(original, decoded);
        }

        [Theory]
        [InlineData("float32", "ABCD")]
        [InlineData("float32", "CDAB")]
        [InlineData("float64", "ABCD")]
        public void 编解码对称_浮点往返(string type, string order)
        {
            string original = "3.5";
            int reglen = ModbusValueCodec.InferRegLength(type, 0);
            var encoded = ModbusValueCodec.Encode(original, type, order, reglen);
            Assert.NotNull(encoded);
            var decoded = ModbusValueCodec.Decode(encoded!, type, order, -1);
            Assert.Equal(3.5, double.Parse(decoded, System.Globalization.CultureInfo.InvariantCulture), 3);
        }

        [Fact]
        public void 编码_bcd不支持写_返回null()
        {
            Assert.Null(ModbusValueCodec.Encode("1234", "bcd", "ABCD", 1));
        }

        [Fact]
        public void 编码_string不支持写_返回null()
        {
            Assert.Null(ModbusValueCodec.Encode("AB", "string", "ABCD", 1));
        }

        [Fact]
        public void 编码_非法值_返回null()
        {
            Assert.Null(ModbusValueCodec.Encode("notanumber", "int16", "ABCD", 1));
        }
    }
}
#endif
