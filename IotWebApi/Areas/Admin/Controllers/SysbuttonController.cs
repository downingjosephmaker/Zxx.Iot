using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 平台按钮管理
    /// </summary>
    [ApiController]
    [ControllSort("1-19")]
    public class SysbuttonController : ControllerBaseApi
    {
        /// <summary> 
        /// 批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string SaveBatch(List<SysButton> list)
        {
            Message = "按钮信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<SysButton> insertlist = new List<SysButton>();
                List<SysButton> updatelist = new List<SysButton>();
                DateTime time = DateTime.Now;
                foreach (var item in list)
                {
                    item.UpdateId = optmdl.UserID;
                    item.UpdateTime = time.ToDateTimeString();
                    item.UpdateName = optmdl.UserName;
                    if (item.ButtonId == 0)
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
                Status = SysButtonDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) SysButtonDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) SysButtonDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    Message = "按钮信息保存成功。";
                }
            }
            return Message;
        }

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="_ButtonId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string DeleteByPk(int _ButtonId)
        {
            Message = "平台按钮管理删除失败。";
            Status = SysButtonDAO.Instance.DeleteBy(t => t.ButtonId == _ButtonId);
            if (Status)
            {
                Message = "设备表删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name="_ButtonId">主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public SysButton GetInfoByPk(int _ButtonId)
        {
            var entity = SysButtonDAO.Instance.GetOneBy(t => t.ButtonId == _ButtonId);
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
        public List<SysButton> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = SysButtonDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}