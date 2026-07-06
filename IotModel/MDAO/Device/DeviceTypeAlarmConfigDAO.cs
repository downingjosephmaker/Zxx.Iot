namespace IotModel
{
    public sealed partial class DeviceTypeAlarmConfigDAO : DbContext<DeviceTypeAlarmConfig>
    {
        private static DeviceTypeAlarmConfigDAO instance;
        public static DeviceTypeAlarmConfigDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeviceTypeAlarmConfigDAO();
                }
                return instance;
            }
        }

    }
}