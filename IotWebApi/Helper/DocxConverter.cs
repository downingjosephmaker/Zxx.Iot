using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace IotWebApi.Helper
{
    /// <summary>
    /// HTML转DOCX转换器
    /// </summary>
    public class DocxConverter
    {
        /// <summary>
        /// 将HTML文本转换为DOCX格式
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <param name="filePath">输出文件路径</param>
        public void ConvertHtmlToDocx(string html, string filePath)
        {
            // 强制为表格添加背景色样式
            html = ForceApplyTableStyles(html);
            
            // 创建Word文档
            using (WordprocessingDocument doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                // 添加主文档部分
                MainDocumentPart mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // 清理HTML并解析内容
                var htmlDoc = new HtmlDocument();
                // 设置正确的解析选项以确保所有样式属性都被读取
                htmlDoc.OptionFixNestedTags = true;
                htmlDoc.OptionAutoCloseOnEnd = true;
                htmlDoc.OptionReadEncoding = true;
                htmlDoc.OptionDefaultStreamEncoding = Encoding.UTF8;
                // 自动检测编码
                htmlDoc.OptionCheckSyntax = true;
                // 移除不支持的选项

                // 预处理HTML，强制为表格相关元素添加内联背景色
                html = PreprocessHtml(html);
                
                htmlDoc.LoadHtml(html);

                // 创建转换器实例
                DocxConverter converter = new DocxConverter();

                // 获取HTML的body标签内容
                HtmlNode bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
                if (bodyNode == null)
                {
                    bodyNode = htmlDoc.DocumentNode;
                }

                // 添加预处理步骤 - 确保样式属性被正确处理
                converter.CleanAndNormalizeHtml(bodyNode);

                // 处理HTML节点
                converter.ProcessNode(bodyNode, body, mainPart);

                // 保存文档
                mainPart.Document.Save();
            }
        }

        /// <summary>
        /// 清理和规范化HTML，确保样式被正确处理
        /// </summary>
        private void CleanAndNormalizeHtml(HtmlNode node)
        {
            // 如果是表格相关元素，确保背景色正确
            if (node.Name == "table" || node.Name == "tr" || node.Name == "td" || node.Name == "th")
            {
                string bgColor = null;
                
                // 首先从style属性中提取背景色
                if (node.Attributes["style"] != null)
                {
                    string style = node.Attributes["style"].Value;
                    var bgColorMatch = Regex.Match(style, @"background-color\s*:\s*([^;]+)");
                    if (bgColorMatch.Success)
                    {
                        bgColor = bgColorMatch.Groups[1].Value.Trim();
                    }
                }
                
                // 如果没有从style中找到背景色，检查bgcolor属性
                if (string.IsNullOrEmpty(bgColor) && node.Attributes["bgcolor"] != null)
                {
                    bgColor = node.Attributes["bgcolor"].Value;
                }
                
                // 如果是表头行或表头单元格，强制使用蓝色背景
                if (string.IsNullOrEmpty(bgColor) && 
                    (node.Name == "th" || 
                     (node.Name == "tr" && node.SelectNodes(".//th") != null && node.SelectNodes(".//th").Count > 0) ||
                     (node.ParentNode != null && node.ParentNode.Name == "tr" && 
                      node.ParentNode.SelectNodes(".//th") != null && node.ParentNode.SelectNodes(".//th").Count > 0)))
                {
                    bgColor = "#4F81BD"; // 蓝色背景
                }
                
                // 如果找到背景色，确保它作为内联样式存在，便于后续处理
                if (!string.IsNullOrEmpty(bgColor))
                {
                    string currentStyle = node.GetAttributeValue("style", "");
                    if (!currentStyle.Contains("background-color"))
                    {
                        node.SetAttributeValue("style", 
                            currentStyle + (string.IsNullOrEmpty(currentStyle) ? "" : ";") + 
                            $"background-color:{bgColor}");
                    }
                }
            }

            // 递归处理所有子节点
            foreach (var childNode in node.ChildNodes.ToList())
            {
                CleanAndNormalizeHtml(childNode);
            }
        }

        /// <summary>
        /// 生成样式定义
        /// </summary>
        private void GenerateStyleDefinitionsPart(StyleDefinitionsPart styleDefinitionsPart)
        {
            // 基本样式定义
            Styles styles = new Styles();

            // 默认段落样式
            Style style = new Style() { Type = StyleValues.Paragraph, StyleId = "Normal", Default = true };
            StyleName styleName = new StyleName() { Val = "Normal" };
            style.Append(styleName);
            styles.Append(style);

            // 标题样式
            style = new Style() { Type = StyleValues.Paragraph, StyleId = "Heading1" };
            styleName = new StyleName() { Val = "heading 1" };
            ParagraphProperties paragraphProperties = new ParagraphProperties();

            RunProperties runProperties = new RunProperties();
            RunFonts runFonts = new RunFonts() { Ascii = "Calibri Light", HighAnsi = "Calibri Light" };
            Bold bold = new Bold();
            Color color = new Color() { Val = "2F5496" };
            FontSize fontSize = new FontSize() { Val = "32" };

            runProperties.Append(runFonts);
            runProperties.Append(bold);
            runProperties.Append(color);
            runProperties.Append(fontSize);

            style.Append(styleName);
            style.Append(paragraphProperties);
            style.Append(runProperties);
            styles.Append(style);

            styleDefinitionsPart.Styles = styles;
        }

        /// <summary>
        /// 处理HTML节点转为Word元素
        /// </summary>
        private void ProcessNode(HtmlNode node, Body body, MainDocumentPart mainPart)
        {
            // 处理文本节点
            if (node.NodeType == HtmlNodeType.Text)
            {
                string text = node.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var paragraph = new Paragraph();
                    var run = new Run();
                    run.AppendChild(new Text(text));
                    paragraph.AppendChild(run);
                    body.AppendChild(paragraph);
                }
                return;
            }

            // 特殊节点处理
            if (node.Name.ToLower() == "br")
            {
                body.AppendChild(new Paragraph());
                return;
            }

            // 处理段落和标题等
            if (node.Name.ToLower() == "p")
            {
                var paragraph = new Paragraph();
                ProcessChildNodes(node, paragraph, mainPart);
                body.AppendChild(paragraph);
                return;
            }

            if (node.Name.ToLower() == "h1")
            {
                var paragraph = new Paragraph();
                var paragraphProperties = new ParagraphProperties();
                paragraphProperties.ParagraphStyleId = new ParagraphStyleId() { Val = "Heading1" };
                paragraph.AppendChild(paragraphProperties);

                ProcessChildNodes(node, paragraph, mainPart);
                body.AppendChild(paragraph);
                return;
            }

            if (node.Name.ToLower() == "h2" || node.Name.ToLower() == "h3")
            {
                var paragraph = new Paragraph();
                var run = new Run();
                var runProperties = new RunProperties();
                runProperties.AppendChild(new Bold());
                runProperties.AppendChild(new FontSize() { Val = "28" });
                run.AppendChild(runProperties);

                run.AppendChild(new Text(node.InnerText));
                paragraph.AppendChild(run);
                body.AppendChild(paragraph);
                return;
            }

            if (node.Name.ToLower() == "table")
            {
                ProcessTable(node, body, mainPart);
                return;
            }

            // 处理子节点
            foreach (var childNode in node.ChildNodes)
            {
                ProcessNode(childNode, body, mainPart);
            }
        }

        /// <summary>
        /// 处理子节点到段落
        /// </summary>
        private void ProcessChildNodes(HtmlNode parentNode, Paragraph paragraph, MainDocumentPart mainPart)
        {
            foreach (var node in parentNode.ChildNodes)
            {
                if (node.NodeType == HtmlNodeType.Text)
                {
                    string text = node.InnerText;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var run = new Run();

                        // 检查父节点是否有样式需要应用
                        RunProperties runProps = new RunProperties();
                        bool hasProps = false;

                        // 应用文本样式
                        if (parentNode.Name.ToLower() == "b" || parentNode.Name.ToLower() == "strong")
                        {
                            runProps.AppendChild(new Bold());
                            hasProps = true;
                        }
                        if (parentNode.Name.ToLower() == "i" || parentNode.Name.ToLower() == "em")
                        {
                            runProps.AppendChild(new Italic());
                            hasProps = true;
                        }
                        if (parentNode.Name.ToLower() == "u")
                        {
                            runProps.AppendChild(new Underline() { Val = UnderlineValues.Single });
                            hasProps = true;
                        }
                        if (parentNode.Name.ToLower() == "strike" || parentNode.Name.ToLower() == "s" || parentNode.Name.ToLower() == "del")
                        {
                            runProps.AppendChild(new Strike());
                            hasProps = true;
                        }

                        // 应用字体颜色
                        if (parentNode.Attributes["style"] != null)
                        {
                            string style = parentNode.Attributes["style"].Value.ToLower();

                            // 提取颜色
                            if (style.Contains("color:"))
                            {
                                int start = style.IndexOf("color:") + "color:".Length;
                                int end = style.IndexOf(";", start);
                                if (end == -1) end = style.Length;

                                string colorValue = style.Substring(start, end - start).Trim();
                                string hexColor = ConvertHtmlColorToHex(colorValue);

                                if (!string.IsNullOrEmpty(hexColor))
                                {
                                    runProps.AppendChild(new Color() { Val = hexColor });
                                    hasProps = true;
                                }
                            }

                            // 提取字体大小
                            if (style.Contains("font-size:"))
                            {
                                int start = style.IndexOf("font-size:") + "font-size:".Length;
                                int end = style.IndexOf(";", start);
                                if (end == -1) end = style.Length;

                                string sizeValue = style.Substring(start, end - start).Trim();

                                if (sizeValue.EndsWith("px"))
                                {
                                    if (double.TryParse(sizeValue.Substring(0, sizeValue.Length - 2), out double pixelSize))
                                    {
                                        // 将像素大小转换为半点 (1pt = 2半点)
                                        int halfPoints = (int)(pixelSize * 2);
                                        runProps.AppendChild(new FontSize() { Val = halfPoints.ToString() });
                                        hasProps = true;
                                    }
                                }
                                else if (sizeValue.EndsWith("pt"))
                                {
                                    if (double.TryParse(sizeValue.Substring(0, sizeValue.Length - 2), out double pointSize))
                                    {
                                        // 将点大小转换为半点 (1pt = 2半点)
                                        int halfPoints = (int)(pointSize * 2);
                                        runProps.AppendChild(new FontSize() { Val = halfPoints.ToString() });
                                        hasProps = true;
                                    }
                                }
                            }

                            // 提取字体
                            if (style.Contains("font-family:"))
                            {
                                int start = style.IndexOf("font-family:") + "font-family:".Length;
                                int end = style.IndexOf(";", start);
                                if (end == -1) end = style.Length;

                                string fontFamily = style.Substring(start, end - start).Trim().Replace("'", "").Replace("\"", "");
                                if (!string.IsNullOrEmpty(fontFamily))
                                {
                                    string[] fonts = fontFamily.Split(',');
                                    if (fonts.Length > 0)
                                    {
                                        string primaryFont = fonts[0].Trim();
                                        runProps.AppendChild(new RunFonts() { Ascii = primaryFont, HighAnsi = primaryFont });
                                        hasProps = true;
                                    }
                                }
                            }
                        }

                        if (hasProps)
                        {
                            run.AppendChild(runProps);
                        }

                        run.AppendChild(new Text(text));
                        paragraph.AppendChild(run);
                    }
                }
                else if (node.Name.ToLower() == "b" || node.Name.ToLower() == "strong")
                {
                    var run = new Run();
                    var runProperties = new RunProperties();
                    runProperties.AppendChild(new Bold());
                    run.AppendChild(runProperties);
                    run.AppendChild(new Text(node.InnerText));
                    paragraph.AppendChild(run);
                }
                else if (node.Name.ToLower() == "i" || node.Name.ToLower() == "em")
                {
                    var run = new Run();
                    var runProperties = new RunProperties();
                    runProperties.AppendChild(new Italic());
                    run.AppendChild(runProperties);
                    run.AppendChild(new Text(node.InnerText));
                    paragraph.AppendChild(run);
                }
                else if (node.Name.ToLower() == "u")
                {
                    var run = new Run();
                    var runProperties = new RunProperties();
                    runProperties.AppendChild(new Underline() { Val = UnderlineValues.Single });
                    run.AppendChild(runProperties);
                    run.AppendChild(new Text(node.InnerText));
                    paragraph.AppendChild(run);
                }
                else if (node.Name.ToLower() == "a")
                {
                    var run = new Run();
                    var runProperties = new RunProperties();
                    runProperties.AppendChild(new Color() { Val = "0000FF" });
                    runProperties.AppendChild(new Underline() { Val = UnderlineValues.Single });
                    run.AppendChild(runProperties);
                    run.AppendChild(new Text(node.InnerText));
                    paragraph.AppendChild(run);
                }
                else
                {
                    ProcessChildNodes(node, paragraph, mainPart);
                }
            }
        }

        /// <summary>
        /// 处理表格
        /// </summary>
        private void ProcessTable(HtmlNode tableNode, Body body, MainDocumentPart mainPart)
        {
            try
            {
                // 使用专用表格转换器处理表格，确保背景色正确转换
                Table table = HtmlTableToDocxConverter.ConvertHtmlTableToDocxTable(tableNode, mainPart);
                body.AppendChild(table);
            }
            catch (Exception)
            {
                // 如果转换失败，使用默认的表格处理逻辑
                // 创建基本表格
                var table = new Table();
                
                // 设置表格属性
                var tablePr = new TableProperties();
                var tableBorders = new TableBorders(
                    new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                    new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                    new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                    new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                    new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                    new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
                );
                tablePr.AppendChild(tableBorders);
                var tableWidth = new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct };
                tablePr.AppendChild(tableWidth);
                table.AppendChild(tablePr);
                
                // 处理表格行
                var rows = tableNode.SelectNodes(".//tr");
                if (rows != null)
                {
                    bool isFirstRow = true;
                    foreach (var rowNode in rows)
                    {
                        var row = new TableRow();
                        
                        // 为第一行应用蓝色背景色作为表头
                        if (isFirstRow)
                        {
                            var rowProperties = new TableRowProperties();
                            var rowShading = new Shading()
                            {
                                Val = ShadingPatternValues.Clear,
                                Color = "auto",
                                Fill = "4F81BD" // 蓝色
                            };
                            rowProperties.AppendChild(rowShading);
                            row.AppendChild(rowProperties);
                            isFirstRow = false;
                        }
                        else
                        {
                            // 检查行是否有背景色
                            if (rowNode.Attributes["style"] != null)
                            {
                                string style = rowNode.Attributes["style"].Value;
                                var bgColorMatch = Regex.Match(style, @"background-color\s*:\s*([^;]+)");
                                if (bgColorMatch.Success)
                                {
                                    string bgColor = bgColorMatch.Groups[1].Value.Trim();
                                    if (bgColor.StartsWith("#")) bgColor = bgColor.Substring(1);
                                    
                                    var rowProperties = new TableRowProperties();
                                    var rowShading = new Shading()
                                    {
                                        Val = ShadingPatternValues.Clear,
                                        Color = "auto",
                                        Fill = bgColor
                                    };
                                    rowProperties.AppendChild(rowShading);
                                    row.AppendChild(rowProperties);
                                }
                            }
                        }
                        
                        // 处理单元格
                        var cells = rowNode.SelectNodes(".//td|.//th");
                        if (cells != null)
                        {
                            foreach (var cellNode in cells)
                            {
                                var tableCell = new TableCell();
                                var cellProperties = new TableCellProperties();
                                
                                // 检查单元格是否有背景色
                                string cellBgColor = null;
                                if (cellNode.Attributes["style"] != null)
                                {
                                    string style = cellNode.Attributes["style"].Value;
                                    var bgColorMatch = Regex.Match(style, @"background-color\s*:\s*([^;]+)");
                                    if (bgColorMatch.Success)
                                    {
                                        cellBgColor = bgColorMatch.Groups[1].Value.Trim();
                                        if (cellBgColor.StartsWith("#")) cellBgColor = cellBgColor.Substring(1);
                                        
                                        var shading = new Shading()
                                        {
                                            Val = ShadingPatternValues.Clear,
                                            Color = "auto",
                                            Fill = cellBgColor
                                        };
                                        cellProperties.AppendChild(shading);
                                    }
                                }
                                
                                // 如果单元格是th，强制应用蓝色背景
                                if (cellNode.Name.ToLower() == "th" && string.IsNullOrEmpty(cellBgColor))
                                {
                                    var shading = new Shading()
                                    {
                                        Val = ShadingPatternValues.Clear,
                                        Color = "auto",
                                        Fill = "4F81BD" // 蓝色
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
                                if (cellNode.Attributes["colspan"] != null)
                                {
                                    int colspan;
                                    if (int.TryParse(cellNode.Attributes["colspan"].Value, out colspan) && colspan > 1)
                                    {
                                        cellProperties.AppendChild(new GridSpan() { Val = colspan });
                                    }
                                }
                                
                                if (cellNode.Attributes["rowspan"] != null)
                                {
                                    int rowspan;
                                    if (int.TryParse(cellNode.Attributes["rowspan"].Value, out rowspan) && rowspan > 1)
                                    {
                                        cellProperties.AppendChild(new VerticalMerge() { Val = MergedCellValues.Restart });
                                    }
                                }
                                
                                tableCell.AppendChild(cellProperties);
                                
                                // 处理单元格内容
                                var paragraph = new Paragraph();
                                var run = new Run();
                                run.AppendChild(new Text(cellNode.InnerText.Trim()));
                                paragraph.AppendChild(run);
                                tableCell.AppendChild(paragraph);
                                
                                row.AppendChild(tableCell);
                            }
                        }
                        
                        table.AppendChild(row);
                    }
                }
                
                body.AppendChild(table);
            }
        }

        /// <summary>
        /// 提取节点的背景色
        /// </summary>
        private string ExtractBackgroundColor(HtmlNode node)
        {
            string bgcolor = null;

            // 1. 检查style属性中的background-color
            if (node.Attributes["style"] != null)
            {
                string style = node.Attributes["style"].Value;

                // 匹配background-color或background属性
                var bgColorMatch = Regex.Match(style, @"background-color\s*:\s*([^;]+)");
                if (bgColorMatch.Success)
                {
                    bgcolor = bgColorMatch.Groups[1].Value.Trim();
                }
                else
                {
                    var bgMatch = Regex.Match(style, @"background\s*:\s*([^;]+)");
                    if (bgMatch.Success)
                    {
                        string bg = bgMatch.Groups[1].Value.Trim();
                        // 从background属性中提取颜色
                        var colorMatch = Regex.Match(bg, @"#[0-9a-fA-F]{3,6}|rgb\s*\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*\)|rgba\s*\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*[\d\.]+\s*\)|[a-zA-Z]+");
                        if (colorMatch.Success)
                        {
                            bgcolor = colorMatch.Value;
                        }
                    }
                }
            }

            // 2. 检查bgcolor属性
            if (bgcolor == null && node.Attributes["bgcolor"] != null)
            {
                bgcolor = node.Attributes["bgcolor"].Value;
            }

            // 3. 检查class属性中的背景色类名
            if (bgcolor == null && node.Attributes["class"] != null)
            {
                string className = node.Attributes["class"].Value;
                bgcolor = GetColorFromClassName(className);
            }

            // 4. 转换颜色格式为OpenXML兼容格式（6位十六进制，不带#）
            if (!string.IsNullOrEmpty(bgcolor))
            {
                bgcolor = ConvertHtmlColorToHex(bgcolor);
            }

            return bgcolor;
        }

        /// <summary>
        /// 解析类名中的颜色信息
        /// </summary>
        private string GetColorFromClassName(string className)
        {
            // 根据项目中的CSS命名约定来实现
            // 这里作为示例，假设使用类似'bg-blue', 'background-red'的命名方式
            var colorClasses = new Dictionary<string, string>
            {
                { "red", "#FF0000" },
                { "blue", "#0000FF" },
                { "green", "#00FF00" },
                { "yellow", "#FFFF00" },
                { "gray", "#808080" },
                // 添加更多常用颜色映射
            };

            foreach (var color in colorClasses.Keys)
            {
                if (className.Contains($"bg-{color}") || className.Contains($"background-{color}"))
                {
                    return colorClasses[color];
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 将HTML颜色转换为十六进制
        /// </summary>
        private string ConvertHtmlColorToHex(string htmlColor)
        {
            if (string.IsNullOrEmpty(htmlColor))
                return null;

            htmlColor = htmlColor.Trim().ToLower();

            // 已经是十六进制格式
            if (htmlColor.StartsWith("#"))
            {
                // 移除#并确保是6位
                string hex = htmlColor.Substring(1);
                if (hex.Length == 3)
                {
                    // 转换3位到6位 (#abc -> #aabbcc)
                    hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
                }
                return hex;
            }

            // RGB格式，如rgb(255, 0, 0)
            if (htmlColor.StartsWith("rgb"))
            {
                int start = htmlColor.IndexOf("(") + 1;
                int end = htmlColor.IndexOf(")");
                if (end > start)
                {
                    string[] values = htmlColor.Substring(start, end - start).Split(',');
                    if (values.Length >= 3)
                    {
                        try
                        {
                            int r = int.Parse(values[0].Trim());
                            int g = int.Parse(values[1].Trim());
                            int b = int.Parse(values[2].Trim());
                            return $"{r:X2}{g:X2}{b:X2}";
                        }
                        catch { }
                    }
                }
            }

            // 命名颜色
            if (IsNamedColor(htmlColor))
            {
                return GetHexColorFromName(htmlColor).Substring(1); // 移除#
            }

            return null;
        }

        /// <summary>
        /// 检查是否为命名颜色
        /// </summary>
        private bool IsNamedColor(string color)
        {
            if (string.IsNullOrEmpty(color))
                return false;

            color = color.Trim().ToLower();

            // 常见HTML颜色名称列表
            string[] namedColors = new string[] {
                "black", "white", "red", "green", "blue", "yellow", "purple",
                "gray", "silver", "maroon", "olive", "navy", "teal", "aqua",
                "orange", "pink", "brown", "cyan", "magenta", "lime"
            };

            return namedColors.Contains(color);
        }

        /// <summary>
        /// 从命名颜色获取十六进制颜色
        /// </summary>
        private string GetHexColorFromName(string colorName)
        {
            if (string.IsNullOrEmpty(colorName))
                return "#000000";

            colorName = colorName.Trim().ToLower();

            // 常见HTML颜色的十六进制表示
            Dictionary<string, string> colorMap = new Dictionary<string, string>
            {
                { "black", "#000000" },
                { "white", "#FFFFFF" },
                { "red", "#FF0000" },
                { "green", "#008000" },
                { "blue", "#0000FF" },
                { "yellow", "#FFFF00" },
                { "purple", "#800080" },
                { "gray", "#808080" },
                { "silver", "#C0C0C0" },
                { "maroon", "#800000" },
                { "olive", "#808000" },
                { "navy", "#000080" },
                { "teal", "#008080" },
                { "aqua", "#00FFFF" },
                { "orange", "#FFA500" },
                { "pink", "#FFC0CB" },
                { "brown", "#A52A2A" },
                { "cyan", "#00FFFF" },
                { "magenta", "#FF00FF" },
                { "lime", "#00FF00" }
            };

            if (colorMap.ContainsKey(colorName))
                return colorMap[colorName];

            return "#000000"; // 默认黑色
        }

        /// <summary>
        /// 对HTML进行预处理，确保表格样式被正确解析
        /// </summary>
        private string PreprocessHtml(string html)
        {
            try
            {
                // 1. 使用临时文档解析HTML，用于提取样式
                var tempDoc = new HtmlDocument();
                tempDoc.LoadHtml(html);

                // 2. 查找所有表格相关元素
                var tableElements = tempDoc.DocumentNode.SelectNodes("//table|//tr|//td|//th");
                if (tableElements != null)
                {
                    foreach (var element in tableElements)
                    {
                        // 处理可能的背景色
                        ProcessBackgroundColor(element);
                    }
                }

                // 返回修改后的HTML
                return tempDoc.DocumentNode.OuterHtml;
            }
            catch (Exception)
            {
                // 如果处理出错，返回原始HTML
                return html;
            }
        }

        /// <summary>
        /// 处理元素的背景色，确保转换为内联样式
        /// </summary>
        private void ProcessBackgroundColor(HtmlNode node)
        {
            string backgroundColor = null;

            // 1. 检查style属性中的背景色
            if (node.Attributes["style"] != null)
            {
                string style = node.Attributes["style"].Value;
                var bgColorMatch = Regex.Match(style, @"background-color\s*:\s*([^;]+)");
                if (bgColorMatch.Success)
                {
                    // 已经有内联背景色，不需要处理
                    return;
                }

                // 检查background属性
                var bgMatch = Regex.Match(style, @"background\s*:\s*([^;]+)");
                if (bgMatch.Success)
                {
                    string bg = bgMatch.Groups[1].Value.Trim();
                    // 从background属性中提取颜色
                    var colorMatch = Regex.Match(bg, @"#[0-9a-fA-F]{3,6}|rgb\s*\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*\)|rgba\s*\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*[\d\.]+\s*\)|[a-zA-Z]+");
                    if (colorMatch.Success)
                    {
                        backgroundColor = colorMatch.Value;
                    }
                }
            }

            // 2. 检查bgcolor属性
            if (backgroundColor == null && node.Attributes["bgcolor"] != null)
            {
                backgroundColor = node.Attributes["bgcolor"].Value;
            }

            // 3. 根据class推断背景色
            if (backgroundColor == null && node.Attributes["class"] != null)
            {
                string className = node.Attributes["class"].Value.ToLower();
                
                // 表头或header类
                if (className.Contains("header") || className.Contains("head"))
                {
                    backgroundColor = "#4F81BD"; // 蓝色
                }
                // 根据常见CSS类名推断颜色
                else if (className.Contains("blue"))
                {
                    backgroundColor = "#4F81BD";
                }
                else if (className.Contains("green") || className.Contains("success"))
                {
                    backgroundColor = "#9BBB59";
                }
                else if (className.Contains("red") || className.Contains("danger"))
                {
                    backgroundColor = "#C0504D";
                }
                else if (className.Contains("yellow") || className.Contains("warning"))
                {
                    backgroundColor = "#F79646";
                }
                else if (className.Contains("gray") || className.Contains("grey") || className.Contains("secondary"))
                {
                    backgroundColor = "#A5A5A5";
                }
            }

            // 4. 如果是th元素，默认添加蓝色背景
            if (backgroundColor == null && node.Name.ToLower() == "th")
            {
                backgroundColor = "#4F81BD";
            }

            // 如果图片中的表格颜色是天蓝色，直接为所有表格元素设置背景色
            if (backgroundColor == null && (node.Name.ToLower() == "table" || node.Name.ToLower() == "tr" || 
                                          node.Name.ToLower() == "td" || node.Name.ToLower() == "th"))
            {
                if (node.Name.ToLower() == "th" || 
                    (node.ParentNode != null && node.ParentNode.Name.ToLower() == "tr" && 
                     node.ParentNode.SelectNodes("th") != null))
                {
                    backgroundColor = "#4F81BD"; // 表头蓝色
                }
            }

            // 如果找到背景色，确保添加为内联样式
            if (!string.IsNullOrEmpty(backgroundColor))
            {
                string currentStyle = node.GetAttributeValue("style", "");
                // 检查当前样式是否已包含背景色
                if (!currentStyle.Contains("background-color"))
                {
                    string newStyle = currentStyle + (string.IsNullOrEmpty(currentStyle) ? "" : ";") + 
                        $"background-color:{backgroundColor}";
                    node.SetAttributeValue("style", newStyle);
                }
            }
        }

        /// <summary>
        /// 强制应用表格样式，确保表头蓝色背景
        /// </summary>
        private string ForceApplyTableStyles(string html)
        {
            // 创建临时HTML文档进行处理
            var tempDoc = new HtmlDocument();
            tempDoc.LoadHtml(html);
            
            var tables = tempDoc.DocumentNode.SelectNodes("//table");
            if (tables != null)
            {
                foreach (var table in tables)
                {
                    // 检查表格内的特殊行（特定标签行）
                    var allRows = table.SelectNodes(".//tr");
                    if (allRows != null)
                    {
                        foreach (var row in allRows)
                        {
                            // 提取行的文本内容
                            string rowText = row.InnerText.Trim().ToLower();
                            
                            // 1. 检查是否为请求参数标题行 - 橙色背景
                            if (rowText.Contains("请求参数") || rowText.Contains("body请求") || 
                                rowText.Contains("post请求") || rowText.Contains("参数列表"))
                            {
                                ApplyRowStyle(row, "#F79646", "white"); // 橙色
                                continue;
                            }
                            
                            // 2. 检查是否为返回状态码或结果集标题行 - 绿色背景
                            if (rowText.Contains("返回状态码") || rowText.Contains("结果集参数") || 
                                rowText.Contains("回参") || rowText.Contains("返回值"))
                            {
                                ApplyRowStyle(row, "#9BBB59", "white"); // 绿色
                                continue;
                            }
                            
                            // 3. 检查是否为状态码行 - 蓝色背景
                            if (rowText.Contains("状态码") && !rowText.Contains("返回状态码"))
                            {
                                ApplyRowStyle(row, "#4F81BD", "white"); // 蓝色
                                continue;
                            }
                            
                            // 4. 检查是否包含"参数名"等表头文字 - 蓝色背景
                            if (rowText.Contains("参数名") || rowText.Contains("参数类型") || 
                                rowText.Contains("是否必填") || rowText.Contains("说明"))
                            {
                                ApplyRowStyle(row, "#4F81BD", "white"); // 蓝色
                                continue;
                            }
                        }
                    }
                    
                    // 特殊处理：请求方式行保持白色
                    ProcessSpecialRows(table, "请求方式", "#FFFFFF", "black");
                }
            }
            
            return tempDoc.DocumentNode.OuterHtml;
        }
        
        /// <summary>
        /// 应用行样式
        /// </summary>
        private void ApplyRowStyle(HtmlNode row, string bgColor, string textColor)
        {
            // 应用行样式
            string currentStyle = row.GetAttributeValue("style", "");
            string newStyle = currentStyle + (string.IsNullOrEmpty(currentStyle) ? "" : ";") + 
                             $"background-color:{bgColor};color:{textColor}";
            row.SetAttributeValue("style", newStyle);
            
            // 应用到所有单元格
            var cells = row.SelectNodes(".//td|.//th");
            if (cells != null)
            {
                foreach (var cell in cells)
                {
                    string cellStyle = cell.GetAttributeValue("style", "");
                    string newCellStyle = cellStyle + (string.IsNullOrEmpty(cellStyle) ? "" : ";") + 
                                        $"background-color:{bgColor};color:{textColor}";
                    cell.SetAttributeValue("style", newCellStyle);
                }
            }
        }
        
        /// <summary>
        /// 处理特殊行（根据单元格内容）
        /// </summary>
        private void ProcessSpecialRows(HtmlNode table, string keyword, string bgColor, string textColor)
        {
            var rows = table.SelectNodes(".//tr");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    // 检查第一个单元格的内容
                    var firstCell = row.SelectSingleNode(".//td[1]");
                    if (firstCell != null && firstCell.InnerText.Trim().ToLower().Contains(keyword.ToLower()))
                    {
                        ApplyRowStyle(row, bgColor, textColor);
                    }
                }
            }
        }
    }
}