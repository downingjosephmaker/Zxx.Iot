using Microsoft.OpenApi;
using RazorEngine;
using RazorEngine.Templating;

namespace IotWebApi
{
    public class HtmlHelper
    {
        /// <summary>
        /// 将数据遍历静态页面中
        /// </summary>
        /// <param name="templatePath">静态页面地址</param>
        /// <param name="model">获取到的文件数据</param>
        /// <returns></returns>
        public static string GeneritorSwaggerHtml(string templatePath, OpenApiDocument model)
        {
            var template = System.IO.File.ReadAllText(templatePath);
            string docname = $"DRMA_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}";
            var result = Engine.Razor.RunCompile(template, docname, typeof(OpenApiDocument), model);
            return result;
        }
    }
}
