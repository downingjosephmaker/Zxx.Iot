namespace IotModel
{
    public sealed partial class EventReportDayDAO : FullEntityContext<EventReportDayEntity>
    {
        private static EventReportDayDAO instance;
        public static EventReportDayDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventReportDayDAO();
                }
                return instance;
            }
        }

    }
}