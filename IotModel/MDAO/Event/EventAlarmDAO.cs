namespace IotModel
{
    /// <summary>
    /// 告警日志
    /// </summary>
    public sealed partial class EventAlarmDAO : FullEntityContext<EventAlarmEntity>
    {
        private static EventAlarmDAO instance;
        public static EventAlarmDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventAlarmDAO();
                }
                return instance;
            }
        }

    }
}
