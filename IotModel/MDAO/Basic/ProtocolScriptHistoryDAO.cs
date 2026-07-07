namespace IotModel
{
    public sealed partial class ProtocolScriptHistoryDAO : DbContext<ProtocolScriptHistory>
    {
        private static ProtocolScriptHistoryDAO instance;
        public static ProtocolScriptHistoryDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ProtocolScriptHistoryDAO();
                }
                return instance;
            }
        }
    }
}
