namespace IotModel
{
    public sealed partial class DeviceTypeRunDAO : DbContext<DeviceTypeRun>
    {
        private static DeviceTypeRunDAO instance;
        public static DeviceTypeRunDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeviceTypeRunDAO();
                }
                return instance;
            }
        }

    }
}