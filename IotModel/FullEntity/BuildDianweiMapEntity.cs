using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 建筑点位图完整类
    ///</summary>
    [DisplayName("建筑点位图完整类")]
    [FullEntity]
    public class BuildDianweiMapEntity : BuildDianweiMap
    {
        /// <summary>
        /// 建筑点位图完整类
        ///</summary>
        [DisplayName("建筑点位图完整类")]
        public List<Expand_BuildDianweiMap> ExpandObjects { get; set; } = new List<Expand_BuildDianweiMap>();
    }
}
