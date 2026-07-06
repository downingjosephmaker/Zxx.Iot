namespace IotModel
{
    public sealed partial class SysuserLogDAO : DbContext<SysuserLog>
    {
        private static SysuserLogDAO instance;
        public static SysuserLogDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SysuserLogDAO();
                }
                return instance;
            }
        }

    }
}