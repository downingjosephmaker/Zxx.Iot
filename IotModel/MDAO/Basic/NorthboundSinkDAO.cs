namespace IotModel
{
    public sealed partial class NorthboundSinkDAO : DbContext<NorthboundSink>
    {
        private static NorthboundSinkDAO instance;
        public static NorthboundSinkDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NorthboundSinkDAO();
                }
                return instance;
            }
        }
    }
}
