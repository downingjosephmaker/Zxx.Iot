using IotDriverCore;
using Opc.Ua;
using Opc.Ua.Server;

namespace IotPlugin.OpcUa.Sim
{
    /// <summary>
    /// 模拟OPC UA服务器(StandardServer挂单个模拟节点管理器,地址空间来自SimPoint清单)
    /// </summary>
    internal sealed class OpcUaSimServer : StandardServer
    {
        private readonly List<SimDevice> _devices;

        public OpcUaSimServer(List<SimDevice> devices) => _devices = devices;

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            var nodeManagers = new List<INodeManager> { new OpcUaSimNodeManager(server, configuration, _devices) };
            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }
    }
}
