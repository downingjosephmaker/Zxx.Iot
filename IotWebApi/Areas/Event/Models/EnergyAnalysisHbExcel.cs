using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Excel;

namespace IotWebApi.Areas.Event.Models
{
    /*
    ### ExporterAttribute

    + ** Name**： 名称(当前Sheet 名称)
    + ** HeaderFontSize**：头部字体大小
    + ** FontSize**：正文字体大小
    + ** MaxRowNumberOnASheet**：Sheet最大允许的行数，设置了之后将输出多个Sheet
    + ** TableStyle**：表格样式风格
    + ** AutoFitAllColumn**：*** bool*** 自适应所有列
    + ** Author**：作者
    + ** ExporterHeaderFilter**：头部筛选器
    + ** AutoCenter**：设置后可将整个表都进行居中

    ### ExporterHeaderAttribute

    + ** DisplayName**： 显示名称
    + ** FontSize**： 字体大小
    + ** IsBold**： 是否加粗
    + ** Format**： 格式化
    + ** IsAutoFit**： 是否自适应
    + ** IsIgnore**： 是否忽略
    + ** AutoCenterColumn**： 设置列居中
    */
    /// <summary>
    /// 环比分析导出模板
    /// </summary>
    [ExcelExporter(Name = "环比分析", HeaderFontSize = 16, FontSize = 14, TableStyle = OfficeOpenXml.Table.TableStyles.Dark9)]
    public class EnergyAnalysisHbExcel
    {
        /// <summary>
        /// 序号
        /// </summary>
        [ExporterHeader(DisplayName = "序号", IsBold = true, IsAutoFit = true)]
        public int RowNo { get; set; }
        /// <summary>
        /// 日期
        ///</summary>
        [ExporterHeader(DisplayName = "日期", IsBold = true, IsAutoFit = true)]
        public string DateStr { get; set; }
        /// <summary>
        /// 能耗(kW·h)
        ///</summary>
        [ExporterHeader(DisplayName = "能耗(kW·h)", IsBold = true, IsAutoFit = true)]
        public string BenQi { get; set; }
        /// <summary>
        /// 增减值(kW·h)
        ///</summary>
        [ExporterHeader(DisplayName = "增减值(kW·h)", IsBold = true, IsAutoFit = true)]
        public string ZengJianZhi { get; set; }
        /// <summary>
        /// 增减率(%)
        ///</summary>
        [ExporterHeader(DisplayName = "增减率(%)", IsBold = true, IsAutoFit = true)]
        public string ZengJianLv { get; set; }
    }

    /// <summary>
    /// 同比分析导出模板
    /// </summary>
    [ExcelExporter(Name = "同比分析", HeaderFontSize = 16, FontSize = 14, TableStyle = OfficeOpenXml.Table.TableStyles.Dark9)]
    public class EnergyAnalysisTbExcel
    {
        /// <summary>
        /// 序号
        /// </summary>
        [ExporterHeader(DisplayName = "序号", IsBold = true, IsAutoFit = true)]
        public int RowNo { get; set; }
        /// <summary>
        /// 日期
        ///</summary>
        [ExporterHeader(DisplayName = "日期", IsBold = true, IsAutoFit = true)]
        public string DateStr { get; set; }
        /// <summary>
        /// 本期能耗(kW·h)
        ///</summary>
        [ExporterHeader(DisplayName = "本期能耗(kW·h)", IsBold = true, IsAutoFit = true)]
        public string BenQi { get; set; }
        /// <summary>
        /// 同期能耗(kW·h)
        ///</summary>
        [ExporterHeader(DisplayName = "同期能耗(kW·h)", IsBold = true, IsAutoFit = true)]
        public string TongQi { get; set; }
        /// <summary>
        /// 增减值(kW·h)
        ///</summary>
        [ExporterHeader(DisplayName = "增减值(kW·h)", IsBold = true, IsAutoFit = true)]
        public string ZengJianZhi { get; set; }
        /// <summary>
        /// 增减率(%)
        ///</summary>
        [ExporterHeader(DisplayName = "增减率(%)", IsBold = true, IsAutoFit = true)]
        public string ZengJianLv { get; set; }
    }

}
