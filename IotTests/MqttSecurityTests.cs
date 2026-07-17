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

        [Fact]
        public void Acl_blocks_cross_device_topic()
        {
            IotWebApi.Services.Mqtt.MqttAclMap.Bind("clientA", "devA");
            Assert.True(IotWebApi.Services.Mqtt.MqttAclMap.Match("clientA", "zhjngk/receive/webapi/devA"));   // 自己的 topic
            Assert.False(IotWebApi.Services.Mqtt.MqttAclMap.Match("clientA", "zhjngk/receive/webapi/devB"));  // 冒充他人被拒
            Assert.True(IotWebApi.Services.Mqtt.MqttAclMap.Match("clientGlobal", "any/topic"));               // 未绑定(全局)放行
            IotWebApi.Services.Mqtt.MqttAclMap.Unbind("clientA");
        }
    }
}
