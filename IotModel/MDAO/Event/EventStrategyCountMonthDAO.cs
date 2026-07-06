namespace IotModel
{
    public sealed partial class EventStrategyCountMonthDAO : DbContext<EventStrategyCountMonth>
    {
        private static EventStrategyCountMonthDAO instance;
        public static EventStrategyCountMonthDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventStrategyCountMonthDAO();
                }
                return instance;
            }
        }

    }
}