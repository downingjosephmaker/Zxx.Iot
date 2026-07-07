using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.OpenApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Quartz;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using IotWebApi;
using IotWebApi.Services;

//SysRelatedDAO.Instance.InitTables(); //初始化表格
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ApplicationName = "IotWebApi",
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = "wwwroot",
    Args = args,
    //EnvironmentName ="Production",  ////Development
});
AppSetting.IsDevelopment = builder.Environment.IsDevelopment(); //判断是否是开发环境

//设置接口超时时间和上传大小-Kestrel
builder.WebHost.ConfigureKestrel(u =>
{
    u.Limits.MaxRequestBodySize = int.MaxValue;
    u.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
    u.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
});

string urls = AppSetting.GetConfig("Urls");
if (!urls.IsZxxNullOrEmpty())
    builder.WebHost.UseUrls(urls);

#region 配置Serilog（统一由 Zhjngk.ServiceLog 管理）

// 从配置读取 Loki 地址（appsettings.json 的 "Loki": { "Url": "..." }），未配置则不推 Loki
string lokiUrl = AppSetting.GetConfig("Loki:Url");
// 一行完成所有 Serilog 初始化。
// Sink 策略按运行环境自动判断：
//   · Docker 容器内 → 只输出 stdout（供 docker logs / Alloy 采集），不写本地文件
//   · 非容器（开发机/直接部署）→ 写文件(按天+按20MB滚动) + 控制台
// Loki 配置了 URL 则直推（注意：若同时用 Alloy 采 stdout，会重复，二选一）
LogBootstrap.Init(appName: "zhjngk-webapi", options: new LogOptions
{
    RetainedFileCount = 300,
    LokiUrl = lokiUrl,
    LogDir = "Logs",           // 日志目录（Docker 部署时需挂 volume 持久化）
    EnableFile = true,            // 强制写文件（Docker 内也写，双保险：stdout 实时 + 文件本地备份）
});
builder.Host.UseSerilog();

#endregion

Log.Information("当前环境: {Environment}", builder.Environment.EnvironmentName);

// 注册编码提供程序使用更多编码支持，例如 GBK
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .SetIsOriginAllowedToAllowWildcardSubdomains()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

//// 全局 映射器注册
//builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

//避免文档生成器和数据解析中参数字母大小写不匹配的问题。
builder.Services.AddControllers().AddJsonOptions(options =>
{
    //数据格式原属性名
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    ////将枚举值转换为字符串
    //options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

    options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new DateTimeNullableConvert());

    //设置定义的JsonResult中的编码解析器
    options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
});

//Swagger Post Body 配置
OperatorCommon.GetAllEnum();
builder.Services.AddScoped<SwaggerGenerator>(); //注册SwaggerGenerator,这样就可以直接使用依赖注入
builder.Services.AddSwaggerGen(c =>
{
    //根据ApiGroupNames枚举值生成接口文档，Skip(1)是因为Enum第一个FieldInfo返回的是一个Int值
    typeof(ApiGroupNames).GetFields().Skip(1).ToList().ForEach(f =>
    {
        var info = f.GetCustomAttributes(typeof(GroupInfoAttribute), false).OfType<GroupInfoAttribute>().FirstOrDefault();
        var openApiInfo = new OpenApiInfo
        {
            Title = info?.Title,
            Version = info?.Version,
            Description = info?.Description,
            Contact = new OpenApiContact { Name = "浙江圣博科技" },
        };
        c.SwaggerDoc(f.Name, openApiInfo);
    });

    //判断接口归属哪个分组
    c.DocInclusionPredicate((docName, apiDescription) =>
    {
        if (!apiDescription.TryGetMethodInfo(out MethodInfo method)) return false;

        //1.全部接口
        if (docName == "All") return true;

        //排除有该特性标记的下的值
        var actionlist = apiDescription.ActionDescriptor.EndpointMetadata.FirstOrDefault(x => x is ApiGroupAttribute);

        //2.得到的是未分组的接口***************
        if (docName == "NoGroup") return actionlist == null;

        //3.返回对应已经分好组的接口
        if (actionlist != null)
        {
            //判断是否包含组名
            var actionfilter = actionlist as ApiGroupAttribute;
            return actionfilter.GroupName.Any(x => x.ToString().Trim() == docName);
        }
        return false;
    });

    string xmlpath = Path.Combine(AppContext.BaseDirectory, "Swagger.xml");  //swagger注释注入,xml文件放置位置
    if (File.Exists(xmlpath))
        c.IncludeXmlComments(xmlpath, true);  //显示控制器注释

    c.OperationFilter<SwaggerBodyOperationFilter>();
    c.SchemaFilter<SwaggerSchemaFilter>();
    c.DocumentFilter<SwaggerDocumentFilter>();
});

//接口文档导出功能类
builder.Services.AddScoped<HtmlConversionHelper>();

builder.Services.AddMvcCore(option =>
{
    option.Filters.Add(new TokenAuthorizationFilter());
    option.Filters.AddService<CustomActionFilterAttribute>();
    option.Filters.Add(new IotTripartiteAuthorizationFilter());
})
.AddAuthorization();

// 注册全局过滤器依赖注入
builder.Services.AddScoped<CustomActionFilterAttribute>();

//数据返回JSON  解析器更新，Microsoft.AspNetCore.Mvc.NewtonsoftJson  Newtonsoft.Json和Newtonsoft.Json.Serialization;
builder.Services.AddMvc().AddNewtonsoftJson(options =>
{
    //设置时间格式
    options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
    //忽略循环引用
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    ////数据格式首字母小写
    //options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    //数据格式原属性名
    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
    //忽略空值
    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
});

// 配置Quartz服务
builder.Services.AddQuartz(q =>
{
    // 使用默认的RAM JobStore
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    q.UseDefaultThreadPool(tp =>
    {
        tp.MaxConcurrency = 30; // 设置最大并发执行的作业数
    });
});

// 注册QuartzService服务（自定义 HostedService，负责 scheduler 生命周期与任务加载）
// 注意：不再额外注册 AddQuartzHostedService。两个 HostedService 同时管同一 scheduler 会导致
//       停止时双重 Shutdown、启动顺序不确定等问题。QuartzService.StopAsync 已调用
//       _scheduler.Shutdown(true) 实现等待作业完成的优雅停止，能力完整。
builder.Services.AddSingleton<QuartzService>();  //单例器注册为依赖项使用
builder.Services.AddHostedService(sp => sp.GetRequiredService<QuartzService>());  //后台注册依赖项，它会将服务加到后台服务集合中，当应用程序启动时将自动启动这些服务

var ismain = AppSetting.GetConfig("DataSync:IsMain").ToLower();

//注册SignalR信号中心
builder.Services.AddSignalR(options =>
{
    //设置服务端向客户端 ping的间隔
    options.KeepAliveInterval = TimeSpan.FromSeconds(60);
    //设置客户端超时时间
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(130);
    options.MaximumReceiveMessageSize = 1024 * 1024 * 10; // 数据包大小10M，默认限制为32K
});

//配置 IIS 的请求体大小
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});
//文件上传大小配置
builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = int.MaxValue;
    x.MultipartBodyLengthLimit = int.MaxValue;
    x.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // 确保即使用户不同意 Cookie 策略也能使用
});

//事件总线服务注册
builder.Services.AddEventBusSetup();

builder.Services.AddSingleton<PluginService>();

// 遥测批量写入服务(Binary COPY写TimescaleDB遥测窄表,未配置连接串时不启用)
builder.Services.AddSingleton<TelemetryPointMap>();  //点位映射解析器,写入器与最新值服务共用
builder.Services.AddSingleton<TelemetryWriteService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TelemetryWriteService>());

// 最新值缓存服务(内存实时更新,批量刷Redis与telemetry_latest,实时查询不扫时序表)
builder.Services.AddSingleton<TelemetryLatestService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TelemetryLatestService>());

// 策略合并服务(三级scope逐字段合并,StrategyChangedEvent触发热重载)
builder.Services.AddSingleton<StrategyMergeService>();

// 采集侧异常值过滤链(范围/幅度/连续容错)
builder.Services.AddSingleton<ValueFilterService>();

// 推送策略引擎(对外发布节流+静默兜底,最新值缓存不受约束)
builder.Services.AddSingleton<PushGateService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PushGateService>());

// 上下线判定服务(离线疑似中间态防抖+上线通知限频)
builder.Services.AddSingleton<OfflineDebounceService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OfflineDebounceService>());

// 告警引擎(基于最新值缓存评估两级告警规则,三型防抖,独立事件通道)
builder.Services.AddSingleton<AlarmEngineService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AlarmEngineService>());

// 数据入库服务(消费插件上行事件,攒批写入数据库)
builder.Services.AddSingleton<DataPointIngestService>();  //单例注册,供PluginEventHandler入队使用
builder.Services.AddHostedService(sp => sp.GetRequiredService<DataPointIngestService>());  //后台注册依赖项,应用启动时自动启动消费循环

var app = builder.Build();
app.Use(next => context =>
{
    context.Request.EnableBuffering();
    return next(context);
});

app.UseSession();

var _SwaggerConfig = AppSetting.GetT<SwaggerConfig>("SwaggerConfig");
if (_SwaggerConfig.IsShowEnable)
{
    List<string> notswaggerlist = new();
    if (!_SwaggerConfig.NotGroupName.IsZxxNullOrEmpty())
    {
        notswaggerlist.AddRange(_SwaggerConfig.NotGroupName.Split(",").ToList());
    }
    app.UseMiddleware<SwaggerAuthorizeMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        #region 分组方法一
        //根据ApiGroupNames枚举值生成接口文档，Skip(1)是因为Enum第一个FieldInfo返回的是一个Int值
        typeof(ApiGroupNames).GetFields().Skip(1).ToList().ForEach(f =>
        {
            //获取枚举值上的特性
            var info = f.GetCustomAttributes(typeof(GroupInfoAttribute), false).OfType<GroupInfoAttribute>().FirstOrDefault();
            if (notswaggerlist.Count > 0)
            {
                if (!notswaggerlist.Contains(info.PrimaryKey))
                {
                    c.SwaggerEndpoint($"{_SwaggerConfig.File}/swagger/{f.Name}/swagger.json", info != null ? info.Title : f.Name);
                }
            }
            else
            {
                c.SwaggerEndpoint($"{_SwaggerConfig.File}/swagger/{f.Name}/swagger.json", info != null ? info.Title : f.Name);
            }
        });
        #endregion

        //设置过滤框中大小写严格。
        c.EnableFilter();
        // 展开设置
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);//DocExpansion设置为none，折叠所有方法
        c.DefaultModelsExpandDepth(-1);//DefaultModelsExpandDepth设置为-1 可不显示models（Schemas）

        //已选择的接口地址添加到URL导航栏，方便定位
        c.EnableDeepLinking();
        c.DisplayRequestDuration();
        c.EnableValidator();
        c.ShowCommonExtensions();

        #region 自定义样式

        //css 注入
        c.InjectStylesheet($"{_SwaggerConfig.File}/css/swaggerdoc.css");
        c.InjectStylesheet($"{_SwaggerConfig.File}/css/app.min.css");
        //js 注入
        c.InjectJavascript($"{_SwaggerConfig.File}/js/jquery.js");
        c.InjectJavascript($"{_SwaggerConfig.File}/js/swaggerdoc.js");
        c.InjectJavascript($"{_SwaggerConfig.File}/js/app.min.js");
        c.InjectJavascript($"{_SwaggerConfig.File}/js/swaggertranslator.js");

        #endregion
    });
}

app.UseDefaultFiles();

var provider = new FileExtensionContentTypeProvider();
//添加以下的映射关系
provider.Mappings[".apk"] = "application/vnd.android.package-archive";
provider.Mappings[".nupkg"] = "application/zip";
provider.Mappings[".mp4"] = "video/mp4";
provider.Mappings[".txt"] = "text/plain";
provider.Mappings[".png"] = "image/png";
provider.Mappings[".bmp"] = "image/bmp";
provider.Mappings[".gif"] = "image/gif";
provider.Mappings[".jpeg"] = "image/jpeg";
provider.Mappings[".jpg"] = "image/jpg";
provider.Mappings[".flv"] = "application/octet-stream";
provider.Mappings[".doc"] = "application/msword";
provider.Mappings[".xls"] = "application/vnd.ms-excel";
provider.Mappings[".pdf"] = "application/pdf";
provider.Mappings[".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
provider.Mappings[".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
provider.Mappings[".exe"] = "application/x-msdownload";

//添加API访问静态文件的地址(文件夹)
app.UseStaticFiles(new StaticFileOptions
{
    //FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(AppContext.BaseDirectory),
    ContentTypeProvider = provider,//应用新的映射关系
});

//cookie
app.UseCookiePolicy();

//错误页
app.UseStatusCodePages();

app.UseRouting();

app.UseCors();

app.UseAuthorization();


//事件总线订阅监听
app.ConfigureEventBus();

// 使用控制路由 依靠特性[Route()]
app.MapControllers();

//SignalR信号中心端点，路径须与前端配置 VITE_BASE_URL_WIRHURL 保持一致
app.MapHub<IotWebApi.Services.Jobs.ChatServer>("/signalr/chatHub");

// 监听应用启动完成事件，执行初始化逻辑
app.Lifetime.ApplicationStarted.Register(() =>
{
    // 等应用启动延迟2秒执行，确保所有服务都已初始化
    Task.Delay(2000).ContinueWith(_ =>
    {
        try
        {
            // 获取QuartzService实例
            var quartzService = app.Services.GetService<QuartzService>();
            if (quartzService != null)
            {
                // 初始化系统任务
                IotWebApi.Services.Jobs.JobInitializer.InitializeJobs(quartzService);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"任务初始化失败: {ex.Message}");
        }
    });
});

//等待数据库连接就绪，连接失败时阻塞重试而不是崩溃退出
await DatabaseHealthCheck.WaitForConnectionAsync(retryIntervalSeconds: 30);

app.Run();
