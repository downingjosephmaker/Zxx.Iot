using CenboEventBus;
using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using Microsoft.AspNetCore.Mvc;

namespace IotWebApi.Controllers
{
    /// <summary>
    /// 设备指令下发(§5/§6.3:手动下发走产品命令白名单,ClassName过白名单,ConContent由前端按ConTemplate占位填充;
    /// 与规则引擎RunCommand同构——PluginGuid空=广播全部已加载插件,插件对不支持的控制类型自行忽略,
    /// 控制结果经既有PluginControlResultMessage链路回流审计)
    /// </summary>
    [ApiController]
    [ControllSort("7-9")]
    public class DeviceControlController : ControllerBaseApi
    {
        private const string CONTROL_CATEGORY = "设备指令下发";

        /// <summary>
        /// 下行控制类型白名单(与RuleLinkageService.CommandWhitelist同源;§6.3:仅允许既有协议控制类型)
        /// </summary>
        private static readonly HashSet<string> CommandWhitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            "netmodbuswrite", "netdlt645timesync", "netdlt645read", "netcjt188read", "netcjt188valve",
            "nets7write", "netopcuawrite"
        };

        /// <summary>
        /// 手动下发设备命令
        /// </summary>
        /// <param name="para">下发参数</param>
        /// <param name="commandBus">插件命令总线(DI注入)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Device)]
        public string SendCommand(DeviceCommandSendPara para,
            [FromServices] IEventBus<PluginCommandEvent> commandBus)
        {
            Status = false;
            Message = "指令下发失败。";
            if (para == null || para.CommandId <= 0)
            {
                Message = "下发参数不完整。";
                return Message;
            }
            if (!para.DeviceIds.IsZxxAny())
            {
                Message = "未指定下发设备。";
                return Message;
            }
            var deviceids = para.DeviceIds.Where(t => t > 0).Distinct().ToList();
            if (!deviceids.IsZxxAny())
            {
                Message = "未指定有效设备。";
                return Message;
            }

            // 命令须为已启用的白名单命令,ConContent由前端按ConTemplate占位填充后上送
            var command = ProductCommandDAO.Instance.GetOneBy(t => t.SnowId == para.CommandId && t.IsEnable);
            if (command == null)
            {
                Message = "命令不存在或已停用。";
                return Message;
            }
            if (command.ClassName.IsZxxNullOrEmpty() || !CommandWhitelist.Contains(command.ClassName.Trim()))
            {
                Message = $"控制类型[{command.ClassName}]不在白名单,已拒绝下发。";
                return Message;
            }
            if (para.ConContent.IsZxxNullOrEmpty())
            {
                Message = "下行内容为空。";
                return Message;
            }

            var optmdl = Request.GetToken();
            var commandGuid = SnowModel.Instance.NewId().ToString();
            var message = new PluginMessage
            {
                MessageType = PluginMessageEnum.设备控制,
                MessageJson = new
                {
                    CommandId = commandGuid,
                    ClassName = command.ClassName,
                    ConContent = para.ConContent,
                    DeviceIds = deviceids
                }.ToJson()
            };

            // 设备不归属特定插件,广播全部已加载插件(与规则引擎PluginGuid空时一致)
            var guids = OperatorCommon.DicPlugins.Keys.ToList();
            if (!guids.IsZxxAny())
            {
                Message = "无已加载的协议插件,命令未下发。";
                return Message;
            }
            foreach (var guid in guids)
            {
                commandBus.Publish(new PluginCommandEvent(guid, message));
            }

            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                $"用户[{optmdl?.UserName}]下发命令[{command.CommandName}/{command.ClassName}]至设备{deviceids.ToJson()},命令ID[{commandGuid}]",
                CONTROL_CATEGORY);

            Status = true;
            Message = $"命令[{command.CommandName}]已下发至{deviceids.Count}台设备。";
            return Message;
        }
    }

    /// <summary>
    /// 设备指令下发参数
    /// </summary>
    public class DeviceCommandSendPara
    {
        /// <summary>
        /// 产品命令主键(product_command.snow_id)
        /// </summary>
        public long CommandId { get; set; }

        /// <summary>
        /// 目标设备ID集合
        /// </summary>
        public List<int> DeviceIds { get; set; } = new();

        /// <summary>
        /// 下行内容(前端按ConTemplate占位填充后的最终JSON串)
        /// </summary>
        public string ConContent { get; set; }
    }
}
