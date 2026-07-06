using CenBoCommon.Zxx;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace IotWebApi
{
    /// <summary>
    /// 文档过滤器，对文档描述进行修改(包括【枚举】类型描述)
    /// </summary>
    public class SwaggerDocumentFilter : IDocumentFilter
    {
        /// <summary>
        /// 应用文档过滤器，对文档描述进行修改(包括【枚举】类型描述)
        /// </summary>
        /// <param name="swaggerDoc"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            List<OpenApiTag> list = new();
            List<DocumentInfo> newlist = new();

            // oldlist 在原代码中未声明即引用（被旧版本编译错误掩盖）。原代码在分支内使用
            // x.Name / x.Description（与下方 DocumentInfo 的 DocName/DocDescription 不同），
            // 说明它本应是另一类“既有文档信息”数据源。为保持原有“空则跳过整段”的运行行为，
            // 声明一个匹配已用成员的占位类型，列表留空（分支永不执行）。
            List<OldDocumentInfo> oldlist = new();

            var apicontrollerslist = context.ApiDescriptions.ToList();
            if (oldlist.Count > 0)
            {
                OperatorCommon.diccontronllersummary.Clear();
                oldlist.ForEach(x =>
                {
                    var actiond = apicontrollerslist.Find(t => t.ActionDescriptor.DisplayName.Contains($"{x.Name}Controller"));
                    if (actiond != null)
                    {
                        int actionindex = actiond.ActionDescriptor.DisplayName.IndexOf($"{x.Name}Controller");
                        string typename = actiond.ActionDescriptor.DisplayName.Substring(0, actionindex) + $"{x.Name}Controller";
                        var type = Type.GetType(typename);
                        if (type != null)
                        {
                            var attribute = Attribute.GetCustomAttribute(type, typeof(ControllSortAttribute));
                            if (attribute != null)
                            {
                                string sort = (attribute as ControllSortAttribute).Sort;
                                DocumentInfo document = new();
                                document.DocName = x.Name;
                                document.DocDescription = x.Description;
                                document.DocGroup = 0;
                                document.DocSort = 0;
                                if (!string.IsNullOrEmpty(sort))
                                {
                                    document.DocGroup = sort.Split('-')[0].ToInt();
                                    document.DocSort = sort.Split('-')[1].ToInt();
                                }
                                newlist.Add(document);
                            }
                            if (!OperatorCommon.diccontronllersummary.Keys.Contains(x.Name))
                                OperatorCommon.diccontronllersummary.Add(x.Name, x.Description);
                        }
                    }
                });
            }
            if (newlist.Count > 0)
            {
                var _newlist = newlist.OrderBy(t => t.DocGroup).ThenBy(t => t.DocSort).ThenBy(t => t.DocName).ToList();
                _newlist.ForEach(x =>
                {
                    OpenApiTag tag = new();
                    tag.Name = x.DocName;
                    tag.Description = x.DocDescription;
                    list.Add(tag);
                });
                // 2.x：Tags 变为 ISet<OpenApiTag>，用 HashSet 承载
                swaggerDoc.Tags = new HashSet<OpenApiTag>(list);
            }

            OperatorCommon.dicroutesummary.Clear();
            swaggerDoc.Paths.ToList().ForEach(t =>
            {
                string keyvalue = "";
                foreach (var x in t.Value.Operations.Values)
                {
                    keyvalue = x.Summary;
                    continue;
                }
                if (!t.Key.IsZxxNullOrEmpty())
                    OperatorCommon.dicroutesummary.Add(t.Key.TrimStart('/'), keyvalue);
            });

            #region 枚举处理

            var dict = OperatorCommon.DicAllEnum;
            foreach (var (typeName, property) in swaggerDoc.Components.Schemas)
            {
                if (property.Enum is not { Count: > 0 }) continue;
                var itemType = dict.ContainsKey(typeName) ? dict[typeName] : null;
                // 2.x：枚举项由 OpenApiInteger 变为 JsonNode；Description 在具体 OpenApiSchema 上才可写
                var enumlist = property.Enum.ToList();
                if (property is OpenApiSchema propMutable)
                {
                    propMutable.Description += DescribeEnum(itemType, enumlist);
                }
            }

            foreach (var itemPaths in swaggerDoc.Paths)
            {
                foreach (var itemOperation in itemPaths.Value.Operations)
                {
                    foreach (var itemParameter in itemOperation.Value.Parameters)
                    {
                        var refId = itemParameter?.Schema?.GetReferenceId();
                        if (refId == null || !dict.ContainsKey(refId))
                        {
                            continue;
                        }

                        var itemType = swaggerDoc.Components.Schemas[refId];
                        // 2.x：IOpenApiParameter.Description 仅具体 OpenApiParameter 可写
                        if (itemParameter is OpenApiParameter paramMutable)
                        {
                            paramMutable.Description = itemType.Description;
                        }
                    }
                }
            }

            #endregion
        }

        /// <summary>
        /// 拼接枚举描述。2.x 中枚举项为 JsonNode，从中取出整数值。
        /// </summary>
        private string DescribeEnum(Type type, List<JsonNode> enums)
        {
            var enumDescriptions = new List<string>();
            foreach (var item in enums)
            {
                if (type == null) continue;
                int value = item is JsonValue jv ? jv.GetValue<int>() : Convert.ToInt32(item.ToString());
                var enumValue = Enum.Parse(type, value.ToString());
                var desc = GetDescription(type, enumValue);

                enumDescriptions.Add(string.IsNullOrEmpty(desc)
                    ? $"{value}:{Enum.GetName(type, enumValue)}; "
                    : $"{value}:{Enum.GetName(type, enumValue)},{desc}; ");
            }

            return "  " + string.Join("  ", enumDescriptions);
        }

        private string GetDescription(Type t, object value)
        {
            foreach (var mInfo in t.GetMembers())
            {
                if (mInfo.Name != t.GetEnumName(value)) continue;
                foreach (var attr in Attribute.GetCustomAttributes(mInfo))
                {
                    if (attr.GetType() == typeof(DescriptionAttribute))
                    {
                        return ((DescriptionAttribute)attr).Description;
                    }
                }
            }

            return string.Empty;
        }
    }

    public class DocumentInfo
    {
        /// <summary>
        /// 控制器名称
        /// </summary>
        public string DocName { get; set; }

        /// <summary>
        /// 控制器描述
        /// </summary>
        public string DocDescription { get; set; }

        /// <summary>
        /// 控制器分组排序
        /// </summary>
        public int DocGroup { get; set; }

        /// <summary>
        /// 控制器排序
        /// </summary>
        public int DocSort { get; set; }

    }

    /// <summary>
    /// 仅用于承载 SwaggerDocumentFilter 中原本未声明的 oldlist 的成员签名（Name/Description）。
    /// 迁移时保持原分支语义，列表恒为空，运行时不会构造实例。
    /// </summary>
    internal class OldDocumentInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

}
