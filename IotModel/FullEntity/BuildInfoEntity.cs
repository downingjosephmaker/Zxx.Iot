using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 建筑完整类
    ///</summary>
    [DisplayName("建筑完整类")]
    [FullEntity]
    public class BuildInfoEntity : BuildInfo
    {
        /// <summary>
        /// 建筑拓展类
        ///</summary>
        [DisplayName("建筑拓展类")]
        public List<Expand_DeptBuild> ExpandObjects { get; set; } = new List<Expand_DeptBuild>();
    }
}
