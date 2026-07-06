using Microsoft.OpenApi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IotWebApi
{
    public class SwaggerDocGen
    {
        public IDictionary<string, IOpenApiSchema> _schemas = null;
        public object GetExample(IOpenApiSchema apiSchema)
        {
            object example;
            if (apiSchema.IsObject(_schemas))
            {
                var key = apiSchema.GetReferenceId();
                example = GetExample(key);
            }
            else if (apiSchema.IsArray())
            {
                example = apiSchema.IsBaseTypeArray()
                ? new[] { GetDefaultValue(apiSchema.Items.Type.ToTypeString()) }
                : new[] { GetExample(apiSchema.Items.GetReferenceId()) };
            }
            else if (apiSchema.IsEnum(_schemas))
            {
                var key = apiSchema.GetReferenceId();
                example = GetEnum(key).Min();
            }
            else
            {
                example = GetDefaultValue(apiSchema.Type.ToTypeString());
            }

            return example;
        }

        /// <summary>
        /// 获取枚举（返回各枚举项的整数值；2.x 中枚举项由 OpenApiInteger 变为 JsonNode）
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        private IEnumerable<int> GetEnum(string enumType) =>
            GetEnumSchema(enumType).Enum.Select(x => x.GetValue<int>());

        /// <summary>
        /// 获取枚举 Schema
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        private IOpenApiSchema GetEnumSchema(string enumType) => _schemas.SingleOrDefault(x => x.Key == enumType).Value;

        /// <summary>
        /// 获取枚举的值
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        private int[] GetEnumValues(string enumType) => GetEnum(enumType).ToArray();

        /// <summary>
        /// 递归获取 Body 示例
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private object GetExample(string key)
        {
            if (key == null || _schemas.ContainsKey(key) == false) return null;
            var schema = _schemas.SingleOrDefault(x => x.Key == key).Value;
            if (schema.Properties == null || schema.Properties.Any() == false) return null;
            Dictionary<string, object> example = new Dictionary<string, object>();
            foreach (var (s, value) in schema.Properties)
            {
                if (value.IsObject(_schemas))
                {
                    var objKey = value.GetReferenceId();
                    example.Add(s, objKey == key ? null : GetExample(objKey));
                }
                else if (value.IsArray())
                {
                    example.Add(s,
                    value.IsBaseTypeArray()
                    ? new[] { GetDefaultValue(value.Items.Type.ToTypeString()) }
                    : new[] { GetExample(value.Items.GetReferenceId()) });
                }
                else
                {
                    example.Add(s,
                    value.IsEnum(_schemas)
                    ? GetEnumValues(value.GetReferenceId()).Min()
                    : GetDefaultValue((value.Format ?? value.Type.ToTypeString())));
                }
            }

            return example;
        }

        /// <summary>
        /// 获取类型默认值
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object GetDefaultValue(string type)
        {
            var number = new[]
            {
                "byte", "decimal", "double", "enum", "float", "int32", "int64", "sbyte", "short", "uint", "ulong",
                "ushort"
                };
            if (number.Any(x => type == x)) return 0;
            switch (type)
            {
                case "string":
                    return "string";
                case "bool":
                case "boolean":
                    return false;
                case "date-Time":
                case "datetime":
                    return DateTime.Now;
                default:
                    return null;
            }
        }

    }

    /// <summary>
    /// JsonSchemaType? 与字符串互转的辅助扩展（仅本迁移文件使用）。
    /// </summary>
    internal static class JsonSchemaTypeExtension
    {
        public static string ToTypeString(this JsonSchemaType? type)
        {
            // OpenApi 2.x 中 Type 变为 JsonSchemaType 枚举（可能为 Null|Integer 等组合），
            // 这里只取主类型（去掉 Null 标志），映射回旧代码使用的字符串值。
            if (type == null) return null;
            var t = type.Value & ~JsonSchemaType.Null;
            switch (t)
            {
                case JsonSchemaType.Integer: return "integer";
                case JsonSchemaType.Number: return "number";
                case JsonSchemaType.String: return "string";
                case JsonSchemaType.Boolean: return "boolean";
                case JsonSchemaType.Object: return "object";
                case JsonSchemaType.Array: return "array";
                default: return t.ToString().ToLowerInvariant();
            }
        }
    }
}
