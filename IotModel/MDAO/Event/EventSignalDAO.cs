namespace IotModel
{
    public sealed partial class EventSignalDAO : DbContext<EventSignal>
    {
        private static EventSignalDAO instance;
        public static EventSignalDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventSignalDAO();
                }
                return instance;
            }
        }
    }
}
