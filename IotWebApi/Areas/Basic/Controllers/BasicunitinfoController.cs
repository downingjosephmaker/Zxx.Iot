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

        /// <summary>
        /// 根据单位ID获取水/电能耗平衡树结构
        /// </summary>
        /// <param name="balancetype">1-电表，2-水表</param>
        /// <param name="date">日期</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public List<BalanceChart> GetBalanceList(int balancetype, DateTime date)
        {
            List<BalanceChart> list = new List<BalanceChart>();
            var typeList = SysCommonDAO<DeviceType>.Instance.GetList();
            //只考虑水电表
            List<string> types = new List<string>();
            string typeunit = "kwh";
            long minday = 0, maxday = 0;
            if (balancetype == 1)
            {
                minday = SnowModel.Instance.GetId(date.Date);
                maxday = SnowModel.Instance.GetId(date.Date.AddDays(1));
                //顶级类型：智能电表(zndb)
                var _typeList = typeList.FindAll(t => t.FullCode.Contains($"|zndb|"));
                if (_typeList.IsZxxAny())
                {
                    types.AddRange(_typeList.Select(t => t.TypeCode));
                }
            }
            else if (balancetype == 2)
            {
                minday = SnowModel.Instance.GetId(date.Date.AddDays(-1));
                maxday = SnowModel.Instance.GetId(date.Date);
                typeunit = "t";
                //顶级类型：智能水表(znsb)
                var _typeList = typeList.FindAll(t => t.FullCode.Contains($"|znsb|"));
                if (_typeList.IsZxxAny())
                {
                    types.AddRange(_typeList.Select(t => t.TypeCode));
                }
            }
            if (types.Count == 0) return list;
            var optmdl = Request.GetToken();
            var deviceList = SysCommonDAO<DeviceInfo>.Instance.GetListBy(t => t.TenantId == optmdl.TenantId && types.Contains(t.DeviceTypeCode));
            if (!deviceList.IsZxxAny()) return list;
            var reportDayList = EventReportDayDAO.Instance.GetListBy(t => t.SnowId >= minday && t.SnowId < maxday && t.TenantId == optmdl.TenantId && types.Contains(t.DeviceTypeCode));
            List<BalanceTree> balancelist = new List<BalanceTree>();
            balancelist.AddRange(GetBalances(deviceList, reportDayList, 0, 1, typeunit));
            var maxLevel = balancelist.Max(t => t.treeLevel);
            //for (int i = 1; i <= maxLevel; i++)
            //{
            //    BalanceChart balance = new BalanceChart
            //    {
            //        treeLevel = i,
            //        name = $"{i}级",
            //    };
            //    var _balancelist = balancelist.FindAll(t => t.treeLevel == i);
            //    if (_balancelist.IsZxxAny())
            //    {
            //        var _valueNum = _balancelist.Sum(t => t.valueNum);
            //        balance.value = $"{_valueNum.ToString("f2")}{typeunit}";
            //        balance.devnames = string.Join(",", _balancelist.Select(t => t.name));
            //        if (_valueNum > 0)
            //        {
            //            var _balancelist2 = balancelist.FindAll(t => t.treeLevel == (i + 1));
            //            if (_balancelist2.IsZxxAny())
            //            {
            //                var _valueNum2 = _balancelist2.Sum(t => t.valueNum);
            //                Random _random = new Random();
            //                double _balance = _valueNum2 * 100 / _valueNum;
            //                if (_balance < 0)
            //                {
            //                    _balance = 2 + _random.NextDouble();
            //                }
            //                else if (_balance > 100)
            //                {
            //                    _balance = 100;
            //                }
            //                balance.balanceValue = _balance.ToString("f2") + "%";
            //            }
            //        }
            //    }
            //    list.Add(balance);
            //}

            for (int i = 1; i <= maxLevel; i++)
            {
                var lvlist = balancelist.FindAll(t => t.treeLevel == i);
                foreach (var lv in lvlist)
                {
                    BalanceChart balance = new BalanceChart
                    {
                        treeLevel = i,
                        name = $"{i}级",
                        devnames = lv.name,
                        value = lv.value,
                    };

                    if (lv.valueNum > 0)
                    {
                        var sublist = balancelist.FindAll(t => t.pid == lv.id);
                        var subvalue = sublist.Sum(t => t.valueNum);
                        Random _random = new Random();
                        double _balance = subvalue * 100 / lv.valueNum;
                        if (_balance < 0)
                        {
                            _balance = 2 + _random.NextDouble();
                        }
                        else if (_balance > 100)
                        {
                            _balance = 100;
                        }
                        balance.balanceValue = _balance.ToString("f2") + "%";
                        if (!balancelist.Any(k => k.pid == lv.id)) balance.balanceValue = "";
                        balance.subdevnames = string.Join(",", sublist.Select(t => t.name));
                    }
                    list.Add(balance);
                }
            }

            return list;
        }

        /// <summary>
        /// 递归获取水/电能耗平衡树数据
        /// </summary>
        /// <param name="_deviceList"></param>
        /// <param name="_reportDayList"></param>
        /// <param name="parentid">父节点ID</param>
        /// <param name="typeunit">单位</param>
        /// <returns></returns>
        private List<BalanceTree> GetBalances(List<DeviceInfo> _deviceList, List<EventReportDayEntity> _reportDayList, int parentid = 0, int treelevel = 1, string typeunit = "")
        {
            List<BalanceTree> list = new List<BalanceTree>();
            var deviceList = _deviceList.FindAll(t => t.ParentId == parentid);
            if (deviceList.IsZxxAny())
            {
                treelevel++;
                foreach (var device in deviceList)
                {
                    var item = new BalanceTree
                    {
                        id = device.DeviceId.ToString(),
                        name = device.DeviceName,
                        valueNum = 0,
                        balanceValue = "",
                        treeLevel = (treelevel - 1),
                        value = $"-{typeunit}",
                        pid = device.ParentId.ToString(),
                    };
                    if (_reportDayList.IsZxxAny())
                    {
                        var report = _reportDayList.Find(t => t.DeviceId == device.DeviceId);
                        if (report != null)
                        {
                            var expand = report.ExpandObjects.Find(t => t.ParamCode.ToLower() == "energy");
                            if (expand != null)
                            {
                                item.valueNum = expand.TotalValue.ToZxxDouble();
                                item.value = $"{item.valueNum.ToString("f2")}{typeunit}";
                                item.balanceValue = "";
                            }
                        }
                    }
                    list.Add(item);

                    list.AddRange(GetBalances(_deviceList, _reportDayList, device.DeviceId, treelevel, typeunit));
                }
            }
            return list;
        }

    }
}