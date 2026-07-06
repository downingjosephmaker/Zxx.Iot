namespace IotModel
{
    /// <summary>
    /// 插件数据访问对象
    /// </summary>
    public sealed partial class SysPluginDAO : FullEntityContext<SysPluginEntity>
    {
        private static SysPluginDAO _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取单例
        /// </summary>
        public static SysPluginDAO Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SysPluginDAO();
                        }
                    }
                }
                return _instance;
            }
        }
    }
}