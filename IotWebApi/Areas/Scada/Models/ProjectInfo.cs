using System.ComponentModel;
using IotModel;

namespace IotWebApi.Areas.Scada.Models
{
    /// <summary>
    /// 项目信息
    /// </summary>
    public class ProjectInfo : DashProject
    {
        /// <summary>
        /// 项目内容
        ///</summary>
        public string ContentData { get; set; }
    }

    /// <summary>
    /// 项目信息数据
    /// </summary>
    public class ProjectInfoData
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public long ProjectId { get; set; }
        /// <summary>
        /// 缩略图路径
        /// </summary>
        public string Thumbnail { get; set; }
        /// <summary>
        /// 项目内容
        ///</summary>
        public string ContentData { get; set; }
    }
}
