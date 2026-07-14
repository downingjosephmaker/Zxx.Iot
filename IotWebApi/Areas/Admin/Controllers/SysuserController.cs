using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Areas.Admin.Models;
using IotWebApi.Common.Baking;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 用户信息表
    /// </summary>
    [ApiController]
    [ControllSort("1-10")]
    public class SysuserController : ControllerBaseApi
    {

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name="model">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public List<SysuserOutput> GetListByPage(ActionPara model)
        {
            var optmdl = Request.GetToken();
            List<SysuserOutput> list = new List<SysuserOutput>();
            //去掉部门限制
            if (model.sconlist.Count > 0)
            {
                model.sconlist.RemoveAll(t => t.ParamName.ToLower() == "");
                model.sconlist.RemoveAll(t => t.ParamName.ToLower() == "deptid");
            }
            int totalNumber = 0;
            var rolelist = GetRoleList();
            if (rolelist.IsZxxAny())
            {
                SelectCondition rolecon = new SelectCondition
                {
                    ParamName = "RoleId",
                    ParamType = "in",
                    ParamValue = rolelist.Select(t => t.RoleId).ListIntZdToString(),
                };
                model.sconlist.Add(rolecon);
            }
            var userlist = SysUserDAO.Instance.GetListByPage(model, ref totalNumber);
            if (userlist.IsZxxAny())
            {
                foreach (var item in userlist)
                {
                    var role = rolelist.Find(t => t.RoleId == item.RoleId);
                    var user = new SysuserOutput();
                    item.CopyTypeValue(user);
                    user.RoleName = role == null ? "未知" : role.RoleName;
                    list.Add(user);
                }
            }
            TotalCount = totalNumber;

            return list;
        }

        /// <summary>
        /// 用户新增
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string InsertUser(SysUserEntity info)
        {
            Message = "用户表信息保存失败。";
            Status = false;
            var optmdl = Request.GetToken();
            var temp = SysUserDAO.Instance.GetOneBy(s => s.UserUid == info.UserUid);
            if (temp != null)
            {
                //用户已存在
                Message = $"账号{temp.UserUid}已存在,不能新增。";
                return Message;
            }
            //超管可为任意租户建号;非超管一律落到自己的租户,防跨租户建号
            info.TenantId = optmdl.IsSystem ? info.TenantId : optmdl.TenantId;

            //角色必须属于目标租户,或为平台共享角色(TenantId=0);防把A租户的用户配成B租户的角色
            var role = SysRoleDAO.Instance.GetOneBy(t => t.RoleId == info.RoleId);
            if (role == null || (role.TenantId != 0 && role.TenantId != info.TenantId))
            {
                Message = "角色不存在或不属于该租户。";
                return Message;
            }

            info.PasswordSalt = Guid.NewGuid().ToString("N").ToUpper();
            string password = EncryptsHelper.MD5Make32(info.Password).ToUpper();
            info.Password = HashCryto.GetHash2String(string.Concat(password.ToUpper(), info.PasswordSalt), HashAlgorithmType.SHA256);

            info.CreateId = optmdl.UserID;
            info.CreateTime = DateTime.Now.ToDateTimeString();
            info.CreateName = optmdl.UserName;
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = SysUserDAO.Instance.Insert(info);
            if (Status) Message = "用户信息新增成功。";

            return Message;
        }

        /// <summary>
        /// 用户修改
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string UpdateUser(SysUserEntity info)
        {
            Message = "用户信息更新失败。";
            Status = false;
            var optmdl = Request.GetToken();
            if (optmdl == null)
            {
                Message = "Token令牌传递错误。";
                return Message;
            }

            //查得到即有权管:SysUser 走租户 QueryFilter,非超管只能看到自己租户子树内的用户
            var target = SysUserDAO.Instance.GetOneBy(t => t.UserId == info.UserId);
            if (target == null)
            {
                Message = "账号不存在。";
                return Message;
            }
            //不得改自己的角色,防自我提权
            if (target.UserId == optmdl.UserID && info.RoleId != target.RoleId)
            {
                Message = "不能修改自己的角色。";
                return Message;
            }
            //角色必须属于该用户所在租户,或为平台共享角色(TenantId=0)
            var role = SysRoleDAO.Instance.GetOneBy(t => t.RoleId == info.RoleId);
            if (role == null || (role.TenantId != 0 && role.TenantId != target.TenantId))
            {
                Message = "角色不存在或不属于该租户。";
                return Message;
            }

            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = SysUserDAO.Instance.UpdateColumns(info, it => new
            {
                it.RoleId,
                it.TrueName,
                it.UserXb,
                it.UserPhone,
                it.UpdateId,
                it.UpdateTime,
                it.UpdateName,
                it.UserRemark,
            });
            if (Status) Message = "用户信息更新成功。";

            return Message;
        }

        /// <summary>
        /// 根据用户ID删除用户信息
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string DeleteUser(int userid)
        {
            Message = "用户信息删除失败。";
            Status = false;
            var optmdl = Request.GetToken();

            var user = SysUserDAO.Instance.GetOneBy(s => s.UserId == userid);
            if (user == null) return "账号不存在。";
            if (user.UserId == optmdl.UserID)
            {
                Message = "不能删除当前登录的账号。";
                return Message;
            }

            Status = SysUserDAO.Instance.DeleteBy(t => t.UserId == userid);
            if (Status)
            {
                Message = "用户信息删除成功。";
            }

            return Message;
        }

        /// <summary>
        /// 根据用户ID修改用户启用状态
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string EnableUser(int userid)
        {
            Message = "用户启用状态变更失败。";
            Status = false;
            var optmdl = Request.GetToken();
            if (optmdl == null)
            {
                Message = "Token令牌传递错误。";
                return Message;
            }
            var info = SysUserDAO.Instance.GetOneBy(s => s.UserId == userid);
            if (info == null)
            {
                Message = "账号不存在。";
                return Message;
            }
            if (info.UserId == optmdl.UserID)
            {
                Message = "不能变更当前登录账号的启用状态。";
                return Message;
            }
            int oldenable = info.IsEnable;
            if (oldenable == 1)
            {
                info.IsEnable = 0;
            }
            else if (oldenable == 0)
            {
                info.IsEnable = 1;
            }
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = SysUserDAO.Instance.UpdateColumns(info, it => new
            {
                it.IsEnable,
                it.UpdateId,
                it.UpdateTime,
                it.UpdateName,
            });
            if (Status) Message = "用户启用状态变更成功。";

            return Message;
        }

        /// <summary>
        /// 管理员根据用户ID重置密码
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string PostResetPwd(int userid)
        {
            Message = "重置密码失败。";
            Status = false;
            var optmdl = Request.GetToken();
            if (optmdl == null)
            {
                Message = "Token令牌传递错误。";
                return Message;
            }
            var info = SysUserDAO.Instance.GetOneBy(s => s.UserId == userid);
            if (info == null)
            {
                Message = $"账号{info.UserUid}不存在。";
                return Message;
            }
            string password = EncryptsHelper.MD5Make32("new12356").ToUpper();
            info.Password = HashCryto.GetHash2String(string.Concat(password.ToUpper(), info.PasswordSalt), HashAlgorithmType.SHA256);
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = SysUserDAO.Instance.UpdateColumns(info, it => new
            {
                it.Password,
                it.UpdateId,
                it.UpdateTime,
                it.UpdateName,
            });
            if (Status) Message = "重置密码成功。";
            return Message;
        }

        /// <summary>
        /// 用户本人修改密码
        /// </summary>
        /// <param name="model">修改密码模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string PostEditPwd(ChangePasswordDto model)
        {
            Message = "用户修改密码失败。";
            Status = false;
            if (model.OldPwd.IsZxxNullOrEmpty())
            {
                Message = "旧密码不能为空。";
                return Message;
            }
            if (model.NewPwd.IsZxxNullOrEmpty())
            {
                Message = "新密码不能为空。";
                return Message;
            }
            var optmdl = Request.GetToken();
            if (optmdl == null)
            {
                Message = "Token令牌传递错误。";
                return Message;
            }
            var info = SysUserDAO.Instance.GetOneBy(s => s.UserId == optmdl.UserID);
            if (info == null)
            {
                Message = $"账号{info.UserUid}不存在。";
                return Message;
            }
            var PrivateKey = AppSetting.GetConfig("DefaultValues:PrivateKey");
            var _net_privatekey = RsaUtil.LoadPrivateKey(PrivateKey);
            var _oldpwd = RSAHelper.RSADecrypt(_net_privatekey, model.OldPwd);
            string oldpassword = HashCryto.GetHash2String(string.Concat(_oldpwd.ToUpper(), info.PasswordSalt), HashAlgorithmType.SHA256);
            if (oldpassword != info.Password)
            {
                Message = $"用户旧密码输入错误。";
                return Message;
            }
            var password = RSAHelper.RSADecrypt(_net_privatekey, model.NewPwd);
            info.Password = HashCryto.GetHash2String(string.Concat(password.ToUpper(), info.PasswordSalt), HashAlgorithmType.SHA256);
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = SysUserDAO.Instance.UpdateColumns(info, it => new
            {
                it.Password,
                it.UpdateId,
                it.UpdateTime,
                it.UpdateName,
            });
            if (Status) Message = "用户修改密码成功。";
            return Message;
        }

        /// <summary>
        /// 用户默认租户配置
        /// </summary>
        /// <param name="userinfo">模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string PostSetUnit(UserInfo userinfo)
        {
            Message = "用户默认租户设置失败。";
            Status = false;
            var optmdl = Request.GetToken();
            var info = SysUserDAO.Instance.GetOneBy(s => s.UserId == userinfo.UserId);
            if (info == null)
            {
                Message = $"账号{info.UserUid}不存在。";
                return Message;
            }
            //var role = SysRoleDAO.Instance.GetOneBy(t => t.RoleId == info.RoleId);
            //if (role != null && role.TreeLevel > 3)
            //{
            //    Message = $"账号{info.TrueName}不需要设置默认租户。";
            //    return Message;
            //}
            info.TenantId = userinfo.TenantId;
            info.UpdateId = optmdl.UserID;
            info.UpdateTime = DateTime.Now.ToDateTimeString();
            info.UpdateName = optmdl.UserName;
            Status = SysUserDAO.Instance.UpdateColumns(info, it => new
            {
                it.TenantId,
                it.UpdateId,
                it.UpdateTime,
                it.UpdateName,
            });
            if (Status) Message = "用户默认租户设置成功。";

            return Message;
        }

        /// <summary>
        /// 根据用户旧Token获取新Token
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string ChangeUserToken()
        {
            Message = "用户Token传递失败。";
            Status = false;

            var optmdl = Request.GetToken();
            OperatorModelLogin model = new OperatorModelLogin
            {
                UserID = optmdl.UserID,
                UserName = optmdl.UserName,
                LoginTime = DateTime.Now,
                SourceType = optmdl.SourceType,
                TenantId = optmdl.TenantId,
                TenantName = optmdl.TenantName,
                IsSystem = optmdl.IsSystem,
            };
            var res = EncryptsHelper.Encrypt(model.ToJson());
            Status = true;
            Message = "用户Token获取成功。";

            return res;
        }

        /// <summary>
        /// 根据用户权限获取角色列表(树结构)
        /// </summary>
        /// <param name="tenantId">目标租户ID。传 -1(默认)不按租户过滤;传 >=0 时只返回该租户自建角色与平台共享角色(TenantId=0)</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public List<SysRole> GetRoleList(int tenantId = -1)
        {
            List<SysRole> list = new List<SysRole>();
            Message = "角色列表获取失败。";
            Status = false;
            var optmdl = Request.GetToken();
            var rolelist = SysRoleDAO.Instance.GetList();
            if (rolelist.IsZxxAny())
            {
                var roleinlist = rolelist.FindAll(t => t.TreeLevel > optmdl._Sysrole.TreeLevel
                                            && t.FullCode.Contains($"|{optmdl._Sysrole.RoleId}|"));
                //为指定租户挑角色时,只能选该租户自建角色或平台共享角色
                if (tenantId >= 0) roleinlist = roleinlist.FindAll(t => t.TenantId == tenantId || t.TenantId == 0);
                if (roleinlist.IsZxxAny()) list.AddRange(roleinlist);
            }
            if (list.Count > 0)
            {
                Status = true;
                Message = "角色列表获取成功。";
            }
            TotalCount = list.Count;

            return list;
        }

    }
}