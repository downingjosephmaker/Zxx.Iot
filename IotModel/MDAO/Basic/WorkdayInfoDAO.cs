using System;

namespace IotModel
{
    public sealed partial class WorkdayInfoDAO : DbContext<WorkdayInfo>
    {
        private static WorkdayInfoDAO _instance;
        private static readonly object _lock = new object();

        public static WorkdayInfoDAO Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new WorkdayInfoDAO();
                        }
                    }
                }
                return _instance;
            }
        }

    }
}