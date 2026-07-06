using IotLog;
using RestSharp;
using Serilog;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace IotWebApi
{
    /// <summary>
    /// HTTP 请求帮助类，提供 GET、POST、PUT、DELETE 等 HTTP 方法的同步和异步调用
    /// </summary>
    public static class HttpHelper
    {
        #region 私有字段和属性

        private static readonly ConcurrentDictionary<string, RestClient> _clientCache = new ConcurrentDictionary<string, RestClient>();

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取或创建 RestClient 实例
        /// </summary>
        /// <param name="baseUrl">基础 URL</param>
        /// <param name="isSolr">是否为 Solr 请求</param>
        /// <returns>RestClient 实例</returns>
        private static RestClient GetOrCreateClient(string baseUrl, bool isSolr = false)
        {
            return _clientCache.GetOrAdd(baseUrl, url =>
            {
                int timeout = 0;
                try
                {
                    timeout = AppSetting.GetInt("HttpMqtt:OutTime") * 1000;
                }
                catch { }
                if (timeout == 0) timeout = 50 * 1000;

                var options = new RestClientOptions(url)
                {
                    Timeout = TimeSpan.FromSeconds(timeout), // 请求超时时间
                    ThrowOnAnyError = false, // 遇到错误不抛出异常
                    FollowRedirects = true, // 支持重定向
                    ConfigureMessageHandler = handler =>
                    {
                        if (handler is HttpClientHandler httpHandler)
                        {
                            httpHandler.AllowAutoRedirect = true; // 启用自动重定向
                        }
                        return handler;
                    }
                };

                // 配置 HTTPS 的证书验证
                if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    options.RemoteCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                }

                // 创建 RestClient 实例
                var client = new RestClient(options);
                if (isSolr)
                {
                    client.AddDefaultHeader("Connection", "Keep-Alive");
                    client.AddDefaultHeader("Keep-Alive", "timeout=60, max=1000");
                }
                return client;
            });
        }

        /// <summary>
        /// 通用异步请求方法
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="method">HTTP 方法</param>
        /// <param name="headers">请求头</param>
        /// <param name="body">请求体</param>
        /// <param name="dataFormat">数据格式</param>
        /// <returns>响应内容</returns>
        private static async Task<string> ExecuteRequestAsync(string url, Method method, Dictionary<string, string> headers = null, object body = null, DataFormat dataFormat = DataFormat.Json)
        {
            Stopwatch stopwatch = null;
            if (Log.Logger.IsEnabled(LogEventLevel.Information))
                stopwatch = Stopwatch.StartNew();

            try
            {
                var uri = new Uri(url);
                var isSolr = url.ToLower().Contains("solr");
                var baseUrl = uri.GetLeftPart(UriPartial.Authority); // 提取基础 URL
                var client = GetOrCreateClient(baseUrl, isSolr);
                var request = new RestRequest(uri.PathAndQuery, method);

                // 设置请求头
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.AddHeader(header.Key, header.Value);
                    }
                }

                // 设置请求体
                if (body != null)
                {
                    if (dataFormat == DataFormat.Json)
                    {
                        request.AddJsonBody(body);
                    }
                    else if (dataFormat == DataFormat.Xml)
                    {
                        request.AddXmlBody(body);
                    }
                    else
                    {
                        request.AddParameter("text/plain", body, ParameterType.RequestBody);
                    }
                }

                // 执行请求
                var response = await client.ExecuteAsync(request);

                if (Log.Logger.IsEnabled(LogEventLevel.Information))
                {
                    if (stopwatch != null)
                    {
                        stopwatch.Stop();
                        LogHelper.SysLogWrite(nameof(HttpHelper), "ExecuteRequestAsync",
                            $"Http-url:{response.ResponseUri}，结果长度:{response.Content?.Length}字节，耗时：{stopwatch.ElapsedMilliseconds}ms", "HTTP请求");
                    }
                    if (response.ErrorException != null)
                    {
                        LogHelper.ErrorLogWrite(nameof(HttpHelper), "ExecuteRequestAsync", response.ErrorException.ToString(), "HTTP请求");
                    }
                }

                return response.Content ?? string.Empty;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(nameof(HttpHelper), "ExecuteRequestAsync", ex.ToString(), "HTTP请求");
                return string.Empty;
            }
        }

        /// <summary>
        /// 通用同步请求方法
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="method">HTTP 方法</param>
        /// <param name="headers">请求头</param>
        /// <param name="body">请求体</param>
        /// <param name="dataFormat">数据格式</param>
        /// <returns>响应内容</returns>
        private static string ExecuteRequest(string url, Method method, Dictionary<string, string> headers = null, object body = null, DataFormat dataFormat = DataFormat.Json)
        {
            return ExecuteRequestAsync(url, method, headers, body, dataFormat).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 执行表单数据请求的通用方法
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="formData">表单数据</param>
        /// <param name="headers">请求头</param>
        /// <param name="isMultipart">是否为 multipart 表单</param>
        /// <returns>响应内容</returns>
        private static async Task<string> ExecuteFormRequestAsync(string url, Dictionary<string, string> formData, Dictionary<string, string> headers = null, bool isMultipart = true)
        {
            Stopwatch stopwatch = null;
            if (Log.Logger.IsEnabled(LogEventLevel.Information))
                stopwatch = Stopwatch.StartNew();

            try
            {
                var uri = new Uri(url);
                var isSolr = url.ToLower().Contains("solr");
                var baseUrl = uri.GetLeftPart(UriPartial.Authority); // 提取基础 URL
                var client = GetOrCreateClient(baseUrl, isSolr);
                var request = new RestRequest(uri.PathAndQuery, Method.Post);

                if (isMultipart)
                {
                    request.AlwaysMultipartFormData = true;
                }
                else
                {
                    request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                }

                // 设置请求头
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.AddHeader(header.Key, header.Value);
                    }
                }

                // 添加表单数据
                foreach (var item in formData)
                {
                    request.AddParameter(item.Key, item.Value);
                }

                // 执行请求
                var response = await client.ExecuteAsync(request);

                if (Log.Logger.IsEnabled(LogEventLevel.Information))
                {
                    if (stopwatch != null)
                    {
                        stopwatch.Stop();
                        LogHelper.SysLogWrite(nameof(HttpHelper), "ExecuteFormRequestAsync",
                            $"Http-url:{response.ResponseUri}，结果长度:{response.Content?.Length}字节，耗时：{stopwatch.ElapsedMilliseconds}ms", "HTTP请求");
                    }
                    if (response.ErrorException != null)
                    {
                        LogHelper.ErrorLogWrite(nameof(HttpHelper), "ExecuteFormRequestAsync", response.ErrorException.ToString(), "HTTP请求");
                    }
                }

                return response.Content ?? string.Empty;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(nameof(HttpHelper), "ExecuteFormRequestAsync", ex.ToString(), "HTTP请求");
                return string.Empty;
            }
        }

        #endregion

        #region GET 方法

        /// <summary>
        /// 异步 GET 请求
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static async Task<string> ManGetAsync(string url, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Get, headers);
        }

        /// <summary>
        /// 同步 GET 请求
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static string ManGet(string url, Dictionary<string, string> headers = null)
        {
            return ExecuteRequest(url, Method.Get, headers);
        }

        #endregion

        #region POST 方法

        /// <summary>
        /// 异步 POST 请求（无请求体）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static async Task<string> ManPostAsync(string url, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Post, headers);
        }

        /// <summary>
        /// 同步 POST 请求（无请求体）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static string ManPost(string url, Dictionary<string, string> headers = null)
        {
            return ExecuteRequest(url, Method.Post, headers);
        }

        /// <summary>
        /// 异步 POST 请求（JSON 数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="data">JSON 数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static async Task<string> ManPostBodyJsonAsync(string url, object data, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Post, headers, data, DataFormat.Json);
        }

        /// <summary>
        /// 同步 POST 请求（JSON 数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="data">JSON 数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static string ManPostBodyJson(string url, object data, Dictionary<string, string> headers = null)
        {
            return ExecuteRequest(url, Method.Post, headers, data, DataFormat.Json);
        }

        /// <summary>
        /// 异步 POST 请求（XML 数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="data">XML 数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static async Task<string> ManPostBodyXmlAsync(string url, object data, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Post, headers, data, DataFormat.Xml);
        }

        /// <summary>
        /// 同步 POST 请求（XML 数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="data">XML 数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static string ManPostBodyXml(string url, object data, Dictionary<string, string> headers = null)
        {
            return ExecuteRequest(url, Method.Post, headers, data, DataFormat.Xml);
        }

        /// <summary>
        /// 异步 POST 请求（文本数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="data">文本数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static async Task<string> ManPostBodyTextAsync(string url, string data, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Post, headers, data, DataFormat.None);
        }

        /// <summary>
        /// 同步 POST 请求（文本数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="data">文本数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static string ManPostBodyText(string url, string data, Dictionary<string, string> headers = null)
        {
            return ExecuteRequest(url, Method.Post, headers, data, DataFormat.None);
        }

        /// <summary>
        /// 异步 POST 请求（表单数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="formData">表单数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static async Task<string> ManPostFormDataAsync(string url, Dictionary<string, string> formData, Dictionary<string, string> headers = null)
        {
            return await ExecuteFormRequestAsync(url, formData, headers, true);
        }

        /// <summary>
        /// 同步 POST 请求（表单数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="formData">表单数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static string ManPostFormData(string url, Dictionary<string, string> formData, Dictionary<string, string> headers = null)
        {
            return ExecuteFormRequestAsync(url, formData, headers, true).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步 POST 请求（WWW 表单数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="formData">表单数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static async Task<string> ManPostWWWAsync(string url, Dictionary<string, string> formData, Dictionary<string, string> headers = null)
        {
            return await ExecuteFormRequestAsync(url, formData, headers, false);
        }

        /// <summary>
        /// 同步 POST 请求（WWW 表单数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="formData">表单数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static string ManPostWWW(string url, Dictionary<string, string> formData, Dictionary<string, string> headers = null)
        {
            return ExecuteFormRequestAsync(url, formData, headers, false).GetAwaiter().GetResult();
        }

        #endregion

        #region PUT 方法

        /// <summary>
        /// 异步 PUT 请求（无请求体）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static async Task<string> ManPutAsync(string url, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Put, headers);
        }

        /// <summary>
        /// 同步 PUT 请求（无请求体）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static string ManPut(string url, Dictionary<string, string> headers = null)
        {
            return ExecuteRequest(url, Method.Put, headers);
        }

        /// <summary>
        /// 异步 PUT 请求（JSON 数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="data">JSON 数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static async Task<string> ManPutBodyJsonAsync(string url, object data, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Put, headers, data, DataFormat.Json);
        }

        /// <summary>
        /// 同步 PUT 请求（JSON 数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="data">JSON 数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static string ManPutBodyJson(string url, object data, Dictionary<string, string> headers = null)
        {
            return ExecuteRequest(url, Method.Put, headers, data, DataFormat.Json);
        }

        /// <summary>
        /// 异步 PUT 请求（XML 数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="data">XML 数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static async Task<string> ManPutBodyXmlAsync(string url, object data, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Put, headers, data, DataFormat.Xml);
        }

        /// <summary>
        /// 同步 PUT 请求（XML 数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="data">XML 数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static string ManPutBodyXml(string url, object data, Dictionary<string, string> headers = null)
        {
            return ExecuteRequest(url, Method.Put, headers, data, DataFormat.Xml);
        }

        /// <summary>
        /// 异步 PUT 请求（文本数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="data">文本数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static async Task<string> ManPutBodyTextAsync(string url, string data, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Put, headers, data, DataFormat.None);
        }

        /// <summary>
        /// 同步 PUT 请求（文本数据）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="data">文本数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static string ManPutBodyText(string url, string data, Dictionary<string, string> headers = null)
        {
            return ExecuteRequest(url, Method.Put, headers, data, DataFormat.None);
        }

        #endregion

        #region DELETE 方法

        /// <summary>
        /// 异步 DELETE 请求
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static async Task<string> ManDeleteAsync(string url, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Delete, headers);
        }

        /// <summary>
        /// 同步 DELETE 请求
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="headers">请求头</param>
        /// <returns>响应内容</returns>
        public static string ManDelete(string url, Dictionary<string, string> headers = null)
        {
            return ExecuteRequest(url, Method.Delete, headers);
        }

        #endregion
    }
}
