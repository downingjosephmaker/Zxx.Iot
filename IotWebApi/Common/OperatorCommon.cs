using CenBoCommon.Zxx;
using CenboEventBus;
using IotModel;
using IotWebApi.Services.Jobs;
using Newtonsoft.Json;
using System.Runtime.Loader;
using System.Text;

namespace IotWebApi
{
    /// <summary>
    /// 公共方法类
    /// </summary>
    public static class OperatorCommon
    {
        /// <summary>
        /// 验证Token正确性，并返回用户模型
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static OperatorModel GetToken(this HttpRequest httpRequest)
        {
            OperatorModel model = null;

            Microsoft.Extensions.Primitives.StringValues sourcetype = "";
            httpRequest.Headers.TryGetValue("tokenout", out sourcetype);
            if (sourcetype == "out")
            {
                model = new OperatorModel()
                {
                    UserID = 1,
                    UserName = "开发管理员",
                    SourceType = "Other"
                };
                return model;
            }

            Microsoft.Extensions.Primitives.StringValues checktoken = "";
            if (httpRequest.Headers.TryGetValue("token", out checktoken))
            {
                if (checktoken.ToString().IsZxxNullOrEmpty()) return new OperatorModel();
                string key = EncryptsHelper.Decrypt(checktoken.ToString());
                var modellogin = JsonConvert.DeserializeObject<OperatorModelLogin>(key);
                if (modellogin.UserID == 0) throw new Exception("用户Token无效");

                model = new OperatorModel();
                modellogin.CopyTypeValue(model);

                //从数据库获取用户信息
                model._Sysuser = SysUserDAO.Instance.GetOneBy(t => t.UserId == model.UserID);

                //从数据库获取角色信息
                model._Sysrole = SysRoleDAO.Instance.GetOneBy(t => t.RoleId == model._Sysuser.RoleId);

                //从数据库获取权限建筑/单位
                var _SysrelList = SysRelatedDAO.Instance.GetListBy(t => t.UserId == model.UserID);
                if (_SysrelList.IsZxxAny())
                {
                    foreach (var item in _SysrelList)
                    {
                        var unit = BasicunitInfoDAO.Instance.GetOneBy(t => t.TenantId == item.TenantId);
                        if (unit != null)
                        {
                            if (!model._UnitAllList.Contains(unit)) model._UnitAllList.Add(unit);
                        }
                    }
                }

                //缓存权限单位ID
                if (model._UnitAllList.Count > 0) model._UnitIdList.AddRange(model._UnitAllList.Select(t => t.TenantId));
            }

            return model;
        }

        /// <summary>
        /// 验证Iot正确性，并返回用户模型
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static OperatorModel GetIotTripartite(this HttpRequest httpRequest)
        {
            OperatorModel model = new OperatorModel();
            if (!httpRequest.Headers.TryGetValue("CB-IOT-ID", out Microsoft.Extensions.Primitives.StringValues iotIdValues))
            {
                return model;
            }
            string iotId = iotIdValues.ToString();
            var _Sysuser = SysUserDAO.Instance.GetOneBy(t => t.UserUid == iotId);
            if (_Sysuser == null) return model;
            {
                model = new OperatorModel
                {
                    IsSystem = false,
                    DepartSelectLevel = 2,
                    SourceType = "Web",
                    TenantId = _Sysuser.TenantId,
                    UserID = _Sysuser.UserId,
                    UserName = _Sysuser.TrueName,
                };

                //从数据库获取用户信息
                model._Sysuser = _Sysuser;

                //单位名从租户表关联查(sys_user.unit_name 冗余列已删)
                if (_Sysuser.TenantId > 0)
                {
                    var unitinfo = BasicunitInfoDAO.Instance.GetOneBy(t => t.TenantId == _Sysuser.TenantId);
                    if (unitinfo != null) model.UnitName = unitinfo.UnitName;
                }

                //从数据库获取角色信息
                model._Sysrole = SysRoleDAO.Instance.GetOneBy(t => t.RoleId == model._Sysuser.RoleId);

                //从数据库获取权限建筑/单位
                var _SysrelList = SysRelatedDAO.Instance.GetListBy(t => t.UserId == model.UserID);
                if (_SysrelList.IsZxxAny())
                {
                    foreach (var item in _SysrelList)
                    {
                        var unit = BasicunitInfoDAO.Instance.GetOneBy(t => t.TenantId == item.TenantId);
                        if (unit != null)
                        {
                            if (!model._UnitAllList.Contains(unit)) model._UnitAllList.Add(unit);
                        }
                    }
                }

                //缓存权限单位ID
                if (model._UnitAllList.Count > 0) model._UnitIdList.AddRange(model._UnitAllList.Select(t => t.TenantId));
            }

            return model;
        }

        /// <summary>
        /// 获取POST请求参数2
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static string GetPostData(this HttpRequest httpRequest)
        {
            string result = "";
            try
            {
                httpRequest.EnableBuffering();
                //leaveOpen:true标识StreamReader释放时不会自动关闭流        　　
                using var reader = new StreamReader(httpRequest.Body, leaveOpen: true, encoding: Encoding.UTF8);
                result = reader.ReadToEndAsync().Result;
                //Action中可再次读取流
                httpRequest.Body.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception)
            {
                result = "";
            }
            return result;
        }

        public static bool CheckSourceType(this HttpRequest httpRequest)
        {
            bool result = false;

            try
            {
                Microsoft.Extensions.Primitives.StringValues sourcetype = "";
                httpRequest.Headers.TryGetValue("sourcetype", out sourcetype);
                if (sourcetype == "xddp")
                {
                    result = true;
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// 获取API调用来源
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static string GetApiDySource(this HttpRequest httpRequest)
        {
            string path = "";
            Microsoft.Extensions.Primitives.StringValues str = "";
            if (httpRequest.Headers.TryGetValue("ServiceName", out str))
            {
                path = str.ToString();
            }
            return path;
        }

        /// <summary>
        /// 获取菜单路径
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static string GetMenuPath(this HttpRequest httpRequest)
        {
            string path = "";
            Microsoft.Extensions.Primitives.StringValues menupath = "";

            if (httpRequest.Headers.TryGetValue("menupath", out menupath))
            {
                var menupathstr = System.Web.HttpUtility.UrlDecode(menupath.ToString());
                path = menupathstr.ToString();
            }
            return path;
        }

        public static string GetHeaderIp(this HttpRequest _Request)
        {
            string ipAddress = _Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = _Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = _Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = _Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            return ipAddress.Replace("::ffff:", "");
        }

        /// <summary>
        /// 判断是否为Linux系统
        /// </summary>
        /// <returns></returns>
        public static bool IsLinux()
        {
            // 判断是否为 Unix 平台，并且是 Linux
            return Environment.OSVersion.Platform == PlatformID.Unix &&
                   System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
        }

        /// <summary>
        /// 控制器方法路径和注释 字典
        /// </summary>
        public static Dictionary<string, string> dicroutesummary = new Dictionary<string, string>();
        /// <summary>
        /// 控制器名称和注释字典
        /// </summary>
        public static Dictionary<string, string> diccontronllersummary = new Dictionary<string, string>();

        /// <summary>
        /// 枚举对象
        /// </summary>
        public static Dictionary<string, Type> DicAllEnum = new Dictionary<string, Type>();
        public static void GetAllEnum()
        {
            DicAllEnum.Clear();
            List<string> xiangmunames = new List<string>();
            xiangmunames.Add("IotWebApi.dll");
            xiangmunames.Add("ZhjngkModel.dll");

            var assemblyList = AppDomain.CurrentDomain.GetAssemblies().Where(a => xiangmunames.Contains(a.ManifestModule.Name));
            if (assemblyList.IsZxxAny())
            {
                foreach (var assembly in assemblyList)
                {
                    Type[] types = assembly.GetTypes();
                    foreach (Type item in types)
                    {
                        if (item.IsEnum) DicAllEnum.TryAdd(item.Name, item);
                    }
                }
            }
        }

        /// <summary>
        /// 相对路径首部
        /// </summary>
        public static string NetYingShefile = "";

        private static string _netLocalFile = "";
        /// <summary>
        /// 全路径首部
        /// </summary>
        public static string NetLocalfile
        {
            get
            {
                //_netLocalFile = AppSetting.GetConfig("FileConfig:FilesPath");
                //if (_netLocalFile.IsZxxNullOrEmpty())
                //{
                //    _netLocalFile = Path.Combine(AppContext.BaseDirectory, "files");
                //}
                _netLocalFile = Path.Combine(AppContext.BaseDirectory, "files");
                if (!Directory.Exists(_netLocalFile)) Directory.CreateDirectory(_netLocalFile);
                return _netLocalFile;
            }
            set { }
        }

        /// <summary>
        /// 用户状态信息
        /// </summary>
        public static List<ZxxUserInfo> UserList = new List<ZxxUserInfo>();

        public static string SwaggerApiVersion = "1.0.0";

        /// <summary>
        /// 存储已加载的插件字典，key为PluginGuid
        /// </summary>
        public static Dictionary<string, ICenBoPlugin> DicPlugins = new();

        /// <summary>
        /// 存储每个插件的AssemblyLoadContext，key为PluginGuid
        /// </summary>
        public static Dictionary<string, AssemblyLoadContext> PluginLoadContexts = new();

        private static Dictionary<string, string> _DicIotToken = new();
        /// <summary>
        /// 中台token
        /// </summary>
        public static Dictionary<string, string> DicIotToken
        {
            get
            {
                lock (_DicIotToken)
                {
                    if (_DicIotToken.Count == 0)
                    {
                        _DicIotToken.Add("zhrg", "d121856b394222e91b4a039e7e2da570");
                        _DicIotToken.Add("dxkzq", "5ab7fceeea3af43c6db39a726cd96a4c");
                    }
                }
                return _DicIotToken;
            }
        }

    }
}
