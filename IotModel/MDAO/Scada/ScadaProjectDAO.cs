namespace IotModel
{
    /// <summary>
    /// 组态项目数据访问对象
    /// </summary>
    public sealed partial class ScadaProjectDAO : DbContext<ScadaProject>
    {
        private static ScadaProjectDAO instance;
        private static readonly object _lock = new object();

        public static ScadaProjectDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_lock)
                    {
                        if (instance == null)
                        {
                            instance = new ScadaProjectDAO();
                        }
                    }
                }
                return instance;
            }
        }
    }
}