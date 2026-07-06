using System.ComponentModel;

namespace IotWebApi.Areas.Admin.Models
{
    /// <summary>
    /// 日出日落时间结果
    /// </summary>
    public class SunTimesResult
    {
        /// <summary>
        /// 区域ID
        /// </summary>
        [DisplayName("区域ID")]
        public string AreaId { get; set; }

        /// <summary>
        /// 区域名称
        /// </summary>
        [DisplayName("区域名称")]
        public string AreaName { get; set; }

        /// <summary>
        /// 纬度
        /// </summary>
        [DisplayName("纬度")]
        public decimal Latitude { get; set; }

        /// <summary>
        /// 经度
        /// </summary>
        [DisplayName("经度")]
        public decimal Longitude { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        [DisplayName("日期")]
        public string Date { get; set; }

        /// <summary>
        /// 日出时间
        /// </summary>
        [DisplayName("日出时间")]
        public string Sunrise { get; set; }

        /// <summary>
        /// 日落时间
        /// </summary>
        [DisplayName("日落时间")]
        public string Sunset { get; set; }

        /// <summary>
        /// 天亮时间
        /// </summary>
        [DisplayName("天亮时间")]
        public string Dawn { get; set; }

        /// <summary>
        /// 天黑时间
        /// </summary>
        [DisplayName("天黑时间")]
        public string Dusk { get; set; }
    }
}
