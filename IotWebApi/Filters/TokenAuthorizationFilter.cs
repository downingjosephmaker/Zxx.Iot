using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Net;

namespace IotWebApi
{
    /// <summary>
    /// 接口认证过滤器
    /// </summary>
    public class TokenAuthorizationFilter : IAuthorizationFilter
    {
        /// <summary>
        /// 认证处理
        /// </summary>
        /// <param name="context"></param>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // 检查是否允许匿名访问
            if (context.Filters.Any(item => item is IAllowAnonymousFilter)) return;
            // 检查是否为控制器动作
            if (context.ActionDescriptor is not ControllerActionDescriptor) return;
            // 检查是否需要认证
            if (!context.ActionDescriptor.EndpointMetadata.Any(attr => attr.GetType() == typeof(TokenAttribute))) return;

            Microsoft.Extensions.Primitives.StringValues checktoken = "";
            MetaData data = new MetaData();
            if (context.HttpContext.Request.Headers.TryGetValue("token", out checktoken))
            {
                try
                {
                    string key = EncryptsHelper.Decrypt(checktoken.ToString());
                    var model = JsonConvert.DeserializeObject<OperatorModel>(key);
                    if (model != null)
                    {
                        string timeouthour = AppSetting.GetConfig("DefaultValues:tokentimeouthour");
                        if (!string.IsNullOrWhiteSpace(timeouthour))
                        {
                            if (model.LoginTime.AddHours(timeouthour.ToZxxInt()) < DateTime.Now)
                            {
                                context.HttpContext.Response.Headers["WWW-Authenticate"] = "Unauthorized";
                                data.Status = false;
                                data.Message = "token已超时，请重新登录。";
                                data.Result = context.HttpContext.Request.Path.Value;
                                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Result = new JsonResult(data);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    context.HttpContext.Response.Headers["WWW-Authenticate"] = "Unauthorized";
                    data.Status = false;
                    data.Message = "用户授权的TOKEN验证不通过，请重新登录获取。";
                    data.Result = context.HttpContext.Request.Path.Value;
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    context.Result = new JsonResult(data);
                }
            }
            else
            {
                context.HttpContext.Response.Headers["WWW-Authenticate"] = "Unauthorized";
                data.Status = false;
                data.Message = "请提供用户授权的TOKEN。";
                data.Result = context.HttpContext.Request.Path.Value;
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Result = new JsonResult(data);
            }

        }
    }
}
