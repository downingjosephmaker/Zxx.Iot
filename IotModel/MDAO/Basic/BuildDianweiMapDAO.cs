namespace IotModel
{
    public sealed partial class BuildDianweiMapDAO : FullEntityContext<BuildDianweiMapEntity>
    {
        private static BuildDianweiMapDAO instance;
        public static BuildDianweiMapDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BuildDianweiMapDAO();
                }
                return instance;
            }
        }

    }
}