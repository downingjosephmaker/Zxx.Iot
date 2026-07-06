using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary>
    /// 控制日志（关系型分表版本，按周分表）
    /// </summary>
    [ApiController]
    [ControllSort("25-11")]
    public class EventControlDbController : ControllerBaseApi
    {

        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public EventControl GetInfoByPk(long _SnowId)
        {
            var entity = SysCommonDAO<EventControl>.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        [ApiGroup(ApiGroupNames.Event)]
        public List<EventControl> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            model.sconlist.Add(new SelectCondition
            {
                ParamName = "LinkType",
                ParamType = "=",
                ParamValue = "API"
            });
            var list = SysCommonDAO<EventControl>.Instance.GetListByPage(model, ref totalNumber);
            if (list.Count > 0)
            {
                list.ForEach(t =>
                {
                    t.BuildName = t.BuildName.BeautifyFullName();
                    t.DeptName = t.DeptName.BeautifyFullName();
                    t.DeviceName = t.DeviceName.BeautifyFullName();
                });
            }
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 批量保存
        /// </summary>
        /// <param name="list">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Event)]
        public string SaveBatch(List<EventControl> list)
        {
            Status = false;
            Message = "设备控制日志保存失败。";

            var unitlist = BasicunitInfoDAO.Instance.GetList();
            var buildlist = BuildInfoDAO.Instance.GetList();
            var deptlist = DeptInfoDAO.Instance.GetList();
            var typelist = DeviceTypeDAO.Instance.GetList();

            foreach (var zklog in list)
            {
                var unit = unitlist.FirstOrDefault(t => t.UnitId == zklog.UnitId);
                if (unit != null) zklog.UnitName = unit.UnitName;
                var dept = deptlist.FirstOrDefault(t => t.DeptId == zklog.DeptId);
                if (dept != null) zklog.DeptName = dept.FullName.BeautifyFullName();
                var build = buildlist.FirstOrDefault(t => t.BuildId == zklog.BuildId);
                if (build != null) zklog.BuildName = build.FullName.BeautifyFullName();
                var devtype = typelist.FirstOrDefault(t => t.TypeCode == zklog.DeviceTypeCode);
                if (devtype != null) zklog.DeviceTypeName = devtype.TypeName;
            }

            Status = SysCommonDAO<EventControl>.Instance.SaveBatch(list);
            if (Status)
            {
                Message = "设备控制日志保存成功。";
            }

            return Message;
        }

    }
}
