using Microsoft.AspNetCore.Mvc;
using CenBoCommon.Zxx;
using IotModel;

namespace IotWebApi
{
    /// <summary> 
    /// 软件升级
    /// </summary>
    [ApiController]
    [ControllSort("5-2")]
    public class AppversionController : ControllerBaseApi
    {
        /// <summary>
        /// 获取APK最新版本信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [ApiGroup(ApiGroupNames.Basic)]
        public AndroidAppversion GetNewInfo()
        {
            var entity = AndroidAppversionDAO.Instance.GetOneBy(t => t.AppStatus == 1);
            return entity;
        }

    }
}