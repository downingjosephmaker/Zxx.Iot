namespace IotModel
{
    /// <summary>
    /// 大屏项目数据访问对象
    /// </summary>
    public sealed partial class DashProjectDAO : DbContext<DashProject>
    {
        private static DashProjectDAO instance;
        private static readonly object _lock = new object();

        public static DashProjectDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_lock)
                    {
                        if (instance == null)
                        {
                            instance = new DashProjectDAO();
                        }
                    }
                }
                return instance;
            }
        }
    }
}