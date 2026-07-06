using CenBoCommon.Zxx;
using Dm.util;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi
{
    /// <summary> 
    /// 告警类型管理
    /// </summary>
    [ApiController]
    [ControllSort("5-10")]
    public class AlarmConfigController : ControllerBaseApi
    {
        /// <summary> 
        /// 批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string SaveBatch(List<AlarmConfig> list)
        {
            Message = "报警相关配置信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<AlarmConfig> insertlist = new List<AlarmConfig>();
                List<AlarmConfig> updatelist = new List<AlarmConfig>();
                DateTime time = DateTime.Now;
                foreach (var item in list)
                {
                    item.UpdateId = optmdl.UserID;
                    item.UpdateTime = time.ToDateTimeString();
                    item.UpdateName = optmdl.UserName;
                    if (item.Id == 0)
                    {
                        item.CreateId = optmdl.UserID;
                        item.CreateTime = time.ToDateTimeString();
                        item.CreateName = optmdl.UserName;
                        insertlist.Add(item);
                    }
                    else
                    {
                        updatelist.Add(item);
                    }
                }
                Status = AlarmConfigDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) AlarmConfigDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) AlarmConfigDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    Message = "报警相关配置信息保存成功。";
                }
            }

            return Message;
        }

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="_Id">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string DeleteByPk(int _Id)
        {
            Message = "报警相关配置删除失败。";
            Status = AlarmConfigDAO.Instance.DeleteBy(t => t.Id == _Id);
            if (Status) Message = "报警相关配置删除成功。";
            return Message;
        }

        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name="_Id">主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public AlarmConfig GetInfoByPk(int _Id)
        {
            var entity = AlarmConfigDAO.Instance.GetOneBy(t => t.Id == _Id);
            return entity;
        }

        /// <summary>
        /// 获取告警等级列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public List<string> GetDisAlarmGrade()
        {
            List<string> strings = new List<string>();
            var list = AlarmConfigDAO.Instance.GetList();
            if (list.IsZxxAny())
            {
                strings.AddRange(list.Select(t => t.AlarmGrade).Distinct());
            }
            return strings;
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
        public List<AlarmConfig> GetListByPage(ActionPara model)
        {
            var list = AlarmConfigDAO.Instance.GetListByPage(model, ref TotalCount);
            return list;
        }

    }
}