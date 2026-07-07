namespace IotModel
{
    public sealed partial class ProtocolScriptDAO : DbContext<ProtocolScript>
    {
        private static ProtocolScriptDAO instance;
        public static ProtocolScriptDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ProtocolScriptDAO();
                }
                return instance;
            }
        }
    }
}
