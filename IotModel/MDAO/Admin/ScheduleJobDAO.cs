namespace IotModel
{
    public sealed partial class ScheduleJobDAO : DbContext<ScheduleJob>
    {
        private static ScheduleJobDAO instance;
        public static ScheduleJobDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ScheduleJobDAO();
                }
                return instance;
            }
        }
    }
}