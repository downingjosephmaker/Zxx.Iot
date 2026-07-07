namespace IotDriverCore
{
    /// <summary>
    /// 驱动元数据自描述(平台读取驱动名称/版本/说明,插件列表零配置展示)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DriverInfoAttribute : Attribute
    {
        /// <summary>
        /// 驱动名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 驱动版本
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// 驱动说明
        /// </summary>
        public string Description { get; }

        public DriverInfoAttribute(string name, string version, string description = "")
        {
            Name = name;
            Version = version;
            Description = description;
        }
    }

    /// <summary>
    /// 驱动连接参数标注(平台反射驱动配置类属性生成前端表单,新驱动零UI代码)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigParameterAttribute : Attribute
    {
        /// <summary>
        /// 参数显示名
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 参数说明
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool Required { get; }

        public ConfigParameterAttribute(string displayname, string description = "", bool required = true)
        {
            DisplayName = displayname;
            Description = description;
            Required = required;
        }
    }
}
