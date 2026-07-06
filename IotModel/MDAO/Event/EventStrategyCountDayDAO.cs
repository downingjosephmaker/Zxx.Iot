namespace IotModel
{
    public sealed partial class EventStrategyCountDayDAO : DbContext<EventStrategyCountDay>
    {
        private static EventStrategyCountDayDAO instance;
        public static EventStrategyCountDayDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventStrategyCountDayDAO();
                }
                return instance;
            }
        }

    }
}