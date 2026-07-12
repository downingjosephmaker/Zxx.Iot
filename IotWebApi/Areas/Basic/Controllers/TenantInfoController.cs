using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Services.Jobs;

namespace IotWebApi.Controllers
{
    /// <summary>
    /// 租户信息
    /// </summary>
    [ApiController]
    [ControllSort("5-11")]
    public class TenantInfoController : ControllerBaseApi
    {
        /// <summary>
        /// 租户信息批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string SaveBatch(List<TenantInfo> list)
        {
            Message = "租户信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<TenantInfo> insertlist = new List<TenantInfo>();
                List<TenantInfo> updatelist = new List<TenantInfo>();
                DateTime time = DateTime.Now;
                foreach (var item in list)
                {
                    item.UpdateId = optmdl.UserID;
                    item.UpdateTime = time.ToDateTimeString();
                    item.UpdateName = optmdl.UserName;
                    if (item.TenantId == 0)
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
                // 逐条调用 DAO override（内含树形 full_code/tree_level 维护 + 自带事务），
                // 不再外套 TranAction 以免嵌套事务；逐条保证后插子节点能读到先插父节点。
                Status = true;
                foreach (var item in insertlist)
                {
                    if (!TenantInfoDAO.Instance.Insert(item)) { Status = false; break; }
                }
                if (Status)
                {
                    foreach (var item in updatelist)
                    {
                        if (!TenantInfoDAO.Instance.Update(item)) { Status = false; break; }
                    }
                }
                if (Status)
                {
                    if (insertlist.Count > 0)
                    {
                        Task.Run(() =>
                        {
                            var tenantlist = TenantInfoDAO.Instance.GetList();
                            if (tenantlist.IsZxxAny())
                            {
                                var comfortAlllist = DeviceComfortDAO.Instance.GetList();
                                List<DeviceComfort> comfortlist = new List<DeviceComfort>();
                                foreach (var item in tenantlist)
                                {
                                    // 已有舒适度配置的租户跳过（旧代码此处逻辑反转，会重复播种）
                                    bool isnew = !comfortAlllist.IsZxxAny()
                                        || !comfortAlllist.Any(t => t.TenantId == item.TenantId);
                                    if (isnew)
                                    {
                                        DeviceComfort xiaji = new DeviceComfort
                                        {
                                            SnowId = SnowModel.Instance.NewId(),
                                            ComfortName = "春夏秋季",
                                            EnvirHumidity = 50,
                                            ComfortFormula = "(1.818*T+18.18)*(0.88+0.002*H)+(T-32)/(45-T)+8.6",
                                            MonthFormula = "M > 3 AND M < 11",
                                            CreateId = optmdl.UserID,
                                            CreateTime = time.ToDateTimeString(),
                                            CreateName = optmdl.UserName,
                                            UpdateId = optmdl.UserID,
                                            UpdateTime = time.ToDateTimeString(),
                                            UpdateName = optmdl.UserName,
                                            TenantId = item.TenantId
                                        };
                                        comfortlist.Add(xiaji);
                                        DeviceComfort dongxiaji = new DeviceComfort
                                        {
                                            SnowId = SnowModel.Instance.NewId(),
                                            ComfortName = "冬春季",
                                            EnvirHumidity = 40,
                                            ComfortFormula = "(1.818*T+18.18)*(0.88+0.002*H)+(T-32)/(45-T)+18.2",
                                            MonthFormula = "(M >= 1 AND M <= 3) OR (M >= 11 AND M <= 12)",
                                            CreateId = optmdl.UserID,
                                            CreateTime = time.ToDateTimeString(),
                                            CreateName = optmdl.UserName,
                                            UpdateId = optmdl.UserID,
                                            UpdateTime = time.ToDateTimeString(),
                                            UpdateName = optmdl.UserName,
                                            TenantId = item.TenantId
                                        };
                                        comfortlist.Add(dongxiaji);
                                    }
                                }
                                if (comfortlist.Count > 0) DeviceComfortDAO.Instance.InsertRange(comfortlist);
                            }
                        });
                    }
                    Message = "租户信息保存成功。";
                }
            }
            return Message;
        }

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="_TenantId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string DeleteByPk(int _TenantId)
        {
            Message = "租户信息删除失败。";
            // 级联删除：按祖先链 full_code 含 |TenantId| 一次删整棵子树
            var self = TenantInfoDAO.Instance.GetOneBy(t => t.TenantId == _TenantId);
            int parentId = self?.ParentId ?? 0;
            Status = TenantInfoDAO.Instance.DeleteBy(t => t.FullCode.Contains($"|{_TenantId}|"));
            if (Status)
            {
                // 若父级已无其它子节点，回填 has_child=false
                if (parentId > 0)
                {
                    var parent = TenantInfoDAO.Instance.GetOneBy(t => t.TenantId == parentId);
                    if (parent != null)
                    {
                        bool stillHasChild = TenantInfoDAO.Instance.GetListBy(t => t.ParentId == parentId).IsZxxAny();
                        if (parent.HasChild != stillHasChild)
                        {
                            parent.HasChild = stillHasChild;
                            TenantInfoDAO.Instance.UpdateColumns(parent, it => new { it.HasChild });
                        }
                    }
                }
                Message = "租户信息删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name="_TenantId">主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public TenantInfo GetInfoByPk(int _TenantId)
        {
            var entity = TenantInfoDAO.Instance.GetOneBy(t => t.TenantId == _TenantId);
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
        public List<TenantInfo> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = TenantInfoDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}
