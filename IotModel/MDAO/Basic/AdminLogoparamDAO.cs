using System;

namespace IotModel
{
    public sealed partial class AdminLogoparamDAO : DbContext<AdminLogoparam>
    {
        private static AdminLogoparamDAO instance;
        public static AdminLogoparamDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AdminLogoparamDAO();
                }
                return instance;
            }
        }


    }
}