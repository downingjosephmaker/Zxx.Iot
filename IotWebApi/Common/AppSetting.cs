using CenBoCommon.Zxx;

namespace IotWebApi
{
    /// <summary>
    /// 配置文件读取类
    /// </summary>
    public class AppSetting
    {
        /// <summary>
        /// 是否开发环境
        /// </summary>
        public static bool IsDevelopment = false;
        private static readonly object objLock = new object();
        private static AppSetting instance = null;

        private IConfigurationRoot Config { get; }

        private AppSetting()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile(IsDevelopment ? "appsettings.Development.json" : "appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables(); //环境变量覆盖配置文件(嵌套键用双下划线,如 DefaultValues__DesKey)
            Config = builder.Build();
        }

        /// <summary>
        /// 单例模式
        /// </summary>
        /// <returns></returns>
        public static AppSetting GetInstance()
        {
            if (instance == null)
            {
                lock (objLock)
                {
                    if (instance == null)
                    {
                        instance = new AppSetting();
                    }
                }
            }

            return instance;
        }

        public static string GetConfig(string name)
        {
            return GetInstance().Config.GetSection(name).Value;
        }

        public static int GetInt(string name)
        {
            return GetInstance().Config.GetSection(name).Value.ToZxxInt();
        }

        public static T GetT<T>(string name)
        {
            return GetInstance().Config.GetSection(name).Get<T>();
        }

    }
}
