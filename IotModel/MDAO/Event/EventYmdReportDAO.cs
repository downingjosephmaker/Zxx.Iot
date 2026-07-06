namespace IotModel
{
    public sealed partial class EventYmdReportDAO : DbContext<EventYmdReport>
    {
        private static EventYmdReportDAO instance;
        public static EventYmdReportDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventYmdReportDAO();
                }
                return instance;
            }
        }

    }
}