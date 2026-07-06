using System;
using System.Runtime.Loader;
using System.Reflection;
using System.IO;

namespace IotWebApi.Helper
{
    /// <summary>
    /// 用于加载DinkToPdf原生库的自定义程序集加载上下文
    /// </summary>
    public class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        public CustomAssemblyLoadContext() : base(isCollectible: true)
        {
        }

        /// <summary>
        /// 加载非托管库
        /// </summary>
        /// <param name="path">库文件路径</param>
        public void LoadUnmanagedLibrary(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Native library not found: {path}");
            }
            LoadUnmanagedDll(path);
        }

        /// <summary>
        /// 加载非托管DLL
        /// </summary>
        /// <param name="absolutePath">DLL绝对路径</param>
        /// <returns>指向已加载库的指针</returns>
        protected override IntPtr LoadUnmanagedDll(string absolutePath)
        {
            return LoadUnmanagedDllFromPath(absolutePath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // 我们不加载托管程序集，只加载非托管库
            return null;
        }
    }
} 