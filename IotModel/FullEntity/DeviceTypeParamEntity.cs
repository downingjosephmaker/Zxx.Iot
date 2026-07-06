using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备类型参数表完整类
    ///</summary>
    [DisplayName("设备参数表完整类")]
    [FullEntity]
    public class DeviceTypeParamEntity : DeviceTypeParam
    {
        /// <summary>
        /// 状态参数拓展属性
        ///</summary>
        [DisplayName("状态参数拓展属性")]
        public List<Expand_ParamStatusValue> ExpandStatusValues { get; set; } = new();
    }
}
