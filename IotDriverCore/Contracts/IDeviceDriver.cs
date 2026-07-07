namespace IotDriverCore
{
    /// <summary>
    /// 协议驱动统一契约(§6.1:连接管理/批量读/下行写三段语义;
    /// 驱动类用DriverInfoAttribute标注元数据,连接参数属性用ConfigParameterAttribute标注,
    /// 平台反射生成前端配置表单,新增驱动零UI代码)
    /// </summary>
    public interface IDeviceDriver : IDisposable
    {
        /// <summary>
        /// 建立与设备/链路的连接(客户端型驱动拨出;服务端型驱动返回监听是否就绪)
        /// </summary>
        Task<bool> ConnectAsync(CancellationToken ct);

        /// <summary>
        /// 当前连接状态
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 关闭连接并释放链路资源
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// 批量读(框架把点表按地址连续性合包为PointBatch后调用,见PointBatchBuilder)
        /// </summary>
        Task<DriverReadResult> ReadBatchAsync(PointBatch batch, CancellationToken ct);

        /// <summary>
        /// 下行写/服务调用(写优先:写入期间暂停本链路轮询)
        /// </summary>
        Task<DriverWriteResult> WriteAsync(DeviceCommand cmd, CancellationToken ct);
    }
}
