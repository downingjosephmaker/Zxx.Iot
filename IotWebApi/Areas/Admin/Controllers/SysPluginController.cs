using CenBoCommon.Zxx;
using CenboEventBus;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using IotModel;
using IotWebApi.Services;

namespace IotWebApi.Areas.Admin.Controllers
{
    /// <summary>
    /// 插件管理
    /// </summary>
    [ApiController]
    [ControllSort("1-30")]
    public class SysPluginController : ControllerBaseApi
    {
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
            var job = SysPluginDAO.Instance.GetOneBy(t => t.PluginGuid == guid);
            return job;
        }

        /// <summary>
        /// 根据插件Guid删除
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
            Status = SysPluginDAO.Instance.DeleteBy(t => t.PluginGuid == guid);
            if (Status)
            {
                await _pluginService.LoadOrUpdatePluginsAsync();
                Message = "插件删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 启用/禁用插件
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
            var optmdl = Request.GetToken();
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
            Status = SysPluginDAO.Instance.UpdateColumns(info, it => new { it.PluginStatus, it.UpdateId, it.UpdateName, it.UpdateTime });
            if (Status)
            {
                if (OperatorCommon.DicPlugins.TryGetValue(info.PluginGuid, out var plugin))
                {
                    if (pluginstatus == 1)
                    {
                        await plugin.PluginStart(info.PluginConfig);
                    }
                    else
                    {
                        await plugin.PluginStop();
                    }
                }
                Message = "插件操作成功。";
            }
            return Message;
        }

        /// <summary>
        /// 插件新增
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
            var optmdl = Request.GetToken();
            info.CreateId = optmdl.UserID;
            info.CreateTime = DateTime.Now.ToDateTimeString();
            info.CreateName = optmdl.UserName;
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = SysPluginDAO.Instance.Insert(info);
            if (Status)
            {
                await _pluginService.LoadOrUpdatePluginsAsync();
                Message = "插件信息新增成功。";
            }
            return Message;
        }

        /// <summary>
        /// 插件修改
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string Update(SysPluginEntity info)
        {
            Status = false;
            Message = "插件信息更新失败。";
            var optmdl = Request.GetToken();
            var temp = SysPluginDAO.Instance.GetOneBy(t => t.PluginGuid == info.PluginGuid);
            if (temp == null)
            {
                Message = $"插件[{info.PluginName}]不存在";
                return Message;
            }
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = SysPluginDAO.Instance.UpdateIgnoreColumns(info, it => new
            {
                it.CreateId,
                it.CreateName,
                it.CreateTime,
                it.PluginStatus
            });
            Message = "插件信息更新成功。";
            return Message;
        }

        /// <summary>
        /// 上传插件
        /// </summary>
        /// <param name="file">附件</param>
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
            if (file == null)
            {
                data.Message = "上传插件不能为空";
                return data;
            }
            var optmdl = Request.GetToken();
            var _pluginDirectory = Path.Combine(OperatorCommon.NetLocalfile, "plugins");
            if (!Directory.Exists(_pluginDirectory)) Directory.CreateDirectory(_pluginDirectory);

            var pluginList = SysPluginDAO.Instance.GetList();
            string pluginGuid = "";

            // 1. 保存文件
            var fileName = Path.GetFileName(file.FileName);
            var savePath = Path.Combine(_pluginDirectory, fileName);
            if (System.IO.File.Exists(savePath))
            {
                if (pluginList.IsZxxAny())
                {
                    var plugin = pluginList.First(t => t.PluginPath.Contains(fileName));
                    if (plugin != null)
                    {
                        pluginGuid = plugin.PluginGuid;
                    }
                }
                // 1. 卸载上下文
                if (OperatorCommon.PluginLoadContexts.TryGetValue(pluginGuid, out var oldContext))
                {
                    oldContext.Unload();
                    OperatorCommon.PluginLoadContexts.Remove(pluginGuid);
                }
                // 2. 强制GC
                GC.Collect();
                GC.WaitForPendingFinalizers();
                // 3. 再尝试删除/覆盖
                try
                {
                    System.IO.File.Delete(savePath); // 或直接覆盖
                }
                catch (Exception ex)
                {
                    // 文件仍被占用，提示用户
                    data.Message = "插件DLL被占用，请稍后重试或重启服务。";
                    return data;
                }
            }
            using (var stream = new FileStream(savePath, FileMode.OpenOrCreate))
            {
                await file.CopyToAsync(stream);
            }

            string PluginPath = Path.Combine(OperatorCommon.NetYingShefile, "plugins", fileName);

            // 2. 使用AssemblyLoadContext加载DLL并查找插件GUID
            Assembly pluginAssembly = null;
            PluginLoadContext loadContext = null;
            try
            {
                loadContext = new PluginLoadContext(savePath);
                pluginAssembly = loadContext.LoadFromAssemblyPath(savePath);
                var pluginTypes = pluginAssembly.GetTypes()
                    .Where(t => typeof(ICenBoPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
                foreach (var pluginType in pluginTypes)
                {
                    var plugin = (ICenBoPlugin)Activator.CreateInstance(pluginType);
                    pluginGuid = plugin.PluginGuid;
                    break; // 只取第一个
                }
            }
            catch
            {
                // 忽略异常，后续处理
            }
            if (!string.IsNullOrEmpty(pluginGuid))
            {
                if (OperatorCommon.DicPlugins.TryGetValue(pluginGuid, out var oldPlugin))
                {
                    await oldPlugin.PluginStop();
                    OperatorCommon.DicPlugins.Remove(pluginGuid);

                    // 卸载旧的AssemblyLoadContext
                    if (OperatorCommon.PluginLoadContexts != null && OperatorCommon.PluginLoadContexts.TryGetValue(pluginGuid, out var oldContext))
                    {
                        oldContext.Unload();
                        OperatorCommon.PluginLoadContexts.Remove(pluginGuid);
                    }
                }
            }

            // 3. 反射加载dll并注册/更新数据库（用新的loadContext）
            var pluginTypes2 = pluginAssembly.GetTypes()
                .Where(t => typeof(ICenBoPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
            int successCount = 0;

            foreach (var pluginType in pluginTypes2)
            {
                PluginPath = PluginPath.Replace(@"\", "/");
                var plugin = (ICenBoPlugin)Activator.CreateInstance(pluginType);
                var entity = new SysPluginEntity
                {
                    PluginGuid = plugin.PluginGuid,
                    PluginName = plugin.PluginName,
                    PluginType = plugin.PluginType,
                    PluginModelPath = plugin.PluginModelPath,
                    PluginVersion = plugin.PluginVersion,
                    PluginDesc = plugin.PluginDesc,
                    PluginPath = PluginPath,
                    PluginStatus = 0, // 默认未启用
                    CreateTime = DateTime.Now.ToDateTimeString(),
                    UpdateTime = DateTime.Now.ToDateTimeString(),
                    CreateId = optmdl.UserID,
                    CreateName = optmdl.UserName,
                    UpdateId = optmdl.UserID,
                    UpdateName = optmdl.UserName,
                };

                var exist = pluginList.Find(t => t.PluginGuid == entity.PluginGuid);
                if (exist == null)
                {
                    SysPluginDAO.Instance.Insert(entity);
                }
                else
                {
                    exist.PluginName = entity.PluginName;
                    exist.PluginType = entity.PluginType;
                    exist.PluginModelPath = plugin.PluginModelPath;
                    exist.PluginVersion = entity.PluginVersion;
                    exist.PluginDesc = entity.PluginDesc;
                    exist.PluginPath = entity.PluginPath;
                    exist.UpdateTime = entity.UpdateTime;
                    exist.UpdateId = optmdl.UserID;
                    exist.UpdateName = optmdl.UserName;
                    SysPluginDAO.Instance.UpdateColumns(exist, it => new
                    {
                        it.PluginName,
                        it.PluginType,
                        it.PluginVersion,
                        it.PluginDesc,
                        it.PluginPath,
                        it.UpdateId,
                        it.UpdateTime,
                        it.UpdateName
                    });
                }
                // 注册新的AssemblyLoadContext
                if (OperatorCommon.PluginLoadContexts != null)
                    OperatorCommon.PluginLoadContexts[plugin.PluginGuid] = loadContext;
                successCount++;
            }

            // 4. 刷新插件服务
            await _pluginService.LoadOrUpdatePluginsAsync();

            if (successCount > 0)
            {
                data.Status = true;
                data.Message = $"插件上传并注册成功，共发现{successCount}个插件。";
            }
            else
            {
                data.Message = "未发现有效插件类型。";
            }

            return data;
        }

    }

}