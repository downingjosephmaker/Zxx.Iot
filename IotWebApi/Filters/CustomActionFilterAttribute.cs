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
            using (LogContext.PushProperty("TraceId", _traceId))
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
        private string _traceId = "";   // 实际使用的 TraceId（优先上游 X-Trace-Id，否则 SnowId）
        private OperatorModel tokenmdl = null;
        private Stopwatch sw = new();
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            sw.Restart();

            // 请求级日志上下文：TraceId 贯穿授权→执行→结果全链路，
            // 该请求线程内的所有 LogHelper/ILogger 调用自动携带 TraceId（Serilog LogContext 机制）
            IDisposable traceScope = null;

            try
            {
                var actionDesc = context.ActionDescriptor as ControllerActionDescriptor;

                // TraceId 必须在 NoOptLog 检查之前建立，确保所有请求（含服务间调用）都有 TraceId
                // 优先用上游传入的 X-Trace-Id（如 Service.4G_ZT 的"ZT-{时间}"），没有则生成新的
                _traceId = context.HttpContext.Request.Headers["X-Trace-Id"].FirstOrDefault();
                if (string.IsNullOrEmpty(_traceId)) _traceId = SnowModel.Instance.NewId().ToString();
                traceScope = LogContext.PushProperty("TraceId", _traceId);
                traceScope = LogContext.PushProperty("Action", $"{actionDesc?.ControllerName}.{actionDesc?.ActionName}");

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

                        //批量添加建筑/部门权限
                        if (tokenmdl != null && paramJson.Contains("sconlist"))
                        {
                            bool refreshModel = false;
                            var model = paramJson.ToObject<ActionPara>();
                            bool islistpage = false;
                            if (actionDesc.ActionName.ToLower() == "getlistbypage") islistpage = true;
                            if (!tokenmdl.IsSystem || islistpage)
                            {
                                List<DeptInfo> departInfo = new();
                                foreach (var item in tokenmdl._DeptInfoDic)
                                {
                                    departInfo.AddRange(item.Value);
                                }
                                var departCon = model.sconlist.Find(s => s.ParamName.ToLower() == "deptid" && s.ParamType == "=" && !s.ParamValue.IsZxxNullOrEmpty());
                                if (departCon != null)
                                {
                                    var deptid = departCon.ParamValue.ToZxxInt();
                                    var tempDepart = DeptInfoDAO.Instance.GetOneBy(s => s.DeptId == deptid);
                                    if (tempDepart != null)
                                    {
                                        if (departInfo.Any(s => s.FullCode.Contains($"|{tempDepart.DeptId}|") && s.TreeLevel > tempDepart.TreeLevel))
                                        {
                                            model.sconlist.Remove(departCon);
                                            var thirdIdList = departInfo.Where(s => s.FullCode.Contains($"|{tempDepart.DeptId}|")).Select(s => s.DeptId);
                                            if (thirdIdList.IsZxxAny())
                                            {
                                                model.sconlist.Add(new SelectCondition()
                                                {
                                                    ParamName = "DeptId",
                                                    ParamType = "in",
                                                    ParamValue = thirdIdList.ListIntZdToString(","),
                                                });
                                            }
                                        }
                                    }
                                }
                                else   //部门权限为必需条件
                                {
                                    //model.sconlist.Add(new SelectCondition()
                                    //{
                                    //    ParamName = "dept_id",
                                    //    ParamValue = tokenmdl._DeptIdList.ListIntZdToString(","),
                                    //    ParamType = "in",
                                    //});
                                }

                                //建筑权限为非必需条件
                                List<BuildInfo> buildinfo = new List<BuildInfo>();
                                foreach (var item in tokenmdl._BuildInfoDic)
                                {
                                    buildinfo.AddRange(item.Value);
                                }
                                var buildCon = model.sconlist.Find(s => s.ParamName.ToLower() == "buildid" && s.ParamType == "=" && !s.ParamValue.IsZxxNullOrEmpty());
                                if (buildCon != null)
                                {
                                    var buildid = buildCon.ParamValue.ToZxxInt();
                                    var tempBuild = BuildInfoDAO.Instance.GetOneBy(s => s.BuildId == buildid);
                                    if (tempBuild != null)
                                    {
                                        if (buildinfo.Any(s => s.FullCode.Contains(tempBuild.FullCode) && s.TreeLevel > tempBuild.TreeLevel))
                                        {
                                            model.sconlist.Remove(buildCon);
                                            var thirdIdList = buildinfo.Where(s => s.FullCode.Contains(tempBuild.FullCode)).Select(s => s.BuildId);
                                            if (thirdIdList.IsZxxAny())
                                            {
                                                model.sconlist.Add(new SelectCondition()
                                                {
                                                    ParamName = "BuildId",
                                                    ParamType = "in",
                                                    ParamValue = thirdIdList.ListIntZdToString(),
                                                });
                                            }
                                        }
                                    }
                                }

                                refreshModel = true;
                            }
                            if (islistpage)
                            {
                                var unitCon = model.sconlist.Find(s => s.ParamName.ToLower() == "unitid" && !s.ParamValue.IsZxxNullOrEmpty());
                                if (unitCon == null)
                                {
                                    bool isAddUnitId = true;
                                    List<string> strings = new List<string>()
                                    {
                                        "sysuser","basicunitinfo"
                                    };
                                    if (strings.Contains(actionDesc.ControllerName.ToLower()) && tokenmdl.IsSystem)
                                    {
                                        isAddUnitId = false;
                                    }
                                    if (isAddUnitId)
                                    {
                                        var returnType = actionDesc.MethodInfo.ReturnType;
                                        Type actualReturnType = returnType;
                                        // 如果是异步方法，例如 Task<T>，您可能想获取 T 的类型
                                        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                                        {
                                            actualReturnType = returnType.GetGenericArguments()[0];
                                        }
                                        Type typeToInspect = null;
                                        if (actualReturnType.IsGenericType &&
                                             (actualReturnType.GetGenericTypeDefinition() == typeof(List<>) ||
                                              actualReturnType.GetGenericTypeDefinition() == typeof(IList<>) ||
                                              actualReturnType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                                              actualReturnType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                                        {
                                            typeToInspect = actualReturnType.GetGenericArguments()[0];
                                        }
                                        else if (actualReturnType.IsArray)
                                        {
                                            typeToInspect = actualReturnType.GetElementType();
                                        }
                                        else if (actualReturnType != typeof(void))
                                        {
                                            typeToInspect = actualReturnType;
                                        }
                                        if (typeToInspect != null)
                                        {
                                            PropertyInfo[] properties = typeToInspect.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                            if (properties.Any(t => t.Name.ToLower() == "unitid"))
                                            {
                                                model.sconlist.Add(new SelectCondition()
                                                {
                                                    ParamName = "UnitId",
                                                    ParamType = "=",
                                                    ParamValue = tokenmdl.UnitId.ToString(),
                                                });
                                                refreshModel = true;
                                            }
                                        }
                                    }
                                }
                            }
                            if (refreshModel)
                            {
                                // 将修改后的内容写回 Body
                                var modifiedBytes = Encoding.UTF8.GetBytes(model.ToJson());
                                request.Body = new MemoryStream(modifiedBytes);
                                request.Body.Position = 0; // 重置流位置
                            }
                        }

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
            finally
            {
                // 释放 LogContext：using 块结束时弹出 TraceId，避免污染其他请求
                traceScope?.Dispose();
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
