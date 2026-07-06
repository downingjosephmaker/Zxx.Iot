namespace IotModel
{
    public sealed partial class EventReportMonthDAO : FullEntityContext<EventReportMonthEntity>
    {
        private static EventReportMonthDAO instance;
        public static EventReportMonthDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventReportMonthDAO();
                }
                return instance;
            }
        }

    }
}