namespace IotModel
{
    public sealed partial class DeviceAlarmConfigDAO : DbContext<DeviceAlarmConfig>
    {
        private static DeviceAlarmConfigDAO instance;
        public static DeviceAlarmConfigDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeviceAlarmConfigDAO();
                }
                return instance;
            }
        }

    }
}