using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using IotModel;
using IotWebApi.Areas.Basic.Models;
using IotWebApi.Services.Jobs;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 单位基本信息
    /// </summary>
    [ApiController]
    [ControllSort("5-11")]
    public class BasicunitinfoController : ControllerBaseApi
    {
        /// <summary> 
        /// 单位基本信息批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string SaveBatch(List<BasicunitInfoEntity> list)
        {
            Message = "单位基本信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<BasicunitInfoEntity> insertlist = new List<BasicunitInfoEntity>();
                List<BasicunitInfoEntity> updatelist = new List<BasicunitInfoEntity>();
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
                    if (!BasicunitInfoDAO.Instance.Insert(item)) { Status = false; break; }
                }
                if (Status)
                {
                    foreach (var item in updatelist)
                    {
                        if (!BasicunitInfoDAO.Instance.Update(item)) { Status = false; break; }
                    }
                }
                if (Status)
                {
                    if (insertlist.Count > 0)
                    {
                        Task.Run(() =>
                        {
                            var unitlist = BasicunitInfoDAO.Instance.GetList();
                            if (unitlist.IsZxxAny())
                            {
                                var comfortAlllist = DeviceComfortDAO.Instance.GetList();
                                List<DeviceComfort> comfortlist = new List<DeviceComfort>();
                                foreach (var item in unitlist)
                                {
                                    bool isnew = true;
                                    if (!comfortAlllist.IsZxxAny())
                                    {
                                        if (comfortAlllist.Any(t => t.TenantId == item.TenantId)) isnew = false;
                                    }
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
                    Message = "单位基本信息保存成功。";
                }
            }
            return Message;
        }

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="_UnitId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string DeleteByPk(int _UnitId)
        {
            Message = "单位基本信息删除失败。";
            // 级联删除：按祖先链 full_code 含 |UnitId| 一次删整棵子树（照搬旧 BuildInfo 范式）
            var self = BasicunitInfoDAO.Instance.GetOneBy(t => t.TenantId == _UnitId);
            int parentId = self?.ParentId ?? 0;
            Status = BasicunitInfoDAO.Instance.DeleteBy(t => t.FullCode.Contains($"|{_UnitId}|"));
            if (Status)
            {
                // 若父级已无其它子节点，回填 has_child=false
                if (parentId > 0)
                {
                    var parent = BasicunitInfoDAO.Instance.GetOneBy(t => t.TenantId == parentId);
                    if (parent != null)
                    {
                        bool stillHasChild = BasicunitInfoDAO.Instance.GetListBy(t => t.ParentId == parentId).IsZxxAny();
                        if (parent.HasChild != stillHasChild)
                        {
                            parent.HasChild = stillHasChild;
                            BasicunitInfoDAO.Instance.UpdateColumns(parent, it => new { it.HasChild });
                        }
                    }
                }
                Message = "单位基本信息删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name="_UnitId">主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public BasicunitInfoEntity GetInfoByPk(int _UnitId)
        {
            var entity = BasicunitInfoDAO.Instance.GetOneBy(t => t.TenantId == _UnitId);
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
        public List<BasicunitInfoEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = BasicunitInfoDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 获取权限单位列表
        /// </summary>
        /// <param name="model">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public List<BasicunitInfoEntity> GetQxListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var optmdl = Request.GetToken();
            var redlist = SysRelatedDAO.Instance.GetListBy(t => t.UserId == optmdl.UserID);
            if (redlist.IsZxxAny())
            {
                string unitids = string.Join(",", redlist.Select(t => t.TenantId));
                model.sconlist.Add(new SelectCondition
                {
                    ParamName = "TenantId",
                    ParamType = "in",
                    ParamValue = unitids,
                });
            }
            else
            {
                if (!optmdl.IsSystem)
                {
                    model.sconlist.Add(new SelectCondition
                    {
                        ParamName = "TenantId",
                        ParamType = "=",
                        ParamValue = optmdl.TenantId.ToString(),
                    });
                }
            }
            var list = BasicunitInfoDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}
