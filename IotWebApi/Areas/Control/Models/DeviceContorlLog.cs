using System.ComponentModel;

namespace IotWebApi.Areas.Control.Models
{
    /// <summary>
    /// 设备控制日志
    /// </summary>
    public class DeviceContorlLog
    {
        /// <summary>
        /// 设备ID
        ///</summary>
        [DisplayName("设备ID")]
        public int EquipId { get; set; }
        /// <summary>
        /// 操作结果(成功 失败)
        ///</summary>
        [DisplayName("操作结果(成功 失败)")]
        public string OptResult { get; set; }
        /// <summary>
        /// 操作内容描述(包含开始和结束时间点)
        ///</summary>
        [DisplayName("操作内容描述(包含开始和结束时间点)")]
        public string OptContent { get; set; }
        /// <summary>
        /// 操作时间
        ///</summary>
        [DisplayName("操作时间")]
        public string EventTime { get; set; }
        /// <summary>
        /// 控制Josn
        ///</summary>
        [DisplayName("控制Josn")]
        public string OptJosn { get; set; }
        /// <summary>
        /// 设备类型名称
        ///</summary>
        [DisplayName("设备类型名称")]
        public string DeviceTypeName { get; set; }
    }
}
