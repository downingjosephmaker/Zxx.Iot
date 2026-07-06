using System.ComponentModel;

namespace IotWebApi.Services.Jobs
{
    /// <summary>
    /// SignalR数据传输模型
    /// </summary>
    public class ChatInfo
    {
        /// <summary>
        /// 请求来源(Web/API)
        /// </summary>
        [DisplayName("请求来源(Web/API)")]
        public string SourceType { get; set; }

        /// <summary>
        /// 类名称
        /// </summary>
        [DisplayName("类名称")]
        public string ClassName { get; set; }

        /// <summary>
        /// 类-类型
        /// </summary>
        [DisplayName("类-类型")]
        public Type ClassType { get; set; }

        /// <summary>
        /// 类方法
        /// </summary>
        [DisplayName("类方法")]
        public string MethodName { get; set; }

    }
}
