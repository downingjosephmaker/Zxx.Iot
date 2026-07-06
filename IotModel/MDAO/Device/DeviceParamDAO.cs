namespace IotModel
{
    public sealed partial class DeviceParamDAO : FullEntityContext<DeviceParamEntity>
    {
        private static DeviceParamDAO instance;
        public static DeviceParamDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeviceParamDAO();
                }
                return instance;
            }
        }

    }
}