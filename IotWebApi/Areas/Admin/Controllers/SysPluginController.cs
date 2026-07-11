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
    /// 插件管理(B-1.6:zip/DLL上传落版本化目录files/plugins/{guid}/{时间戳}/+即时装卸;
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
        /// 根据插件Guid删除(D11修复:先停并卸载运行实例,再删登记)
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
            await _pluginService.UnloadOneAsync(guid);
            Status = SysPluginDAO.Instance.DeleteBy(t => t.PluginGuid == guid);
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
                await _pluginService.ReloadOneAsync(info.PluginGuid);
            }
            Message = "插件信息更新成功。";
            return Message;
        }

        /// <summary>
        /// 上传插件(zip整包或单DLL;解压/落位到版本化目录files/plugins/{guid}/{时间戳}/,
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
            var stagingDir = Path.Combine(OperatorCommon.NetLocalfile, "plugins", "_staging", timestamp);
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

                // 2. 临时可回收ALC逐个DLL反射识别插件主程序集(识别完即卸载,只保留元数据字符串)
                string mainDll = null, pluginGuid = null, pluginName = null, pluginType = null,
                       pluginVersion = null, pluginDesc = null, pluginModelPath = null, pluginManifest = null;
                foreach (var dllpath in Directory.GetFiles(stagingDir, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    var inspectContext = new PluginLoadContext(dllpath);
                    try
                    {
                        var assembly = inspectContext.LoadFromAssemblyPath(dllpath);
                        var plugintype = assembly.GetTypes()
                            .FirstOrDefault(t => typeof(ICenBoPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
                        if (plugintype == null) continue;
                        var plugin = (ICenBoPlugin)Activator.CreateInstance(plugintype);
                        pluginGuid = plugin.PluginGuid;
                        pluginName = plugin.PluginName;
                        pluginType = plugin.PluginType;
                        pluginVersion = plugin.PluginVersion;
                        pluginDesc = plugin.PluginDesc;
                        pluginModelPath = plugin.PluginModelPath;
                        try { pluginManifest = plugin.PluginManifest; }
                        catch (Exception ex)
                        {
                            LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"插件[{pluginGuid}]Manifest读取失败：{ex}", PLUGIN_CATEGORY);
                        }
                        mainDll = Path.GetFileName(dllpath);
                        break; // 只取第一个实现
                    }
                    catch
                    {
                        // 依赖DLL/非托管DLL反射失败属正常,继续尝试下一个
                    }
                    finally
                    {
                        inspectContext.Unload();
                    }
                }
                if (mainDll == null || pluginGuid.IsZxxNullOrEmpty())
                {
                    data.Message = "未发现有效插件类型(须实现ICenBoPlugin)。";
                    return data;
                }

                // 3. 落位版本化目录(旧版本目录不动,规避运行中ALC的文件锁)
                var versionRoot = Path.Combine(OperatorCommon.NetLocalfile, "plugins", pluginGuid);
                Directory.CreateDirectory(versionRoot);
                var versionDir = Path.Combine(versionRoot, timestamp);
                Directory.Move(stagingDir, versionDir);
                // 落库口径=相对NetLocalfile(PluginService.ReloadOneCoreAsync用NetLocalfile拼接解析,
                // NetLocalfile物理根已是{BaseDirectory}/files,带files/前缀会双重拼接致File.Exists恒假)
                string pluginPath = Path.Combine("plugins", pluginGuid, timestamp, mainDll).Replace(@"\", "/");

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
                data.Message = $"插件[{pluginName}]上传并登记成功" + (hotreloaded ? "，已即时热更新。" : "，默认停用，请配置后启用。");
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

    }

}
