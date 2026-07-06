using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using IotWebApi.Areas.Scada.Models;

namespace IotWebApi.Areas.Scada.Controllers
{
    /// <summary>
    /// 大屏项目数据控制器
    /// </summary>
    [ApiController]
    [ControllSort("26-3")]
    public class DashProjectDataController : ControllerBaseApi
    {
        /// <summary>
        /// 大屏图表接口示例
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Scada)]
        public ChartData DemoChart()
        {
            Status = false;
            Message = "查询失败。";
            ChartData result = new ChartData();

            var optmdl = Request.GetToken();
            /*
             * {"dimensions":["product","data1","data2"],"source":[{"product":"Mon","data1":120,"data2":130},{"product":"Tue","data1":200,"data2":130},{"product":"Wed","data1":150,"data2":312},{"product":"Thu","data1":80,"data2":268},{"product":"Fri","data1":70,"data2":155},{"product":"Sat","data1":110,"data2":117},{"product":"Sun","data1":130,"data2":160}]}
             */
            //var devlist = dev
            var fieldArr = new[] { "product", "data1", "data2" };
            result.dimensions.AddRange(fieldArr);
            var rand = new Random();
            for (int i = 0; i < 7; i++)
            {
                var week = i + 1;
                if (week == 7) week = 0;
                var xv = ((DayOfWeek)week).ToString().Substring(0, 3);
                var yv1 = rand.Next(1000, 10000);
                var yv2 = rand.Next(1000, 10000);
                var jo = new JObject();
                jo[fieldArr[0]] = xv;
                for (int j = 1; j < fieldArr.Length; j++)
                {
                    jo[fieldArr[j]] = rand.Next(1000, 10000);
                }
            }

            Status = true;
            Message = "查询成功";
            return result;
        }

    }
}