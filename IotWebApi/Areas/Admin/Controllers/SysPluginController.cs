using CenBoCommon.Zxx;
using CenboEventBus;
using IotLog;
using IotModel;
using IotWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.IO.Compression;

namespace IotWebApi.Areas.Admin.Controllers
{
    /// <summary>
    /// 插件管理(B-1.6:zip/DLL上传落版本化目录plugins/{guid}/{时间戳}/(部署目录下,不在/files静态映射内)+即时装卸;
    /// 上传/启停/删除/配置保存等价远程代码执行入口,仅超级管理员可操作(Q6),
    /// 前端meta.roles只是展示层,后端必须收口)
    /// </summary>
    [ApiController]
    [ControllSort("1-30")]
    public class SysPluginController : ControllerBaseApi
    {
        private const string PLUGIN_CATEGORY = "插件管理";

        private readonly PluginService _pluginService;

        /// <summary>
        /// 构造函数-获取依赖注入
        /// </summary>
        /// <param name="pluginService"></param>
        public SysPluginController(PluginService pluginService)
        {
            _pluginService = pluginService;
        }

        /// <summary>
        /// 超管校验(非超管返回true并置好失败消息)
        /// </summary>
        private bool DenyIfNotSuperAdmin(out OperatorModel optmdl)
        {
            optmdl = Request.GetToken();
            if (optmdl == null || !optmdl.IsSystem)
            {
                Status = false;
                Message = "仅超级管理员可执行插件管理操作。";
                return true;
            }
            return false;
        }

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name="model">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public List<SysPluginEntity> GetListByPage(ActionPara model)
        {
            // 读侧同样收口:plugin_config含明文凭据(如OpcUa账号口令),不能对任意登录用户敞开
            if (DenyIfNotSuperAdmin(out _)) return new List<SysPluginEntity>();
            int totalNumber = 0;
            var list = SysPluginDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 根据插件Guid获取插件详情
        /// </summary>
        /// <param name="guid">插件Guid</param>
        /// <returns>插件详情</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public SysPluginEntity GetInfoByGuid(string guid)
        {
            if (DenyIfNotSuperAdmin(out _)) return null;
            var job = SysPluginDAO.Instance.GetOneBy(t => t.PluginGuid == guid);
            return job;
        }

        /// <summary>
        /// 获取插件配置表单Schema(读sys_plugin.plugin_manifest的configSchema节,
        /// 与product_command.ParamSchema同构,前端动态表单直接渲染)
        /// </summary>
        /// <param name="guid">插件Guid</param>
        /// <returns>configSchema JSON(一层properties)</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string GetConfigSchema(string guid)
        {
            Status = false;
            // schema的default取自插件本地Config实例当前值,可能携带本地已配置的凭据,同样仅超管可读
            if (DenyIfNotSuperAdmin(out _)) return "";
            var info = SysPluginDAO.Instance.GetOneBy(t => t.PluginGuid == guid);
            if (info == null)
            {
                Message = "插件Guid不存在。";
                return "";
            }
            if (info.PluginManifest.IsZxxNullOrEmpty())
            {
                Message = "插件无自描述清单(旧插件需重新上传或加载一次以回写Manifest)。";
                return "";
            }
            try
            {
                var schema = JObject.Parse(info.PluginManifest)["configSchema"];
                if (schema == null)
                {
                    Message = "插件清单中未声明配置Schema。";
                    return "";
                }
                Status = true;
                return schema.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch
            {
                Message = "插件清单JSON解析失败。";
                return "";
            }
        }

        /// <summary>
        /// 全部已加载插件声明的控制命令(B-1.4白名单单源化:
        /// 前端command/linkage表单下拉与后端下发校验共用同一数据)
        /// </summary>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public List<PluginService.PluginCommandInfo> GetSupportedCommands()
        {
            return PluginService.GetSupportedCommands();
        }

        /// <summary>
        /// 已启用插件的驱动认领清单(C-3点表按驱动裁剪:产品类型编码→采集字段子集fieldGroup+寻址说明;
        /// 仅元数据投影不含配置凭据,点表配置页非超管也要用,故与GetSupportedCommands同口径仅[Token])
        /// </summary>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public List<PluginDriverClaim> GetDriverClaims()
        {
            var result = new List<PluginDriverClaim>();
            var plugins = SysPluginDAO.Instance.GetListBy(t => t.PluginStatus == 1);
            if (!plugins.IsZxxAny()) return result;
            foreach (var item in plugins)
            {
                var claim = new PluginDriverClaim
                {
                    PluginGuid = item.PluginGuid,
                    PluginName = item.PluginName
                };
                if (!item.PluginConfig.IsZxxNullOrEmpty())
                {
                    try
                    {
                        var codes = JObject.Parse(item.PluginConfig)["DeviceTypeCodes"]?.Value<string>() ?? "";
                        claim.DeviceTypeCodes = codes
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToList();
                    }
                    catch { /* 配置非法JSON时按未认领 */ }
                }
                if (!item.PluginManifest.IsZxxNullOrEmpty())
                {
                    try
                    {
                        var manifest = JObject.Parse(item.PluginManifest);
                        claim.FieldGroup = manifest["fieldGroup"]?.Value<string>() ?? "";
                        claim.Addressing = manifest["addressing"]?.Value<string>() ?? "";
                    }
                    catch { /* Manifest非法JSON时不裁剪 */ }
                }
                result.Add(claim);
            }
            return result;
        }

        /// <summary>
        /// 根据插件Guid删除(D11+补审修复:先删登记再卸载——
        /// 排队中的重载在闸门内重读DB已无此行即走卸载分支,不会把删除中的插件复活成孤儿实例)
        /// </summary>
        /// <param name="guid">插件Guid</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public async Task<string> Delete(string guid)
        {
            Status = false;
            Message = "插件删除失败。";
            if (DenyIfNotSuperAdmin(out _)) return Message;
            Status = SysPluginDAO.Instance.DeleteBy(t => t.PluginGuid == guid);
            // 行已不存在时也执行卸载:可顺手清除历史竞态遗留的无行孤儿实例
            await _pluginService.UnloadOneAsync(guid);
            if (Status)
            {
                Message = "插件删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 启用/禁用插件(D6/D8修复:启用即加载并启动——未加载的插件也能拉起;
        /// 停用即Stop运行实例并卸载ALC)
        /// </summary>
        /// <param name="guid">插件Guid</param>
        /// <param name="pluginstatus">插件状态(0:禁用,1:启用)</param>
        /// <returns>操作结果</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public async Task<string> EnablePlugin(string guid, int pluginstatus)
        {
            Status = false;
            Message = "插件操作失败。";
            if (DenyIfNotSuperAdmin(out var optmdl)) return Message;
            var info = SysPluginDAO.Instance.GetOneBy(t => t.PluginGuid == guid);
            if (info == null)
            {
                Message = "插件Guid不存在。";
                return Message;
            }

            // 更新插件状态
            info.PluginStatus = pluginstatus;
            info.UpdateId = optmdl.UserID;
            info.UpdateName = optmdl.UserName;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.ExpandObject = null;
            Status = SysPluginDAO.Instance.UpdateColumns(info, it => new { it.PluginStatus, it.UpdateId, it.UpdateName, it.UpdateTime });
            if (Status)
            {
                if (pluginstatus == 1)
                {
                    bool loaded = await _pluginService.ReloadOneAsync(guid);
                    Message = loaded ? "插件已启用并加载。" : "插件已启用，但加载失败，详见系统日志。";
                }
                else
                {
                    await _pluginService.UnloadOneAsync(guid);
                    Message = "插件已停用并卸载。";
                }
            }
            return Message;
        }

        /// <summary>
        /// 保存插件配置(B-1.7:plugin_config为唯一事实源,校验JSON后落库,
        /// 已启用插件即时重载生效,不再需要重启进程)
        /// </summary>
        /// <param name="guid">插件Guid</param>
        /// <param name="configjson">配置JSON(与GetConfigSchema的schema同构)</param>
        /// <returns>操作结果</returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public async Task<string> SaveConfig(string guid, string configjson)
        {
            Status = false;
            Message = "插件配置保存失败。";
            if (DenyIfNotSuperAdmin(out var optmdl)) return Message;
            var info = SysPluginDAO.Instance.GetOneBy(t => t.PluginGuid == guid);
            if (info == null)
            {
                Message = "插件Guid不存在。";
                return Message;
            }
            if (configjson.IsZxxNullOrEmpty())
            {
                Message = "配置内容不能为空。";
                return Message;
            }
            try { JObject.Parse(configjson); }
            catch
            {
                Message = "配置内容不是合法JSON。";
                return Message;
            }

            info.PluginConfig = configjson;
            info.UpdateId = optmdl.UserID;
            info.UpdateName = optmdl.UserName;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.ExpandObject = null;  //防FullEntity写路径用空壳拓展对象覆写plugin_config
            Status = SysPluginDAO.Instance.UpdateColumns(info, it => new { it.PluginConfig, it.UpdateId, it.UpdateName, it.UpdateTime });
            if (Status)
            {
                if (info.PluginStatus == 1)
                {
                    bool reloaded = await _pluginService.ReloadOneAsync(guid);
                    Message = reloaded ? "插件配置已保存并即时生效。" : "配置已保存，但插件重载失败，详见系统日志。";
                }
                else
                {
                    Message = "插件配置已保存(插件未启用,启用时生效)。";
                }
            }
            return Message;
        }

        /// <summary>
        /// 插件新增(手工登记场景,如带依赖的整目录部署后补记录)
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public async Task<string> Insert(SysPluginEntity info)
        {
            Status = false;
            Message = "插件表信息保存失败。";
            if (DenyIfNotSuperAdmin(out var optmdl)) return Message;
            info.CreateId = optmdl.UserID;
            info.CreateTime = DateTime.Now.ToDateTimeString();
            info.CreateName = optmdl.UserName;
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            info.ExpandObject = null;
            Status = SysPluginDAO.Instance.Insert(info);
            if (Status)
            {
                await _pluginService.ReloadOneAsync(info.PluginGuid);
                Message = "插件信息新增成功。";
            }
            return Message;
        }

        /// <summary>
        /// 插件修改(元数据维护;配置只经SaveConfig修改,Manifest只由上传/加载反射回写;
        /// 路径变更且插件已启用时即时重载)
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public async Task<string> Update(SysPluginEntity info)
        {
            Status = false;
            Message = "插件信息更新失败。";
            if (DenyIfNotSuperAdmin(out var optmdl)) return Message;
            var temp = SysPluginDAO.Instance.GetOneBy(t => t.PluginGuid == info.PluginGuid);
            if (temp == null)
            {
                Message = $"插件[{info.PluginName}]不存在";
                return Message;
            }
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            info.ExpandObject = null;
            Status = SysPluginDAO.Instance.UpdateIgnoreColumns(info, it => new
            {
                it.CreateId,
                it.CreateName,
                it.CreateTime,
                it.PluginStatus,
                it.PluginConfig,
                it.PluginManifest
            });
            if (Status && temp.PluginStatus == 1 && temp.PluginPath != info.PluginPath)
            {
                // 补审修复:重载结果须回传——路径改坏时旧实例已按新路径重载失败,不能报"更新成功"了事
                bool reloaded = await _pluginService.ReloadOneAsync(info.PluginGuid);
                Message = reloaded ? "插件信息更新成功，已按新路径重载。"
                    : "插件信息更新成功，但按新路径重载失败(插件当前未运行)，请检查DLL与系统日志。";
                return Message;
            }
            Message = "插件信息更新成功。";
            return Message;
        }

        /// <summary>
        /// 上传插件(zip整包或单DLL;解压/落位到版本化目录plugins/{guid}/{时间戳}/,
        /// 规避Windows文件锁与ALC协作式卸载不确定性;临时可回收ALC反射元数据登记入库,
        /// 新插件默认停用;已启用的插件上传后即时热更新到新版本)
        /// </summary>
        /// <param name="file">附件(.dll或.zip)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public async Task<MetaData> UploadPluginFile(IFormFile file)
        {
            MetaData data = new()
            {
                Status = false,
                Message = "上传插件失败"
            };
            if (DenyIfNotSuperAdmin(out var optmdl))
            {
                data.Message = Message;
                return data;
            }
            if (file == null)
            {
                data.Message = "上传插件不能为空";
                return data;
            }
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".dll" && ext != ".zip")
            {
                data.Message = "仅支持上传DLL或zip插件包";
                return data;
            }

            // 1. 落入临时目录(zip解压/单DLL直存)
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var stagingDir = Path.Combine(OperatorCommon.PluginLocalRoot, "_staging", timestamp);
            Directory.CreateDirectory(stagingDir);
            try
            {
                if (ext == ".zip")
                {
                    using var archive = new ZipArchive(file.OpenReadStream(), ZipArchiveMode.Read);
                    archive.ExtractToDirectory(stagingDir);  //内置ZipSlip路径穿越防护
                }
                else
                {
                    var savePath = Path.Combine(stagingDir, Path.GetFileName(file.FileName));
                    using var stream = new FileStream(savePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }

                // 1.5 剥离旁置的共享程序集副本(须由宿主deferral统一提供;旁置陈旧副本会造成跨ALC类型不同一)
                PluginLoadContext.StripSharedAssemblies(stagingDir);

                // 2. 临时可回收ALC逐个DLL反射识别插件主程序集(识别完即卸载,只保留元数据字符串)
                PluginMeta meta = null;
                foreach (var dllpath in Directory.GetFiles(stagingDir, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    meta = InspectPluginDll(dllpath);
                    if (meta != null) break; // 只取第一个实现
                }
                if (meta == null || meta.PluginGuid.IsZxxNullOrEmpty())
                {
                    data.Message = "未发现有效插件类型(须实现ICenBoPlugin)。";
                    return data;
                }
                string mainDll = meta.MainDll, pluginGuid = meta.PluginGuid, pluginName = meta.PluginName,
                       pluginType = meta.PluginType, pluginVersion = meta.PluginVersion, pluginDesc = meta.PluginDesc,
                       pluginModelPath = meta.PluginModelPath, pluginManifest = meta.PluginManifest;

                // 3. 落位版本化目录(旧版本目录不动,规避运行中ALC的文件锁)
                var versionRoot = Path.Combine(OperatorCommon.PluginLocalRoot, pluginGuid);
                Directory.CreateDirectory(versionRoot);
                var versionDir = Path.Combine(versionRoot, timestamp);
                Directory.Move(stagingDir, versionDir);
                // 落库口径=相对PluginLocalRoot(与PluginService.ReloadOneCoreAsync解析基座一致)
                string pluginPath = Path.Combine(pluginGuid, timestamp, mainDll).Replace(@"\", "/");

                // 4. 登记/更新sys_plugin(新插件默认停用;DB配置为空时用Manifest缺省配置回填)
                string defaultConfig = "";
                if (!pluginManifest.IsZxxNullOrEmpty())
                {
                    try { defaultConfig = JObject.Parse(pluginManifest)["defaultConfig"]?.ToString(Newtonsoft.Json.Formatting.None) ?? ""; }
                    catch { /* Manifest非法JSON时不回填 */ }
                }
                var exist = SysPluginDAO.Instance.GetOneBy(t => t.PluginGuid == pluginGuid);
                if (exist == null)
                {
                    var entity = new SysPluginEntity
                    {
                        PluginGuid = pluginGuid,
                        PluginName = pluginName,
                        PluginType = pluginType,
                        PluginModelPath = pluginModelPath,
                        PluginVersion = pluginVersion,
                        PluginDesc = pluginDesc,
                        PluginPath = pluginPath,
                        PluginStatus = 0, // 默认未启用
                        PluginConfig = defaultConfig,
                        PluginManifest = pluginManifest ?? "",
                        CreateTime = DateTime.Now.ToDateTimeString(),
                        UpdateTime = DateTime.Now.ToDateTimeString(),
                        CreateId = optmdl.UserID,
                        CreateName = optmdl.UserName,
                        UpdateId = optmdl.UserID,
                        UpdateName = optmdl.UserName,
                    };
                    SysPluginDAO.Instance.Insert(entity);
                }
                else
                {
                    exist.PluginName = pluginName;
                    exist.PluginType = pluginType;
                    exist.PluginModelPath = pluginModelPath;
                    exist.PluginVersion = pluginVersion;
                    exist.PluginDesc = pluginDesc;
                    exist.PluginPath = pluginPath;
                    exist.PluginManifest = pluginManifest ?? "";
                    exist.UpdateTime = DateTime.Now.ToDateTimeString();
                    exist.UpdateId = optmdl.UserID;
                    exist.UpdateName = optmdl.UserName;
                    exist.ExpandObject = null;  //防FullEntity写路径用空壳拓展对象覆写plugin_config
                    bool fillconfig = exist.PluginConfig.IsZxxNullOrEmpty() && !defaultConfig.IsZxxNullOrEmpty();
                    if (fillconfig)
                    {
                        exist.PluginConfig = defaultConfig;
                        SysPluginDAO.Instance.UpdateColumns(exist, it => new
                        {
                            it.PluginName,
                            it.PluginType,
                            it.PluginModelPath,
                            it.PluginVersion,
                            it.PluginDesc,
                            it.PluginPath,
                            it.PluginManifest,
                            it.PluginConfig,
                            it.UpdateId,
                            it.UpdateTime,
                            it.UpdateName
                        });
                    }
                    else
                    {
                        SysPluginDAO.Instance.UpdateColumns(exist, it => new
                        {
                            it.PluginName,
                            it.PluginType,
                            it.PluginModelPath,
                            it.PluginVersion,
                            it.PluginDesc,
                            it.PluginPath,
                            it.PluginManifest,
                            it.UpdateId,
                            it.UpdateTime,
                            it.UpdateName
                        });
                    }
                }

                // 5. 清理旧版本目录(保留最新2个;被未卸载ALC占用的删除失败跳过,待下次上传重试)
                CleanupOldVersions(versionRoot, 2);

                // 6. 已启用的插件即时热更新到新版本(停用的等待启用时加载)
                bool hotreloaded = false;
                if (exist != null && exist.PluginStatus == 1)
                {
                    hotreloaded = await _pluginService.ReloadOneAsync(pluginGuid);
                }

                data.Status = true;
                // 补审修复:区分四种真实状态,已启用但热更失败时不再误提示"默认停用请配置后启用"
                if (exist == null)
                    data.Message = $"插件[{pluginName}]上传并登记成功，默认停用，请配置后启用。";
                else if (exist.PluginStatus != 1)
                    data.Message = $"插件[{pluginName}]上传并更新成功，插件当前停用，启用后按新版本加载。";
                else if (hotreloaded)
                    data.Message = $"插件[{pluginName}]上传并登记成功，已即时热更新。";
                else
                    data.Message = $"插件[{pluginName}]上传落位成功，但热更新失败(插件当前未运行)，请检查配置与系统日志。";
                return data;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), PLUGIN_CATEGORY);
                data.Message = $"上传插件失败：{ex.Message}";
                return data;
            }
            finally
            {
                // 残留临时目录清理(识别失败/异常路径;成功路径已Move走不存在)
                try { if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true); } catch { }
            }
        }

        /// <summary>
        /// 清理旧版本目录(保留最新keep个;旧版本可能仍被未卸载的ALC占用,删除失败跳过待下次)
        /// </summary>
        private static void CleanupOldVersions(string versionRoot, int keep)
        {
            try
            {
                var stale = Directory.GetDirectories(versionRoot)
                    .OrderByDescending(t => Path.GetFileName(t))
                    .Skip(keep);
                foreach (var dir in stale)
                {
                    try { Directory.Delete(dir, true); }
                    catch { /* 文件被占用,待ALC回收后下次清理 */ }
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), PLUGIN_CATEGORY);
            }
        }

        /// <summary>
        /// 反射识别单个DLL是否为 ICenBoPlugin 插件主程序集，返回其元数据；非插件/依赖DLL返回null。
        /// 用临时可回收ALC加载，识别完即卸载，只保留元数据字符串。上传与扫描共用。
        /// </summary>
        private static PluginMeta InspectPluginDll(string dllpath)
        {
            var inspectContext = new PluginLoadContext(dllpath);
            try
            {
                var assembly = inspectContext.LoadFromAssemblyPath(dllpath);
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (System.Reflection.ReflectionTypeLoadException rtle)
                {
                    // 部分类型依赖缺失(如外部包DLL未随行)时,仍从可加载的类型里找插件主类
                    types = rtle.Types.Where(t => t != null).ToArray();
                }
                var plugintype = types
                    .FirstOrDefault(t => typeof(ICenBoPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
                if (plugintype == null) return null;
                var plugin = (ICenBoPlugin)Activator.CreateInstance(plugintype);
                var meta = new PluginMeta
                {
                    MainDll = Path.GetFileName(dllpath),
                    PluginGuid = plugin.PluginGuid,
                    PluginName = plugin.PluginName,
                    PluginType = plugin.PluginType,
                    PluginVersion = plugin.PluginVersion,
                    PluginDesc = plugin.PluginDesc,
                    PluginModelPath = plugin.PluginModelPath
                };
                try { meta.PluginManifest = plugin.PluginManifest; }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件[{meta.PluginGuid}]Manifest读取失败：{ex}", PLUGIN_CATEGORY);
                }
                return meta;
            }
            catch (Exception ex)
            {
                // 依赖DLL/非托管DLL反射失败属正常;实现了ICenBoPlugin却实例化失败(如缺外部包)时记日志便于排查
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"识别插件DLL[{Path.GetFileName(dllpath)}]跳过：{ex.Message}", PLUGIN_CATEGORY);
                return null;
            }
            finally
            {
                inspectContext.Unload();
            }
        }

        /// <summary>
        /// 扫描插件存储目录(PluginLocalRoot)并批量登记/更新到 sys_plugin。
        /// 递归识别每个 ICenBoPlugin 主程序集，按 Guid 去重(同 Guid 取版本目录名最大者=最新)，
        /// 新插件默认停用；已存在的仅刷新元数据与落位路径(不动运行状态与已有配置)。
        /// 用于批量入库(免逐个上传)与 DB↔磁盘记录自愈。等价 RCE 入口，仅超管可执行。
        /// </summary>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public MetaData ScanAndRegister()
        {
            var data = new MetaData { Status = false, Message = "扫描插件失败" };
            if (DenyIfNotSuperAdmin(out var optmdl))
            {
                data.Message = Message;
                return data;
            }
            try
            {
                string root = OperatorCommon.PluginLocalRoot;
                var allDlls = Directory.GetFiles(root, "*.dll", SearchOption.AllDirectories);
                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"扫描插件根：{root}，发现 {allDlls.Length} 个DLL", PLUGIN_CATEGORY);
                // 候选：递归所有dll，跳过_staging；按Guid分组，同Guid取所在目录名最大者(时间戳/版本更新)
                var candidates = new Dictionary<string, (PluginMeta Meta, string RelPath, string DirName)>();
                foreach (var dllpath in allDlls)
                {
                    var rel = Path.GetRelativePath(root, dllpath).Replace(@"\", "/");
                    if (rel.StartsWith("_staging/", StringComparison.OrdinalIgnoreCase)) continue;
                    var meta = InspectPluginDll(dllpath);
                    if (meta == null || meta.PluginGuid.IsZxxNullOrEmpty()) continue;
                    var dirName = Path.GetFileName(Path.GetDirectoryName(dllpath)) ?? "";
                    if (candidates.TryGetValue(meta.PluginGuid, out var prev) &&
                        string.CompareOrdinal(prev.DirName, dirName) >= 0) continue;
                    candidates[meta.PluginGuid] = (meta, rel, dirName);
                }

                int inserted = 0, updated = 0;
                var names = new List<string>();
                foreach (var kv in candidates)
                {
                    var (m, relPath, _) = kv.Value;
                    string defaultConfig = "";
                    if (!m.PluginManifest.IsZxxNullOrEmpty())
                    {
                        try { defaultConfig = JObject.Parse(m.PluginManifest)["defaultConfig"]?.ToString(Newtonsoft.Json.Formatting.None) ?? ""; }
                        catch { /* Manifest非法JSON时不回填 */ }
                    }
                    var exist = SysPluginDAO.Instance.GetOneBy(t => t.PluginGuid == kv.Key);
                    if (exist == null)
                    {
                        SysPluginDAO.Instance.Insert(new SysPluginEntity
                        {
                            PluginGuid = kv.Key,
                            PluginName = m.PluginName,
                            PluginType = m.PluginType,
                            PluginModelPath = m.PluginModelPath,
                            PluginVersion = m.PluginVersion,
                            PluginDesc = m.PluginDesc,
                            PluginPath = relPath,
                            PluginStatus = 0, // 默认停用
                            PluginConfig = defaultConfig,
                            PluginManifest = m.PluginManifest ?? "",
                            CreateTime = DateTime.Now.ToDateTimeString(),
                            UpdateTime = DateTime.Now.ToDateTimeString(),
                            CreateId = optmdl.UserID,
                            CreateName = optmdl.UserName,
                            UpdateId = optmdl.UserID,
                            UpdateName = optmdl.UserName,
                        });
                        inserted++;
                    }
                    else
                    {
                        exist.PluginName = m.PluginName;
                        exist.PluginType = m.PluginType;
                        exist.PluginModelPath = m.PluginModelPath;
                        exist.PluginVersion = m.PluginVersion;
                        exist.PluginDesc = m.PluginDesc;
                        exist.PluginPath = relPath;
                        exist.PluginManifest = m.PluginManifest ?? "";
                        exist.UpdateTime = DateTime.Now.ToDateTimeString();
                        exist.UpdateId = optmdl.UserID;
                        exist.UpdateName = optmdl.UserName;
                        exist.ExpandObject = null; // 防FullEntity写路径用空壳拓展对象覆写plugin_config
                        SysPluginDAO.Instance.UpdateColumns(exist, it => new
                        {
                            it.PluginName,
                            it.PluginType,
                            it.PluginModelPath,
                            it.PluginVersion,
                            it.PluginDesc,
                            it.PluginPath,
                            it.PluginManifest,
                            it.UpdateId,
                            it.UpdateTime,
                            it.UpdateName
                        });
                        updated++;
                    }
                    names.Add(m.PluginName);
                }

                data.Status = true;
                data.Message = candidates.Count == 0
                    ? "扫描完成：插件目录下未发现有效插件(须实现ICenBoPlugin)。请先将插件目录拷入 plugins 存储根后再扫描。"
                    : $"扫描完成：新增登记 {inserted} 个、更新 {updated} 个（{string.Join("、", names)}）。新插件默认停用，配置后启用。";
                return data;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), PLUGIN_CATEGORY);
                data.Message = $"扫描插件失败：{ex.Message}";
                return data;
            }
        }

        /// <summary>插件反射识别元数据(上传/扫描共用)</summary>
        private sealed class PluginMeta
        {
            public string MainDll { get; set; }
            public string PluginGuid { get; set; }
            public string PluginName { get; set; }
            public string PluginType { get; set; }
            public string PluginVersion { get; set; }
            public string PluginDesc { get; set; }
            public string PluginModelPath { get; set; }
            public string PluginManifest { get; set; }
        }

    }

    /// <summary>
    /// 插件驱动认领(点表按驱动裁剪的元数据投影,不含任何配置凭据)
    /// </summary>
    public sealed class PluginDriverClaim
    {
        /// <summary>插件Guid</summary>
        public string PluginGuid { get; set; } = "";
        /// <summary>插件名称</summary>
        public string PluginName { get; set; } = "";
        /// <summary>采集字段子集标识(modbus|dlt645|cjt188|s7|opcua,空=未声明不裁剪)</summary>
        public string FieldGroup { get; set; } = "";
        /// <summary>点表寻址说明(表单提示文本)</summary>
        public string Addressing { get; set; } = "";
        /// <summary>认领的产品类型编码清单</summary>
        public List<string> DeviceTypeCodes { get; set; } = new();
    }

}
