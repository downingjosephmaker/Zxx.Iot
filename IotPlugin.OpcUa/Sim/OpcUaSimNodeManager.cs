using IotDriverCore;
using IotDriverCore.Simulation;
using Opc.Ua;
using Opc.Ua.Server;

namespace IotPlugin.OpcUa.Sim
{
    /// <summary>
    /// 模拟节点管理器(把SimPoint清单托管为可读写变量节点并按生成器周期刷新;
    /// 服务器命名空间表=[UA核心0,应用URI1,本命名空间2],故点表惯用的"ns=2;s=xxx"直接命中;
    /// Di里写其他命名空间索引时仅取其标识符部分,同样挂到本命名空间下)
    /// </summary>
    internal sealed class OpcUaSimNodeManager : CustomNodeManager2
    {
        /// <summary>
        /// 模拟点位运行态
        /// </summary>
        private sealed class SimNode
        {
            public BaseDataVariableState Variable = null!;
            public IValueGenerator Generator = null!;
            public double Scale = 1;
            /// <summary>客户端写入后不再随生成器刷新</summary>
            public bool Overridden;
        }

        private readonly List<SimDevice> _devices;
        private readonly List<SimNode> _nodes = new();
        private Timer? _refreshTimer;

        public OpcUaSimNodeManager(IServerInternal server, ApplicationConfiguration configuration, List<SimDevice> devices)
            : base(server, configuration, "urn:ZxxIot:OpcUaSim")
        {
            _devices = devices;
        }

        /// <summary>
        /// 构建地址空间:Objects下挂Sim文件夹,每个点位一个可读写变量节点
        /// </summary>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                var folder = new FolderState(null)
                {
                    SymbolicName = "Sim",
                    ReferenceTypeId = ReferenceTypeIds.Organizes,
                    TypeDefinitionId = ObjectTypeIds.FolderType,
                    NodeId = new NodeId("Sim", NamespaceIndex),
                    BrowseName = new QualifiedName("Sim", NamespaceIndex),
                    DisplayName = "Sim",
                    EventNotifier = EventNotifiers.None
                };
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var references))
                {
                    references = new List<IReference>();
                    externalReferences[ObjectIds.ObjectsFolder] = references;
                }
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, folder.NodeId));
                folder.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);

                foreach (var device in _devices)
                {
                    foreach (var point in device.Points)
                    {
                        var variable = CreateVariable(folder, point);
                        var node = new SimNode
                        {
                            Variable = variable,
                            Generator = GeneratorFactory.Create(point.Generator),
                            Scale = point.Scale <= 0 ? 1 : point.Scale
                        };
                        // 客户端写入即接管值,生成器停止刷新该点(与104从站遥控语义一致)
                        variable.OnSimpleWriteValue = (ISystemContext context, NodeState state, ref object value) =>
                        {
                            node.Overridden = true;
                            return ServiceResult.Good;
                        };
                        _nodes.Add(node);
                    }
                }
                AddPredefinedNode(SystemContext, folder);
            }
            _refreshTimer = new Timer(Refresh, null, 1000, 1000);
        }

        /// <summary>
        /// 按点位Di创建变量节点(取NodeId标识符部分挂到本命名空间;
        /// DataType用BaseDataType兼容任意写入类型,免去Variant类型不匹配拒写)
        /// </summary>
        private BaseDataVariableState CreateVariable(FolderState parent, SimPoint point)
        {
            object identifier;
            try
            {
                identifier = NodeId.Parse(point.Di).Identifier;
            }
            catch
            {
                identifier = point.Di;
            }
            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = point.ParamCode,
                ReferenceTypeId = ReferenceTypeIds.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId(identifier, NamespaceIndex),
                BrowseName = new QualifiedName(point.ParamCode, NamespaceIndex),
                DisplayName = point.ParamCode,
                DataType = DataTypeIds.BaseDataType,
                ValueRank = ValueRanks.Scalar,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite,
                Historizing = false,
                Value = 0.0,
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };
            parent.AddChild(variable);
            return variable;
        }

        /// <summary>
        /// 秒级刷新:生成器产值写入变量并通知订阅(客户端写死的点位跳过)
        /// </summary>
        private void Refresh(object? _)
        {
            try
            {
                var now = DateTime.Now;
                lock (Lock)
                {
                    foreach (var node in _nodes)
                    {
                        if (node.Overridden) continue;
                        node.Variable.Value = node.Generator.Next(now) / node.Scale;
                        node.Variable.Timestamp = DateTime.UtcNow;
                        node.Variable.ClearChangeMasks(SystemContext, false);
                    }
                }
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Dispose();
                _refreshTimer = null;
            }
            base.Dispose(disposing);
        }
    }
}
