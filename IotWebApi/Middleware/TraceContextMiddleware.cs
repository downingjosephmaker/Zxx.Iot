using Serilog.Context;
using System.Diagnostics;

namespace IotWebApi.Middleware
{
    /// <summary>
    /// 请求级 Trace 上下文中间件：在管道最前面 push Trace/Action 到 Serilog LogContext，
    /// 贯穿整个请求生命周期（Controller Action、Filter、下游服务调用）。
    /// <para>之前在 CustomActionFilterAttribute.OnAuthorizationAsync 中 push，但 IAsyncAuthorizationFilter
    /// 方法返回后 AsyncLocal 回退，Controller Action 执行期间 Trace 丢失。
    /// Middleware 的 await next(context) 保证 LogContext 覆盖整个请求。</para>
    /// </summary>
    public class TraceContextMiddleware
    {
        private readonly RequestDelegate _next;

        public TraceContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 优先用上游传入的 trace-id（如采集服务的控制链路 Trace），否则取 ASP.NET Core 的 Activity TraceId
            string traceId = context.Request.Headers["trace-id"].FirstOrDefault();
            if (string.IsNullOrEmpty(traceId))
                traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

            // 从路由推导 Controller.Action（供输出模板的 {Action} 字段）
            string action = "";
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                var actionDesc = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>();
                if (actionDesc != null)
                    action = $"{actionDesc.ControllerName}.{actionDesc.ActionName}";
            }

            // 暴露到 HttpContext.Items，供 Controller 构建 MQTT/插件 traceId 时复用
            context.Items["__TraceId"] = traceId;
            context.Items["__TraceAction"] = action;

            // push 到 LogContext，await next 保证整个请求（含 Controller Action）都在作用域内
            using (LogContext.PushProperty("Trace", traceId))
            using (LogContext.PushProperty("Action", action))
            {
                await _next(context);
            }
        }
    }
}
