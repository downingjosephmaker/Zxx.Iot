using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using System.Net;

namespace IotWebApi
{
    /// <summary>
    /// 第三方授权接口过滤器
    /// </summary>
    public class IotTripartiteAuthorizationFilter : IAuthorizationFilter
    {
        private const string IOT_ID_HEADER = "CB-IOT-ID";
        private const string IOT_KEY_HEADER = "CB-IOT-KEY";

        /// <summary>
        /// 验证授权
        /// </summary>
        /// <param name="context"></param>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // 检查是否允许匿名访问
            if (context.Filters.Any(item => item is IAllowAnonymousFilter)) return;
            // 检查是否为控制器动作
            if (context.ActionDescriptor is not ControllerActionDescriptor) return;
            // 检查是否需要认证
            if (!context.ActionDescriptor.EndpointMetadata.Any(attr => attr.GetType() == typeof(IotTripartiteAttribute))) return;

            // 执行认证
            if (!IsAuthorized(context))
            {
                HandleUnauthorizedAccess(context);
            }
        }

        /// <summary>
        /// 验证用户授权
        /// </summary>
        private bool IsAuthorized(AuthorizationFilterContext context)
        {
            var headers = context.HttpContext.Request.Headers;
            if (!TryGetHeaderValues(headers, out string iotId, out string iotKey)) return false;

            return ValidateCredentials(iotId, iotKey);
        }

        /// <summary>
        /// 尝试获取请求头值
        /// </summary>
        private static bool TryGetHeaderValues(IHeaderDictionary headers, out string iotId, out string iotKey)
        {
            iotId = string.Empty;
            iotKey = string.Empty;

            if (!headers.TryGetValue(IOT_ID_HEADER, out StringValues iotIdValues) ||
                !headers.TryGetValue(IOT_KEY_HEADER, out StringValues iotKeyValues))
            {
                return false;
            }

            iotId = iotIdValues.ToString();
            iotKey = iotKeyValues.ToString();

            return !string.IsNullOrWhiteSpace(iotId) && !string.IsNullOrWhiteSpace(iotKey);
        }

        /// <summary>
        /// 验证凭据
        /// </summary>
        private bool ValidateCredentials(string iotId, string iotKey)
        {
            try
            {
                var decryptedId = EncryptsHelper.Decrypt(iotKey);
                return string.Equals(decryptedId, iotId, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 处理未授权访问
        /// </summary>
        private static void HandleUnauthorizedAccess(AuthorizationFilterContext context)
        {
            var response = context.HttpContext.Response;
            var request = context.HttpContext.Request;

            response.Headers["WWW-Authenticate"] = "Unauthorized";
            response.StatusCode = (int)HttpStatusCode.Unauthorized;

            var errorData = new MetaData
            {
                Status = false,
                Message = "用户授权的应用ID和应用密钥验证不通过。",
                Result = request.Path.Value
            };

            context.Result = new JsonResult(errorData);
        }
    }
}
