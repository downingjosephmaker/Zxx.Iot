using System;

namespace IotModel
{
    public sealed partial class AdminMqttparamDAO : DbContext<AdminMqttparam>
    {
        private static AdminMqttparamDAO instance;
        public static AdminMqttparamDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AdminMqttparamDAO();
                }
                return instance;
            }
        }

        public override void Init()
        {
            try
            {
                AdminMqttparam mqttparam = new AdminMqttparam
                {
                    SystemTitle = "mqtt����",
                    MqttServer = "192.168.0.76",
                    MqttClientPort = 1883,
                    MqttServerPort = 13386,
                    MqttUser = "cenbo",
                    MqttPass = "veITwUIjDR",
                    TenantId = 1,
                    MqttSubTopicWebApi = "zhjngk/receive/webapi",
                    MqttSubTopicWebReal = "zhjngk/receive/webreal",
                    MqttSubTopicWebAlarm = "zhjngk/receive/webalarm"
                };
                Insert(mqttparam);
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
            }
        }

    }
}