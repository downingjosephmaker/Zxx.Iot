using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Reflection;

namespace IotWebApi
{
    /// <summary>
    /// Header和Body参数添加实现
    /// Api级别过滤器，对不同的api进行定制化过滤
    /// </summary>
    public class SwaggerBodyOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<IOpenApiParameter>();

            if (operation.RequestBody != null)
            {
                operation.RequestBody.Content.Remove("application/json-patch+json");
                operation.RequestBody.Content.Remove("text/json");
                operation.RequestBody.Content.Remove("application/*+json");
                operation.RequestBody.Description = "Body参数";
            }

            if (operation.Responses.Count > 0)
            {
                var res200 = operation.Responses["200"];
                if (res200 != null)
                {
                    res200.Description = "成功";
                    res200.Content.Remove("text/plain");
                    res200.Content.Remove("text/json");
                }
            }

            //先判断是否是匿名访问,
            var descriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            if (descriptor != null)
            {
                var attrs = context.ApiDescription.CustomAttributes();
                foreach (var attr in attrs)
                {
                    if (attr.GetType() == typeof(TokenAttribute))   //添加令牌输入块
                    {
                        string token = AppSetting.GetConfig("DefaultValues:token");
                        var schema = new OpenApiSchema()
                        {
                            Type = JsonSchemaType.String,
                        };
                        schema.Example = SwaggerOpenApiEntity.CreateFor(schema, token);
                        operation.Parameters.Add(new OpenApiParameter()
                        {
                            Name = "token",
                            In = ParameterLocation.Header,
                            Description = "令牌",
                            Style = ParameterStyle.Simple,
                            Required = true,
                            Schema = schema
                        });
                    }
                    else if (attr.GetType() == typeof(StaffTokenAttribute))   //添加令牌输入块
                    {
                        var schema = new OpenApiSchema()
                        {
                            Type = JsonSchemaType.String,
                        };
                        operation.Parameters.Add(new OpenApiParameter()
                        {
                            Name = "stafftoken",
                            In = ParameterLocation.Header,
                            Description = "罪犯令牌",
                            Style = ParameterStyle.Simple,
                            Required = true,
                            Schema = schema
                        });
                    }
                    else if (attr.GetType() == typeof(IotTripartiteAttribute))   //添加IOT令牌输入块
                    {
                        var schema = new OpenApiSchema()
                        {
                            Type = JsonSchemaType.String,
                        };
                        operation.Parameters.Add(new OpenApiParameter()
                        {
                            Name = "CB-IOT-ID",
                            In = ParameterLocation.Header,
                            Description = "应用ID",
                            Style = ParameterStyle.Simple,
                            Required = true,
                            Schema = schema
                        });
                        operation.Parameters.Add(new OpenApiParameter()
                        {
                            Name = "CB-IOT-KEY",
                            In = ParameterLocation.Header,
                            Description = "应用密钥",
                            Style = ParameterStyle.Simple,
                            Required = true,
                            Schema = schema
                        });
                    }
                    else if (attr.GetType() == typeof(RequestBodyAttribute))   //添加Request Body令牌输入块
                    {
                        if (operation.RequestBody != null)
                        {
                            continue;
                        }
                        operation.RequestBody = new OpenApiRequestBody()
                        {
                            Description = "Body参数",
                            Required = true
                        };

                        OpenApiSchema schema = new OpenApiSchema();
                        var attrbody = attr as RequestBodyAttribute;
                        if (attrbody.type != null)
                        {
                            if (attrbody.bodytype == 2)
                            {
                                var schemaitem = SetOpenApiSchema(attrbody.type);
                                context.SchemaRepository.Schemas.Add(attrbody.type.Name, schemaitem);

                                schema.Type = JsonSchemaType.Array;
                                // 2.x：Items 通过引用对象表示，引用指向上面注册到 SchemaRepository 的同名 schema
                                schema.Items = new OpenApiSchemaReference(attrbody.type.Name, null);
                            }
                            else
                            {
                                schema = SetOpenApiSchema(attrbody.type);
                                context.SchemaRepository.Schemas.Add(attrbody.type.Name, schema);
                            }
                        }
                        else
                        {
                            var actionDesc = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
                            string controllername = actionDesc.ControllerName;
                            if (controllername == "WeatherForecast")
                            {
                                schema.Type = JsonSchemaType.Object;
                                schema.Title = "WeatherBody";
                                schema.Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["Date"] = new OpenApiSchema() { Type = JsonSchemaType.Integer, Description = "日期" },
                                    ["TemperatureC"] = new OpenApiSchema() { Type = JsonSchemaType.Integer, Description = "温度" },
                                    ["error"] = new OpenApiSchema()
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema>()
                                        {
                                            ["message"] = new OpenApiSchema() { Type = JsonSchemaType.String },
                                            ["stackTrace"] = new OpenApiSchema() { Type = JsonSchemaType.String }
                                        }
                                    },
                                };
                                context.SchemaRepository.Schemas.Add("WeatherBody", schema);
                            }
                        }

                        operation.RequestBody.Content.Add("application/json", new OpenApiMediaType()
                        {
                            Schema = schema
                        });

                    }
                }
            }
        }

        private OpenApiSchema SetOpenApiSchema(Type _type)
        {
            Dictionary<string, string> typechange = new Dictionary<string, string>();
            typechange.Add("int32", "integer");
            typechange.Add("int64", "integer");
            typechange.Add("double", "number");
            typechange.Add("float", "number");
            typechange.Add("decimal", "number");
            typechange.Add("single", "number");
            typechange.Add("string", "string");
            typechange.Add("datetime", "string");
            typechange.Add("boolean", "boolean");

            Dictionary<string, string> formatchange = new Dictionary<string, string>();
            formatchange.Add("double", "double");
            formatchange.Add("decimal", "double");
            formatchange.Add("single", "float");
            formatchange.Add("float", "float");
            formatchange.Add("boolean", "");
            formatchange.Add("int32", "int32");
            formatchange.Add("int64", "int64");
            formatchange.Add("string", "");
            formatchange.Add("datetime", "datetime");

            var schdic = new Dictionary<string, IOpenApiSchema>();
            OpenApiSchema schemaitem = new OpenApiSchema();
            schemaitem.Type = JsonSchemaType.Object;

            schemaitem.Title = _type.Name;
            foreach (var cus in _type.GetCustomAttributes())
            {
                if (cus is DisplayNameAttribute)
                {
                    schemaitem.Description = (cus as DisplayNameAttribute).DisplayName;
                }
            }
            var properarray = _type.GetProperties();
            foreach (PropertyInfo fi in properarray)
            {
                foreach (var cusa in fi.CustomAttributes)
                {
                    if (cusa.AttributeType.Name == "DisplayNameAttribute")
                    {
                        string typen = fi.PropertyType.Name;
                        bool isNullable = false;
                        if (typen == "Nullable`1")
                        {
                            isNullable = true;
                            typen = fi.PropertyType.GenericTypeArguments[0].Name;
                        }
                        var typeStr = typechange[typen.ToLower()];
                        var childschema = new OpenApiSchema()
                        {
                            Type = ToJsonSchemaType(typeStr, isNullable),
                            Format = formatchange[typen.ToLower()],
                            Description = cusa.ConstructorArguments[0].Value.ToString()
                        };

                        if (typen.ToLower() == "datetime")
                        {
                            childschema.Example = SwaggerOpenApiEntity.CreateFor(childschema, DateTime.Now.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        else if (typen.ToLower() == "string")
                        {
                            childschema.Example = SwaggerOpenApiEntity.CreateFor(childschema, "");
                        }

                        string dvalue = AppSetting.GetConfig($"DefaultValues:{fi.Name}");
                        if (!string.IsNullOrEmpty(dvalue))
                        {
                            childschema.Example = SwaggerOpenApiEntity.CreateFor(childschema, dvalue);
                        }
                        if (fi.Name.ToLower() == "endtime")
                        {
                            childschema.Format = "datetime";
                            childschema.Example = SwaggerOpenApiEntity.CreateFor(childschema, DateTime.Now.Date.AddDays(1).ToString("yyyy-MM-dd"));
                        }
                        else if (fi.Name.ToLower() == "starttime")
                        {
                            childschema.Format = "datetime";
                            childschema.Example = SwaggerOpenApiEntity.CreateFor(childschema, DateTime.Now.Date.AddDays(-4).ToString("yyyy-MM-dd"));
                        }

                        schdic.Add(fi.Name, childschema);
                    }
                }
            }

            schemaitem.Properties = schdic;

            return schemaitem;
        }

        /// <summary>
        /// 将旧的字符串类型名（"integer"/"number"/"string"/"boolean"/"object"/"array"）
        /// 映射为 OpenApi 2.x 的 JsonSchemaType；可空时叠加 Null 标志（替代旧的 Nullable 属性）。
        /// </summary>
        private static JsonSchemaType ToJsonSchemaType(string typeStr, bool nullable)
        {
            JsonSchemaType t;
            switch (typeStr)
            {
                case "integer": t = JsonSchemaType.Integer; break;
                case "number": t = JsonSchemaType.Number; break;
                case "boolean": t = JsonSchemaType.Boolean; break;
                case "object": t = JsonSchemaType.Object; break;
                case "array": t = JsonSchemaType.Array; break;
                case "string":
                default: t = JsonSchemaType.String; break;
            }
            return nullable ? (t | JsonSchemaType.Null) : t;
        }
    }
}
