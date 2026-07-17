using Xunit;
using IotWebApi;

namespace IotTests
{
    public class MqttSecurityTests
    {
        [Fact]
        public void Pbkdf2_roundtrip_verifies()
        {
            var hash = EncryptsHelper.Pbkdf2Hash("s3cret!", out var salt);
            Assert.True(EncryptsHelper.Pbkdf2Verify("s3cret!", salt, hash));
            Assert.False(EncryptsHelper.Pbkdf2Verify("wrong", salt, hash));
            Assert.False(EncryptsHelper.Pbkdf2Verify("x", "==not-base64==", "also!!bad"));   // 畸形 base64 优雅拒绝,不抛异常
        }
    }
}
