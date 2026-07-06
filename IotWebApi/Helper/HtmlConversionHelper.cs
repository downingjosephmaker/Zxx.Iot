using Microsoft.AspNetCore.StaticFiles;
using System.Text;
using IotWebApi.Helper;

namespace IotWebApi
{
    /// <summary>
    /// HTML转换辅助类
    /// 提供将HTML转换为各种文档格式的功能
    /// </summary>
    public class HtmlConversionHelper
    {
        /// <summary>
        /// 静态页面转文件
        /// </summary>
        /// <param name="html">静态页面html</param>
        /// <param name="type">文件类型</param>
        /// <param name="contenttype">上下文类型</param>
        /// <returns>文件流</returns>
        public Stream ConvertHtmlToFile(string html, string type, out string contenttype)
        {
            string fileName = Guid.NewGuid().ToString() + type;
            string dirPath = Path.Combine(OperatorCommon.NetLocalfile, "ApiFiles");
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
            var filePath = Path.Combine(dirPath, fileName);
            FileStream fileStream = null;
            var provider = new FileExtensionContentTypeProvider();
            contenttype = provider.Mappings[type];

            try
            {
                // 根据不同的文件类型进行不同的处理
                switch (type.ToLower())
                {
                    case ".html":
                        SaveHtmlToFile(html, filePath);
                        break;
                    case ".docx":
                        SaveHtmlToDocx(html, filePath);
                        break;
                    case ".svg":
                        SaveHtmlToSvg(html, filePath);
                        break;
                    case ".xml":
                        // 保存为XML格式
                        SaveHtmlToFile(html, filePath);
                        break;
                    default:
                        // 默认保存为HTML
                        SaveHtmlToFile(html, filePath);
                        break;
                }

                // 打开文件流并返回
                fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                var fileData = ByteHelper.StreamToBytes(fileStream);
                var outputStream = ByteHelper.BytesToStream(fileData);
                return outputStream;
            }
            catch (Exception ex)
            {
                throw new Exception($"转换HTML到{type}文件失败: {ex.Message}", ex);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
                if (File.Exists(filePath))
                {
                    // 操作完成后删除临时文件
                    File.Delete(filePath);
                }
            }
        }

        /// <summary>
        /// 保存HTML到文件
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <param name="filePath">文件路径</param>
        private void SaveHtmlToFile(string html, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
            {
                writer.Write(html);
            }
        }

        /// <summary>
        /// 保存HTML到DOCX文件
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <param name="filePath">文件路径</param>
        private void SaveHtmlToDocx(string html, string filePath)
        {
            // 使用新的DocxConverter类处理DOCX转换
            var docxConverter = new DocxConverter();
            docxConverter.ConvertHtmlToDocx(html, filePath);
        }

        /// <summary>
        /// 保存HTML到SVG文件
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <param name="filePath">文件路径</param>
        private void SaveHtmlToSvg(string html, string filePath)
        {
            // 构建SVG结构
            var svg = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>
<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" width=""800"" height=""600"">
    <foreignObject width=""100%"" height=""100%"">
        <div xmlns=""http://www.w3.org/1999/xhtml"" style=""font-family: Arial, Helvetica, sans-serif;"">
            {html}
        </div>
    </foreignObject>
</svg>";

            // 保存SVG文件
            File.WriteAllText(filePath, svg, Encoding.UTF8);
        }
    }
}