#See https://aka.ms/customizecontainer to learn more about how VS uses this Dockerfile to build images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN ln -sf /usr/share/zoneinfo/Asia/Shanghai /etc/localtime

# 先把全部文件拷入 /app（含 libiomp5.so 和 runtimes/）
COPY . .

# ============================================================================
# 修复 ML.NET(Microsoft.ML.Mkl.Redist 5.0.0) 在 Linux 上的原生库加载问题
# ----------------------------------------------------------------------------
# 此时 /app 下已有：
#   /app/libiomp5.so                                      (Intel OpenMP 运行时)
#   /app/runtimes/linux-x64/native/libMklImports.so       (ML.NET MKL 库)
#   /app/runtimes/linux-x64/native/libMklProxyNative.so
#
# 关键背景：插件(Zhjngk.Plugin.DeviceAnalytics 等)通过 AssemblyLoadContext
#   从 /app/files/plugins/ 动态加载，该目录下只有插件 DLL，没有任何 native 库。
#   插件触发 ML.NET SSA/FFT 时，DllImport("MklImports") 的 native 解析不会查
#   /app/runtimes/linux-x64/native/（那是宿主 default ALC 的路径），导致：
#     System.DllNotFoundException: MklImports ... FftUtils..cctor()
#   因此 native 库必须装到【系统库目录 /lib/x86_64-linux-gnu/】让所有进程可见。
#
# 三个问题：
#   ① libiomp5.so：NuGet 包未带 Linux 版 → 装系统库目录
#   ② libMklImports.so / libMklProxyNative.so：宿主可从 /app/runtimes 解析，
#      但【插件进程解析不到】→ 也装系统库目录（这是修复插件报错的关键）
#   ③ ML.NET 的 P/Invoke 找 "MklImports"(不带 lib 前缀)，文件名却带 lib → 建软链接
#
# 注意：基础镜像 mcr.microsoft.com/dotnet/aspnet:10.0 基于 Debian，
#       动态库搜索路径是 /lib/x86_64-linux-gnu/，不是 /usr/lib64(RHEL/OpenEuler 路径)。
# ============================================================================
RUN set -eux; \
    test -f /app/libiomp5.so; \
    test -f /app/runtimes/linux-x64/native/libMklImports.so; \
    test -f /app/runtimes/linux-x64/native/libMklProxyNative.so; \
    cp /app/libiomp5.so /lib/x86_64-linux-gnu/libiomp5.so; \
    cp /app/runtimes/linux-x64/native/libMklImports.so     /lib/x86_64-linux-gnu/libMklImports.so; \
    cp /app/runtimes/linux-x64/native/libMklProxyNative.so /lib/x86_64-linux-gnu/libMklProxyNative.so; \
    chmod 755 /lib/x86_64-linux-gnu/libiomp5.so \
              /lib/x86_64-linux-gnu/libMklImports.so \
              /lib/x86_64-linux-gnu/libMklProxyNative.so; \
    ln -sf libMklImports.so     /lib/x86_64-linux-gnu/MklImports.so; \
    ln -sf libMklProxyNative.so /lib/x86_64-linux-gnu/MklProxyNative.so; \
    ln -sf libMklImports.so     /app/runtimes/linux-x64/native/MklImports.so; \
    ln -sf libMklProxyNative.so /app/runtimes/linux-x64/native/MklProxyNative.so; \
    ldconfig; \
    ldconfig -p | grep -iE 'libiomp5|libMklImports|libMklProxyNative'

ENTRYPOINT ["dotnet", "IotWebApi.dll"]
