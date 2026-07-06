using Microsoft.OpenApi;
using System.Collections.Generic;

namespace IotWebApi.Common
{
    public class SwaggerHelper
    {
        public static Dictionary<string, string> GetRequestBody(IOpenApiRequestBody requestBody, Dictionary<string, IOpenApiSchema> dicapi)
        {
            Dictionary<string, string> dicschema = new Dictionary<string, string>();

            foreach (var content in requestBody.Content)
            {
                var _dicschema = GetRequestSchemaList(content.Value.Schema, dicapi, true, "");
                foreach (var item in _dicschema)
                {
                    dicschema.Add(item.Key, item.Value);
                }
            }

            return dicschema;
        }

        private static Dictionary<string, string> GetRequestSchemaList(IOpenApiSchema _Schema, Dictionary<string, IOpenApiSchema> dicapi, bool isfirst = false, string sskey = "")
        {
            Dictionary<string, string> dicschema = new Dictionary<string, string>();
            string schemakey = "";
            string bodytxt = "";
            var schemaRefId = _Schema.GetReferenceId();
            if (schemaRefId != null)
            {
                schemakey = schemaRefId;
                bodytxt = "Body请求参数（单体）";
            }
            else if (_Schema.Items != null && _Schema.Items.GetReferenceId() != null)
            {
                schemakey = _Schema.Items.GetReferenceId();
                bodytxt = "Body请求参数（集合）";
            }
            if (!string.IsNullOrEmpty(schemakey))
            {
                if (isfirst)
                {
                    if (dicapi.ContainsKey(schemakey))
                    {
                        dicschema.Add(schemakey, bodytxt);
                    }
                }
                else
                {
                    if (dicapi.ContainsKey(schemakey))
                    {
                        dicschema.Add(schemakey, $"{sskey}({dicapi[schemakey].Description})");
                    }
                }

                foreach (var proper in dicapi[schemakey].Properties)
                {
                    if (proper.Value.Items != null)
                    {
                        string childkey = "";
                        if (proper.Value.Type == JsonSchemaType.Array)
                        {
                            childkey = $"【{proper.Key}(集合)】:{proper.Value.Description}";
                        }
                        else
                        {
                            childkey = $"【{proper.Key}(单体)】:{proper.Value.Description}";
                        }
                        var dic = GetRequestSchemaList(proper.Value, dicapi, false, childkey);
                        foreach (var item in dic)
                        {
                            dicschema.Add(item.Key, item.Value);
                        }
                    }
                }
            }

            return dicschema;
        }

        public static Dictionary<string, string> GetResponses(OpenApiResponses Responses, Dictionary<string, IOpenApiSchema> dicapi)
        {
            Dictionary<string, string> dicschema = new Dictionary<string, string>();

            foreach (var response in Responses.Values)
            {
                foreach (var content in response.Content)
                {
                    var _dicschema = GetResponseSchemaList(content.Value.Schema, dicapi, true, "");
                    foreach (var item in _dicschema)
                    {
                        dicschema.Add(item.Key, item.Value);
                    }
                }
            }

            return dicschema;
        }

        private static Dictionary<string, string> GetResponseSchemaList(IOpenApiSchema _Schema, Dictionary<string, IOpenApiSchema> dicapi, bool isfirst = false, string sskey = "")
        {
            Dictionary<string, string> dicschema = new Dictionary<string, string>();
            string schemakey = "";
            string bodytxt = "结果集参数";
            var schemaRefId = _Schema.GetReferenceId();
            if (schemaRefId != null)
            {
                schemakey = schemaRefId;
                bodytxt = "结果集参数（单体）";
            }
            else if (_Schema.Items != null && _Schema.Items.GetReferenceId() != null)
            {
                schemakey = _Schema.Items.GetReferenceId();
                bodytxt = "结果集参数（集合）";
            }
            if (!string.IsNullOrEmpty(schemakey))
            {
                if (isfirst)
                {
                    if (dicapi.ContainsKey(schemakey))
                    {
                        dicschema.Add(schemakey, bodytxt);
                    }
                }
                else
                {
                    if (dicapi.ContainsKey(schemakey))
                    {
                        dicschema.Add(schemakey, $"{sskey}({dicapi[schemakey].Description})");
                    }
                }

                foreach (var proper in dicapi[schemakey].Properties)
                {
                    if (proper.Value.Items != null)
                    {
                        string childkey = "";
                        if (proper.Value.Type == JsonSchemaType.Array)
                        {
                            childkey = $"【{proper.Key}(集合)】:{proper.Value.Description}";
                        }
                        else
                        {
                            childkey = $"【{proper.Key}(单体)】:{proper.Value.Description}";
                        }
                        string childschemakey = proper.Value.Items.GetReferenceId();
                        if (schemakey != childschemakey)
                        {
                            var dic = GetResponseSchemaList(proper.Value, dicapi, false, childkey);
                            foreach (var item in dic)
                            {
                                if (dicschema.ContainsKey(item.Key)) continue;
                                dicschema.Add(item.Key, item.Value);
                            }
                        }
                    }
                }
            }

            return dicschema;
        }

    }
}
