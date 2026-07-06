using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 状态参数拓展
    ///</summary>
    [DisplayName("状态参数拓展")]
    [Expand]
    public class Expand_ParamStatusValue
    {
        /// <summary>
        /// key
        ///</summary>
        [DisplayName("key")]
        public int StatusKey { get; set; }
        /// <summary>
        /// value
        ///</summary>
        [DisplayName("value")]
        public string StatusValue { get; set; }
    }
}