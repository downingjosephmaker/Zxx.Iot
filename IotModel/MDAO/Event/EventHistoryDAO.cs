namespace IotModel
{
    /// <summary>
    /// 历史记录
    /// </summary>
    public sealed partial class EventHistoryDAO : FullEntityContext<EventHistoryEntity>
    {
        private static EventHistoryDAO instance;
        public static EventHistoryDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventHistoryDAO();
                }
                return instance;
            }
        }

    }
}
