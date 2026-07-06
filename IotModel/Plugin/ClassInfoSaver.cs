using CenBoCommon.Zxx;
using IotLog;
using NetTaste;
using NewLife;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IotModel
{
    /// <summary>
    /// 保存类信息
    /// </summary>
    public static class ClassInfoSaver
    {
        public static int CheckValueFlag = 1;
        public static List<Expand_DeviceParam> SaveDeviceParam<T>(this T t, List<Expand_DeviceParam> paramList)
        {
            string timestr = DateTime.Now.ToDateTimeString();
            var proper = typeof(T).GetProperties();
            List<Expand_DeviceParam> newlist = new();
            foreach (var field in proper)
            {
                var par = paramList.FirstOrDefault(p => p.ParamCode.ToLower() == field.Name.ToLower());
                if (par != null)
                {
                    par.CollectTime = timestr;
                    par.ParamLastValue = par.ParamValue;
                    par.IsAlarm = 0;
                    var fvalue = field.GetValue(t);
                    if (par.StatusValues.IsZxxAny())
                    {
                        var kvlist = par.StatusValues;
                        var pvalue = field.GetValue(t)?.ToString();
                        //告警类参数：code 含 fault/alm/alarm 字样
                        var isAlarmCode = par.ParamCode.ToLower().Contains("fault") || par.ParamCode.ToLower().Contains("alm") || par.ParamCode.ToLower().Contains("alarm");
                        if (par.ValueType == "状态")
                        {
                            var kv = kvlist.Find(t => t.StatusKey.ToString() == pvalue);
                            if (kv != null)
                            {
                                par.ParamValue = kv.StatusValue;
                            }
                        }
                        else if (par.ValueType == "数位")
                        {
                            // 直接使用本次采集的原始值
                            int valueI = pvalue.ToInt();

                            var sb = new StringBuilder();
                            for (int i = 0; i < 16; i++)
                            {
                                var kv = kvlist.Find(t => t.StatusKey == i);
                                if (((valueI >> i) & 1) == 1 && kv != null)
                                {
                                    // 简单的去重（如果同一个状态值重复出现）
                                    string val = kv.StatusValue;
                                    if (!sb.ToString().Contains(val))
                                    {
                                        sb.Append(val).Append('|');
                                    }
                                }
                            }

                            // 赋值给 ParamValue，去除末尾的 '|'；告警类空时为"正常"，非告警类空时为""
                            if (isAlarmCode)
                            {
                                par.ParamValue = sb.Length > 0 ? sb.ToString().TrimEnd('|') : "正常";
                            }
                            else
                            {
                                par.ParamValue = sb.Length > 0 ? sb.ToString().TrimEnd('|') : "";
                            }
                        }
                        //告警类参数统一判定：value 不为"正常"即告警
                        if (isAlarmCode)
                        {
                            par.IsAlarm = par.ParamValue.IsNullOrEmpty() || !par.ParamValue.Contains("正常") ? 1 : 0;
                        }
                    }
                    else if (par.ValueType == "数值")
                    {
                        if (!par.ParamFormula.IsZxxNullOrEmpty())
                        {
                            if (fvalue != null)
                            {
                                var tempvalue = ExpressoFormula.CalculateString(par.ParamFormula, par.ParamCode, fvalue.ToZxxDouble(), par.DecimalDigit);
                                if (CheckValue(par.ParamLastValue, tempvalue, par.ParamChangeValue))
                                {
                                    par.ParamValue = tempvalue;
                                    if (par.ParamMinValue == 0 && par.ParamMaxValue == 0)
                                    {
                                        par.IsAlarm = 0;
                                    }
                                    else if (par.ParamValue.ToDecimal() < par.ParamMinValue || par.ParamValue.ToDecimal() > par.ParamMaxValue)
                                    {
                                        par.IsAlarm = 1;
                                    }
                                }
                                else
                                {
                                    LogHelper.SysLogWrite("ClassInfoSaver", "Save", $"跳变超限2：原始值:{fvalue}-计算值:{tempvalue}-历史值:{par.ParamLastValue}-跳变阈值:{par.ParamChangeValue}", "数据告警");
                                }
                            }
                        }
                        else
                        {
                            var tempvalue = field.GetValue(t).ToZxxDouble().ToString($"F{par.DecimalDigit}");
                            if (CheckValue(par.ParamLastValue, tempvalue, par.ParamChangeValue))
                            {
                                par.ParamValue = tempvalue;
                                if (par.ParamMinValue == 0 && par.ParamMaxValue == 0)
                                {
                                    par.IsAlarm = 0;
                                }
                                else if (par.ParamValue.ToDecimal() < par.ParamMinValue || par.ParamValue.ToDecimal() > par.ParamMaxValue)
                                {
                                    par.IsAlarm = 1;
                                }
                            }
                            else
                            {
                                LogHelper.SysLogWrite("ClassInfoSaver", "Save", $"跳变超限2：原始值:{fvalue}-计算值:{tempvalue}-历史值:{par.ParamLastValue}-跳变阈值:{par.ParamChangeValue}", "数据告警");
                            }
                        }

                    }
                    else
                        par.ParamValue = field.GetValue(t)?.ToString();
                    if (!par.ParamValue.IsNullOrEmpty())
                        newlist.Add(par);
                }
            }
            return newlist;
        }

        public static bool CheckParamAlarm(this Expand_DeviceParam par)
        {
            par.IsAlarm = 0;
            if (par.StatusValues.IsZxxAny())
            {
                var kvlist = par.StatusValues;
                //告警类参数：code 含 fault/alm/alarm 字样
                var isAlarmCode = par.ParamCode.ToLower().Contains("fault") || par.ParamCode.ToLower().Contains("alm") || par.ParamCode.ToLower().Contains("alarm");
                if (par.ValueType == "状态")
                {
                    var kv = kvlist.Find(t => t.StatusKey.ToString() == par.ParamValue);
                    if (kv != null)
                    {
                        par.ParamValue = kv.StatusValue;
                    }
                }
                else if (par.ValueType == "数位")
                {

                    int valueI = par.ParamValue == null ? 0 : par.ParamValue.ToInt();

                    var sb = new StringBuilder();
                    for (int i = 0; i < 16; i++)
                    {
                        var kv = kvlist.Find(t => t.StatusKey == i);
                        if (((valueI >> i) & 1) == 1 && kv != null)
                        {
                            sb.Append(kv.StatusValue).Append('|');
                        }
                    }

                    // 赋值给 ParamValue，去除末尾的 '|'；告警类空时为"正常"，非告警类空时为""
                    if (isAlarmCode)
                    {
                        par.ParamValue = sb.Length > 0 ? sb.ToString().TrimEnd('|') : "正常";
                    }
                    else
                    {
                        par.ParamValue = sb.Length > 0 ? sb.ToString().TrimEnd('|') : "";
                    }
                }
                //告警类参数统一判定：value 不为"正常"即告警
                if (isAlarmCode)
                {
                    par.IsAlarm = par.ParamValue.IsNullOrEmpty() || !par.ParamValue.Contains("正常") ? 1 : 0;
                }
            }
            else if (par.ValueType == "数值")
            {
                if (!par.ParamFormula.IsZxxNullOrEmpty())
                {
                    try
                    {
                        par.ParamValue = ExpressoFormula.CalculateString(par.ParamFormula, par.ParamCode, par.ParamValue.ToZxxDouble(), par.DecimalDigit);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.SysLogWrite("ClassInfoSaver", "Save", $"错误参数打印4：{par.ParamCode}-{par.ParamValue}-小数{par.DecimalDigit}-{par.ParamFormula}-@@@-{ex.ToString()}", "数据告警");
                        //XTrace.WriteException(ex);
                    }
                    if (par.ParamMinValue == 0 && par.ParamMaxValue == 0)
                    {
                        par.IsAlarm = 0;
                    }
                    else if (par.ParamValue.ToDecimal() < par.ParamMinValue || par.ParamValue.ToDecimal() > par.ParamMaxValue)
                    {
                        par.IsAlarm = 1;
                    }
                }
                else
                {
                    par.ParamValue = par.ParamValue.ToZxxDouble().ToString($"F{par.DecimalDigit}");
                }
            }

            return par.IsAlarm == 1;
        }
        public static List<Expand_DeviceParam> SaveDeviceParamSrc<T>(this T t, List<Expand_DeviceParam> paramList)
        {
            string timestr = DateTime.Now.ToDateTimeString();
            var proper = typeof(T).GetProperties();
            List<Expand_DeviceParam> newlist = new();
            foreach (var field in proper)
            {
                var par = paramList.FirstOrDefault(p => p.ParamCode.ToLower() == field.Name.ToLower());
                if (par != null)
                {
                    par.CollectTime = timestr;
                    par.ParamLastValue = par.ParamValue;
                    par.ParamValue = field.GetValue(t)?.ToString();
                    newlist.Add(par);
                }
            }
            return newlist;
        }

        public static List<Expand_DeviceParam> SaveDeviceParam(this List<Expand_DeviceParam> paramList)
        {
            List<Expand_DeviceParam> newlist = new();
            foreach (var par in paramList)
            {
                //告警类参数：code 含 fault/alm/alarm 字样
                var isAlarmCode = par.ParamCode.ToLower().Contains("fault") || par.ParamCode.ToLower().Contains("alm") || par.ParamCode.ToLower().Contains("alarm");
                if (par.ValueType == "数位")
                {
                    if (!string.IsNullOrEmpty(par.ParamValue) && par.StatusValues.Count > 0)
                    {
                        var valueI = par.ParamValue.ToZxxInt();
                        var nv = "";
                        for (int i = 0; i < 16; i++)
                        {
                            var kv = par.StatusValues.Find(t => t.StatusKey == i);
                            if (((valueI >> i) & 1) == 1 && kv != null)
                            {
                                nv += $"{kv.StatusValue}|";
                            }
                        }
                        nv = nv.TrimEnd('|');
                        //告警类空时为"正常"，非告警类空时为""
                        if (nv == "") nv = isAlarmCode ? "正常" : "";
                        par.ParamValue = nv;
                    }
                }
                else if (par.ValueType == "状态")
                {
                    if (!string.IsNullOrEmpty(par.ParamValue) && par.StatusValues.Count > 0)
                    {
                        var key = par.ParamValue.ToZxxInt();
                        var kv = par.StatusValues.Find(t => t.StatusKey == key);
                        if (kv != null)
                        {
                            par.ParamValue = kv.StatusValue.ToString();
                        }
                    }
                }
                //告警类参数统一判定：value 不为"正常"即告警
                if (isAlarmCode)
                {
                    par.IsAlarm = par.ParamValue.IsNullOrEmpty() || !par.ParamValue.Contains("正常") ? 1 : 0;
                }
                newlist.Add(par);
            }
            return newlist;
        }


        public static Expand_DeviceParam GetDeviceParam(this DeviceParamEntity pa, string codeStr)
        {
            if (pa != null && pa.ExpandObjects.IsZxxAny())
            {
                return pa.ExpandObjects.Find(it => it.ParamCode.ToLower() == codeStr.ToLower());
            }
            return null;
        }

        public static string GetDeviceParamValue(this DeviceParamEntity pa, string codeStr)
        {
            var va = pa.GetDeviceParam(codeStr);
            if (va != null)
                return va.ParamValue;
            return "";
        }

        public static string GetDeviceParamValueWithUnit(this DeviceParamEntity pa, string codeStr)
        {
            var va = pa.GetDeviceParam(codeStr);
            if (va != null)
                return va.ParamValue + va.ValueUnit;
            return "";
        }

        public static string GetDeviceExpandParamValue(this List<Expand_DeviceParam> palist, string codeStr)
        {
            if (palist.IsZxxAny())
            {
                var va = palist.Find(it => it.ParamCode.ToLower() == codeStr.ToLower());
                if (va != null)
                    return va.ParamValue;
            }
            return "";
        }

        /// <summary>
        /// 参数配置接口日志用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="paramList"></param>
        /// <returns>需要更新的集合</returns>
        public static List<Expand_DeviceParam> GetDeviceSetParam<T>(this T t, List<Expand_DeviceParam> paramList)
        {
            List<Expand_DeviceParam> newList = new List<Expand_DeviceParam>();
            string timestr = DateTime.Now.ToDateTimeString();
            var proper = typeof(T).GetProperties();
            foreach (var field in proper)
            {
                var par = paramList.FirstOrDefault(p => p.ParamCode.ToLower() == field.Name.ToLower());
                if (par != null)
                {
                    var dv = "";
                    var dvattr = (DefaultValueAttribute)field.GetCustomAttribute(typeof(DefaultValueAttribute));
                    if (dvattr != null) 
                    {
                        dv = dvattr.Value.ToString();
                    }
                    var npv = field.GetValue(t)?.ToString();
                    if (npv == dv) continue;
                    
                    par.ParamValue = npv;
                    Expand_DeviceParam newpar = new Expand_DeviceParam();
                    par.CopyTypeValue(newpar);
                    newList.Add(newpar);
                }
            }
            return newList;
        }

        /// <summary>
        /// 设备参数入库保存
        /// </summary>
        /// <typeparam name="T">解析后类</typeparam>
        /// <param name="t"></param>
        /// <param name="deviceParams"></param>
        /// <returns></returns>
        public static bool SaveParamSet<T>(this T t, List<DeviceParamEntity> deviceParams)
        {
            bool isres = false;
            try
            {
                var paramList = t.GetDeviceSetParam(deviceParams[0].ExpandObjects);
                foreach (var param in deviceParams)
                {
                    if (param.ExpandObjects.IsZxxAny())
                        param.ExpandObjects.ForEach(k =>
                        {
                            var _param = paramList.Find(p => p.ParamCode.ToLower() == k.ParamCode.ToLower());
                            if (_param != null)
                            {
                                k.ParamLastValue = k.ParamValue;
                                if (k.StatusValues.IsZxxAny())
                                {
                                    var kvlist = k.StatusValues;
                                    var pvalue = _param.ParamValue;
                                    //告警类参数：code 含 fault/alm/alarm 字样
                                    var isAlarmCode = k.ParamCode.ToLower().Contains("fault") || k.ParamCode.ToLower().Contains("alm") || k.ParamCode.ToLower().Contains("alarm");
                                    if (k.ValueType == "状态")
                                    {
                                        var kv = kvlist.Find(t => t.StatusKey.ToString() == pvalue);
                                        if (kv != null)
                                        {
                                            k.ParamValue = kv.StatusValue;
                                        }
                                    }
                                    else if (k.ValueType == "数位")
                                    {
                                        int valueI = k.ParamValue == null ? 0 : pvalue.ToInt();

                                        var sb = new StringBuilder();
                                        for (int i = 0; i < 16; i++)
                                        {
                                            var kv = kvlist.Find(t => t.StatusKey == i);
                                            if (((valueI >> i) & 1) == 1 && kv != null)
                                            {
                                                sb.Append(kv.StatusValue).Append('|');
                                            }
                                        }

                                        // 赋值给 ParamValue，去除末尾的 '|'；告警类空时为"正常"，非告警类空时为""
                                        if (isAlarmCode)
                                        {
                                            k.ParamValue = sb.Length > 0 ? sb.ToString().TrimEnd('|') : "正常";
                                        }
                                        else
                                        {
                                            k.ParamValue = sb.Length > 0 ? sb.ToString().TrimEnd('|') : "";
                                        }
                                    }
                                    //告警类参数统一判定：value 不为"正常"即告警
                                    if (isAlarmCode)
                                    {
                                        k.IsAlarm = k.ParamValue.IsNullOrEmpty() || !k.ParamValue.Contains("正常") ? 1 : 0;
                                    }
                                }
                                else
                                    k.ParamValue = _param.ParamValue;
                            }
                        });
                }
                isres = DeviceParamDAO.Instance.UpdateColumns(deviceParams, it => new { it.ExpandJson });
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return isres;
        }

        /// <summary>
        /// 批量设备参数入库保存
        /// </summary>
        /// <typeparam name="T">解析后类</typeparam>
        /// <param name="t"></param>
        /// <param name="deviceParams"></param>
        /// <returns></returns>
        public static List<DeviceParamEntity> SaveDevicesParamSet(this List<DeviceData> t)
        {
            List<DeviceParamEntity> isres = new();
            try
            {
                if (!t.IsZxxAny()) return isres;
                var ids = t.Select(t => t.DeviceId).Distinct().ToList();
                var deviceParams = DeviceParamDAO.Instance.GetListBy(t => ids.Contains(t.DeviceId));
                if (!deviceParams.IsZxxAny()) return isres;
                foreach (var item in t)
                {
                    var param = deviceParams.Find(it => it.DeviceId == item.DeviceId);
                    var paramList = item.deviceparam;
                    if (param.ExpandObjects.IsZxxAny())
                        param.ExpandObjects.ForEach(k =>
                        {
                            var _param = paramList.Find(p => p.ParamCode.ToLower() == k.ParamCode.ToLower());
                            if (_param != null)
                            {
                                if (_param.ParamValue != null && !k.ParamName.Contains("有功功率") && !k.ParamName.Contains("无功功率") && !k.ParamName.Contains("功率因") && _param.ParamValue.Contains("-") && param.DeviceTypeCode.ToLower().Contains("modbus") && k.IsShow)
                                {
                                    LogHelper.SysLogWrite("ClassInfoSaver", "Save", $"负值参数：ID:{item.device.DeviceId}-name:{item.device.DeviceName}-code:{_param.ParamCode}-value:{_param.ParamValue}-oldvalue:{k.ParamValue}-time:{_param.CollectTime}", "数据告警");
                                }
                                else
                                {
                                    k.ParamLastValue = k.ParamValue;
                                    k.ParamValue = _param.ParamValue;
                                    k.CollectTime = _param.CollectTime;
                                }
                            }
                        });
                }
                isres = deviceParams;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return isres;
        }

        /// <summary>
        /// 判断数据有效性
        /// </summary>
        /// <param name="lastvalue"></param>
        /// <param name="newvalue"></param>
        /// <param name="ParamChangeValue"></param>
        /// <returns></returns>
        public static bool CheckValue(string lastvalue, string newvalue, decimal ParamChangeValue)
        {
            bool result = false;
            decimal lastv;
            decimal newv;

            //不执行跳变值判断，直接返回true;
            if (CheckValueFlag == 0) return true;

            //跳变值不大于0时，不判断，直接返回true
            if (ParamChangeValue == 0) return true;

            if (!decimal.TryParse(lastvalue, out lastv)) return result;
            if (!decimal.TryParse(newvalue, out newv)) return result;

            // 计算绝对值差
            decimal difference = Math.Abs(newv - lastv);

            // 检查差值是否超过允许的跳变值
            if (difference > ParamChangeValue)
            {
                return false; // 差值超过允许范围
            }

            return true; // 差值在允许范围内
        }

    }
}
