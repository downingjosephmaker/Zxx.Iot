using CenBoCommon.Zxx;
using CenboEventBus;
using IotLog;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Context;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using IotModel;

namespace IotWebApi
{

    /// <summary>
    /// 全局方法过滤器
    /// OnActionExecuting(全局-控制器-方法)||OnActionExecuted(方法-控制器-全局)
    /// </summary>
    public class CustomActionFilterAttribute : Attribute, IActionFilter, IAsyncAuthorizationFilter
    {
        /// <summary>
        /// IActionFilter.OnActionExecuted在Controller的Action方法执行完后执行
        /// </summary>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // 结果阶段重新建立 TraceId/Action 上下文（OnAuthorizationAsync 的 using 已结束）
            using (LogContext.PushProperty("Trace", _traceId))
            using (LogContext.PushProperty("Action", $"{(context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName}.{(context.ActionDescriptor as ControllerActionDescriptor)?.ActionName}"))
            {
                try
                {
                    string interfacename = context.ActionDescriptor.AttributeRouteInfo.Template;
                    var actionDesc = context.ActionDescriptor as ControllerActionDescriptor;
                    string controllername = actionDesc.ControllerName.ToLower();
                    bool isFilterLogAction = IsFilterLogAction(context.ActionDescriptor);
                    if (controllername.ToLower() == "swagger")
                    {
                        return;
                    }
                    if (context.Result is FileStreamResult)
                    {
                        return;
                    }

                    sw.Stop();
                    long esecond = sw.ElapsedMilliseconds;

                    string tokenstr = tokenmdl?.ToJson();
                    int userid = tokenmdl?.UserID ?? 0;
                    string username = tokenmdl?.UserName ?? "未知";
                    string sourcetype = tokenmdl?.SourceType;

                    MetaData data = null;

                    string cllname = actionDesc.ControllerName;
                    string acname = actionDesc.ActionName;
                    if (context.Exception == null)
                    {
                        if (((ObjectResult)context.Result).Value == null)
                        {
                            data = new()
                            {
                                Status = true,
                                Message = "无数据。"
                            };
                            var conl = context.Controller as ControllerBaseApi;
                            if (conl != null)
                            {
                                data.Status = conl.Status;
                                data.Total = conl.TotalCount;
                                if (!conl.Message.IsZxxNullOrEmpty())
                                {
                                    data.Message = conl.Message;
                                }
                            }

                            ObjectResult result = new ObjectResult(data);
                            result.StatusCode = 200;
                            context.Result = result;
                            context.Exception = null;
                        }
                        else
                        {
                            if (((ObjectResult)context.Result).Value is MetaData)
                            {
                                data = ((ObjectResult)context.Result).Value as MetaData;
                            }
                            else
                            {
                                var reValue = ((ObjectResult)context.Result).Value;
                                data = new()
                                {
                                    Status = true,
                                    Message = "信息处理成功。"
                                };
                                data.Result = reValue.ToJson();
                                data.Total = 1;

                                var conl = context.Controller as ControllerBaseApi;
                                if (conl != null)
                                {
                                    data.Total = conl.TotalCount;
                                    data.Status = conl.Status;
                                    if (!conl.Message.IsZxxNullOrEmpty())
                                    {
                                        data.Message = conl.Message;
                                    }
                                }
                                if (data.Total == 1 && !data.Message.IsZxxNullOrEmpty())
                                {
                                    if (data.Result.IndexOf("[") == 0)
                                    {
                                        var list = JsonConvert.DeserializeObject<List<JObject>>(data.Result);
                                        data.Total = list.Count;
                                    }
                                }
                                if ((data.Result.IsZxxNullOrEmpty() && data.Message.IsZxxNullOrEmpty()) || data.Total == 0)
                                {
                                    data.Message = "无数据";
                                }
                                ObjectResult result = new ObjectResult(data);
                                result.StatusCode = 200;
                                context.Result = result;
                                context.Exception = null;
                            }
                        }

                        if (actionDesc.ControllerName.ToLower().Contains("login"))
                        {
                            if (data.Result.Contains("UserID"))
                            {
                                var model = JsonConvert.DeserializeObject<OperatorModelLogin>(data.Result);
                                if (model != null)
                                {
                                    userid = model.UserID;
                                    username = model.UserName;
                                    sourcetype = model.SourceType;
                                }
                            }
                        }
                        if (!isFilterLogAction)
                        {
                            string datatype = $"结果{SnowId}";
                            if (!apidysource.IsZxxNullOrEmpty()) datatype = $"{datatype}({userip}):{apidysource}";
                            _ = Task.Run(() =>
                            {
                                LogHelper.SysLogWrite(cllname, acname, $"耗时({esecond}ms):{data.ToJson()}", datatype);
                            });
                        }
                    }
                    else
                    {
                        string mesg = context.Exception.ToString();

                        // 区分异常类型：校验错误返回字段级详情（业务可对外），其他异常返回通用提示
                        // DbContext 的 ValidationException 已通过 rethrow 保留类型直达此处
                        string userMessage;
                        if (context.Exception is ValidationException)
                        {
                            // 校验错误：返回具体字段错误（如"工单编号长度不能超过50"），前端可据此引导用户修正
                            userMessage = context.Exception.Message;
                        }
                        else
                        {
                            // 先尝试识别 SQL 错误，转成可读中文提示（如"字段xxx长度超过限制"）
                            string sqlHint = SqlErrorMessageHelper.TryTranslate(context.Exception);
                            if (!string.IsNullOrEmpty(sqlHint))
                            {
                                // SQL 错误：返回字段级提示，帮助用户定位问题
                                userMessage = sqlHint;
                            }
                            else
                            {
                                // 其他系统异常：堆栈不应对外，返回通用提示
                                userMessage = "操作失败，请检查输入或联系管理员。";
                            }
                        }

                        data = new()
                        {
                            Status = false,
                            Message = userMessage
                        };
                        ObjectResult result = new ObjectResult(data);
                        result.StatusCode = 200;
                        context.Result = result;
                        context.Exception = null;

                        if (!isFilterLogAction)
                        {
                            string datatype = $"错误{SnowId}";
                            if (!apidysource.IsZxxNullOrEmpty()) datatype = $"{datatype}({userip}):{apidysource}";
                            _ = Task.Run(() =>
                            {
                                LogHelper.ErrorLogWrite(cllname, acname, mesg, datatype);
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite("CustomActionFilterAttribute", "OnActionExecuted", ex.ToString(), "方法错误");
                }
            } // using LogContext.PushProperty 结束
        }

        /// <summary>
        /// IActionFilter.OnActionExecuting在Controller的Action方法执行前，但是Action方法的参数模型绑定完成后执行
        /// </summary>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            //Do something...
            //context.Result = new EmptyResult();//在IActionFilter.OnActionExecuting方法中，context的Result属性只要被赋值了不为null，就不会执行Controller的Action了，也不会执行该IActionFilter拦截器的OnActionExecuted方法，同时在该IActionFilter拦截器之后注册的其它Filter拦截器也都不会被执行了
        }
        private string paramcontent = "";
        private string menufullname = "";
        private string userip = "";
        private string apidysource = "";//API调用来源
        private long SnowId = 0;
        private string _traceId = "";   // 实际使用的 TraceId（由 TraceContextMiddleware 统一生成，此处仅读取）
        private OperatorModel tokenmdl = null;
        private Stopwatch sw = new();
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            sw.Restart();

            var actionDesc = context.ActionDescriptor as ControllerActionDescriptor;

            // TraceId 由 TraceContextMiddleware 统一生成并 push 到 LogContext，
            // 此处仅读取（供 OnActionExecuted 结果阶段重建日志上下文使用）
            _traceId = context.HttpContext.Items["__TraceId"] as string ?? "";

            try
            {
                // NoOptLog 只跳过审计日志和业务日志记录，不跳过 TraceId
                if (IsFilterLogAction(context.ActionDescriptor)) return;

                SnowId = SnowModel.Instance.NewId();

                var request = context.HttpContext.Request;
                string paramJson = "";
                try
                {
                    tokenmdl = request.GetToken();
                }
                catch { }

                //leaveOpen:true标识StreamReader释放时不会自动关闭流
                if (request.Body != null)
                {
                    try
                    {
                        //开启多次读取body流
                        request.EnableBuffering();
                        using var sr = new StreamReader(request.Body, leaveOpen: true, encoding: Encoding.UTF8);
                        paramJson = await sr.ReadToEndAsync();

                        // 租户隔离由 TenantIsolation.Attach 统一接管(挂载点:DbContext.GetOperDb/TranAction/SqlSugar_Db):
                        // QueryFilter.AddTableFilter<ITenantEntity> 查询过滤 + 插入回填 + 缓存出口 FilterTenantScope。
                        // 原先在此按 tokenmdl 向 sconlist 注入 "UnitId=当前租户" 的旧机制已移除:它靠属性名字符串 "unitid"
                        // 反射匹配,且注入的是 =当前租户(与决策 B1 父见子孙冲突)。sys_user 现已实现 ITenantEntity 随全局
                        // 过滤器隔离。

                        //Action中可再次读取流
                        request.Body.Seek(0, SeekOrigin.Begin);
                    }
                    catch (Exception ex) { }
                }

                if (request.ContentLength > 0)
                {
                    paramcontent = $"参数：{paramJson}";
                }
                else
                {
                    string param = request.QueryString.Value.Replace("?", "").Replace("&", ",");
                    paramcontent = $"参数：{param}";
                }
                menufullname = request.GetMenuPath();
                userip = request.GetHeaderIp();
                apidysource = request.GetApiDySource();

                string datatype = $"请求{SnowId}";
                if (!apidysource.IsZxxNullOrEmpty()) datatype = $"{datatype}({userip}):{apidysource}";
                LogHelper.SysLogWrite(actionDesc.ControllerName, actionDesc.ActionName, paramcontent, datatype);
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("CustomActionFilterAttribute", "OnAuthorizationAsync", ex.ToString(), "方法错误");
            }
        }

        private static bool IsFilterLogAction(ActionDescriptor actionDesc)
        {
            bool hasNoOptLog = actionDesc.EndpointMetadata.Any(t => t is NoOptLogAttribute);
            if (actionDesc is ControllerActionDescriptor controllerActionDesc)
            {
                hasNoOptLog = hasNoOptLog
                    || controllerActionDesc.ControllerTypeInfo.GetCustomAttribute<NoOptLogAttribute>() != null
                    || controllerActionDesc.MethodInfo.GetCustomAttribute<NoOptLogAttribute>() != null;
            }
            return hasNoOptLog;
        }
    }
}
