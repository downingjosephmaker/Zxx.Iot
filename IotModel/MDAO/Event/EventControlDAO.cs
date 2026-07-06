namespace IotModel
{
    /// <summary>
    /// 控制日志
    /// </summary>
    public sealed partial class EventControlDAO : DbContext<EventControl>
    {
        private static EventControlDAO instance;
        public static EventControlDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventControlDAO();
                }
                return instance;
            }
        }

    }
}
