namespace IotModel
{
    /// <summary>
    /// 组态项目数据访问对象
    /// </summary>
    public sealed partial class ScadaProjectDataDAO : DbContext<ScadaProjectData>
    {
        private static ScadaProjectDataDAO instance;
        private static readonly object _lock = new object();

        public static ScadaProjectDataDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_lock)
                    {
                        if (instance == null)
                        {
                            instance = new ScadaProjectDataDAO();
                        }
                    }
                }
                return instance;
            }
        }
    }
}