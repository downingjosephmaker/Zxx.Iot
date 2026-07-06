using CenBoCommon.Zxx;
using Microsoft.OpenApi;
using SqlSugar;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace IotWebApi
{
    /// <summary>
    /// 模型过滤器，修改或增删组件定义的模型
    /// </summary>
    public class SwaggerSchemaFilter : ISchemaFilter
    {
        static readonly ConcurrentDictionary<Type, Tuple<string, object>[]> dict = new ConcurrentDictionary<Type, Tuple<string, object>[]>();
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            // Swashbuckle 生成组件时传入的通常是 OpenApiSchema 具体类（非引用），可安全向下转型用于修改属性。
            var mutable = schema as OpenApiSchema;

            // 【枚举类型】context.Type.IsEnum 时单独处理：枚举是值类型，不满足下面的 IsClass 条件。
            // Swashbuckle 对枚举生成独立 schema（含 enum 数值列表），但默认无中文描述。
            // 此处把枚举名（中文标识符）作为描述写入，Swagger UI 会显示在枚举 schema 上。
            if (context.Type.IsEnum)
            {
                var enumNames = Enum.GetNames(context.Type);
                if (enumNames.Length > 0 && schema.Description.IsZxxNullOrEmpty())
                {
                    // 枚举值映射：名称=数值，便于对照（如 "APP=1、电话=2、扫码=3"）
                    var pairs = enumNames.Select(name => $"{name}={Convert.ToInt64(Enum.Parse(context.Type, name))}");
                    if (mutable != null) mutable.Description = "可选值：" + string.Join("、", pairs);
                }
                return;
            }

            if (context.Type.IsClass && schema.Description == null && context.Type != typeof(string))
            {
                //类名称
                var classattribute = context.Type.GetCustomAttributes(typeof(DisplayNameAttribute), false);
                if (classattribute.Length > 0)
                {
                    if (mutable != null) mutable.Description = (classattribute[0] as DisplayNameAttribute).DisplayName;
                }
                else
                {
                    // 无 [DisplayName] 的类（如 [Expand] 标记的扩展类），用类名作兜底描述
                    if (schema.Description.IsZxxNullOrEmpty())
                    {
                        if (mutable != null) mutable.Description = context.Type.Name;
                    }
                }

                //字段名称
                // 注意：OpenApi 2.x 中 IOpenApiSchema.Properties 可能为 null（1.x 总是非空字典）。
                //       引用类型 schema 才有 Properties；基础类型/数组 schema 的 Properties 为 null。
                //       这里用 props 局部变量统一兜底为空字典，避免每个访问点重复判空。
                var props = schema.Properties ?? new Dictionary<string, IOpenApiSchema>(StringComparer.OrdinalIgnoreCase);
                var excludedProperties = context.Type.GetProperties();
                foreach (var property in excludedProperties)
                {
                    var attribute = property.GetCustomAttributes(typeof(DisplayNameAttribute), false);
                    if (attribute.Length > 0)
                    {
                        var displayDesc = (attribute[0] as DisplayNameAttribute).DisplayName;
                        // 写入字段描述：匹配 schema.Properties 的 key（可能 PascalCase 或 camelCase）
                        string matchedKey = null;
                        if (props.ContainsKey(property.Name))
                        {
                            matchedKey = property.Name;
                        }
                        else
                        {
                            foreach (string key in props.Keys)
                            {
                                if (string.Equals(key, property.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    matchedKey = key;
                                    break;
                                }
                            }
                        }
                        if (matchedKey != null)
                        {
                            SetFieldDescription(props[matchedKey], displayDesc);
                        }
                    }

                    // 【字段约束】反射读取 ORM 特性，写入 OpenApiSchema（与 EntityValidator 同源，保证文档与校验一致）
                    ApplyFieldConstraints(schema, property);
                }
            }

            // Properties 可能为 null（基础类型/数组 schema），统一兜底。
            var allProps = schema.Properties ?? new Dictionary<string, IOpenApiSchema>(StringComparer.OrdinalIgnoreCase);
            if (!allProps.IsZxxAny())
            {
                if (context.ParameterInfo != null)
                {
                    string conname = context.ParameterInfo.Name.ToLower();
                    FieldExample(schema, conname);
                }
            }
            else
            {
                foreach (var propertie in allProps)
                {
                    if (propertie.Value.Default != null)
                    {
                        if (propertie.Value is OpenApiSchema propMutable)
                        {
                            propMutable.Example = propertie.Value.Default;
                        }
                        continue;
                    }
                    FieldExample(propertie.Value, propertie.Key);
                }
            }
        }

        /// <summary>
        /// 给字段 schema 设置描述，兼容 $ref 引用类型字段。
        /// OpenAPI 3.0 规范要求 $ref 不能与其他属性并存，旧实现用 allOf 包装引用类型字段。
        /// OpenApi 2.x 中引用类型字段表示为 OpenApiSchemaReference（不可添加 AllOf），但 2.x 允许在 $ref 旁直接写 Description，
        /// 因此对引用类型字段直接设置 Description（在序列化时会与 $ref 一并保留）；对内联字段沿用 allOf 包装逻辑。
        /// </summary>
        private static void SetFieldDescription(IOpenApiSchema propSchema, string description)
        {
            if (propSchema is OpenApiSchemaReference refSchema)
            {
                // 引用类型字段：2.x 支持在 $ref 旁设置 Description（proxy 对象可写）
                refSchema.Description = description;
            }
            else if (propSchema is OpenApiSchema inlineSchema)
            {
                // 内联字段且自身携带 Reference（少数情况）：把引用搬到 AllOf，描述放外层，避免序列化丢弃
                if (inlineSchema is OpenApiSchemaReference)
                {
                    // 不会进入此分支（前面已处理 OpenApiSchemaReference）
                }
                else
                {
                    // 内联字段：直接设置（不会被序列化丢弃）
                    inlineSchema.Description = description;
                }
            }
        }

        /// <summary>
        /// 反射读取字段的 ORM 特性，将约束写入 OpenApiSchema
        /// 与 EntityValidator 读取同样的特性（SugarColumn/IntRange/EnumRange），保证文档与运行时校验同源。
        /// </summary>
        private static void ApplyFieldConstraints(IOpenApiSchema schema, System.Reflection.PropertyInfo property)
        {
            // 找到 schema.Properties 中对应的 key（Swagger 通常用 camelCase，实体属性是 PascalCase）
            // 注意：OpenApi 2.x 中 Properties 可能为 null，这里兜底为空字典。
            var props = schema.Properties ?? new Dictionary<string, IOpenApiSchema>(StringComparer.OrdinalIgnoreCase);
            string propKey = null;
            if (props.ContainsKey(property.Name))
            {
                propKey = property.Name;
            }
            else
            {
                foreach (string key in props.Keys)
                {
                    if (string.Equals(key, property.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        propKey = key;
                        break;
                    }
                }
            }
            if (propKey == null) return;

            var propSchema = props[propKey];

            // 1. 读取 SugarColumn（SqlSugar）或 OpenSearchFieldAttribute（OS）的可空/长度约束
            var lengthAttr = property.GetCustomAttributes(typeof(SugarColumn), false).FirstOrDefault() as SugarColumn;
            int? length = null;
            bool isNullable = true;

            if (lengthAttr != null)
            {
                length = lengthAttr.Length > 0 ? lengthAttr.Length : (int?)null;
                isNullable = lengthAttr.IsNullable;
            }
            else
            {
                // 兼容 ZhjngkModelOS 的 OpenSearchFieldAttribute（按名称反射，避免硬依赖）
                var osAttr = property.GetCustomAttributes(typeof(Attribute), false).FirstOrDefault(a => a.GetType().Name == "OpenSearchFieldAttribute");
                if (osAttr != null)
                {
                    var osType = osAttr.GetType();
                    var lenProp = osType.GetProperty("Length");
                    var nullProp = osType.GetProperty("IsNullable");
                    if (lenProp != null)
                    {
                        var lenVal = lenProp.GetValue(osAttr) as int?;
                        if (lenVal.HasValue && lenVal.Value > 0) length = lenVal.Value;
                    }
                    if (nullProp != null)
                    {
                        isNullable = (bool)(nullProp.GetValue(osAttr) ?? true);
                    }
                }
            }

            // 必填：IsNullable == false 时加入 schema.Required
            if (!isNullable)
            {
                if (schema.Required == null && schema is OpenApiSchema reqMutable)
                {
                    reqMutable.Required = new HashSet<string>();
                }
                if (schema.Required != null && !schema.Required.Contains(propKey))
                {
                    schema.Required.Add(propKey);
                }
            }

            // 最大长度：字符串类型且 Length > 0（MaxLength 在 2.x 仍为 int?）
            if (length.HasValue && property.PropertyType == typeof(string))
            {
                if (propSchema is OpenApiSchema propMutable) propMutable.MaxLength = length.Value;
            }

            // 2. 读取 IntRangeAttribute（按名称反射，特性定义在 ZhjngkModel/ZhjngkModelOS）
            var intRangeAttr = property.GetCustomAttributes(typeof(Attribute), false).FirstOrDefault(a => a.GetType().Name == "IntRangeAttribute");
            if (intRangeAttr != null)
            {
                var attrType = intRangeAttr.GetType();
                var minProp = attrType.GetProperty("Min");
                var maxProp = attrType.GetProperty("Max");
                if (propSchema is OpenApiSchema propMutable)
                {
                    if (minProp != null)
                    {
                        var minVal = minProp.GetValue(intRangeAttr);
                        if (minVal != null) propMutable.Minimum = Convert.ToDecimal(minVal).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    if (maxProp != null)
                    {
                        var maxVal = maxProp.GetValue(intRangeAttr);
                        if (maxVal != null) propMutable.Maximum = Convert.ToDecimal(maxVal).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
            }

            // 3. 读取 EnumRangeAttribute：描述追加说明
            var enumRangeAttr = property.GetCustomAttributes(typeof(Attribute), false).FirstOrDefault(a => a.GetType().Name == "EnumRangeAttribute");
            if (enumRangeAttr != null && property.PropertyType.IsEnum)
            {
                // 枚举的可选值列出，便于调用方参考
                var enumNames = Enum.GetNames(property.PropertyType);
                if (enumNames.Length > 0)
                {
                    var enumDesc = "可选值：" + string.Join("、", enumNames);
                    var newDesc = propSchema.Description.IsZxxNullOrEmpty()
                        ? enumDesc
                        : $"{propSchema.Description}（{enumDesc}）";
                    if (propSchema is OpenApiSchema propMutable) propMutable.Description = newDesc;
                }
            }
        }

        /// <summary>
        /// 字段默认值处理
        /// </summary>
        /// <param name="schema">Restful API的结构</param>
        /// <param name="conname">字段</param>
        private void FieldExample(IOpenApiSchema schema, string conname)
        {
            var mutable = schema as OpenApiSchema;
            if (conname.ToLower().Contains("time") || conname.ToLower().Contains("date"))
            {
                if (schema.Format == "date-time")
                {
                    if (mutable != null) { mutable.Format = null; mutable.Example = SwaggerOpenApiEntity.CreateFor(schema, DateTime.Now.ToDateTimeString()); }
                }
                if (schema.Format == "date")
                {
                    if (mutable != null) { mutable.Format = null; mutable.Example = SwaggerOpenApiEntity.CreateFor(schema, DateTime.Now.ToDateString()); }
                }
                if (schema.Type == JsonSchemaType.String)
                {
                    if (mutable != null) mutable.Example = SwaggerOpenApiEntity.CreateFor(schema, DateTime.Now.ToDateTimeString());
                }
            }
            else
            {
                if (schema.Type == JsonSchemaType.String && schema.Format != "date-time")
                {
                    if (mutable != null) mutable.Example = SwaggerOpenApiEntity.CreateFor(schema, "");
                }
                if (schema.Format == "date-time")
                {
                    if (mutable != null) { mutable.Format = null; mutable.Example = SwaggerOpenApiEntity.CreateFor(schema, DateTime.Now.ToDateTimeString()); }
                }
                if (schema.Format == "date")
                {
                    if (mutable != null) { mutable.Format = null; mutable.Example = SwaggerOpenApiEntity.CreateFor(schema, DateTime.Now.ToDateString()); }
                }
                if (schema.Description != null && schema.Description.Contains("255:不控制"))
                {
                    if (mutable != null) mutable.Example = SwaggerOpenApiEntity.CreateFor(schema, 255);
                }
            }

            string dvalue = AppSetting.GetConfig($"DefaultValues:{conname}");
            if (!string.IsNullOrEmpty(dvalue))
            {
                if (mutable != null) mutable.Example = SwaggerOpenApiEntity.CreateFor(schema, dvalue);
            }

            if (conname == "endtime")
            {
                if (mutable != null) { mutable.Format = null; mutable.Example = SwaggerOpenApiEntity.CreateFor(schema, DateTime.Now.AddDays(1).ToDateTimeString()); }
            }
            else if (conname == "starttime")
            {
                if (mutable != null) { mutable.Format = null; mutable.Example = SwaggerOpenApiEntity.CreateFor(schema, DateTime.Now.AddDays(-4).ToDateTimeString()); }
            }
            else if (conname == "createtime")
            {
                if (mutable != null) { mutable.Format = null; mutable.Example = SwaggerOpenApiEntity.CreateFor(schema, DateTime.Now.AddDays(-4).ToDateTimeString()); }
            }
            else if (conname == "updatetime")
            {
                if (mutable != null) { mutable.Format = null; mutable.Example = SwaggerOpenApiEntity.CreateFor(schema, DateTime.Now.AddDays(-4).ToDateTimeString()); }
            }
        }

    }
}
