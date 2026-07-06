using Microsoft.OpenApi;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace IotWebApi
{
    public static class SwaggerDocExtension
    {
        /// <summary>
        /// 获取 schema 的引用 Id（兼容 2.x：引用通过 OpenApiSchemaReference 表示，不再有 schema.Reference 属性）。
        /// 非 schema 引用返回 null。
        /// </summary>
        public static string GetReferenceId(this IOpenApiSchema openApiSchema)
        {
            // OpenApiSchemaReference 是引用占位类型，其 Reference.Id 即目标 $ref id
            if (openApiSchema is OpenApiSchemaReference refSchema)
            {
                return refSchema.Reference?.Id;
            }
            return null;
        }

        /// <summary>
        /// 判断是否为 Object 类型
        /// </summary>
        /// <param name="openApiSchema"></param>
        /// <param name="schemas"></param>
        /// <returns></returns>
        public static bool IsObject(this IOpenApiSchema openApiSchema, IDictionary<string, IOpenApiSchema> schemas)
        {
            var refId = openApiSchema.GetReferenceId();
            if (openApiSchema.Type == null && refId != null)
            {
                var target = schemas.FirstOrDefault(x => x.Key == refId).Value;
                return target != null && (target.Enum == null || target.Enum.Count == 0);
            }
            return false;
        }

        /// <summary>
        /// 判断是否为枚举类型
        /// </summary>
        /// <param name="openApiSchema"></param>
        /// <param name="schemas"></param>
        /// <returns></returns>
        public static bool IsEnum(this IOpenApiSchema openApiSchema, IDictionary<string, IOpenApiSchema> schemas)
        {
            var refId = openApiSchema.GetReferenceId();
            if (refId != null)
            {
                var target = schemas.FirstOrDefault(x => x.Key == refId).Value;
                return target != null && target.Enum != null && target.Enum.Count != 0;
            }
            return false;
        }

        /// <summary>
        /// 判断是否为数组类型
        /// </summary>
        /// <param name="openApiSchema"></param>
        /// <returns></returns>
        public static bool IsArray(this IOpenApiSchema openApiSchema)
        {
            return openApiSchema.Type == JsonSchemaType.Array && openApiSchema.Items != null;
        }

        /// <summary>
        /// 判断是否为基础数组类型
        /// </summary>
        /// <param name="openApiSchema"></param>
        /// <returns></returns>
        public static bool IsBaseTypeArray(this IOpenApiSchema openApiSchema)
        {
            return openApiSchema.Type == JsonSchemaType.Array && openApiSchema.Items is { Type: { }, } && openApiSchema.Items.GetReferenceId() == null;
        }

        /// <summary>
        /// 判断是否为基本类型
        /// </summary>
        /// <param name="openApiSchema"></param>
        public static bool IsBaseType(this IOpenApiSchema openApiSchema)
        {
            return openApiSchema.Type != null;
        }

        /// <summary>
        /// 转换为 JSON 字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToWebJson(this object obj)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };
            return JsonSerializer.Serialize(obj, options);
        }

    }
}
