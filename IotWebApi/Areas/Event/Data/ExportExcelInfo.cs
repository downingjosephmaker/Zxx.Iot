using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Excel;
using OfficeOpenXml;
using IotWebApi.Areas.Event.Models;

namespace IotWebApi.Areas.Event.Data
{
    /// <summary>
    /// excel导出通用方法
    /// </summary>
    public static class ExportExcelInfo
    {
        /// <summary>
        /// 根据数据集合和文件路径导出Excel文件
        /// </summary>
        ///  <param name="list">数据集合</param>
        /// <param name="filepath">文件路径</param>
        /// <returns>返回导出文件名</returns>
        public static bool ExportExcelCom<T>(this List<T> list, string filepath) where T : class, new()
        {
            IExporter exporter = new ExcelExporter();
            var exportres = exporter.Export<T>(filepath, list).Result;
            return File.Exists(filepath);
        }

        /// <summary>
        /// 根据数据报表对象和文件路径导出Excel文件
        /// </summary>
        /// <param name="reportTable"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static bool ExportExcelCom(this DataReport reportTable, string filepath)
        {
            // 创建Excel导出对象
            using (var excel = new ExcelPackage())
            {
                // 创建工作表
                var worksheet = excel.Workbook.Worksheets.Add("趋势分析");
                // 写入表头
                for (int i = 0; i < reportTable.ReportColumns.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = reportTable.ReportColumns[i].ColumnCn;
                }
                // 写入数据
                for (int i = 0; i < reportTable.ReportDatas.Count; i++)
                {
                    var dicdata = reportTable.ReportDatas[i] as Dictionary<string, object>;
                    if (dicdata != null)
                    {
                        for (int j = 0; j < reportTable.ReportColumns.Count; j++)
                        {
                            var columnName = reportTable.ReportColumns[j].ColumnEn;
                            worksheet.Cells[i + 2, j + 1].Value = dicdata.ContainsKey(columnName) ? dicdata[columnName] : null;
                        }
                    }
                }
                // 设置样式
                using (var range = worksheet.Cells[1, 1, 1, reportTable.ReportColumns.Count])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(211, 211, 211, 255);
                }
                // 自动调整列宽（Linux 无 GUI 环境下 AutoFitColumns 依赖 SkiaSharp native 库，
                // 版本不匹配会抛异常，try-catch 兜底改为固定列宽）
                try
                {
                    worksheet.Cells.AutoFitColumns();
                }
                catch
                {
                    // AutoFitColumns 失败时设固定列宽兜底
                    for (int i = 0; i < reportTable.ReportColumns.Count; i++)
                    {
                        worksheet.Column(i + 1).Width = 30;
                    }
                }
                // 保存文件
                using (var stream = new MemoryStream())
                {
                    excel.SaveAs(stream);
                    stream.Position = 0;
                    using (var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }

            return File.Exists(filepath);
        }

        /// <summary>
        /// 根据多个数据报表对象和文件路径导出多Sheet的Excel文件
        /// </summary>
        /// <param name="reportTables">数据报表对象字典，Key为Sheet名称，Value为数据报表对象</param>
        /// <param name="filepath">文件路径</param>
        /// <returns>返回是否导出成功</returns>
        public static bool ExportExcelComMultiSheet(this Dictionary<string, DataReport> reportTables, string filepath)
        {
            // 创建Excel导出对象
            using (var excel = new ExcelPackage())
            {
                foreach (var reportItem in reportTables)
                {
                    var sheetName = reportItem.Key;
                    var reportTable = reportItem.Value;
                    
                    // 创建工作表
                    var worksheet = excel.Workbook.Worksheets.Add(sheetName);
                    
                    // 写入表头
                    for (int i = 0; i < reportTable.ReportColumns.Count; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = reportTable.ReportColumns[i].ColumnCn;
                    }
                    
                    // 写入数据
                    for (int i = 0; i < reportTable.ReportDatas.Count; i++)
                    {
                        var dicdata = reportTable.ReportDatas[i] as Dictionary<string, object>;
                        if (dicdata != null)
                        {
                            for (int j = 0; j < reportTable.ReportColumns.Count; j++)
                            {
                                var columnName = reportTable.ReportColumns[j].ColumnEn;
                                worksheet.Cells[i + 2, j + 1].Value = dicdata.ContainsKey(columnName) ? dicdata[columnName] : null;
                            }
                        }
                    }
                    
                    // 设置样式
                    using (var range = worksheet.Cells[1, 1, 1, reportTable.ReportColumns.Count])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(211, 211, 211, 255);
                    }
                    
                    // 自动调整列宽（Linux 兜底）
                    try
                    {
                        worksheet.Cells.AutoFitColumns();
                    }
                    catch
                    {
                        for (int i = 0; i < reportTable.ReportColumns.Count; i++)
                        {
                            worksheet.Column(i + 1).Width = 30;
                        }
                    }
                }
                
                // 保存文件
                using (var stream = new MemoryStream())
                {
                    excel.SaveAs(stream);
                    stream.Position = 0;
                    using (var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }

            return File.Exists(filepath);
        }

        /// <summary>
        /// 根据泛型数据集合字典和文件路径导出多Sheet的Excel文件
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="dataDictionary">数据集合字典，Key为Sheet名称，Value为数据集合</param>
        /// <param name="filepath">文件路径</param>
        /// <returns>返回是否导出成功</returns>
        public static bool ExportExcelComMultiSheet<T>(this Dictionary<string, List<T>> dataDictionary, string filepath) where T : class, new()
        {
            IExporter exporter = new ExcelExporter();
            
            // 创建Excel导出对象
            using (var excel = new ExcelPackage())
            {
                foreach (var dataItem in dataDictionary)
                {
                    var sheetName = dataItem.Key;
                    var dataList = dataItem.Value;
                    
                    // 使用Magicodes.ExporterAndImporter导出到临时文件
                    var tempFilePath = Path.GetTempFileName() + ".xlsx";
                    var exportres = exporter.Export<T>(tempFilePath, dataList).Result;
                    
                    if (File.Exists(tempFilePath))
                    {
                        // 读取临时文件并添加到主Excel文件
                        using (var tempExcel = new ExcelPackage(new FileInfo(tempFilePath)))
                        {
                            var tempWorksheet = tempExcel.Workbook.Worksheets.FirstOrDefault();
                            if (tempWorksheet != null)
                            {
                                // 复制工作表到主Excel文件
                                var newWorksheet = excel.Workbook.Worksheets.Add(sheetName, tempWorksheet);
                            }
                        }
                        
                        // 删除临时文件
                        File.Delete(tempFilePath);
                    }
                }
                
                // 保存文件
                using (var stream = new MemoryStream())
                {
                    excel.SaveAs(stream);
                    stream.Position = 0;
                    using (var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }

            return File.Exists(filepath);
        }

    }
}