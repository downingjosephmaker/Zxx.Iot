using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace IotWebApi
{
    /// <summary>
    /// 根据字段 schema 的类型/格式生成对应的示例值（2.x：IOpenApiAny 已移除，改用 JsonNode 表示示例值）。
    /// 保留原有的类型/格式判定逻辑与值转换逻辑。
    /// </summary>
    public static class SwaggerOpenApiEntity
    {
        public static JsonNode CreateFor(IOpenApiSchema schema, object value)
        {
            if (value == null) return null;

            if (schema.Type == JsonSchemaType.Integer && schema.Format == "int64" && TryCast(value, out long longValue))
                return longValue;

            else if (schema.Type == JsonSchemaType.Integer && TryCast(value, out int intValue))
                return intValue;

            else if (schema.Type == JsonSchemaType.Number && schema.Format == "double" && TryCast(value, out double doubleValue))
                return doubleValue;

            else if (schema.Type == JsonSchemaType.Number && TryCast(value, out float floatValue))
                return floatValue;

            if (schema.Type == JsonSchemaType.Boolean && TryCast(value, out bool boolValue))
                return boolValue;

            else if (schema.Type == JsonSchemaType.String && schema.Format == "date" && TryCast(value, out DateTime dateValue))
                return dateValue.ToString("yyyy-MM-dd");

            else if (schema.Type == JsonSchemaType.String && schema.Format == "date-time" && TryCast(value, out DateTime dateTimeValue))
                return dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss");

            else if (schema.Type == JsonSchemaType.String && value.GetType().IsEnum)
                return Enum.GetName(value.GetType(), value);

            else if (schema.Type == JsonSchemaType.String)
                return value.ToString();

            return null;
        }

        private static bool TryCast<T>(object value, out T typedValue)
        {
            try
            {
                typedValue = (T)Convert.ChangeType(value, typeof(T));
                return true;
            }
            catch (InvalidCastException)
            {
                typedValue = default(T);
                return false;
            }
        }
    }
}
