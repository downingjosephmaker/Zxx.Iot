using IotModel;
using Microsoft.AspNetCore.Mvc;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// mqtt配置
    /// </summary>
    [ApiController]
    [ControllSort("5-1")]
    public class AdminLogoparamController : ControllerBaseApi
    {

        /// <summary>
        /// 查询Logo信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [ApiGroup(ApiGroupNames.Basic)]
        public AdminLogoparam GetLogo()
        {
            //LogoInfo info = new LogoInfo();
            var LogoParam = AdminLogoparamDAO.Instance.GetOneBy(t => t.Id > 0);
            return LogoParam;
        }

        /// <summary>
        /// 组织新增
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string Save(AdminLogoparam info)
        {
            Status = false;
            Message = "系统Logo保存失败。";
            var optmdl = Request.GetToken();
            info.UnitId = optmdl.UnitId;
            if (info.Id == 0)
                Status = AdminLogoparamDAO.Instance.Insert(info);
            else Status = AdminLogoparamDAO.Instance.Update(info);
            if (Status) Message = "系统Logo保存成功。";
            return Message;
        }

    }
}