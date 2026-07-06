
namespace IotModel
{
    public sealed partial class DeviceRelevanceDAO : DbContext<DeviceRelevance>
    {
        private static DeviceRelevanceDAO instance;
        public static DeviceRelevanceDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeviceRelevanceDAO();
                }
                return instance;
            }
        }

    }
}