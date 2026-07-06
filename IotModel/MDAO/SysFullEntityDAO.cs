namespace IotModel
{
    /// <summary>
    /// 通用查询DAO
    /// </summary>
    /// <typeparam name="T">实体类</typeparam>
    public sealed partial class SysFullEntityDAO<T> : FullEntityContext<T> where T : class, new()
    {
        private static SysFullEntityDAO<T> instance;
        public static SysFullEntityDAO<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SysFullEntityDAO<T>();
                }
                return instance;
            }
        }

    }
}
