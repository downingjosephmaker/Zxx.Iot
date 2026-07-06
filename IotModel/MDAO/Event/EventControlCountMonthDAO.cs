namespace IotModel
{
    public sealed partial class EventControlCountMonthDAO : DbContext<EventControlCountMonth>
    {
        private static EventControlCountMonthDAO instance;
        public static EventControlCountMonthDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventControlCountMonthDAO();
                }
                return instance;
            }
        }

    }
}