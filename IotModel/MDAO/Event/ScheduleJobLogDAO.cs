namespace IotModel
{
    public sealed partial class ScheduleJobLogDAO : DbContext<ScheduleJobLog>
    {
        private static ScheduleJobLogDAO instance;
        public static ScheduleJobLogDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ScheduleJobLogDAO();
                }
                return instance;
            }
        }
    }
}