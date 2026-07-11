using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IotDriverCore
{
    /// <summary>
    /// 插件元数据构建器(反射配置类[ConfigParameter]标注生成一层properties的JSON Schema,
    /// 与product_command.ParamSchema同构,前端动态表单直接渲染;
    /// Manifest=配置schema+当前配置值+控制命令清单+寻址说明,
    /// 由宿主在上传/加载时读取ICenBoPlugin.PluginManifest持久化到sys_plugin.plugin_manifest)
    /// </summary>
    public static class PluginMetaBuilder
    {
        /// <summary>
        /// 插件支持的控制命令描述(ClassName为控制白名单键,宿主聚合各插件清单替代硬编码白名单)
        /// </summary>
        public sealed record PluginCommandMeta(string ClassName, string Description);

        private static readonly JsonSerializerOptions SerializeOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// 构建插件自描述清单JSON
        /// </summary>
        /// <param name="currentConfig">配置实例(本地Config文件有值=迁移来源,否则为类默认值;
        /// 宿主在DB plugin_config为空时用其中defaultConfig回填,完成本地文件到DB的一次性迁移)</param>
        /// <param name="commands">支持的控制ClassName清单</param>
        /// <param name="addressing">点表寻址说明文本(前端插件页展示)</param>
        public static string BuildManifest(object currentConfig, IEnumerable<PluginCommandMeta> commands, string addressing)
        {
            var root = new JsonObject
            {
                ["configSchema"] = BuildConfigSchemaNode(currentConfig.GetType(), currentConfig),
                ["defaultConfig"] = BuildConfigValueNode(currentConfig),
                ["commands"] = new JsonArray(commands.Select(c => (JsonNode)new JsonObject
                {
                    ["className"] = c.ClassName,
                    ["description"] = c.Description
                }).ToArray()),
                ["addressing"] = addressing
            };
            return root.ToJsonString(SerializeOptions);
        }

        /// <summary>
        /// 反射配置类生成JSON Schema(仅收录声明在配置类自身且带[ConfigParameter]的可读写属性,
        /// 不含NewLife Config基类成员;default取实例当前值供表单预填)
        /// </summary>
        private static JsonObject BuildConfigSchemaNode(Type configType, object instance)
        {
            var properties = new JsonObject();
            var required = new JsonArray();
            foreach (var prop in EnumerateConfigProperties(configType))
            {
                var attr = prop.GetCustomAttribute<ConfigParameterAttribute>()!;
                var field = new JsonObject
                {
                    ["title"] = attr.DisplayName,
                    ["type"] = MapJsonType(prop.PropertyType)
                };
                if (!string.IsNullOrEmpty(attr.Description)) field["description"] = attr.Description;
                field["default"] = ToJsonValue(prop.GetValue(instance), prop.PropertyType);
                properties[prop.Name] = field;
                if (attr.Required) required.Add(prop.Name);
            }
            return new JsonObject { ["properties"] = properties, ["required"] = required };
        }

        /// <summary>
        /// 配置实例当前值序列化为JSON对象(与schema同一属性集合,键=属性名)
        /// </summary>
        private static JsonObject BuildConfigValueNode(object config)
        {
            var node = new JsonObject();
            foreach (var prop in EnumerateConfigProperties(config.GetType()))
            {
                node[prop.Name] = ToJsonValue(prop.GetValue(config), prop.PropertyType);
            }
            return node;
        }

        private static IEnumerable<PropertyInfo> EnumerateConfigProperties(Type configType)
        {
            return configType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.CanRead && p.CanWrite && p.GetCustomAttribute<ConfigParameterAttribute>() != null);
        }

        private static string MapJsonType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(bool)) return "boolean";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short)) return "integer";
            if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) return "number";
            return "string";
        }

        private static JsonNode? ToJsonValue(object? value, Type type)
        {
            if (value == null) return null;
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(bool)) return JsonValue.Create((bool)value);
            if (type == typeof(int)) return JsonValue.Create((int)value);
            if (type == typeof(long)) return JsonValue.Create((long)value);
            if (type == typeof(short)) return JsonValue.Create((short)value);
            if (type == typeof(double)) return JsonValue.Create((double)value);
            if (type == typeof(float)) return JsonValue.Create((float)value);
            if (type == typeof(decimal)) return JsonValue.Create((decimal)value);
            return JsonValue.Create(value.ToString());
        }
    }
}
