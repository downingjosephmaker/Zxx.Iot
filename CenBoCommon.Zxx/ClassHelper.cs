using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CenBoCommon.Zxx
{
    public class ClassHelper
    {
        private static string _ClassName = string.Empty;
        public static string ClassName
        {
            get
            {
                _MethodName = "";
                GetCallerInfo();
                return _ClassName;
            }
        }

        private static string _MethodName = string.Empty;
        public static string MethodName
        {
            get
            {
                return _MethodName;
            }
        }

        private static void GetCallerInfo()
        {
            try
            {
                var stackTrace = new StackTrace(true);

                for (int i = 2; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    if (method == null) continue;

                    var declaringType = method.DeclaringType;
                    if (declaringType == null) continue;

                    if (declaringType.IsDefined(typeof(CompilerGeneratedAttribute), false))
                    {
                        if (declaringType.DeclaringType != null)
                        {
                            var generatedTypeName = declaringType.Name;
                            declaringType = declaringType.DeclaringType;

                            var originalMethodName = GetOriginalMethodName(declaringType, generatedTypeName);
                            if (!string.IsNullOrEmpty(originalMethodName))
                            {
                                _ClassName = declaringType.Name;
                                _MethodName = originalMethodName;
                                return;
                            }
                        }
                        continue;
                    }

                    string methodName = method.Name;

                    if (methodName.StartsWith("get_") || methodName.StartsWith("set_"))
                    {
                        methodName = methodName.Substring(4);
                    }

                    if (methodName.Contains("<"))
                    {
                        var match = Regex.Match(methodName, @"<(.+?)>");
                        if (match.Success && match.Groups.Count > 1)
                        {
                            string potentialMethodName = match.Groups[1].Value;

                            int g__Index = potentialMethodName.IndexOf(">g__", StringComparison.Ordinal);
                            if (g__Index >= 0)
                            {
                                potentialMethodName = potentialMethodName.Substring(0, g__Index);
                            }

                            int genericIndex = potentialMethodName.IndexOf('`');
                            if (genericIndex > 0)
                            {
                                potentialMethodName = potentialMethodName.Substring(0, genericIndex);
                            }

                            methodName = potentialMethodName;
                        }
                    }

                    _ClassName = declaringType.Name;
                    _MethodName = methodName;
                    return;
                }
            }
            catch (Exception)
            {
                _ClassName = "Unknown";
                _MethodName = "Unknown";
            }
        }

        /// <param name="outerType">状态机的外层业务类</param>
        /// <param name="generatedTypeName">编译器生成的状态机类名，如 "&lt;SomeMethod&gt;d__0"</param>
        private static string GetOriginalMethodName(Type outerType, string generatedTypeName)
        {
            try
            {
                var methods = outerType.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static);

                // 修复：通过 StateMachineType.Name 精确匹配，原代码返回第一个 async 方法，多方法时必然出错
                foreach (var m in methods)
                {
                    var asyncAttr = m.GetCustomAttribute<AsyncStateMachineAttribute>();
                    if (asyncAttr != null &&
                        string.Equals(asyncAttr.StateMachineType.Name, generatedTypeName, StringComparison.Ordinal))
                    {
                        return m.Name;
                    }
                }

                // 支持 yield return 迭代器方法
                foreach (var m in methods)
                {
                    var iteratorAttr = m.GetCustomAttribute<IteratorStateMachineAttribute>();
                    if (iteratorAttr != null &&
                        string.Equals(iteratorAttr.StateMachineType.Name, generatedTypeName, StringComparison.Ordinal))
                    {
                        return m.Name;
                    }
                }

                // 兜底：直接从生成类型名解析，如 "<MyMethod>d__0" → "MyMethod"
                var match = Regex.Match(generatedTypeName, @"<(.+?)>");
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}