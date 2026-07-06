using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 工作日信息表
    /// </summary>
    [ApiController]
    [ControllSort("5-20")]
    public class WorkdayInfoController : ControllerBaseApi
    {

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string DeleteByPk(long _SnowId)
        {
            Status = false;
            Message = "工作日信息表删除失败。";
            Status = WorkdayInfoDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                Message = "工作日信息表信息删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public WorkdayInfo GetInfoByPk(long _SnowId)
        {
            var entity = WorkdayInfoDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
            return entity;
        }

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name="model">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public List<WorkdayInfo> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = WorkdayInfoDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 根据年份获取工作日列表
        /// </summary>
        /// <param name="year">年份</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string InitWorkday(int year)
        {
            Status = false;
            Message = "工作日信息刷新失败。";

            // 1. 获取API数据
            var apiUrl = $"https://timor.tech/api/holiday/year/{year}";
            var json = HttpHelper.ManGetAsync(apiUrl).Result;
            dynamic result = JsonConvert.DeserializeObject(json);
            if (result == null || result.code != 0)
            {
                Message = $"API返回异常";
                return Message;
            }
            var holidayDict = result.holiday;

            // 2. 构建全年日期字典
            var workdayList = new List<WorkdayInfo>();
            var start = new DateTime(year, 1, 1);
            var end = new DateTime(year, 12, 31);
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                string key = date.ToString("MM-dd");
                dynamic apiDay = holidayDict[key];

                bool isHoliday = false;
                bool isWorkday = false;
                string name = "";
                if (apiDay != null)
                {
                    isHoliday = apiDay.holiday == true;
                    name = apiDay.name;
                    // 补班日（holiday=false，name含"补班"）视为上班日
                    isWorkday = !isHoliday || (name != null && name.ToString().Contains("补班"));
                }
                else
                {
                    // API未覆盖的日期，按自然周规则
                    isHoliday = (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);
                    isWorkday = !isHoliday;
                }

                var entity = new WorkdayInfo
                {
                    SnowId = SnowModel.Instance.GetId(date),
                    WorkYear = year,
                    Date = date,
                    IsWorkday = isWorkday,
                    IsHoliday = isHoliday,
                    IsWeekday = (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                };
                workdayList.Add(entity);
            }

            // 3. 删除当年已存在数据，批量插入新数据
            Status = WorkdayInfoDAO.Instance.TranAction(() =>
            {
                WorkdayInfoDAO.Instance.DeleteBy(t => t.WorkYear == year);
                WorkdayInfoDAO.Instance.InsertRange(workdayList);
            });
            if (Status) Message = "工作日信息刷新成功。";

            return Message;
        }

    }
}