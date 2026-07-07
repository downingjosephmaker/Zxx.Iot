using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;

namespace IotWebApi
{
    /// <summary>
    /// 产品命令白名单管理(§5:声明式命令,前端指令下发表单按ParamSchema动态渲染)
    /// </summary>
    [ApiController]
    [ControllSort("5-18")]
    public class ProductCommandController : ControllerBaseApi
    {
        /// <summary>
        /// 批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string SaveBatch(List<ProductCommand> list)
        {
            Message = "产品命令保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<ProductCommand> insertlist = new List<ProductCommand>();
                List<ProductCommand> updatelist = new List<ProductCommand>();
                DateTime time = DateTime.Now;
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
                Status = ProductCommandDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) ProductCommandDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) ProductCommandDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    Message = "产品命令保存成功。";
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
        [ApiGroup(ApiGroupNames.Basic)]
        public string DeleteByPk(long _SnowId)
        {
            Message = "产品命令删除失败。";
            Status = ProductCommandDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                Message = "产品命令删除成功。";
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
        public ProductCommand GetInfoByPk(long _SnowId)
        {
            var entity = ProductCommandDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        public List<ProductCommand> GetListByPage(ActionPara model)
        {
            var list = ProductCommandDAO.Instance.GetListByPage(model, ref TotalCount);
            return list;
        }

        /// <summary>
        /// 按产品类型编码查询启用命令清单(设备详情指令下发表单数据源)
        /// </summary>
        /// <param name="typecode">产品类型编码</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public List<ProductCommand> GetListByTypeCode(string typecode)
        {
            var list = ProductCommandDAO.Instance.GetListBy(t => t.DeviceTypeCode == typecode && t.IsEnable);
            return list ?? new List<ProductCommand>();
        }
    }
}
