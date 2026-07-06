using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;

namespace IotWebApi
{
    /// <summary>
    /// 控制器文档中排序（组序号-控序号）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ControllSortAttribute : ControllerAttribute, IApiBehaviorMetadata, IFilterMetadata
    {
        public string Sort
        {
            get;
            private set;
        }

        public ControllSortAttribute(string sort)
        {
            Sort = sort;
        }
    }

}
