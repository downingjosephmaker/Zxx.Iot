using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi
{
    /// <summary> 
    /// 用户权限
    /// </summary>
    [ApiController]
    [ControllSort("1-11")]
    public class SysrelatedController : ControllerBaseApi
    {
        /// <summary>
        /// 用户权限批量保存
        /// </summary>
        /// <param name="list">用户权限</param>
        /// <param name="qxtype">1:建筑 2:部门</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string SaveBatch(List<SysRelated> list, int qxtype)
        {
            Message = "用户权限信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                DateTime time = DateTime.Now;
                List<SysRelated> insertlist = new List<SysRelated>();
                List<SysRelated> updatelist = new List<SysRelated>();
                foreach (var item in list)
                {
                    item.UpdateId = optmdl.UserID;
                    item.UpdateTime = time.ToDateTimeString();
                    item.UpdateName = optmdl.UserName;
                    if (item.SnowId == 0)
                    {
                        item.SnowId = SnowModel.Instance.NewId();
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
                // 楼栋/部门层级已剥离,原按 qxtype 区分保存 BuildIds/DeptCodes 的分支合并为单一保存(qxtype 参数保留仅为前端兼容)
                Status = SysRelatedDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) SysRelatedDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) SysRelatedDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    Message = "用户权限信息保存成功。";
                }
            }
            return Message;
        }

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="_SnowId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string DeleteByPk(long _SnowId)
        {
            Message = "用户权限删除失败。";
            Status = SysRelatedDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                Message = "用户权限删除成功。";
            }
            return Message;
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
        public List<SysRelated> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = SysRelatedDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber.ToZxxInt();
            return list;
        }

    }
}
