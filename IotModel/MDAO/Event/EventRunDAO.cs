namespace IotModel
{
    /// <summary>
    /// 运行日志
    /// </summary>
    public sealed partial class EventRunDAO : DbContext<EventRun>
    {
        private static EventRunDAO instance;
        public static EventRunDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventRunDAO();
                }
                return instance;
            }
        }

    }
}
