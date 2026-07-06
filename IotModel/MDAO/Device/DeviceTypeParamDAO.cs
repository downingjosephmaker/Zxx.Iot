namespace IotModel
{
    public sealed partial class DeviceTypeParamDAO : FullEntityContext<DeviceTypeParamEntity>
    {
        private static DeviceTypeParamDAO instance;
        public static DeviceTypeParamDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeviceTypeParamDAO();
                }
                return instance;
            }
        }

    }
}