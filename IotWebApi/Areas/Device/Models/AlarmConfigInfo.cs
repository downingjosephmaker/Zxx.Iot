using System.ComponentModel;
using IotModel;

namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 告警配置查询
    /// </summary>
    public class AlarmConfigSelect
    {
        /// <summary>
        /// 页码
        /// </summary>
        [DisplayName("页码")]
        public int page { get; set; }
        /// <summary>
        /// 行数
        /// </summary>
        [DisplayName("行数")]
        public int pagesize { get; set; }
        /// <summary>
        /// 建筑ID
        /// </summary>
        [DisplayName("建筑ID")]
        public int buildid { get; set; }
        /// <summary>
        /// 类型编码
        /// </summary>
        [DisplayName("类型编码")]
        public string typecode { get; set; }
    }

    /// <summary>
    /// 设备告警统计列表
    /// </summary>
    public class DeviceAlarmInfo
    {
        /// <summary>
        /// 设备ID
        ///</summary>
        [DisplayName("设备ID")]
        public int DeviceId { get; set; }

        /// <summary>
        /// 设备名称
        ///</summary>
        [DisplayName("设备名称")]
        public string DeviceName { get; set; }

        /// <summary>
        /// 单个参数告警数量
        ///</summary>
        [DisplayName("单个参数告警数量")]
        public int OneAlarmNum { get; set; }

        /// <summary>
        /// 组合参数告警数量
        ///</summary>
        [DisplayName("组合参数告警数量")]
        public int MoreAlarmNum { get; set; }
    }

    /// <summary>
    /// 设备告警配置信息
    /// </summary>
    public class DeviceParamDb : DeviceAlarmConfig
    {
        /// <summary>
        /// 报警事件类型
        ///</summary>
        [DisplayName("报警事件类型")]
        public string AlarmConfigEventType { get; set; }
    }

    /// <summary>
    /// 设备参数告警统计列表
    /// </summary>
    public class ParamAlarmInfo : Expand_DeviceParam
    {
        /// <summary>
        /// 告警配置情况
        ///</summary>
        [DisplayName("告警配置情况")]
        public string AlarmText { get; set; }
    }

    /// <summary>
    /// 设备参数报警配置启用模型
    /// </summary>
    public class ParamAlarmConfigEnable : TypeDelAlarmConfig
    {
        /// <summary>
        /// 公式启用 1:启用 0:不启用
        ///</summary>
        [DisplayName("公式启用 1:启用 0:不启用")]
        public int isEnable { get; set; }
    }

    /// <summary>
    /// 设备类型参数报警配置删除模型
    /// </summary>
    public class TypeDelAlarmConfig
    {
        /// <summary>
        /// 配置ID集合
        ///</summary>
        [DisplayName("配置ID集合")]
        public List<long> snowIds { get; set; } = new List<long>();
    }

}
