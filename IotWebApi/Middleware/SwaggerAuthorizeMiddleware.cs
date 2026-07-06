using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace IotWebApi;

/// <summary>
/// Swagger授权访问中间件
/// </summary>
public class SwaggerAuthorizeMiddleware
{
    private readonly RequestDelegate next;
    //private readonly IWebHostEnvironment environment;

    public SwaggerAuthorizeMiddleware(RequestDelegate next)
    {
        this.next = next;
        //this.environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Value != null && context.Request.Path.Value.ToLower().Contains("index.html"))
        {
            // 判断权限是否正确
            if (IsAuthorized(context))
            {
                await next.Invoke(context);
                return;
            }

            // 无权限，跳转swagger登录页
            var returnUrl = context.Request.GetDisplayUrl();
            context.Response.Redirect("/swagger/login.html?returnUrl=" + returnUrl);
        }
        else
            await next.Invoke(context);
    }

    public bool IsAuthorized(HttpContext context)
    {
        var swaggerKey = context.Session.GetInt32("swagger-key");
        if (swaggerKey is 1)
        {
            return true;
        }
        return false;
    }
}