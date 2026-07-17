using Xunit;
using IotWebApi.Services;

namespace IotTests
{
    public class NorthboundDeviceStateTests
    {
        [Theory]
        [InlineData(2)]  // 在线
        [InlineData(1)]  // 掉电
        [InlineData(0)]  // 离线
        public void BuildDeviceStatePayload_carries_three_states(int state)
        {
            var payload = NorthboundForwardService.BuildDeviceStatePayload(123, state, "无数据采集");
            string expected = $"{{\"msgType\":\"deviceState\",\"deviceId\":123,\"deviceState\":{state},\"reason\":\"无数据采集\"}}";
            Assert.Equal(expected, payload);
        }
    }
}
