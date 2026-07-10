using System.ComponentModel;

namespace IotWebApi.Areas.Event.Models
{
    /// <summary>
    /// 曲线数据(多曲线)
    /// </summary>
    public class DataChart
    {
        /// <summary>
        /// 图表X轴值
        ///</summary>
        [DisplayName("图表X轴值")]
        public List<string> ChartX { get; set; } = new List<string>();
        /// <summary>
        /// 图表Y轴值
        ///</summary>
        [DisplayName("图表Y轴值")]
        public List<DataChartChild> ChartTuY { get; set; } = new List<DataChartChild>();
    }

    /// <summary>
    /// 图表Y轴值
    /// </summary>
    public class DataChartChild
    {
        /// <summary>
        /// 图表图例(名称)
        ///</summary>
        [DisplayName("图表图例(名称)")]
        public string ChartTuLi { get; set; }

        /// <summary>
        /// 图表图例ID(或编码)
        ///</summary>
        [DisplayName("图表图例ID(或编码)")]
        public string ChartTuLiId { get; set; }

        /// <summary>
        /// 图表Y轴值
        ///</summary>
        [DisplayName("图表Y轴值")]
        public List<string> ChartY { get; set; } = new List<string>();
    }

    /// <summary>
    /// 数据曲线查询
    /// </summary>
    public class DataChartSelect
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
        /// 设备ID集合
        /// </summary>
        [DisplayName("设备ID集合")]
        public List<int> DeviceIds { get; set; } = new List<int>();
        /// <summary>
        /// 单位ID
        /// </summary>
        [DisplayName("单位ID")]
        public int TenantId { get; set; }
        /// <summary>
        /// 参数编号集合
        /// </summary>
        [DisplayName("参数编号集合")]
        public List<string> ParamCodes { get; set; } = new List<string>();
        /// <summary>
        /// 参数类别
        /// </summary>
        [DisplayName("参数类别")]
        public string ParamTypeName { get; set; } = "";
        /// <summary>
        /// 排序(0:倒序 1:正序)
        /// </summary>
        [DisplayName("排序(0:倒序 1:正序)")]
        public int DataSort { get; set; }
        /// <summary>
        /// 设备大类
        /// </summary>
        [DisplayName("设备大类")]
        public string DataTypeDL { get; set; } = "";
        /// <summary>
        /// 是否合计(0:否 1:是)-只对能耗(energy)数据有效
        /// </summary>
        [DisplayName("是否合计")]
        public int IsTotal { get; set; }
    }
    /// <summary>
    /// 数据查询(表格分页)
    /// </summary>
    public class DataTableSelect : DataChartSelect
    {
        /// <summary>
        /// 页码
        /// </summary>
        [DisplayName("页码")]
        public int page { get; set; } = 0;
        /// <summary>
        /// 行数
        /// </summary>
        [DisplayName("行数")]
        public int pagesize { get; set; } = 0;
    }

    /// <summary>
    /// 统计数据曲线查询
    /// </summary>
    public class DataReportChartSelect : DataChartSelect
    {
        /// <summary>
        /// 数据类型(1:时 2:日 3:周 4:月 5:年)
        /// </summary>
        [DisplayName("数据类型(1:时 2:日 3:周 4:月 5:年)")]
        public int DataType { get; set; }
    }

    /// <summary>
    /// 统计数据查询(表格分页)
    /// </summary>
    public class DataReportTableSelect : DataReportChartSelect
    {
        /// <summary>
        /// 页码
        /// </summary>
        [DisplayName("页码")]
        public int page { get; set; } = 0;
        /// <summary>
        /// 行数
        /// </summary>
        [DisplayName("行数")]
        public int pagesize { get; set; } = 0;
    }

    /// <summary>
    /// 表格信息
    /// </summary>
    public class DataReport
    {
        /// <summary>
        /// 表格表头
        /// </summary>
        [DisplayName("表格表头")]
        public List<ReportColumn> ReportColumns { get; set; } = new List<ReportColumn>();
        /// <summary>
        /// 表格内容
        /// </summary>
        [DisplayName("表格内容")]
        public List<dynamic> ReportDatas { get; set; } = new List<dynamic>();
    }

    /// <summary>
    /// 表格表头
    /// </summary>
    public class ReportColumn
    {
        /// <summary>
        /// 英文
        /// </summary>
        [DisplayName("英文")]
        public string ColumnEn { get; set; }
        /// <summary>
        /// 中文
        /// </summary>
        [DisplayName("中文")]
        public string ColumnCn { get; set; }
    }
}
