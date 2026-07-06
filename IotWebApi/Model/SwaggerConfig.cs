namespace IotWebApi
{
    /// <summary>
    /// Swagger配置
    /// </summary>
    public class SwaggerConfig
    {
        /// <summary>
        /// 外层(/Api)
        /// </summary>
        public string File { get; set; } = "";
        /// <summary>
        /// 不显示的接口分组
        /// </summary>
        public string NotGroupName { get; set; } = "";
        /// <summary>
        /// 是否显示文档
        /// </summary>
        public bool IsShowEnable { get; set; } = false;
    }
}
