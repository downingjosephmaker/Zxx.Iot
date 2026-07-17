using System.Text;
using Xunit;
using IotWebApi.Services.Uplink;
using CenboEventBus;

namespace IotTests
{
    public class UplinkPayloadRouterTests
    {
        [Fact]
        public void Route_contract1_PluginMessage_passthrough()
        {
            var json = "{\"MessageType\":2,\"MessageJson\":\"[{\\\"DeviceId\\\":1}]\"}";
            var msg = UplinkPayloadRouter.Route("dev1", Encoding.UTF8.GetBytes(json));
            Assert.NotNull(msg);
            Assert.Equal(PluginMessageEnum.协议解析, msg!.MessageType);
        }

        [Fact]
        public void Route_contract2_bareDeviceDataList_wrapped()
        {
            var json = "[{\"DeviceId\":5,\"DeviceState\":2}]";
            var msg = UplinkPayloadRouter.Route("dev1", Encoding.UTF8.GetBytes(json));
            Assert.NotNull(msg);
            Assert.Equal(PluginMessageEnum.协议解析, msg!.MessageType);
        }

        [Fact]
        public void Route_unrecognized_returns_null()
        {
            var msg = UplinkPayloadRouter.Route("dev1", new byte[] { 0x01, 0x02, 0x03 });
            Assert.Null(msg);
        }
    }
}
