using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 行政区划完整类
    ///</summary>
    [DisplayName("行政区划完整类")]
    [FullEntity]
    public class SysAreaEntity : SysArea
    {
        /// <summary>
        /// 行政区划拓展类
        ///</summary>
        [DisplayName("行政区划拓展类")]
        public Expand_SysArea ExpandObject { get; set; } = new Expand_SysArea();
    }
}
