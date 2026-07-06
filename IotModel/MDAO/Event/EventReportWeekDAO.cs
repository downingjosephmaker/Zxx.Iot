namespace IotModel
{
    public sealed partial class EventReportWeekDAO : FullEntityContext<EventReportWeekEntity>
    {
        private static EventReportWeekDAO instance;
        public static EventReportWeekDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventReportWeekDAO();
                }
                return instance;
            }
        }

    }
}