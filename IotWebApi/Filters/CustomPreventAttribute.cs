using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;

namespace IotWebApi
{
    public class CustomPreventAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// 用户请求时间间隔(毫秒)
        /// </summary>
        private int RequestInterval = 500;
        /// <summary>
        /// 用户请求总次数
        /// </summary>
        private int RequestCount = 0; //1000
        private IMemoryCache cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));

        public CustomPreventAttribute(int _RequestInterval = 500, int _RequestCount = 0)
        {
            if (_RequestInterval > 0) RequestInterval = _RequestInterval;
            if (_RequestCount > 0) RequestCount = _RequestCount;
        }

        /// <summary>
        /// 控制器中加了该属性的方法中代码执行之前该方法。
        /// 所以可以用做权限校验。
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var usermodel = context.HttpContext.Request.GetToken();
            if (usermodel == null)
            {
                MetaData data = new MetaData();
                data.Status = false;
                data.Message = "网络拥挤，请稍后重试.";
                data.Result = context.HttpContext.Request.Path.Value;
                context.HttpContext.Response.StatusCode = 403;
                context.Result = new JsonResult(data);
                return;
            }

            var action = (Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)context.ActionDescriptor;
            var key = $"{action.ControllerName}-{action.ActionName}-{"RequestInterval"}-{usermodel.UserID}";
            if (cache.TryGetValue(key, out _))
            {
                MetaData data = new MetaData();
                data.Status = false;
                data.Message = "网络拥挤，请稍后重试.";
                data.Result = context.HttpContext.Request.Path.Value;
                context.HttpContext.Response.StatusCode = 403;
                context.Result = new JsonResult(data);
            }
            else
            {
                cache.Set(key, key, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMilliseconds(RequestInterval)));

                var keycount = $"{action.ControllerName}-{action.ActionName}-{"RequestCount"}-{usermodel.UserID}";
                if (cache.TryGetValue(keycount, out int _RequestCount))
                {
                    bool IsCountOut = false;
                    if (RequestCount > 0)
                    {
                        if (RequestCount < _RequestCount)
                        {
                            MetaData data = new MetaData();
                            data.Status = false;
                            data.Message = "请求次数超限，请明天再重试.";
                            data.Result = context.HttpContext.Request.Path.Value;
                            context.HttpContext.Response.StatusCode = 403;
                            context.Result = new JsonResult(data);
                            IsCountOut = true;
                        }
                    }
                    if (!IsCountOut)
                    {
                        _RequestCount++;
                        cache.Set(keycount, _RequestCount);
                    }
                }
                else
                {
                    cache.Set(keycount, 1, new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTimeOffset.Now.Date.AddDays(1).AddSeconds(5)));
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
