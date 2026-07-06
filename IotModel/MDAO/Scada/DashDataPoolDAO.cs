
namespace IotModel
{
    public sealed partial class DashDataPoolDAO : FullEntityContext<DashDataPoolEntity>
    {
        private static DashDataPoolDAO _instance;
        private static readonly object _lock = new object();

        public static DashDataPoolDAO Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DashDataPoolDAO();
                        }
                    }
                }
                return _instance;
            }
        }
    }
}
