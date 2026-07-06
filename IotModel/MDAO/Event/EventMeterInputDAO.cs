namespace IotModel
{
    public sealed partial class EventMeterInputDAO : FullEntityContext<EventMeterInputEntity>
    {
        private static EventMeterInputDAO instance;
        public static EventMeterInputDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventMeterInputDAO();
                }
                return instance;
            }
        }

    }
}