using Magicodes.ExporterAndImporter.Core;

namespace IotWebApi.Areas.Event.Models
{
    /// <summary>
    /// 氢能加氢数据导入DTO
    /// 对应 Excel 列：车牌号、加氢时间、加氢量(KG)/加氢量、加氢站名称
    /// </summary>
    public class HydrogenImportDto
    {
        /// <summary>
        /// 车牌号
        ///</summary>
        [ImporterHeader(Name = "车牌号")]
        public string PlateNo { get; set; }

        /// <summary>
        /// 加氢时间
        ///</summary>
        [ImporterHeader(Name = "加氢时间")]
        public string DTime { get; set; }

        /// <summary>
        /// 加氢量(KG)
        ///</summary>
        [ImporterHeader(Name = "加氢量(KG)")]
        public string DValue { get; set; }

        /// <summary>
        /// 加氢站名称
        ///</summary>
        [ImporterHeader(Name = "加氢站名称")]
        public string StationName { get; set; }
    }
}
