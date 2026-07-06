using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IotWebApi.Helper
{
    /// <summary>
    /// HTML表格转DOCX专用转换器
    /// </summary>
    public class HtmlTableToDocxConverter
    {
        /// <summary>
        /// 将HTML表格转换为DOCX表格
        /// </summary>
        public static Table ConvertHtmlTableToDocxTable(HtmlNode tableNode, MainDocumentPart mainPart)
        {
            if (tableNode == null || tableNode.Name.ToLower() != "table")
            {
                throw new ArgumentException("提供的节点不是有效的表格节点");
            }

            var table = new Table();

            // 设置表格属性
            var tablePr = new TableProperties();
            
            // 设置表格边框
            var tableBorders = new TableBorders(
                new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
            );
            tablePr.AppendChild(tableBorders);
            
            // 设置表格宽度
            var tableWidth = new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct };
            tablePr.AppendChild(tableWidth);
            
            // 添加表格整体背景色 (如果存在)
            string tableBgColor = ExtractBackgroundColor(tableNode);
            if (!string.IsNullOrEmpty(tableBgColor))
            {
                var tableShading = new Shading() 
                { 
                    Val = ShadingPatternValues.Clear,
                    Color = "auto",
                    Fill = tableBgColor
                };
                tablePr.AppendChild(tableShading);
            }
            
            table.AppendChild(tablePr);

            // 建立单元格合并跟踪
            var mergedCells = new Dictionary<string, int>();
            
            // 记录行数
            int rowIndex = 0;
            
            // 处理表格行
            foreach (var rowNode in tableNode.SelectNodes(".//tr"))
            {
                var row = new TableRow();
                
                // 应用行的背景色
                string rowBgColor = null;
                
                // 基于内容确定行样式
                string rowText = rowNode.InnerText.Trim().ToLower();
                
                // 请求参数标题行 - 橙色
                if (rowText.Contains("请求参数") || rowText.Contains("body请求") || 
                    rowText.Contains("post请求") || rowText.Contains("参数列表") ||
                    rowText.Contains("body") && rowText.Contains("参数"))
                {
                    rowBgColor = "F79646"; // 橙色
                }
                // 返回状态码或结果集标题行 - 绿色
                else if (rowText.Contains("返回状态码") || rowText.Contains("结果集参数") || 
                        rowText.Contains("回参") || rowText.Contains("返回值"))
                {
                    rowBgColor = "9BBB59"; // 绿色
                }
                // 参数定义行 - 蓝色
                else if (rowText.Contains("参数名") || rowText.Contains("参数类型") || 
                         rowText.Contains("是否必填") || rowText.Contains("说明") ||
                         rowText.Contains("状态码") && !rowText.Contains("返回状态码"))
                {
                    rowBgColor = "4F81BD"; // 蓝色
                }
                else
                {
                    // 从style属性获取背景色
                    rowBgColor = ExtractBackgroundColor(rowNode);
                }
                
                // 应用行背景色
                if (!string.IsNullOrEmpty(rowBgColor))
                {
                    var rowProperties = new TableRowProperties();
                    var rowShading = new Shading()
                    {
                        Val = ShadingPatternValues.Clear,
                        Color = "auto",
                        Fill = rowBgColor
                    };
                    rowProperties.AppendChild(rowShading);
                    row.AppendChild(rowProperties);
                }
                
                // 记录列数
                int colIndex = 0;
                
                // 处理单元格
                foreach (var cellNode in rowNode.SelectNodes(".//td|.//th"))
                {
                    // 检查是否需要跳过此单元格（因为被前面的rowspan覆盖）
                    string cellKey = $"{rowIndex}_{colIndex}";
                    while (mergedCells.ContainsKey(cellKey))
                    {
                        // 如果这个位置已经被占用，那就是继续的合并单元格
                        var mergedTableCell = new TableCell();
                        var mergedCellProperties = new TableCellProperties();
                        mergedCellProperties.AppendChild(new VerticalMerge() { Val = MergedCellValues.Continue });
                        mergedTableCell.AppendChild(mergedCellProperties);
                        
                        // 空内容
                        mergedTableCell.AppendChild(new Paragraph(new Run(new Text(""))));
                        
                        row.AppendChild(mergedTableCell);
                        
                        colIndex++;
                        cellKey = $"{rowIndex}_{colIndex}";
                    }
                    
                    var tableCell = new TableCell();
                    var cellProperties = new TableCellProperties();
                    
                    // 应用单元格的背景色
                    string cellBgColor = ExtractBackgroundColor(cellNode);
                    // 如果单元格没有背景色，使用行的背景色
                    if (string.IsNullOrEmpty(cellBgColor) && !string.IsNullOrEmpty(rowBgColor))
                    {
                        cellBgColor = rowBgColor;
                    }
                    
                    if (!string.IsNullOrEmpty(cellBgColor))
                    {
                        var shading = new Shading()
                        {
                            Val = ShadingPatternValues.Clear,
                            Color = "auto",
                            Fill = cellBgColor
                        };
                        cellProperties.AppendChild(shading);
                    }
                    
                    // 应用单元格边框
                    var borders = new TableCellBorders(
                        new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                        new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                        new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                        new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
                    );
                    cellProperties.AppendChild(borders);
                    
                    // 处理单元格合并
                    int colspan = 1;
                    if (cellNode.Attributes["colspan"] != null && int.TryParse(cellNode.Attributes["colspan"].Value, out int parsedColspan) && parsedColspan > 1)
                    {
                        colspan = parsedColspan;
                        cellProperties.AppendChild(new GridSpan() { Val = colspan });
                    }
                    
                    int rowspan = 1;
                    if (cellNode.Attributes["rowspan"] != null && int.TryParse(cellNode.Attributes["rowspan"].Value, out int parsedRowspan) && parsedRowspan > 1)
                    {
                        rowspan = parsedRowspan;
                        cellProperties.AppendChild(new VerticalMerge() { Val = MergedCellValues.Restart });
                        
                        // 标记后续行中的单元格为已合并
                        for (int r = 1; r < rowspan; r++)
                        {
                            for (int c = 0; c < colspan; c++)
                            {
                                string mergedKey = $"{rowIndex + r}_{colIndex + c}";
                                mergedCells[mergedKey] = 1; // 使用1标记被占用
                            }
                        }
                    }
                    
                    tableCell.AppendChild(cellProperties);
                    
                    // 处理单元格内容
                    if (cellNode.ChildNodes.Count > 0)
                    {
                        ProcessCellContent(cellNode, tableCell);
                    }
                    else
                    {
                        // 空单元格
                        tableCell.AppendChild(new Paragraph(new Run(new Text(""))));
                    }
                    
                    row.AppendChild(tableCell);
                    colIndex += colspan; // 增加列索引
                }
                
                table.AppendChild(row);
                rowIndex++; // 增加行索引
            }
            
            return table;
        }
        
        /// <summary>
        /// 处理单元格内容
        /// </summary>
        private static void ProcessCellContent(HtmlNode cellNode, TableCell tableCell)
        {
            bool hasAddedParagraph = false;
            
            foreach (var childNode in cellNode.ChildNodes)
            {
                if (childNode.NodeType == HtmlNodeType.Text)
                {
                    if (!string.IsNullOrWhiteSpace(childNode.InnerText))
                    {
                        var paragraph = new Paragraph();
                        var run = new Run();
                        var text = new Text(childNode.InnerText.Trim()) { Space = SpaceProcessingModeValues.Preserve };
                        run.AppendChild(text);
                        paragraph.AppendChild(run);
                        tableCell.AppendChild(paragraph);
                        hasAddedParagraph = true;
                    }
                }
                else if (childNode.Name.ToLower() == "p" || childNode.Name.ToLower() == "div")
                {
                    var paragraph = new Paragraph();
                    var paragraphProperties = new ParagraphProperties();
                    
                    // 处理对齐方式
                    if (childNode.Attributes["align"] != null)
                    {
                        string align = childNode.Attributes["align"].Value.ToLower();
                        JustificationValues justification = JustificationValues.Left;
                        
                        switch (align)
                        {
                            case "center": justification = JustificationValues.Center; break;
                            case "right": justification = JustificationValues.Right; break;
                            case "justify": justification = JustificationValues.Both; break;
                        }
                        
                        paragraphProperties.AppendChild(new Justification() { Val = justification });
                    }
                    
                    paragraph.AppendChild(paragraphProperties);
                    
                    // 处理段落内容
                    foreach (var pChild in childNode.ChildNodes)
                    {
                        if (pChild.NodeType == HtmlNodeType.Text)
                        {
                            if (!string.IsNullOrWhiteSpace(pChild.InnerText))
                            {
                                var run = new Run();
                                var text = new Text(pChild.InnerText.Trim()) { Space = SpaceProcessingModeValues.Preserve };
                                run.AppendChild(text);
                                paragraph.AppendChild(run);
                            }
                        }
                        else if (pChild.Name.ToLower() == "b" || pChild.Name.ToLower() == "strong")
                        {
                            var run = new Run();
                            var runProperties = new RunProperties();
                            runProperties.AppendChild(new Bold());
                            run.AppendChild(runProperties);
                            run.AppendChild(new Text(pChild.InnerText) { Space = SpaceProcessingModeValues.Preserve });
                            paragraph.AppendChild(run);
                        }
                        else if (pChild.Name.ToLower() == "i" || pChild.Name.ToLower() == "em")
                        {
                            var run = new Run();
                            var runProperties = new RunProperties();
                            runProperties.AppendChild(new Italic());
                            run.AppendChild(runProperties);
                            run.AppendChild(new Text(pChild.InnerText) { Space = SpaceProcessingModeValues.Preserve });
                            paragraph.AppendChild(run);
                        }
                        else if (pChild.Name.ToLower() == "u")
                        {
                            var run = new Run();
                            var runProperties = new RunProperties();
                            runProperties.AppendChild(new Underline() { Val = UnderlineValues.Single });
                            run.AppendChild(runProperties);
                            run.AppendChild(new Text(pChild.InnerText) { Space = SpaceProcessingModeValues.Preserve });
                            paragraph.AppendChild(run);
                        }
                        else if (pChild.Name.ToLower() == "span" && pChild.Attributes["style"] != null)
                        {
                            var run = new Run();
                            var runProperties = new RunProperties();
                            bool hasProps = false;
                            
                            string style = pChild.Attributes["style"].Value.ToLower();
                            
                            // 处理文本颜色
                            if (style.Contains("color:"))
                            {
                                var colorMatch = Regex.Match(style, @"color\s*:\s*([^;]+)");
                                if (colorMatch.Success)
                                {
                                    string colorValue = colorMatch.Groups[1].Value.Trim();
                                    string hexColor = ExtractColor(colorValue);
                                    if (!string.IsNullOrEmpty(hexColor))
                                    {
                                        runProperties.AppendChild(new Color() { Val = hexColor });
                                        hasProps = true;
                                    }
                                }
                            }
                            
                            if (hasProps)
                            {
                                run.AppendChild(runProperties);
                            }
                            
                            run.AppendChild(new Text(pChild.InnerText) { Space = SpaceProcessingModeValues.Preserve });
                            paragraph.AppendChild(run);
                        }
                        else
                        {
                            // 其他元素默认文本处理
                            var run = new Run();
                            run.AppendChild(new Text(pChild.InnerText) { Space = SpaceProcessingModeValues.Preserve });
                            paragraph.AppendChild(run);
                        }
                    }
                    
                    tableCell.AppendChild(paragraph);
                    hasAddedParagraph = true;
                }
                else if (childNode.Name.ToLower() == "span" || childNode.Name.ToLower() == "b" || 
                         childNode.Name.ToLower() == "i" || childNode.Name.ToLower() == "u")
                {
                    // 处理基本文本格式元素
                    var paragraph = new Paragraph();
                    var run = new Run();
                    var runProperties = new RunProperties();
                    bool hasProps = false;
                    
                    // 应用样式
                    if (childNode.Name.ToLower() == "b" || childNode.Name.ToLower() == "strong")
                    {
                        runProperties.AppendChild(new Bold());
                        hasProps = true;
                    }
                    if (childNode.Name.ToLower() == "i" || childNode.Name.ToLower() == "em")
                    {
                        runProperties.AppendChild(new Italic());
                        hasProps = true;
                    }
                    if (childNode.Name.ToLower() == "u")
                    {
                        runProperties.AppendChild(new Underline() { Val = UnderlineValues.Single });
                        hasProps = true;
                    }
                    
                    // 检查style属性
                    if (childNode.Attributes["style"] != null)
                    {
                        string style = childNode.Attributes["style"].Value.ToLower();
                        
                        // 处理文本颜色
                        if (style.Contains("color:"))
                        {
                            var colorMatch = Regex.Match(style, @"color\s*:\s*([^;]+)");
                            if (colorMatch.Success)
                            {
                                string colorValue = colorMatch.Groups[1].Value.Trim();
                                string hexColor = ExtractColor(colorValue);
                                if (!string.IsNullOrEmpty(hexColor))
                                {
                                    runProperties.AppendChild(new Color() { Val = hexColor });
                                    hasProps = true;
                                }
                            }
                        }
                    }
                    
                    if (hasProps)
                    {
                        run.AppendChild(runProperties);
                    }
                    
                    run.AppendChild(new Text(childNode.InnerText) { Space = SpaceProcessingModeValues.Preserve });
                    paragraph.AppendChild(run);
                    tableCell.AppendChild(paragraph);
                    hasAddedParagraph = true;
                }
            }
            
            // 确保至少有一个段落
            if (!hasAddedParagraph)
            {
                tableCell.AppendChild(new Paragraph(new Run(new Text(""))));
            }
        }
        
        /// <summary>
        /// 提取节点的背景色
        /// </summary>
        private static string ExtractBackgroundColor(HtmlNode node)
        {
            string bgcolor = null;
            
            // 1. 检查style属性中的background-color
            if (node.Attributes["style"] != null)
            {
                string style = node.Attributes["style"].Value;
                
                // 匹配background-color属性
                var bgColorMatch = Regex.Match(style, @"background-color\s*:\s*([^;]+)");
                if (bgColorMatch.Success)
                {
                    bgcolor = bgColorMatch.Groups[1].Value.Trim();
                    bgcolor = ExtractColor(bgcolor);
                    return bgcolor;
                }
                
                // 匹配background属性
                var bgMatch = Regex.Match(style, @"background\s*:\s*([^;]+)");
                if (bgMatch.Success)
                {
                    string bg = bgMatch.Groups[1].Value.Trim();
                    // 从background属性中提取颜色
                    var colorMatch = Regex.Match(bg, @"#[0-9a-fA-F]{3,6}|rgb\s*\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*\)|rgba\s*\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*[\d\.]+\s*\)|[a-zA-Z]+");
                    if (colorMatch.Success)
                    {
                        bgcolor = colorMatch.Value;
                        bgcolor = ExtractColor(bgcolor);
                        return bgcolor;
                    }
                }
            }
            
            // 2. 检查bgcolor属性
            if (node.Attributes["bgcolor"] != null)
            {
                bgcolor = node.Attributes["bgcolor"].Value;
                bgcolor = ExtractColor(bgcolor);
                return bgcolor;
            }
            
            // 3. 基于内容确定背景色（只针对tr行）
            if (node.Name.ToLower() == "tr")
            {
                string text = node.InnerText.Trim().ToLower();
                
                // 请求参数标题行 - 橙色
                if (text.Contains("请求参数") || text.Contains("body请求参数") || 
                    text.Contains("post请求") || text.Contains("参数列表"))
                {
                    return "F79646"; // 橙色
                }
                
                // 返回状态码或结果集标题行 - 绿色
                if (text.Contains("返回状态码") || text.Contains("结果集参数") || 
                    text.Contains("回参") || text.Contains("返回值"))
                {
                    return "9BBB59"; // 绿色
                }
                
                // 参数名等表头行 - 蓝色
                if (text.Contains("参数名") || text.Contains("参数类型") || 
                    text.Contains("是否必填") || text.Contains("说明") ||
                    text.Contains("状态码") && !text.Contains("返回状态码"))
                {
                    return "4F81BD"; // 蓝色
                }
            }
            
            // 4. 检查class属性中的特殊标记
            if (node.Attributes["class"] != null)
            {
                string className = node.Attributes["class"].Value.ToLower();
                
                // 特殊背景色标记
                if (className.Contains("orange") || className.Contains("warning") || 
                    className.Contains("请求参数"))
                {
                    return "F79646"; // 橙色
                }
                
                if (className.Contains("green") || className.Contains("success") || 
                    className.Contains("状态码"))
                {
                    return "9BBB59"; // 绿色
                }
                
                if (className.Contains("blue") || className.Contains("header") || 
                    className.Contains("参数"))
                {
                    return "4F81BD"; // 蓝色
                }
            }
            
            // 5. 表头单元格默认设置
            if (node.Name.ToLower() == "th")
            {
                return "4F81BD"; // 默认表头蓝色
            }
            
            return null;
        }
        
        /// <summary>
        /// 提取颜色值并转换为十六进制格式
        /// </summary>
        private static string ExtractColor(string colorValue)
        {
            if (string.IsNullOrEmpty(colorValue))
                return null;
                
            colorValue = colorValue.Trim().ToLower();
            
            // 十六进制颜色
            if (colorValue.StartsWith("#"))
            {
                string hex = colorValue.Substring(1);
                
                // 将3位十六进制转换为6位
                if (hex.Length == 3)
                {
                    hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
                }
                
                return hex;
            }
            
            // RGB颜色
            if (colorValue.StartsWith("rgb"))
            {
                var match = Regex.Match(colorValue, @"rgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)");
                if (match.Success)
                {
                    int r = int.Parse(match.Groups[1].Value);
                    int g = int.Parse(match.Groups[2].Value);
                    int b = int.Parse(match.Groups[3].Value);
                    
                    return $"{r:X2}{g:X2}{b:X2}";
                }
            }
            
            // 命名颜色
            Dictionary<string, string> namedColors = new Dictionary<string, string>
            {
                { "aqua", "00FFFF" },
                { "black", "000000" },
                { "blue", "0000FF" },
                { "fuchsia", "FF00FF" },
                { "gray", "808080" },
                { "green", "008000" },
                { "lime", "00FF00" },
                { "maroon", "800000" },
                { "navy", "000080" },
                { "olive", "808000" },
                { "purple", "800080" },
                { "red", "FF0000" },
                { "silver", "C0C0C0" },
                { "teal", "008080" },
                { "white", "FFFFFF" },
                { "yellow", "FFFF00" }
            };
            
            if (namedColors.ContainsKey(colorValue))
            {
                return namedColors[colorValue];
            }
            
            return null;
        }
    }
} 