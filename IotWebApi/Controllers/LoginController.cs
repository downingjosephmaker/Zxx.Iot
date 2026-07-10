using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Common.Baking;

namespace IotWebApi.Controllers
{

    /// <summary>
    /// 登录管理
    /// </summary>
    [ApiController]
    [ControllSort("0-1")]
    public class LoginController : ControllerBaseApi
    {

        /// <summary>
        /// 用户登录(Md5+RSA加密)
        /// </summary>
        /// <param name="login">登录模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        public OperatorModelLogin LoginUserFun(LoginEntity login)
        {
            OperatorModelLogin model = null;
            Status = false;
            Message = "用户信息获取失败。";

            var PrivateKey = AppSetting.GetConfig("DefaultValues:PrivateKey");
            var _net_privatekey = RsaUtil.LoadPrivateKey(PrivateKey);
            var _password = RSAHelper.RSADecrypt(_net_privatekey, login.UserPwd);
            if (_password.IsZxxNullOrEmpty())
            {
                Message = "密码信息错误。";
                return model;
            }
            login.UserPwd = _password;
            var user = SysUserDAO.Instance.GetOneBy(t => t.UserUid == login.UserUid && t.IsEnable == 1);
            if (user != null)
            {
                string passwordHash = HashCryto.GetHash2String(string.Concat(login.UserPwd.ToUpper(), user.PasswordSalt), HashAlgorithmType.SHA256);
                if (user.Password != passwordHash)
                {
                    Message = "用户或密码信息错误。";
                    return model;
                }
                string apiip = Request.GetHeaderIp();
                model = LoginPublicMode.GetLogin(user, login.SourceType.ToString(), apiip);
                Status = true;
                Message = "用户信息获取成功。";
            }

            return model;
        }

        /// <summary>
        /// 获取密码加密后信息
        /// </summary>
        /// <param name="pwd">密码</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        public PwdInfo GetPwdInfo(string pwd)
        {
            PwdInfo model = new PwdInfo();
            Status = false;
            Message = "密码加密后信息获取失败。";

            model.PwdMd5 = EncryptsHelper.MD5Make32(pwd).ToUpper();
            var PublicKey = AppSetting.GetConfig("DefaultValues:PublicKey");
            var net_publickey = RsaUtil.LoadPublicKey(PublicKey);
            model.PwdRsa = RSAHelper.RSAEncrypt(net_publickey, model.PwdMd5);
            Status = true;
            Message = "密码加密后信息获取成功。";

            return model;
        }

        /// <summary>
        /// 获取IOT用户加密信息
        /// </summary>
        /// <param name="iotuid">密码</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        public IotUserInfo GetIotUser(string iotuid)
        {
            IotUserInfo model = new IotUserInfo
            {
                IOT_ID = iotuid,
            };
            Status = false;
            Message = "获取IOT用户加密信息失败。";

            var _Sysuser = SysUserDAO.Instance.GetOneBy(t => t.UserUid == model.IOT_ID);
            if (_Sysuser == null) return model;

            model.IOT_KEY = EncryptsHelper.Encrypt(model.IOT_ID);

            Status = true;
            Message = "获取IOT用户加密信息成功。";

            return model;
        }

        /// <summary>
        /// 文档登录
        /// </summary>
        /// <param name="login">登录模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        public string LoginApiXml(LoginEntity login)
        {
            Status = false;
            Message = "用户信息获取失败。";

            var PrivateKey = AppSetting.GetConfig("DefaultValues:PrivateKey");
            var _net_privatekey = RsaUtil.LoadPrivateKey(PrivateKey);
            var _password = RSAHelper.RSADecrypt(_net_privatekey, login.UserPwd);
            if (_password.IsZxxNullOrEmpty())
            {
                Message = "密码信息错误。";
                return Message;
            }
            login.UserPwd = _password;
            var user = SysUserDAO.Instance.GetOneBy(t => t.UserUid == login.UserUid && t.IsEnable == 1);
            if (user != null)
            {
                string passwordHash = HashCryto.GetHash2String(string.Concat(login.UserPwd.ToUpper(), user.PasswordSalt), HashAlgorithmType.SHA256);
                if (user.Password != passwordHash)
                {
                    Message = "用户或密码信息错误。";
                    return Message;
                }
                bool IsSystem = false;
                var RoleAllList = SysRoleDAO.Instance.GetList();
                if (RoleAllList.IsZxxAny())
                {
                    var role = SysRoleDAO.Instance.GetOneBy(t => t.RoleId == user.RoleId);
                    if (role != null && role.ParentId == 0) IsSystem = true;
                }
                if (IsSystem)
                {
                    Request.HttpContext.Session.SetInt32("swagger-key", 1);
                    Status = true;
                    Message = "用户信息获取成功。";
                }
            }

            return Message;
        }

        /// <summary>
        /// 登出接口
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Token]
        [Route("Api/[controller]/[action]")]
        public string LoginOut()
        {
            Status = false;
            Message = "用户登出失败。";

            var optmdl = Request.GetToken();
            if (optmdl._Sysuser != null)
            {
                string apiip = Request.GetHeaderIp();
                var issuuc = LoginPublicMode.LoginOut(optmdl._Sysuser, optmdl.SourceType, apiip);
                if (issuuc)
                {
                    Status = true;
                    Message = "用户登出成功。";
                }
            }

            return Message;
        }

        /// <summary>
        /// 集团单点登录
        /// </summary>
        /// <param name="data">参数</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        public OperatorModelLogin LoginJtddFun(string data)
        {
            OperatorModelLogin model = null;
            Status = false;
            Message = "用户信息获取失败。";

            string sKey = "ymdwlynypt";
            //string test = "15105829885|lszp9002|何叶恬恬";
            //string testjm = EncryptsHelper.DESEncrypt(test, sKey);

            var login = EncryptsHelper.DESMDecrypt(data, sKey);
            var arry = login.ToStringList('|');
            if (arry.Count < 2) return model;
            string _UserUid = arry[0];
            string _Password = arry[1];
            string _TrueName = arry[2];
            var user = SysUserDAO.Instance.GetOneBy(t => t.UserUid == _UserUid && t.IsEnable == 1);
            if (user != null)
            {
                var _PasswordMd5 = EncryptsHelper.MD5Make32(_Password).ToUpper();
                string passwordHash = HashCryto.GetHash2String(string.Concat(_PasswordMd5, user.PasswordSalt), HashAlgorithmType.SHA256);
                if (user.Password != passwordHash)
                {
                    Message = "用户或密码信息错误。";
                    return model;
                }
                string apiip = Request.GetHeaderIp();
                model = LoginPublicMode.GetLogin(user, "Web", apiip);
                Status = true;
                Message = "用户信息获取成功。";
            }
            else
            {
                DateTime time = DateTime.Now;
                //自动注册
                var info = new SysUserEntity
                {
                    TenantId = 4,
                    UnitName = "嘉兴港野猫墩物流园区",
                    RoleId = 4,
                    UserUid = _UserUid,
                    TrueName = _TrueName,
                    CreateId = 1,
                    UpdateId = 1,
                    CreateName = "开发管理员",
                    UpdateName = "开发管理员",
                    CreateTime = time.ToDateTimeString(),
                    UpdateTime = time.ToDateTimeString(),
                    UserXb = "未知",
                    UserRemark = _Password,
                    IsEnable = 1,
                };
                info.PasswordSalt = Guid.NewGuid().ToString("N").ToUpper();
                string password = EncryptsHelper.MD5Make32(_Password).ToUpper();
                info.Password = HashCryto.GetHash2String(string.Concat(password.ToUpper(), info.PasswordSalt), HashAlgorithmType.SHA256);
                var entity = SysUserDAO.Instance.InsertReturnEntity(info);
                if (entity != null)
                {
                    string apiip = Request.GetHeaderIp();
                    model = LoginPublicMode.GetLogin(entity, "Web", apiip);
                    Status = true;
                    Message = "用户信息获取成功。";
                }
            }

            return model;
        }

    }
}