namespace IotModel
{
    public sealed partial class AlarmMaskDAO : DbContext<AlarmMask>
    {
        private static AlarmMaskDAO instance;
        public static AlarmMaskDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AlarmMaskDAO();
                }
                return instance;
            }
        }
    }
}
