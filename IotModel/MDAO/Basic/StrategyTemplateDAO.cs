using System;

namespace IotModel
{
    public sealed partial class StrategyTemplateDAO : DbContext<StrategyTemplate>
    {
        private static StrategyTemplateDAO _instance;
        private static readonly object _lock = new object();

        public static StrategyTemplateDAO Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new StrategyTemplateDAO();
                        }
                    }
                }
                return _instance;
            }
        }

    }
}