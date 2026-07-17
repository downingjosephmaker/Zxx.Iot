using System.Collections.Concurrent;
using System.Globalization;
using CenBoCommon.Zxx;
using IotDriverCore;
using IotLog;
using IotModel;

namespace IotWebApi.Services
{
    /// <summary>
    /// JS协议脚本管理服务(§6.4:沙箱缓存按脚本版本热切换,升级新实例替换旧实例;
    /// 试运行走独立临时沙箱干跑无副作用,不影响运行时缓存;
    /// 脚本默认禁用,GetSandbox只对启用脚本供给运行时;
    /// 运行时降级:脚本错误滑动窗口超阈值自动禁用并发平台通知)
    /// </summary>
    public class ProtocolScriptService
    {
        private const string Service_CATEGORY = "协议脚本";

        /// <summary>错误滑动窗口时长</summary>
        private static readonly TimeSpan ErrorWindow = TimeSpan.FromMinutes(5);

        /// <summary>窗口内错误数阈值(超过即自动禁用脚本)</summary>
        private const int ErrorThreshold = 100;

        /// <summary>
        /// 脚本错误滑窗计数
        /// </summary>
        private class ErrorCounter
        {
            public DateTime WindowStart;
            public int Count;
        }

        /// <summary>脚本ID→错误滑窗</summary>
        private readonly ConcurrentDictionary<long, ErrorCounter> _errors = new();

        /// <summary>
        /// 告警通知服务(脚本自动禁用时发平台级通知)
        /// </summary>
        private readonly AlarmNotifyService _alarmNotifyService;

        public ProtocolScriptService(AlarmNotifyService alarmNotifyService)
        {
            _alarmNotifyService = alarmNotifyService;
        }
        /// <summary>
        /// 沙箱缓存条目(版本变化即重建)
        /// </summary>
        private class CacheEntry
        {
            public int Version;
            public ScriptSandbox Sandbox;
        }

        /// <summary>脚本ID→沙箱缓存</summary>
        private readonly ConcurrentDictionary<long, CacheEntry> _cache = new();

        /// <summary>
        /// 取启用脚本的运行时沙箱(未启用/不存在/编译失败返回null由调用方降级;版本变化热切换)
        /// </summary>
        public ScriptSandbox GetSandbox(long scriptid)
        {
            var script = ProtocolScriptDAO.Instance.GetOneBy(t => t.SnowId == scriptid);
            if (script == null || !script.IsEnable) return null;
            var entry = _cache.GetOrAdd(scriptid, _ => new CacheEntry { Version = -1 });
            if (entry.Version != script.Version)
            {
                lock (entry)
                {
                    if (entry.Version != script.Version)
                    {
                        entry.Sandbox = new ScriptSandbox(script.ScriptContent ?? "");
                        entry.Version = script.Version;
                    }
                }
            }
            return entry.Sandbox is { Ready: true } ? entry.Sandbox : null;
        }

        /// <summary>
        /// 按产品类型编码取启用脚本的沙箱(§6.5:非JSON载荷按产品挂脚本解码)
        /// </summary>
        public ScriptSandbox GetSandboxByTypeCode(string typecode)
        {
            if (typecode.IsZxxNullOrEmpty()) return null;
            var script = (ProtocolScriptDAO.Instance.GetList()?.Cast<ProtocolScript>() ?? Enumerable.Empty<ProtocolScript>())
                .FirstOrDefault(t => t.IsEnable && string.Equals(t.DeviceTypeCode, typecode, StringComparison.OrdinalIgnoreCase));
            return script == null ? null : GetSandbox(script.SnowId);
        }

        /// <summary>
        /// 试运行干跑(临时沙箱即用即弃;草稿内容优先,为空时按脚本ID取库内内容——
        /// 不校验IsEnable,编辑调试阶段脚本通常尚未启用)
        /// </summary>
        public ScriptRunResult DryRun(long scriptid, string scriptcontent, string funcname, string inputhex, string inputjson, string contextjson)
        {
            string content = scriptcontent;
            if (content.IsZxxNullOrEmpty() && scriptid > 0)
            {
                content = ProtocolScriptDAO.Instance.GetOneBy(t => t.SnowId == scriptid)?.ScriptContent ?? "";
            }
            if (content.IsZxxNullOrEmpty())
            {
                return new ScriptRunResult { FuncName = funcname ?? "", Error = "脚本内容为空" };
            }
            var sandbox = new ScriptSandbox(content);
            return (funcname ?? "").Trim().ToLowerInvariant() switch
            {
                "encode" => sandbox.RunEncode(inputjson, contextjson),
                "splitframes" => sandbox.RunSplitFrames(inputhex, contextjson),
                _ => sandbox.RunDecode(inputhex, contextjson)
            };
        }

        /// <summary>
        /// 清空沙箱缓存(保存/删除脚本后调用,下次取用按新版本重建)
        /// </summary>
        public void Reload() => _cache.Clear();

        #region MQTT非JSON载荷运行时解码(§6.5)

        /// <summary>
        /// 脚本decode返回模型(§6.4三段式;MQTT路径当前消费telemetry,attributes/events/reply预留)
        /// </summary>
        public class ScriptDecodeModel
        {
            /// <summary>遥测数组</summary>
            public List<ScriptTelemetryItem> Telemetry { get; set; }
        }

        /// <summary>
        /// 脚本遥测项(key=点表参数编码,value=工程值——脚本内完成定标,平台不再套ParamFormula)
        /// </summary>
        public class ScriptTelemetryItem
        {
            /// <summary>参数编码</summary>
            public string Key { get; set; }
            /// <summary>工程值(数值或字符串)</summary>
            public object Value { get; set; }
        }

        /// <summary>
        /// MQTT非JSON载荷按产品脚本解码为DeviceData(§6.5:deviceKey=topic末段,
        /// 匹配DeviceInfo.DeviceGateway,纯数字兜底按DeviceId;
        /// 无脚本/未启用/解码失败返回null由调用方丢弃该消息)
        /// </summary>
        public List<DeviceData> DecodePayload(string devicekey, byte[] payload)
        {
            try
            {
                if (devicekey.IsZxxNullOrEmpty() || payload == null || payload.Length == 0) return null;
                var device = DeviceInfoDAO.Instance.GetOneBy(t => t.DeviceGateway == devicekey);
                if (device == null && int.TryParse(devicekey, out int did))
                {
                    device = DeviceInfoDAO.Instance.GetOneBy(t => t.DeviceId == did);
                }
                if (device == null) return null;
                var script = (ProtocolScriptDAO.Instance.GetList()?.Cast<ProtocolScript>() ?? Enumerable.Empty<ProtocolScript>())
                    .FirstOrDefault(t => t.IsEnable && string.Equals(t.DeviceTypeCode, device.DeviceTypeCode, StringComparison.OrdinalIgnoreCase));
                if (script == null) return null;
                var sandbox = GetSandbox(script.SnowId);
                if (sandbox == null) return null;

                string contextjson = new
                {
                    deviceKey = devicekey,
                    deviceId = device.DeviceId,
                    now = DateTime.Now.ToDateTimeString()
                }.ToJson();
                var run = sandbox.RunDecode(Convert.ToHexString(payload), contextjson);
                if (!run.Success)
                {
                    RecordScriptError(script, device.DeviceId, run.Error);
                    return null;
                }
                if (run.ResultJson.IsZxxNullOrEmpty()) return null;  // 脚本返回null=主动丢弃该帧
                var model = run.ResultJson.ToObject<ScriptDecodeModel>();
                if (model?.Telemetry == null || !model.Telemetry.Any()) return null;

                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in model.Telemetry)
                {
                    if (item?.Key == null) continue;
                    values[item.Key] = Convert.ToString(item.Value, CultureInfo.InvariantCulture) ?? "";
                }
                var data = BuildDeviceData(device, values);
                return data == null ? null : new List<DeviceData> { data };
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), Service_CATEGORY);
                return null;
            }
        }

        /// <summary>
        /// 按设备参数模板构建DeviceData(与协议插件同构;脚本已输出工程值故不套ParamFormula,
        /// 按最大/最小合法值标记IsAlarm)
        /// </summary>
        private static DeviceData BuildDeviceData(DeviceInfo device, Dictionary<string, string> values)
        {
            string timestr = DateTime.Now.ToDateTimeString();
            var dev = new DeviceInfoEntity();
            device.CopyTypeValue(dev);
            dev.LastOnlineTime = timestr;
            dev.DeviceState = 2;
            var devparam = DeviceParamDAO.Instance.GetOneBy(t => t.DeviceId == device.DeviceId);
            if (devparam == null || !devparam.ExpandObjects.IsZxxAny()) return null;

            var realparams = new List<Expand_DeviceParam>();
            foreach (var tmpl in devparam.ExpandObjects)
            {
                if (!values.TryGetValue(tmpl.ParamCode, out var raw)) continue;
                var p = new Expand_DeviceParam();
                tmpl.CopyTypeValue(p);
                p.StatusValues = tmpl.StatusValues;
                p.CollectTime = timestr;
                p.ParamLastValue = tmpl.ParamValue;
                p.ParamValue = raw;
                p.IsAlarm = 0;
                if (p.ParamMinValue != 0 || p.ParamMaxValue != 0)
                {
                    decimal pv = p.ParamValue.ToDecimal();
                    if (pv < p.ParamMinValue || pv > p.ParamMaxValue) p.IsAlarm = 1;
                }
                realparams.Add(p);
            }
            if (!realparams.IsZxxAny()) return null;
            return new DeviceData
            {
                DeviceId = dev.DeviceId,
                device = dev,
                deviceparam = realparams,
                paramtype = 0
            };
        }

        /// <summary>
        /// 记录脚本错误并按滑动窗口自动禁用(§6.4降级:5分钟100次→IsEnable=false+平台通知;
        /// 错误明细记系统日志,raw_frame_log落库待遥测链路运行环境接入)
        /// </summary>
        private void RecordScriptError(ProtocolScript script, int deviceid, string error)
        {
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                $"脚本[{script.ScriptName}]解码设备[{deviceid}]失败：{error}", Service_CATEGORY);
            var counter = _errors.GetOrAdd(script.SnowId, _ => new ErrorCounter { WindowStart = DateTime.Now });
            lock (counter)
            {
                if (DateTime.Now - counter.WindowStart > ErrorWindow)
                {
                    counter.WindowStart = DateTime.Now;
                    counter.Count = 0;
                }
                if (++counter.Count < ErrorThreshold) return;
                counter.Count = 0;
            }
            var row = ProtocolScriptDAO.Instance.GetOneBy(t => t.SnowId == script.SnowId);
            if (row == null || !row.IsEnable) return;
            row.IsEnable = false;
            ProtocolScriptDAO.Instance.UpdateColumns(new List<ProtocolScript> { row }, it => new { it.IsEnable });
            Reload();
            string msg = $"协议脚本[{row.ScriptName}]在{ErrorWindow.TotalMinutes}分钟内错误超{ErrorThreshold}次,已自动禁用,请检查脚本或回滚上一版本";
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, msg, Service_CATEGORY);
            _alarmNotifyService.NotifyText(msg);
        }

        #endregion
    }
}
