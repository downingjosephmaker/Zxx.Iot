namespace IotWebApi.Areas.Scada.Models
{
    /// <summary>
    /// 统计图表数据
    /// </summary>
    public class ChartData
    {
        /// <summary>
        /// 图例（格式：x轴，图例1...n）  
        /// </summary>
        public List<string> dimensions { get; set; } = new List<string>();
        /// <summary>
        /// 数据源
        /// </summary>
        public List<dynamic> source { get; set; } = new List<dynamic>();
    }
    /// <summary>
    /// 图表数据源
    /// </summary>
    public class ChartDataSource
    {
        /// <summary>
        /// x轴
        /// </summary>
        public string product { get; set; }
        /// <summary>
        /// 序列1数值
        /// </summary>
        public int data1 { get; set; }
        /// <summary>
        /// 序列2数值
        /// </summary>
        public int data2 { get; set; }
    }
}
