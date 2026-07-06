namespace IotModel
{
    /// <summary>
    /// 推送策略
    /// </summary>
    public sealed partial class PushStrategyDAO : DbContext<PushStrategy>
    {
        private static PushStrategyDAO instance;
        public static PushStrategyDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PushStrategyDAO();
                }
                return instance;
            }
        }

    }
}
