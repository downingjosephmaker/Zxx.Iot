using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using IotWebApi.Common;

namespace IotWebApi.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiController]
    [ControllSort("50-1")]
    public class SwaggerController : ControllerBase
    {
        private readonly SwaggerGenerator _swaggerGenerator;
        private readonly HtmlConversionHelper _htmlConverter;

        public SwaggerController(HtmlConversionHelper htmlConverter, SwaggerGenerator swaggerGenerator)
        {
            _htmlConverter = htmlConverter;
            _swaggerGenerator = swaggerGenerator;
        }
        /// <summary>
        /// 导出文件
        /// </summary>
        /// <param name="type">文件类型</param>
        /// <param name="version">版本号V1</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        public FileResult ExportWord(string type, string version)
        {
            string contenttype = string.Empty;

            var model = _swaggerGenerator.GetSwagger(version); //1. 根据指定版本获取指定版本的json对象。

            var dicapi = model.Components.Schemas.ToDictionary();

            foreach (var apitag in model.Tags)
            {
                string apiname = $"/{apitag.Name}/";
                foreach (var item in model.Paths)
                {
                    if (item.Key.IndexOf(apiname) > -1)
                    {
                        if (item.Value.Operations != null)
                        {
                            foreach (var operation in item.Value.Operations)
                            {
                                var a1 = operation.Value.Summary;
                                var a2 = item.Key;
                                var a3 = operation.Key;
                                if (operation.Value.Parameters != null && operation.Value.Parameters.Count > 0)
                                {
                                    List<IOpenApiParameter> headerlist = new List<IOpenApiParameter>();
                                    List<IOpenApiParameter> querylist = new List<IOpenApiParameter>();
                                    foreach (var param in operation.Value.Parameters)
                                    {
                                        if (param.In == ParameterLocation.Header)
                                        {
                                            headerlist.Add(param);
                                        }
                                        else if (param.In == ParameterLocation.Query)
                                        {
                                            querylist.Add(param);
                                        }
                                    }
                                }

                                if (operation.Value.RequestBody != null && operation.Value.RequestBody.Content.Count > 0)
                                {
                                    //递归写  另外写实体类
                                    //类ID，实体类字段+名称
                                    var dicschema = SwaggerHelper.GetRequestBody(operation.Value.RequestBody, dicapi);
                                    if (dicschema.Count > 0)
                                    {
                                        foreach (string schemakey in dicschema.Keys)
                                        {
                                            foreach (var proper in dicapi[schemakey].Properties)
                                            {
                                                string aa = proper.Key;
                                                var bb = proper.Value;
                                            }
                                        }
                                    }
                                }
                                if (operation.Value.Responses != null && operation.Value.Responses.Values.Count > 0)
                                {
                                    var dicschema = SwaggerHelper.GetResponses(operation.Value.Responses, dicapi);
                                    if (dicschema.Count > 0)
                                    {
                                        foreach (string schemakey in dicschema.Keys)
                                        {
                                            var aaa = dicschema[schemakey];
                                            foreach (var proper in dicapi[schemakey].Properties)
                                            {
                                            var a = proper.Key;
                                            var b = proper.Value.Type;
                                            // 2.x：移除了 Nullable 属性，可空性通过 Type 标志位（JsonSchemaType.Null）表达
                                            var c = proper.Value.Type.HasValue && proper.Value.Type.Value.HasFlag(JsonSchemaType.Null);
                                            var d = proper.Value.Description;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var htmlpath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "swagger", "SwaggerDoc.cshtml");
            var html = HtmlHelper.GeneritorSwaggerHtml(htmlpath, model); //2. 根据模板引擎生成html

            var op = _htmlConverter.ConvertHtmlToFile(html, type, out contenttype); //3.将html导出文件类型

            return File(op, contenttype, $"数据共享平台接口文档{type}");
        }
    }
}
