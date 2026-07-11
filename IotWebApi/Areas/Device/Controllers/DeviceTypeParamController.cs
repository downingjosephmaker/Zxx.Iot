using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using IotWebApi.Areas.Device.Models;
using IotWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using Newtonsoft.Json;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 设备类型参数
    /// </summary>
    [ApiController]
    [ControllSort("7-2")]
    public class DeviceTypeParamController : ControllerBaseApi
    {
        private readonly ConfigReloadNotifier _configReload;

        /// <summary>
        /// 构造函数-获取依赖注入
        /// </summary>
        /// <param name="configReload">配置热刷新通知器(点表变更后去抖广播插件重建采集拓扑,C-4)</param>
        public DeviceTypeParamController(ConfigReloadNotifier configReload)
        {
            _configReload = configReload;
        }

        /// <summary>
        /// 设备类型参数批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string SaveBatch(List<DeviceTypeParamEntity> list)
        {
            Message = "设备类型参数信息保存失败。";
            if (list.IsZxxAny())
            {
                var optmdl = Request.GetToken();
                List<DeviceTypeParamEntity> insertlist = new List<DeviceTypeParamEntity>();
                List<DeviceTypeParamEntity> updatelist = new List<DeviceTypeParamEntity>();
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
                Status = DeviceTypeParamDAO.Instance.TranAction(() =>
                {
                    if (insertlist.Count > 0) DeviceTypeParamDAO.Instance.InsertRange(insertlist);
                    if (updatelist.Count > 0) DeviceTypeParamDAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    {
                        it.CreateId,
                        it.CreateTime,
                        it.CreateName
                    });
                });
                if (Status)
                {
                    Message = "设备类型参数信息保存成功。";
                    _configReload.Notify("点表保存");
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
        [ApiGroup(ApiGroupNames.Device)]
        public string DeleteByPk(long _SnowId)
        {
            Message = "设备类型参数删除失败。";
            Status = DeviceTypeParamDAO.Instance.DeleteBy(t => t.SnowId == _SnowId);
            if (Status)
            {
                Message = "设备类型删除成功。";
                _configReload.Notify("点表删除");
            }
            return Message;
        }

        /// <summary>
        /// 根据设备类型编码集合批量删除数据
        /// </summary>
        /// <param name="typecode">设备类型编码</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string DeleteByTypeCode(string typecode)
        {
            Status = false;
            Message = "设备类型参数删除失败。";
            Status = DeviceTypeParamDAO.Instance.DeleteBy(t => t.DeviceTypeCode == typecode);
            if (Status)
            {
                Message = "设备类型参数删除成功。";
                _configReload.Notify("点表按类型删除");
            }
            return Message;
        }

        /// <summary>
        /// 根据主键集合批量删除数据
        /// </summary>
        /// <param name="ids">主键集合</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string DeleteByIds(string ids)
        {
            Status = false;
            Message = "设备类型参数批量删除失败。";
            Status = DeviceTypeParamDAO.Instance.DeleteBy(t => ids.Contains(t.SnowId.ToString()));
            if (Status)
            {
                Message = "设备类型参数批量删除成功。";
                _configReload.Notify("点表批量删除");
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
        [ApiGroup(ApiGroupNames.Device)]
        public DeviceTypeParamEntity GetInfoByPk(long _SnowId)
        {
            var entity = DeviceTypeParamDAO.Instance.GetOneBy(t => t.SnowId == _SnowId);
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
        [ApiGroup(ApiGroupNames.Device)]
        public List<DeviceTypeParamEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = DeviceTypeParamDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

        /// <summary>
        /// 根据设备类型查询参数类别下拉框(运行数据)
        /// </summary>
        /// <param name="typecode">设备类型</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<ParamTypeInfo> GetParamTypeSelect(string typecode)
        {
            var paramlist = DeviceTypeParamDAO.Instance.GetListBy(t => t.DeviceTypeCode == typecode && t.IsShow);
            var list = GetTypeParamList(paramlist);
            TotalCount = list.IsZxxAny() ? list.Count : 0;
            return list;
        }

        /// <summary>
        /// 参数请求
        /// </summary>
        /// <param name="paramlist"></param>
        /// <param name="isParamType"></param>
        /// <returns></returns>
        private List<ParamTypeInfo> GetTypeParamList(List<DeviceTypeParamEntity> paramlist, bool isParamType = false)
        {
            List<ParamTypeInfo> list = new List<ParamTypeInfo>();
            if (paramlist.IsZxxAny())
            {
                var typelist = paramlist.Select(t => t.ParamTypeName).Distinct();
                foreach (string typename in typelist)
                {
                    if (string.IsNullOrEmpty(typename)) continue;
                    ParamTypeInfo paramtype = new ParamTypeInfo()
                    {
                        ParamTypeName = typename,
                    };
                    var _paramlist = paramlist.FindAll(t => t.ParamTypeName == typename);
                    if (_paramlist.IsZxxAny())
                    {
                        foreach (var item in _paramlist)
                        {
                            if (!paramtype.ParamIds.Any(t => t.ParamCode == item.ParamCode))
                            {
                                ParamInfoCode param = new ParamInfoCode()
                                {
                                    DeviceTypeCode = item.DeviceTypeCode,
                                    ParamCode = item.ParamCode,
                                    ParamName = item.ParamName,
                                    ParamUnit = item.ValueUnit
                                };
                                if (isParamType) param.ParamName = item.ParamTypeName;
                                paramtype.ParamIds.Add(param);
                            }
                        }
                        list.Add(paramtype);
                    }
                }
            }
            List<ParamTypeInfo> orderlist = new List<ParamTypeInfo>();
            //电流放第一位
            var dianliu = list.Find(t => t.ParamTypeName == "电流");
            if (dianliu != null)
            {
                orderlist.Add(dianliu);
                list.RemoveAll(t => t.ParamTypeName == "电流");
            }
            orderlist.AddRange(list);
            return orderlist;
        }

        /// <summary>
        /// 根据设备类型查询参数类别下拉框(极值数据)
        /// </summary>
        /// <param name="typecode">设备类型</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<ParamTypeInfo> GetParamPeakSelect(string typecode)
        {
            var paramlist = DeviceTypeParamDAO.Instance.GetListBy(t => t.DeviceTypeCode == typecode && t.IsPeak);
            var list = GetTypeParamList(paramlist, true);
            TotalCount = list.IsZxxAny() ? list.Count : 0;
            return list;
        }

        /// <summary>
        /// 根据设备类型查询参数类别下拉框(统计数据-设备)
        /// </summary>
        /// <param name="typecode">设备类型</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public List<ParamTypeInfo> GetParamReportSelect(string typecode)
        {
            var paramlist = DeviceTypeParamDAO.Instance.GetListBy(t => t.DeviceTypeCode == typecode && t.IsReport);
            var list = GetTypeParamList(paramlist, true);
            TotalCount = list.IsZxxAny() ? list.Count : 0;
            return list;
        }

        /// <summary>
        /// 根据中台json文件和设备类型编码导入参数信息
        /// </summary>
        /// <param name="file">json文件</param>
        /// <param name="typecode">设备类型编码</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public MetaData ParamAddByType(IFormFile file, string typecode)
        {
            MetaData data = new()
            {
                Status = false,
                Message = "导入参数失败"
            };

            try
            {
                if (file == null || file.Length == 0)
                {
                    data.Message = "上传文件不能为空";
                    return data;
                }

                // 验证文件类型
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".json")
                {
                    data.Message = "只支持.json格式的文件";
                    return data;
                }

                // 验证设备类型
                if (string.IsNullOrWhiteSpace(typecode))
                {
                    data.Message = "设备类型编码不能为空";
                    return data;
                }

                var deviceType = DeviceTypeDAO.Instance.GetOneBy(t => t.TypeCode == typecode);
                if (deviceType == null)
                {
                    data.Message = $"设备类型[{typecode}]不存在";
                    return data;
                }

                // 1. 读取并反序列化JSON文件为ZtTypeJson
                string jsonContent;
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    jsonContent = reader.ReadToEnd();
                }

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    data.Message = "JSON文件内容为空";
                    return data;
                }

                var ztTypeJson = jsonContent.ToObject<ZtTypeJson>();
                if (ztTypeJson == null)
                {
                    data.Message = "JSON文件格式不正确";
                    return data;
                }

                // 2. 反序列化metadata为PointEntity列表
                if (string.IsNullOrWhiteSpace(ztTypeJson.metadata))
                {
                    data.Message = "metadata字段为空，无参数信息";
                    return data;
                }

                var pointList = ztTypeJson.metadata.ToObject<List<PointEntity>>();
                if (pointList == null || !pointList.Any())
                {
                    data.Message = "metadata中没有参数信息";
                    return data;
                }

                var optmdl = Request.GetToken();
                DateTime now = DateTime.Now;

                // 3. 生成DeviceTypeParam列表
                List<DeviceTypeParam> paramList = new List<DeviceTypeParam>();

                foreach (var point in pointList)
                {
                    // 基础参数映射
                    var param = new DeviceTypeParam
                    {
                        SnowId = SnowModel.Instance.NewId(),
                        DeviceTypeCode = typecode,
                        SubChannel = "总路", // 默认总路
                        ParamCode = point.code,
                        ParamName = point.name,
                        ParamTypeName = "", // 可根据features或其他字段设置
                        ParamAddr = 0, // 从configuration.parameter.address获取
                        ParamFormula = $"{point.code}*1", // 默认公式
                        ValueType = "数值", // 默认数值类型
                        StatusValues = null,
                        ValueUnit = "",
                        ParamMaxValue = 0,
                        ParamMinValue = 0,
                        ParamChangeValue = 0,
                        IsShow = true,
                        IsSet = false,
                        IsPeak = false,
                        IsReport = false,
                        IsMapDefault = false,
                        DecimalDigit = 2,
                        CreateId = 1,//optmdl.UserID,
                        CreateTime = now.ToDateTimeString(),
                        CreateName = "开发管理员",//optmdl.UserName,
                        UpdateId = 1,//optmdl.UserID,
                        UpdateTime = now.ToDateTimeString(),
                        UpdateName = "开发管理员"//optmdl.UserName
                    };
                    if (point.configuration.codec != null && point.configuration.codec.provider == "bit")
                    {
                        param.ValueType = "状态";
                        param.StatusValues = ParseStatusValuesFromDescription(point.description);
                    }
                    // 从configuration中提取参数地址
                    if (point.configuration != null &&
                        point.configuration.parameter != null)
                    {
                        param.ParamAddr = point.configuration.parameter.address;
                    }

                    // 根据features设置参数类型和功能
                    if (point.features != null && point.features.Length > 0)
                    {
                        foreach (var feature in point.features)
                        {
                            if (feature.Contains("report", StringComparison.OrdinalIgnoreCase))
                            {
                                param.IsReport = true;
                            }
                            else if (feature.Contains("peak", StringComparison.OrdinalIgnoreCase))
                            {
                                param.IsPeak = true;
                            }
                            else if (feature.Contains("set", StringComparison.OrdinalIgnoreCase))
                            {
                                param.IsSet = true;
                            }
                        }
                    }

                    // 根据accessModes判断是否可配置
                    if (point.accessModes != null && point.accessModes.Any())
                    {
                        param.IsSet = point.accessModes.Any(a =>
                            a.value != null && a.value.Contains("write", StringComparison.OrdinalIgnoreCase));
                    }

                    paramList.Add(param);
                }

                // 4. 保存到数据库
                if (paramList.Any())
                {
                    // 检查是否已存在该类型的参数，如果存在则提示
                    var existingParams = DeviceTypeParamDAO.Instance.GetListBy(t => t.DeviceTypeCode == typecode);
                    if (existingParams != null && existingParams.Any())
                    {
                        data.Message = $"设备类型[{deviceType.TypeName}]已存在参数配置，是否需要先删除？";
                        return data;
                    }

                    // 批量插入
                    bool success = DeviceTypeParamDAO.Instance.TranAction(() =>
                    {
                        SysCommonDAO<DeviceTypeParam>.Instance.InsertRange(paramList);
                    });

                    if (success)
                    {
                        data.Status = true;
                        data.Message = $"成功导入 {paramList.Count} 个参数配置";
                        _configReload.Notify("点表模板导入");
                    }
                    else
                    {
                        data.Message = "保存参数配置失败";
                    }
                }
                else
                {
                    data.Message = "没有可导入的参数信息";
                }
            }
            catch (Exception ex)
            {
                data.Message = $"导入失败: {ex.Message}";
                LogHelper.ErrorLogWrite("DeviceParamController", "ParamAddByType", ex.ToString(), "参数导入");
            }

            return data;
        }

        /// <summary> 
        /// 批量获取某类型的所有设备，并为它们统一补充/设置新增的参数
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string applyNewParamsToDevicesByType(List<DeviceTypeParamEntity> list, string typecode)
        {
            Message = "按设备类型批量增加设备参数信息保存失败。";
            if (list.IsZxxAny() && !typecode.IsNullOrEmpty())
            {
                var optmdl = Request.GetToken();
                List<Expand_DeviceParam> paramlist = new List<Expand_DeviceParam>();
                foreach (var item in list)
                {
                    Expand_DeviceParam param = new Expand_DeviceParam();
                    item.CopyTypeValue(param);
                    param.StatusValues = item.ExpandStatusValues;
                    paramlist.Add(param);
                }
                var deviceParamlist = DeviceParamDAO.Instance.GetListBy(t => t.DeviceTypeCode == typecode);
                List<DeviceParamEntity> updatelist = new List<DeviceParamEntity>();
                if (deviceParamlist.IsZxxAny())
                {
                    foreach (var deviceParam in deviceParamlist)
                    {
                        var _deviceParam = new DeviceParamEntity();
                        deviceParam.CopyTypeValue(_deviceParam);
                        deviceParam.ExpandObjects.AddRange(paramlist);
                        _deviceParam.ExpandJson = deviceParam.ExpandObjects.ToJson();
                        _deviceParam.DeviceTypeCode = typecode;
                        updatelist.Add(_deviceParam);
                        //SysCommonDAO<DeviceParam>.Instance.Update(_deviceParam);
                    }
                }
                Status = DeviceParamDAO.Instance.TranAction(() =>
                {
                    if (updatelist.Count > 0) DeviceParamDAO.Instance.UpdateColumns(updatelist, it => new
                    {
                        it.ExpandJson
                    });
                });
                if (Status)
                {
                    Message = "按设备类型批量增加设备参数信息保存成功。";
                    _configReload.Notify("点表下放设备参数");
                }
            }
            return Message;
        }


        /// <summary>
        /// 从description中提取状态值
        /// </summary>
        private string ParseStatusValuesFromDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return GenerateDefaultStatusValues();
            }

            // 尝试从description中提取状态映射
            // 常见的描述格式如："0:关,1:开" 或 "0=关;1=开" 或 "关(0),开(1)"
            var statusValues = new List<Expand_ParamStatusValue>();

            try
            {
                // 方法1：尝试解析 "0:关,1:开" 格式
                if (description.Contains(":") && description.Contains(","))
                {
                    var pairs = description.Split(',');
                    foreach (var pair in pairs)
                    {
                        if (pair.Contains(':'))
                        {
                            var parts = pair.Split(':');
                            if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int key))
                            {
                                statusValues.Add(new Expand_ParamStatusValue
                                {
                                    StatusKey = key,
                                    StatusValue = parts[1].Trim()
                                });
                            }
                        }
                    }
                }
                // 方法2：尝试解析 "0=关;1=开" 格式
                else if (description.Contains("=") && description.Contains(";"))
                {
                    var pairs = description.Split(';');
                    foreach (var pair in pairs)
                    {
                        if (pair.Contains('='))
                        {
                            var parts = pair.Split('=');
                            if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int key))
                            {
                                statusValues.Add(new Expand_ParamStatusValue
                                {
                                    StatusKey = key,
                                    StatusValue = parts[1].Trim()
                                });
                            }
                        }
                    }
                }
                // 方法3：尝试解析 "关(0),开(1)" 格式
                else if (description.Contains("(") && description.Contains(")"))
                {
                    // 使用正则表达式提取
                    var matches = System.Text.RegularExpressions.Regex.Matches(description, @"([^\(\)]+)\((\d+)\)");
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        if (match.Groups.Count == 3 && int.TryParse(match.Groups[2].Value, out int key))
                        {
                            statusValues.Add(new Expand_ParamStatusValue
                            {
                                StatusKey = key,
                                StatusValue = match.Groups[1].Value.Trim()
                            });
                        }
                    }
                }

                // 如果成功解析到了状态值
                if (statusValues.Any())
                {
                    // 按StatusKey排序
                    statusValues = statusValues.OrderBy(x => x.StatusKey).ToList();
                    return JsonConvert.SerializeObject(statusValues);
                }
            }
            catch
            {
                // 解析失败，返回默认值
            }

            // 如果无法从description解析，返回默认状态值
            return GenerateDefaultStatusValues();
        }

        /// <summary>
        /// 生成默认的状态值
        /// </summary>
        private string GenerateDefaultStatusValues()
        {
            var defaultStatus = new List<Expand_ParamStatusValue>
        {
            new Expand_ParamStatusValue { StatusKey = 0, StatusValue = "关" },
            new Expand_ParamStatusValue { StatusKey = 1, StatusValue = "开" }
        };

            return JsonConvert.SerializeObject(defaultStatus);
        }

        /// <summary>
        /// 根据point信息获取参数类型名称
        /// </summary>
        private string GetParamTypeName(PointEntity point)
        {
            // 根据code或name判断参数类型
            if (point.code != null)
            {
                var code = point.code.ToLower();
                if (code.Contains("temp") || code.Contains("温度"))
                    return "温度";
                if (code.Contains("humidity") || code.Contains("湿度"))
                    return "湿度";
                if (code.Contains("pressure") || code.Contains("压力"))
                    return "压力";
                if (code.Contains("flow") || code.Contains("流量"))
                    return "流量";
                if (code.Contains("status") || code.Contains("状态"))
                    return "状态";
            }

            return "通用";
        }

        /// <summary>
        /// 设置参数的附加属性
        /// </summary>
        private void SetParamAdditionalProperties(DeviceTypeParam param, PointEntity point)
        {
            // 根据不同的参数类型设置不同的属性
            var code = param.ParamCode?.ToLower() ?? "";
            var unit = param.ValueUnit?.ToLower() ?? "";

            // 设置默认的最大最小值
            if (unit.Contains("℃") || unit.Contains("度"))
            {
                param.ParamMaxValue = 50;
                param.ParamMinValue = -20;
            }
            else if (unit.Contains("%") && (code.Contains("humidity") || code.Contains("湿度")))
            {
                param.ParamMaxValue = 100;
                param.ParamMinValue = 0;
            }
            else if (unit.Contains("ppm") && code.Contains("co2"))
            {
                param.ParamMaxValue = 5000;
                param.ParamMinValue = 0;
            }

            // 设置精度
            if (unit.Contains("℃") || unit.Contains("度"))
            {
                param.DecimalDigit = 1;
            }
        }
    }
}