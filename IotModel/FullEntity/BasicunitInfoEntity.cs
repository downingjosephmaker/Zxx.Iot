using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 单位表完整类
    ///</summary>
    [DisplayName("单位表完整类")]
    [FullEntity]
    public class BasicunitInfoEntity : BasicunitInfo
    {
        /// <summary>
        /// 单位表拓展类
        ///</summary>
        [DisplayName("单位表拓展类")]
        public Expand_BasicunitInfo ExpandObject { get; set; } = new Expand_BasicunitInfo();
    }
}
