namespace IotDriverCore
{
    /// <summary>
    /// 协议模拟人格契约(插件可选实现;与采集能力共存于同一插件实例,
    /// 但独立端口/独立生命周期,不与采集通道纠缠——方案A隔离原则)
    /// </summary>
    public interface ISimulatable
    {
        /// <summary>模拟能力元信息(支持的模式/默认端口等,前端渲染用)</summary>
        SimCapability Capability { get; }

        /// <summary>启动模拟(传入选中设备+点表快照+运行参数,返回运行句柄状态)</summary>
        Task<SimStatus> StartSimAsync(SimStartRequest request, CancellationToken ct);

        /// <summary>停止指定模拟实例</summary>
        Task StopSimAsync(string simId);

        /// <summary>查询当前所有模拟实例状态</summary>
        IReadOnlyList<SimStatus> ListSims();

        /// <summary>注入/清除故障(错帧/超时/半包等)</summary>
        Task InjectFaultAsync(string simId, SimFaultSpec fault);

        /// <summary>实时日志回调(收发帧hex摘要,宿主接到后转推SignalR)</summary>
        Action<SimLogEntry>? OnSimLog { get; set; }

        /// <summary>本插件是否负责该设备类型编码(按插件配置DeviceTypeCodes匹配,供路由精确命中)</summary>
        bool OwnsDeviceType(string deviceTypeCode);
    }
}
