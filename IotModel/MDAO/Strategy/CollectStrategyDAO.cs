namespace IotModel
{
    /// <summary>
    /// 采集策略
    /// </summary>
    public sealed partial class CollectStrategyDAO : DbContext<CollectStrategy>
    {
        private static CollectStrategyDAO instance;
        public static CollectStrategyDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CollectStrategyDAO();
                }
                return instance;
            }
        }

    }
}
