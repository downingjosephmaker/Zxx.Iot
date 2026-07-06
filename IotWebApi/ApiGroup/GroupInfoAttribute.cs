using CenBoCommon.Zxx;

namespace IotWebApi
{
    public class GroupInfoAttribute : Attribute
    {

        public string PrimaryKey { get; set; }

        public string Title { get; set; }

        public string Version
        {
            get
            {
                return OperatorCommon.SwaggerApiVersion.IsZxxNullOrEmpty() ? "1.0.0" : OperatorCommon.SwaggerApiVersion;
            }
        }

        public string Description { get; set; }

    }
}
