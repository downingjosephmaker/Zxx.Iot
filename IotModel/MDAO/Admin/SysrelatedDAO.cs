namespace IotModel
{
    public sealed partial class SysRelatedDAO : DbContext<SysRelated>
    {
        private static SysRelatedDAO instance;
        public static SysRelatedDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SysRelatedDAO();
                }
                return instance;
            }
        }

    }
}