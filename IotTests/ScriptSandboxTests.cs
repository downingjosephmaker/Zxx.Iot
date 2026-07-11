using IotDriverCore;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// ScriptSandbox单测(三段式decode/encode/splitFrames+编译失败/未定义函数/超时/CLR禁用安全边界)
    /// </summary>
    public class ScriptSandboxTests
    {
        [Fact]
        public void 正常decode_返回结果JSON()
        {
            var sandbox = new ScriptSandbox("function decode(frame, ctx){ return { value: frame[0] + frame[1] }; }");
            Assert.True(sandbox.Ready);
            var result = sandbox.RunDecode("0102", "{}");
            Assert.True(result.Success);
            Assert.Contains("3", result.ResultJson);
        }

        [Fact]
        public void decode读context_可访问传入上下文()
        {
            var sandbox = new ScriptSandbox("function decode(frame, ctx){ return { scaled: frame[0] * ctx.scale }; }");
            var result = sandbox.RunDecode("0A", "{\"scale\":10}");
            Assert.True(result.Success);
            Assert.Contains("100", result.ResultJson);
        }

        [Fact]
        public void 正常encode_返回结果()
        {
            var sandbox = new ScriptSandbox("function encode(cmd, ctx){ return { hex: cmd.value.toString(16) }; }");
            var result = sandbox.RunEncode("{\"value\":255}", "{}");
            Assert.True(result.Success);
            Assert.Contains("ff", result.ResultJson);
        }

        [Fact]
        public void splitFrames_返回帧与消费字节()
        {
            var sandbox = new ScriptSandbox(
                "function splitFrames(buf, ctx){ return { frames: [[buf[0]]], consumed: 1 }; }");
            var result = sandbox.RunSplitFrames("AABB", "{}");
            Assert.True(result.Success);
            Assert.Contains("consumed", result.ResultJson);
        }

        [Fact]
        public void 编译失败_InitError非空且不可用()
        {
            var sandbox = new ScriptSandbox("function decode( { syntax error");
            Assert.False(sandbox.Ready);
            Assert.NotEmpty(sandbox.InitError);
        }

        [Fact]
        public void 未定义函数_返回失败()
        {
            var sandbox = new ScriptSandbox("var x = 1;");
            var result = sandbox.RunDecode("00", "{}");
            Assert.False(result.Success);
            Assert.Contains("decode", result.Error);
        }

        [Fact]
        public void HasFunction_正确识别()
        {
            var sandbox = new ScriptSandbox("function decode(f,c){return {};}");
            Assert.True(sandbox.HasFunction("decode"));
            Assert.False(sandbox.HasFunction("encode"));
        }

        [Fact]
        public void 死循环_超时收敛为失败不外抛()
        {
            var sandbox = new ScriptSandbox("function decode(f,c){ while(true){} }");
            var result = sandbox.RunDecode("00", "{}");
            Assert.False(result.Success);
            Assert.NotEmpty(result.Error);
        }

        [Fact]
        public void CLR互操作被禁用_无法访问宿主类型()
        {
            // 不开AllowClr:脚本内System应为undefined,访问其成员抛错→收敛为失败
            var sandbox = new ScriptSandbox(
                "function decode(f,c){ return System.IO.File.ReadAllText('x'); }");
            var result = sandbox.RunDecode("00", "{}");
            Assert.False(result.Success);
        }

        [Fact]
        public void console日志_被捕获()
        {
            var sandbox = new ScriptSandbox("function decode(f,c){ console.log('hello'); return {}; }");
            var result = sandbox.RunDecode("00", "{}");
            Assert.True(result.Success);
            Assert.Contains(result.ConsoleLogs, l => l.Contains("hello"));
        }

        [Fact]
        public void decode返回null_结果JSON为空()
        {
            var sandbox = new ScriptSandbox("function decode(f,c){ return null; }");
            var result = sandbox.RunDecode("00", "{}");
            Assert.True(result.Success);
            Assert.Empty(result.ResultJson);
        }
    }
}
