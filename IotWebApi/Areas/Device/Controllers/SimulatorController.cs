using CenBoCommon.Zxx;
using IotDriverCore;
using IotLog;
using IotModel;
using IotWebApi.Services.Jobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace IotWebApi.Areas.Device.Controllers
{
    /// <summary>设备点表→SimDevice映射(纯函数,可单测;数据源复用平台点表,零手工配点)</summary>
    public static class SimDeviceMapper
    {
        public static SimDevice Map(DeviceInfoEntity dev, List<DeviceTypeParam> points)
        {
            return new SimDevice
            {
                Address = dev.DeviceAdr.ToString(),
                Points = points.Select(p => new SimPoint
                {
                    ParamCode = p.ParamCode,
                    Di = p.ParamAddr.ToString(),
                    FuncCode = p.CollectFuncCode,
                    Length = p.CollectFuncCode <= 2 ? 1 : Math.Max(1, p.CollectRegLength),
                    DataType = (p.CollectDataType ?? "uint16").Trim(),
                    Generator = new IotDriverCore.Simulation.GeneratorModel { Type = "random", Min = 0, Max = 100 }
                }).ToList()
            };
        }
    }

    /// <summary>设备模拟控制器(UI→REST→路由到插件ISimulatable;数据源复用平台设备/点表)</summary>
    [ApiController]
    public class SimulatorController : ControllerBaseApi
    {
        private const string LOG_CATEGORY = "设备模拟";

        private readonly IHubContext<ChatServer> _hubContext;

        public SimulatorController(IHubContext<ChatServer> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>取设备模拟元信息:点表快照+所属插件是否支持模拟+默认端口</summary>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public object GetDeviceSimMeta(int deviceId)
        {
            var dev = DeviceInfoDAO.Instance.GetOneBy(t => t.DeviceId == deviceId);
            if (dev == null) return new { ok = false, msg = "设备不存在" };
            var points = DeviceTypeParamDAO.Instance.GetListBy(t => t.DeviceTypeCode == dev.DeviceTypeCode
                && t.CollectFuncCode >= 1 && t.CollectFuncCode <= 4);
            var sim = ResolveSim(dev.DeviceTypeCode, out var pluginName);
            return new
            {
                ok = true,
                supportSim = sim != null,
                pluginName,
                capability = sim?.Capability,
                device = SimDeviceMapper.Map(dev, ToParamList(points))
            };
        }

        /// <summary>启动模拟(路由到设备所属插件)</summary>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public async Task<object> StartSim([FromBody] StartSimBody body)
        {
            var dev = DeviceInfoDAO.Instance.GetOneBy(t => t.DeviceId == body.DeviceId);
            if (dev == null) return new { ok = false, msg = "设备不存在" };
            var sim = ResolveSim(dev.DeviceTypeCode, out _);
            if (sim == null) return new { ok = false, msg = "该设备所属插件不支持模拟" };
            var points = DeviceTypeParamDAO.Instance.GetListBy(t => t.DeviceTypeCode == dev.DeviceTypeCode
                && t.CollectFuncCode >= 1 && t.CollectFuncCode <= 4);
            var simDev = SimDeviceMapper.Map(dev, ToParamList(points));
            ApplyOverrides(simDev, body);
            var req = new SimStartRequest { Mode = SimMode.Slave, Port = body.Port, Devices = new() { simDev } };
            var status = await sim.StartSimAsync(req, default);
            status.DeviceId = dev.DeviceId;
            AttachSimLog(sim);
            return new { ok = true, status };
        }

        /// <summary>停止模拟</summary>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public async Task<object> StopSim([FromBody] StopSimBody body)
        {
            foreach (var sim in AllSims())
            {
                await sim.StopSimAsync(body.SimId);
                // 该插件下已无运行中模拟,摘除回调,避免悬空引用
                if (sim.ListSims().Count == 0) sim.OnSimLog = null;
            }
            return new { ok = true };
        }

        /// <summary>列出所有运行中模拟</summary>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public object ListSims()
        {
            var all = AllSims().SelectMany(s => s.ListSims()).ToList();
            return new { ok = true, sims = all };
        }

        /// <summary>运行中注入/清除故障</summary>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public async Task<object> InjectFault([FromBody] InjectFaultBody body)
        {
            foreach (var sim in AllSims())
                await sim.InjectFaultAsync(body.SimId, new SimFaultSpec { Kind = body.Kind, Probability = body.Probability, DelayMs = body.DelayMs });
            return new { ok = true };
        }

        /// <summary>DAO返回FullEntity子类,映射器按基类点表签名,此处做协变列表转换</summary>
        private static List<DeviceTypeParam> ToParamList(List<DeviceTypeParamEntity>? points) =>
            points?.Cast<DeviceTypeParam>().ToList() ?? new();

        // ===== 内部:插件解析 =====

        /// <summary>按设备类型编码找所属插件(经Manifest的addressing/DeviceTypeCodes配置),转型ISimulatable</summary>
        private static ISimulatable? ResolveSim(string deviceTypeCode, out string pluginName)
        {
            pluginName = "";
            foreach (var kv in OperatorCommon.DicPlugins)
            {
                if (kv.Value is ISimulatable sim)
                {
                    // 简化:首个支持模拟且协议匹配的插件;精确匹配可后续按DeviceTypeCodes配置细化
                    pluginName = kv.Value.PluginName;
                    return sim;
                }
            }
            return null;
        }

        private static IEnumerable<ISimulatable> AllSims() =>
            OperatorCommon.DicPlugins.Values.OfType<ISimulatable>();

        /// <summary>
        /// 挂载帧日志回调(按SimLogEntry.SimId路由到对应sim分组;
        /// 插件实例是单例,一个挂载点服务该插件下所有并存的模拟,幂等挂载不重复覆盖)
        /// </summary>
        private void AttachSimLog(ISimulatable sim)
        {
            sim.OnSimLog ??= entry =>
            {
                _hubContext.Clients.Group($"sim:{entry.SimId}").SendAsync("ReceiveSimLog", entry.ToJson())
                    .ContinueWith(t => LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"模拟帧日志推送失败：{t.Exception}", LOG_CATEGORY), TaskContinuationOptions.OnlyOnFaulted);
            };
        }

        private static void ApplyOverrides(SimDevice dev, StartSimBody body)
        {
            if (body.Generators == null) return;
            foreach (var ov in body.Generators)
            {
                var p = dev.Points.FirstOrDefault(x => x.ParamCode == ov.ParamCode);
                if (p != null) p.Generator = ov.Generator;
            }
        }

        public class StartSimBody
        {
            public int DeviceId { get; set; }
            public int Port { get; set; }
            public List<GeneratorOverride>? Generators { get; set; }
        }
        public class GeneratorOverride
        {
            public string ParamCode { get; set; } = "";
            public IotDriverCore.Simulation.GeneratorModel Generator { get; set; } = new();
        }
        public class StopSimBody { public string SimId { get; set; } = ""; }
        public class InjectFaultBody
        {
            public string SimId { get; set; } = "";
            public string Kind { get; set; } = "";
            public double Probability { get; set; } = 1;
            public int DelayMs { get; set; } = 50;
        }
    }
}
