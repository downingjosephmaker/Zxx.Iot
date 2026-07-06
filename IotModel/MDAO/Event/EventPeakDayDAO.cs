namespace IotModel
{
    public sealed partial class EventPeakDayDAO : FullEntityContext<EventPeakDayEntity>
    {
        private static EventPeakDayDAO instance;
        public static EventPeakDayDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventPeakDayDAO();
                }
                return instance;
            }
        }

    }
}