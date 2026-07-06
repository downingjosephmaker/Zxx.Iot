using Magicodes.ExporterAndImporter.Core;

namespace IotWebApi.Areas.Device.Models
{
    public class WaterUC4GDataInportDto
    {
        /// <summary>
        /// 数据时间
        ///</summary>
        [ImporterHeader(Name = "数据时间")]
        public string DTime { get; set; }
        /// <summary>
        /// 周期量
        ///</summary>
        [ImporterHeader(Name = "周期量")]
        public string DValue { get; set; }
        /// <summary>
        /// 设备编号
        ///</summary>
        [ImporterHeader(Name = "设备编号")]
        public string DeviceGuid { get; set; }
    }
}
