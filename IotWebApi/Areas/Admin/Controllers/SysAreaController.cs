using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using IotModel;
using IotWebApi.Areas.Admin.Models;
using IotWebApi.Helper;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 区域管理控制器
    /// </summary>
    [ApiController]
    [ControllSort("1-15")]
    public class SysAreaController : ControllerBaseApi
    {
        /// <summary>
        /// 区域新增
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string Insert(SysAreaEntity info)
        {
            Status = false;
            Message = "区域信息新增失败。";
            Status = SysAreaDAO.Instance.Insert(info);
            if (Status) Message = "区域信息新增成功。";
            return Message;
        }

        /// <summary>
        /// 区域修改
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string Update(SysAreaEntity info)
        {
            Status = false;
            Message = "区域信息修改失败。";
            var optmdl = Request.GetToken();
            var temp = SysAreaDAO.Instance.GetOneBy(t => t.AreaId == info.AreaId);
            if (temp == null)
            {
                Message = $"区域[{info.AreaName}]不存在。";
                return Message;
            }
            Status = SysAreaDAO.Instance.Update(info);
            if (Status) Message = "区域信息更新成功。";
            return Message;
        }

        /// <summary>
        /// 根据区域ID删除区域信息(包含子区域)
        /// </summary>
        /// <param name="id">区域ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string Delete(int id)
        {
            Status = false;
            Message = "区域信息删除失败。";
            Status = SysAreaDAO.Instance.DeleteBy(t => t.FullCode.Contains($"|{id}|"));
            if (Status) Message = "区域信息删除成功。";
            return "";
        }

        /// <summary>
        /// 根据区域ID查询区域信息
        /// </summary>
        /// <param name="id">区域</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public SysAreaEntity GetInfoByPk(string id)
        {
            var entity = SysAreaDAO.Instance.GetOneBy(t => t.AreaId == id);
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
        [ApiGroup(ApiGroupNames.Admin)]
        public List<SysAreaEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = SysAreaDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 根据当前单位计算当日日出日落、天亮天黑时间
        /// </summary>
        /// <param name="date">日期（可选，默认今天）</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public SunTimesResult GetSunTimes(DateTime? date = null)
        {
            var optmdl = Request.GetToken();
            var unit = BasicunitInfoDAO.Instance.GetOneBy(t => t.UnitId == optmdl.UnitId);
            if (unit == null || string.IsNullOrEmpty(unit.AreaId))
            {
                Status = false;
                Message = $"当前单位[{optmdl.UnitName}]未关联区域信息。";
                return null;
            }
            SysAreaEntity area = null;
            if (unit.AreaId.Contains("|"))
            {
                area = SysAreaDAO.Instance.GetOneBy(t => t.FullCode == unit.AreaId);
            }
            else area = SysAreaDAO.Instance.GetOneBy(t => t.AreaId == unit.AreaId);
            if (area == null)
            {
                Status = false;
                Message = $"区域[{unit.AreaId}]不存在。";
                return null;
            }

            var expand = area.ExpandObject;
            if (expand == null || (expand.Latitude == 0 && expand.Longitude == 0))
            {
                Status = false;
                Message = $"区域[{area.AreaName}]未设置经纬度信息。";
                return null;
            }

            DateTime targetDate = date ?? DateTime.Today;
            var (sunrise, sunset, dawn, dusk) = SunTimesCalculator.Calculate((double)expand.Latitude, (double)expand.Longitude, targetDate);

            return new SunTimesResult
            {
                AreaId = area.AreaId,
                AreaName = area.AreaName,
                Latitude = expand.Latitude,
                Longitude = expand.Longitude,
                Date = targetDate.ToString("yyyy-MM-dd"),
                Sunrise = sunrise?.ToString("HH:mm:ss"),
                Sunset = sunset?.ToString("HH:mm:ss"),
                Dawn = dawn?.ToString("HH:mm:ss"),
                Dusk = dusk?.ToString("HH:mm:ss")
            };
        }

        /// <summary>
        /// 根据国家行政区划刷新区域数据
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string InitArea()
        {
            // 行政区划初始化功能依赖的 DivisionProvince/City/Area 实体及 Administrative/division.txt
            // 数据源已移除，此接口暂不可用。需要时补回 SqlSugar 划区实体与数据文件后恢复实现。
            Status = false;
            Message = "区域信息刷新功能未启用（缺少行政区划数据源）。";
            return Message;
        }
    }
}
