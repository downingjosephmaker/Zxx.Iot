using CenBoCommon.Zxx;
using IotModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IotWebApi
{
    /// <summary>
    /// 令牌管理
    /// </summary>
    [ApiController]
    [ControllSort("0-2")]
    public class SysusertokenController : ControllerBaseApi
    {
        /// <summary>
        /// 刷新令牌(前端无感刷新:软过期后凭旧令牌换新签;
        /// 旧令牌须在tokentimeouthour硬窗口内且用户仍启用,换签重置LoginTime实现滑动会话)
        /// </summary>
        /// <param name="para">刷新参数</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        public RefreshTokenInfo GetRefreshToken(RefreshTokenPara para)
        {
            RefreshTokenInfo model = null;
            Status = false;
            Message = "令牌刷新失败。";

            if (para == null || para.RefreshToken.IsZxxNullOrEmpty())
            {
                Message = "请提供刷新令牌。";
                return model;
            }
            try
            {
                string key = EncryptsHelper.Decrypt(para.RefreshToken);
                var oldmodel = JsonConvert.DeserializeObject<OperatorModelLogin>(key);
                if (oldmodel == null || oldmodel.UserID <= 0)
                {
                    Message = "刷新令牌无效，请重新登录。";
                    return model;
                }

                string timeouthour = AppSetting.GetConfig("DefaultValues:tokentimeouthour");
                if (!string.IsNullOrWhiteSpace(timeouthour)
                    && oldmodel.LoginTime.AddHours(timeouthour.ToZxxInt()) < DateTime.Now)
                {
                    Message = "刷新令牌已超时，请重新登录。";
                    return model;
                }

                var user = SysUserDAO.Instance.GetOneBy(t => t.UserId == oldmodel.UserID && t.IsEnable == 1);
                if (user == null)
                {
                    Message = "用户不存在或已停用，请重新登录。";
                    return model;
                }

                DateTime time = DateTime.Now;
                oldmodel.LoginTime = time;
                oldmodel.TokenExpireTime = time.AddHours(LoginPublicMode.GetTokenRefreshHours());
                oldmodel.LoginToken = "";
                string newtoken = EncryptsHelper.Encrypt(oldmodel.ToJson());

                model = new RefreshTokenInfo
                {
                    AccessToken = newtoken,
                    RefreshToken = newtoken,
                    Expires = oldmodel.TokenExpireTime.ToDateTimeString(),
                };
                Status = true;
                Message = "令牌刷新成功。";
            }
            catch (Exception)
            {
                Message = "刷新令牌解析失败，请重新登录。";
            }

            return model;
        }
    }

    /// <summary>
    /// 刷新令牌参数
    /// </summary>
    public class RefreshTokenPara
    {
        /// <summary>
        /// 刷新令牌(即登录返回的LoginToken)
        /// </summary>
        public string RefreshToken { get; set; }
    }

    /// <summary>
    /// 刷新令牌返回
    /// </summary>
    public class RefreshTokenInfo
    {
        /// <summary>
        /// 访问令牌
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// 刷新令牌(与访问令牌同体,硬窗口由服务端裁决)
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// 软过期时间(到点前端再次换签)
        /// </summary>
        public string Expires { get; set; }
    }
}
