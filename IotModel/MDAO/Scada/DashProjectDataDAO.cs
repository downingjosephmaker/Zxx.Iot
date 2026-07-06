namespace IotModel
{
    /// <summary>
    /// 大屏项目数据访问对象
    /// </summary>
    public sealed partial class DashProjectDataDAO : DbContext<DashProjectData>
    {
        private static DashProjectDataDAO instance;
        private static readonly object _lock = new object();

        public static DashProjectDataDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_lock)
                    {
                        if (instance == null)
                        {
                            instance = new DashProjectDataDAO();
                        }
                    }
                }
                return instance;
            }
        }
    }
}