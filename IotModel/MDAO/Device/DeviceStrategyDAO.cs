namespace IotModel
{
    public sealed partial class DeviceStrategyDAO : DbContext<DeviceStrategy>
    {
        private static DeviceStrategyDAO instance;
        public static DeviceStrategyDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeviceStrategyDAO();
                }
                return instance;
            }
        }

    }
}