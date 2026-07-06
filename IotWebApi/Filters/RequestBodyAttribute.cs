using System;

namespace IotWebApi
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RequestBodyAttribute : Attribute
    {
        public RequestBodyAttribute(Type _type = null, int _bodytype = 1)
        {
            type = _type;
            bodytype = _bodytype;
        }
        /// <summary>
        /// 类名
        /// </summary>
        public Type type { get; set; }

        /// <summary>
        /// 1：单体 2：集合
        /// </summary>
        public int bodytype { get; set; }
    }
}