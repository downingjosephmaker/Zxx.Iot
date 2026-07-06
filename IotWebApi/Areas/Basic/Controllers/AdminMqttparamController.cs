using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Areas.Basic.Models;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// mqtt配置
    /// </summary>
    [ApiController]
    [ControllSort("5-1")]
    public class AdminMqttparamController : ControllerBaseApi
    {

        /// <summary>
        /// 查询Mqtt信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [ApiGroup(ApiGroupNames.Basic)]
        public MqttInfo GetMqtt()
        {
            MqttInfo info = new MqttInfo();
            var MqttParam = AdminMqttparamDAO.Instance.GetOneBy(t => t.Id > 0);
            if (MqttParam != null)
            {
                info.MqttUrl = $"ws://{MqttParam.MqttServer}:{MqttParam.MqttWebClientPort}/ws";
                info.MqttUser = MqttParam.MqttUser;
                info.MqttPass = MqttParam.MqttPass;
                info.WebReal = MqttParam.MqttSubTopicWebReal;
                info.WebAlarm = MqttParam.MqttSubTopicWebAlarm;
            }
            return info;
        }


    }
}