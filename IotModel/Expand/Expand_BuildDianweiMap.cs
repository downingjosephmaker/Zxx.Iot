using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 建筑点位图拓展属性
    ///</summary>
    [DisplayName("建筑点位图拓展属性")]
    [Expand]
    public class Expand_BuildDianweiMap
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        [DisplayName("设备ID")]
        public int DeviceId { get; set; }
        /// <summary>
        /// 设备名称
        /// </summary>
        [DisplayName("设备名称")]
        public string DeviceName { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        [DisplayName("设备类型")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备状态(0:离线 1:正常 2:告警)
        /// </summary>
        [DisplayName("设备状态(0:离线 1:正常 2:告警)")]
        public int Status { get; set; }
        /// <summary>
        /// 显示参数集合
        /// </summary>
        [DisplayName("显示参数集合")]
        public List<Expand_ParamsItem> DisplayParams { get; set; } = new List<Expand_ParamsItem>();
        /// <summary>
        /// 设备坐标
        /// </summary>
        [DisplayName("设备坐标")]
        public List<double> position { get; set; } = new List<double>();
        /// <summary>
        /// 
        /// </summary>
        public string draggable { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int _leaflet_id { get; set; }
    }

    /// <summary>
    /// 显示参数模型
    ///</summary>
    [DisplayName("显示参数模型")]
    [Expand]
    public class Expand_ParamsItem
    {
        /// <summary>
        /// 参数编码
        /// </summary>
        [DisplayName("参数编码")]
        public string ParamCode { get; set; }
        /// <summary>
        /// 参数名称
        /// </summary>
        [DisplayName("参数名称")]
        public string ParamName { get; set; }
        /// <summary>
        /// 参数数值(含单位)
        /// </summary>
        [DisplayName("参数数值(含单位)")]
        public string ParamValue { get; set; }
    }

}
