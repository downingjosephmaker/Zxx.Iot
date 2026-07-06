using System.ComponentModel;

namespace IotWebApi.Areas.Basic.Models
{
    public class DianweiMapInfo
    {
        /// <summary>
        /// 建筑ID
        ///</summary>
        [DisplayName("建筑ID")]
        public int BuildId { get; set; }
        /// <summary>
        /// 后缀名称
        ///</summary>
        [DisplayName("后缀名称")]
        public string MapConfig { get; set; }
    }
}
