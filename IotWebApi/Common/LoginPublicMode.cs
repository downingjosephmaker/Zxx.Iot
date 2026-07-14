using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using IotWebApi.Services.Jobs;

namespace IotWebApi
{
    /// <summary>
    /// 公共登录方法
    /// </summary>
    public static class LoginPublicMode
    {
        /// <summary>
        /// 令牌软过期小时数(DefaultValues:tokenrefreshhour,未配置回落24;
        /// 前端到点走GetRefreshToken换签,服务端硬窗口仍由tokentimeouthour裁决)
        /// </summary>
        public static int GetTokenRefreshHours()
        {
            int hours = AppSetting.GetConfig("DefaultValues:tokenrefreshhour").ToZxxInt();
            return hours > 0 ? hours : 24;
        }

        /// <summary>
        /// 通用登录方法
        /// </summary>
        /// <param name="user"></param>
        /// <param name="sourceType"></param>
        /// <param name="apiip"></param>
        /// <returns></returns>
        public static OperatorModelLogin GetLogin(SysUserEntity user, string sourceType, string apiip)
        {
            OperatorModelLogin model = null;
            try
            {
                DateTime time = DateTime.Now;
                model = new()
                {
                    UserID = user.UserId,
                    UserName = user.TrueName,
                    LoginTime = time,
                    TokenExpireTime = time.AddHours(GetTokenRefreshHours()),
                    SourceType = sourceType,
                    TenantId = user.TenantId,
                };

                var RoleAllList = SysRoleDAO.Instance.GetList();
                if (RoleAllList.IsZxxAny())
                {
                    var role = SysRoleDAO.Instance.GetOneBy(t => t.RoleId == user.RoleId);
                    // 超管须为"平台级根角色"(tenant_id==0 且 parent_id==0);租户自建角色即便顶级也不得提权,防越权看全部租户
                    if (role != null && role.ParentId == 0 && role.TenantId == 0)
                    {
                        model.IsSystem = true;
                    }
                }

                // 租户名从租户表关联查，须在 Token 加密前就位
                if (user.TenantId > 0)
                {
                    var tenantentity = TenantInfoDAO.Instance.GetOneBy(t => t.TenantId == user.TenantId);
                    if (tenantentity != null)
                    {
                        model.TenantName = tenantentity.TenantName;
                    }
                }

                model.LoginToken = EncryptsHelper.Encrypt(model.ToJson());

                user.LoginCount++;
                user.OnlineState = 1;
                user.LastLoginTime = time.ToDateTimeString();

                if (!OperatorCommon.UserList.Any(t => t.Token == model.LoginToken))
                {
                    ZxxUserInfo userInfo = new ZxxUserInfo
                    {
                        UserId = model.UserID,
                        Token = model.LoginToken,
                        UserName = model.UserName,
                        ClientIp = apiip,
                        OperatorModel = model,
                    };
                    OperatorCommon.UserList.Add(userInfo);
                }

                SysuserLog syslog = new SysuserLog
                {
                    SnowId = SnowModel.Instance.NewId(),
                    LogType = "登录",
                    SourceType = sourceType,
                    CreateUserId = user.UserId,
                    CreateUserName = user.TrueName,
                    CreateTime = time.ToDateTimeString(),
                    CreateIp = apiip,
                };

                var isok = SysUserDAO.Instance.UpdateColumns(user, it => new
                {
                    it.OnlineState,
                    it.LastLoginTime,
                    it.LoginCount,
                });
                if (isok)
                {
                    Task.Run(() =>
                    {
                        var cuser = SysUserDAO.Instance.GetOneBy(t => t.UserId == user.UserId);
                        if (cuser != null)
                        {
                            cuser.OnlineState = user.OnlineState;
                            cuser.LastLoginTime = user.LastLoginTime;
                            cuser.LoginCount = user.LoginCount;
                        }
                        SysuserLogDAO.Instance.Insert(syslog);
                    });
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.Message, "登录接口错误");
            }
            return model;
        }

        /// <summary>
        /// 登出接口
        /// </summary>
        /// <param name="user"></param>
        /// <param name="sourceType"></param>
        /// <param name="apiip"></param>
        /// <returns></returns>
        public static bool LoginOut(SysUserEntity user, string sourceType, string apiip)
        {
            try
            {
                DateTime time = DateTime.Now;
                user.OnlineState = 0;
                user.LastOutTime = time.ToDateTimeString();
                SysuserLog syslog = new SysuserLog
                {
                    SnowId = SnowModel.Instance.NewId(),
                    LogType = "登出",
                    SourceType = sourceType,
                    CreateUserId = user.UserId,
                    CreateUserName = user.TrueName,
                    CreateTime = time.ToDateTimeString(),
                    CreateIp = apiip,
                };
                var isok = SysUserDAO.Instance.TranAction(() =>
                {
                    SysUserDAO.Instance.UpdateColumns(user, it => new
                    {
                        it.OnlineState,
                        it.LastOutTime,
                    });
                    SysuserLogDAO.Instance.Insert(syslog);
                });
                return isok;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.Message, "登出接口错误");
            }
            return true;
        }

    }
}
