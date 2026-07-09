using System.ComponentModel;
using IotModel;

namespace IotWebApi.Areas.Event.Models
{
    /// <summary>
    /// 能耗同比/环比分析查询条件
    /// </summary>
    public class EnergyAnalysisSelect
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        [DisplayName("开始时间")]
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        [DisplayName("结束时间")]
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 设备ID
        /// </summary>
        [DisplayName("设备ID")]
        public int DeviceId { get; set; }
        /// <summary>
        /// 参数编号集合
        /// </summary>
        [DisplayName("参数编号集合")]
        public List<string> ParamCodes { get; set; }
        /// <summary>
        /// 参数类别
        /// </summary>
        [DisplayName("参数类别")]
        public string ParamTypeName { get; set; }
        /// <summary>
        /// 数据类型(1:时 2:日 3:周 4:月 5:年)
        /// </summary>
        [DisplayName("数据类型(1:时 2:日 3:周 4:月 5:年)")]
        public int DataType { get; set; }
        /// <summary>
        /// 设备大类
        /// </summary>
        [DisplayName("设备大类")]
        public string DataTypeDL { get; set; } = "";
        /// <summary>
        /// 是否合计(0:否 1:是)-只对能耗(energy)数据有效；合计时康慈单位排除"总表"
        /// </summary>
        [DisplayName("是否合计")]
        public int IsTotal { get; set; }
        /// <summary>
        /// 查询模式(0:仅当前 1:含子集 2:仅子集)-仅对 BuildId/DeptId 维度有效
        /// </summary>
        [DisplayName("查询模式(0:仅当前 1:含子集 2:仅子集)")]
        public int QueryMode { get; set; }
    }

    /// <summary>
    /// 能耗同比/环比分析页面
    /// </summary>
    public class EnergyAnalysis<T>
    {
        /// <summary>
        /// 曲线
        ///</summary>
        [DisplayName("曲线")]
        public DataChart chart { get; set; } = new DataChart();
        /// <summary>
        /// 表格
        ///</summary>
        [DisplayName("表格")]
        public List<T> table { get; set; } = new List<T>();
    }

    /// <summary>
    /// 同比/环比结果
    /// </summary>
    public class EnergyAnalysisTable
    {
        /// <summary>
        /// 日期
        ///</summary>
        [DisplayName("日期")]
        public string DateStr { get; set; } = "";
        /// <summary>
        /// 本期(kW·h)
        ///</summary>
        [DisplayName("本期(kW·h)")]
        public string BenQi { get; set; } = "";
        /// <summary>
        /// 同期(kW·h)
        ///</summary>
        [DisplayName("同期(kW·h)")]
        public string TongQi { get; set; } = "";
        /// <summary>
        /// 增减值(kW·h)
        ///</summary>
        [DisplayName("增减值(kW·h)")]
        public string ZengJianZhi { get; set; } = "0";
        /// <summary>
        /// 增减率(%)
        ///</summary>
        [DisplayName("增减率(%)")]
        public string ZengJianLv { get; set; } = "0";
    }

    /// <summary>
    /// 统计报表类
    /// </summary>
    public class ReportAnalysisInfo
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        public long SnowId { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        public int UnitId { get; set; }
        /// <summary>
        /// 单位名称
        ///</summary>
        [DisplayName("单位名称")]
        public string UnitName { get; set; }
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备类型名称
        ///</summary>
        [DisplayName("设备类型名称")]
        public string DeviceTypeName { get; set; }
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
        /// 记录时间
        ///</summary>
        [DisplayName("记录时间")]
        public string EventTime { get; set; }
        /// <summary>
        /// 年周数
        ///</summary>
        [DisplayName("年周数")]
        public string WeekNum { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        public string ExpandJson { get; set; }
        /// <summary>
        /// 统计表拓展类
        ///</summary>
        [DisplayName("统计表拓展类")]
        public List<Expand_EventReportWeek> ExpandObjects { get; set; } = new List<Expand_EventReportWeek>();
    }

}
