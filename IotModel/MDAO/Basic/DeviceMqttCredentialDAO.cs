namespace IotModel
{
    public sealed partial class DeviceMqttCredentialDAO : DbContext<DeviceMqttCredential>
    {
        private static DeviceMqttCredentialDAO instance;
        public static DeviceMqttCredentialDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeviceMqttCredentialDAO();
                }
                return instance;
            }
        }
    }
}
