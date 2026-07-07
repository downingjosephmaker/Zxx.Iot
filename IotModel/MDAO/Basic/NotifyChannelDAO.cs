namespace IotModel
{
    public sealed partial class NotifyChannelDAO : DbContext<NotifyChannel>
    {
        private static NotifyChannelDAO instance;
        public static NotifyChannelDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NotifyChannelDAO();
                }
                return instance;
            }
        }
    }
}
