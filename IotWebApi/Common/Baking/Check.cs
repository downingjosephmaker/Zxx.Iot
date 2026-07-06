using System.Collections.Generic;
using System;
using System.Collections;

namespace IotWebApi.Common.Baking
{
    /// <summary>
    /// 方法参数检查工具类
    /// </summary>
    public static class StrCheck
    {
        #region 检查对象类型参数

        /// <summary>
        /// 检查参数不为空
        /// </summary>
        /// <param name="value">待检查的参数值</param>
        /// <param name="parameterName">参数名称</param>
        public static void NotNull(object value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);
        }

        #endregion

        #region 检查字符串类型参数

        /// <summary>
        /// 检查一个参数不为Null或者空
        /// </summary>
        /// <param name="value">待检查的参数值</param>
        /// <param name="parameterName">参数名称</param>
        public static void NotNullOrEmpty(string value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);

            if (value.Length == 0)
                throw new ArgumentException(string.Format("参数[{0}]不能为空", parameterName), parameterName);
        }

        /// <summary>
        /// 检查一个参数不为null或空或空白
        /// </summary>
        /// <param name="value">待检查的参数值</param>
        /// <param name="parameterName">参数名称</param>
        public static void NotNullOrEmptyOrWhitespace(string value, string parameterName)
        {
            NotNullOrEmpty(value, parameterName);

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(string.Format("参数[{0}]不能为空白字符", parameterName), parameterName);
        }

        #endregion

        #region 检查集合类型参数

        /// <summary>
        /// 检查集合参数不为null或空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">待检查的集合</param>
        /// <param name="parameterName">参数名称</param>
        public static void NotNullOrEmptyForGenericCollection<T>(ICollection<T> collection, string parameterName)
        {
            NotNullOrEmptyForGenericCollection<T>(collection, parameterName, string.Format("参数[{0}]不能为空", parameterName));
        }

        /// <summary>
        /// 检查集合参数不为null或空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">待检查的集合</param>
        /// <param name="parameterName">参数名称</param>
        /// <param name="errorMessage">指定的错误消息</param>
        public static void NotNullOrEmptyForGenericCollection<T>(ICollection<T> collection, string parameterName, string errorMessage)
        {
            if (collection == null)
                throw new ArgumentNullException(parameterName);

            if (collection.Count == 0)
                throw new ArgumentException(errorMessage, parameterName);
        }

        /// <summary>
        /// 检查集合参数不为null或空
        /// </summary>
        /// <param name="collection">待检查的集合</param>
        /// <param name="parameterName">参数名称</param>
        public static void NotNullOrEmptyForCollection(ICollection collection, string parameterName)
        {
            NotNullOrEmptyForCollection(collection, parameterName, string.Format("参数[{0}]不能为空", parameterName));
        }

        /// <summary>
        /// 检查集合参数不为null或空
        /// </summary>
        /// <param name="collection">待检查的集合</param>
        /// <param name="parameterName">参数名称</param>
        /// <param name="errorMessage">指定的错误消息</param>
        public static void NotNullOrEmptyForCollection(ICollection collection, string parameterName, string errorMessage)
        {
            if (collection == null)
                throw new ArgumentNullException(parameterName);

            if (collection.Count == 0)
                throw new ArgumentException(errorMessage, parameterName);
        }

        #endregion

        /// <summary>
        /// 检查指定类型的参数是一个枚举类型
        /// </summary>
        /// <param name="type">The type argument.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        public static void TypeIsEnum(Type type, string parameterName)
        {
            NotNull(type, "type");

            if (!type.IsEnum)
                throw new ArgumentException(string.Format("Type {0} is not an Enum.", type), parameterName);
        }
    }
}
