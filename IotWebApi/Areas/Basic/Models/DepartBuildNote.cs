using System.ComponentModel;
using IotModel;

namespace IotWebApi.Areas.Basic.Models
{
    /// <summary>
    /// 建筑告警配置
    /// </summary>
    public class BuildNoteModel
    {
        /// <summary>
        /// 建筑ID
        ///</summary>
        [DisplayName("建筑ID")]
        public int BuildId { get; set; }
        /// <summary>
        /// 建筑拓展类
        ///</summary>
        [DisplayName("建筑拓展类")]
        public List<Expand_DeptBuild> ExpandObjects { get; set; } = new List<Expand_DeptBuild>();
    }

    /// <summary>
    /// 组织告警配置
    /// </summary>
    public class DeptNoteModel
    {
        /// <summary>
        /// 组织ID
        ///</summary>
        [DisplayName("组织ID")]
        public int DeptId { get; set; }
        /// <summary>
        /// 组织拓展类
        ///</summary>
        [DisplayName("组织拓展类")]
        public List<Expand_DeptBuild> ExpandObjects { get; set; } = new List<Expand_DeptBuild>();
    }
}
