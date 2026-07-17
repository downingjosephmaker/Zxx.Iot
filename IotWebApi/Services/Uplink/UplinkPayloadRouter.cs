using System.Text;
using CenboEventBus;
using CenBoCommon.Zxx;
using IotModel;
using IotWebApi.Services.Mqtt;

namespace IotWebApi.Services.Uplink
{
    /// <summary>
    /// 上行载荷三层契约解析(传输无关,MQTT/TCP/UDP 共用)。
    /// 返回 null 表示无法识别,调用方应丢弃并记日志。
    /// </summary>
    public static class UplinkPayloadRouter
    {
        public static PluginMessage? Route(string devicekey, byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return null;
            string strdata = Encoding.UTF8.GetString(buffer);

            PluginMessage? message = null;
            // 契约① PluginMessage{MessageType,MessageJson} 原样路由
            try
            {
                var wrapped = strdata.ToObject<PluginMessage>();
                if (wrapped != null && !wrapped.MessageJson.IsZxxNullOrEmpty()) message = wrapped;
            }
            catch { }
            // 契约② 裸 List<DeviceData> 默认按协议解析包装
            if (message == null)
            {
                try
                {
                    var datalist = strdata.ToObject<List<DeviceData>>();
                    if (datalist.IsZxxAny() && datalist.Exists(t => t.DeviceId > 0))
                        message = new PluginMessage { MessageType = PluginMessageEnum.协议解析, MessageJson = strdata };
                }
                catch { }
            }
            // 契约③ 兜底:非JSON载荷按产品挂JS脚本解码
            if (message == null)
            {
                var scriptdata = MqttClientService.ScriptService?.DecodePayload(devicekey, buffer);
                if (scriptdata.IsZxxAny())
                    message = new PluginMessage { MessageType = PluginMessageEnum.协议解析, MessageJson = scriptdata.ToJson() };
            }
            return message;
        }
    }
}
