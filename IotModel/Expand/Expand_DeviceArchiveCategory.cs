using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备档案分类拓展类
    /// </summary>
    [DisplayName("设备档案分类拓展类")]
    [Expand]
    public class Expand_DeviceArchiveCategory
    {
        /// <summary>
        /// 巡检项列表
        /// </summary>
        [DisplayName("巡检项列表")]
        public List<string> InspectionItems { get; set; } = new();

        /// <summary>
        /// 默认异常提示列表
        /// </summary>
        [DisplayName("默认异常提示列表")]
        public List<string> DefaultAbnormalPrompts { get; set; } = new();
    }
}
