namespace IotDriverCore
{
    /// <summary>
    /// 通道物理发送抽象(指令引擎与具体链路解耦:TCP服务端会话/TCP客户端/串口均实现此接口)
    /// </summary>
    public interface IChannelTransport
    {
        /// <summary>
        /// 向指定端点发送原始字节(false=端点不在线或发送失败,引擎将指令回置待发)
        /// </summary>
        bool Send(string endpoint, byte[] payload);
    }
}
