namespace IotModel
{
    public sealed partial class EventControlCountDayDAO : DbContext<EventControlCountDay>
    {
        private static EventControlCountDayDAO instance;
        public static EventControlCountDayDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventControlCountDayDAO();
                }
                return instance;
            }
        }

    }
}