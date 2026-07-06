using System.ComponentModel;

namespace IotWebApi.Areas.Event.Models
{
    /// <summary>
    /// 抄表录入结构
    /// </summary>
    [DisplayName("抄表录入结构")]
    public class MeterInputModel
    {
        /// <summary>
        /// 设备id
        /// </summary>
        [DisplayName("设备id")]
        public int DeviceId { get; set; }
        /// <summary>
        /// 抄表时间
        /// </summary>
        [DisplayName("抄表时间")]
        public string MeterTime { get; set; }
        /// <summary>
        /// 抄表值
        /// </summary>
        [DisplayName("抄表值")]
        public string MeterValue { get; set; }
        /// <summary>
        /// 总工作时间h--设备类型=liuji有效
        /// </summary>
        [DisplayName("总工作时间h--设备类型=liuji有效")]
        public string MeterRunTime { get; set; }
    }

    /// <summary>
    /// 抄表录入查询条件
    /// </summary>
    [DisplayName("抄表录入查询条件")]
    public class MeterInputSearch
    {
        [DisplayName("开始时间")]
        public string starttime { get; set; }

        [DisplayName("结束时间")]
        public string endtime { get; set; }
        /// <summary>
        /// 当前页
        ///</summary>
        [DisplayName("当前页")]
        public int page { get; set; }
        /// <summary>
        /// 每页记录数
        ///</summary>
        [DisplayName("每页记录数")]
        public int pagesize { get; set; } = 20;

        /// <summary>
        /// 建筑ID
        ///</summary>
        [DisplayName("建筑ID")]
        public int BuildId { get; set; }
        /// <summary>
        /// 部门ID
        ///</summary>
        [DisplayName("部门ID")]
        public int DeptId { get; set; }
        /// <summary>
        /// 设备名称
        /// </summary>
        [DisplayName("设备名称")]
        public string DevName { get; set; }
        /// <summary>
        /// 设备大类编号
        /// </summary>
        [DisplayName("设备大类编号")]
        public string DevMasterTypeCode { get; set; }
        /// <summary>
        /// 设备类型集合
        /// </summary>
        [DisplayName("设备类型集合")]
        public List<string> DevTypeCodeList { get; set; } = new List<string>();

    }

}
