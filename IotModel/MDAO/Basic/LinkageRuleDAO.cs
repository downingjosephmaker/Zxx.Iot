namespace IotModel
{
    public sealed partial class LinkageRuleDAO : DbContext<LinkageRule>
    {
        private static LinkageRuleDAO instance;
        public static LinkageRuleDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LinkageRuleDAO();
                }
                return instance;
            }
        }
    }
}
