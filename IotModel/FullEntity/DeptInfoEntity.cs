using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 组织完整类
    ///</summary>
    [DisplayName("组织完整类")]
    [FullEntity]
    public class DeptInfoEntity : DeptInfo
    {
        /// <summary>
        /// 组织拓展类
        ///</summary>
        [DisplayName("组织拓展类")]
        public List<Expand_DeptBuild> ExpandObjects { get; set; } = new List<Expand_DeptBuild>();
    }
}
