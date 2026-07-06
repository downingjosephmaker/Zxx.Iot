namespace IotModel
{
    public sealed partial class EventNoteRecordDAO : DbContext<EventNoteRecord>
    {
        private static EventNoteRecordDAO instance;
        public static EventNoteRecordDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventNoteRecordDAO();
                }
                return instance;
            }
        }

    }
}