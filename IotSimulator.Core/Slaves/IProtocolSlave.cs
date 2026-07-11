namespace IotSimulator.Core.Slaves
{
    /// <summary>
    /// 协议从站抽象(串行一问一答型:645/188——收主站请求帧,编从站应答帧;
    /// Modbus TCP从站由FluentModbus托管寄存器区不走此接口,是另一形态)
    /// </summary>
    public interface IProtocolSlave
    {
        /// <summary>从站地址(路由用,一条连接可挂多从站)</summary>
        string Address { get; }

        /// <summary>
        /// 处理一个请求帧,返回应答帧(null=不应答:地址不匹配/广播/未配置点位/校验失败)
        /// </summary>
        byte[]? HandleFrame(byte[] frame, DateTime now);

        /// <summary>
        /// 篡改一帧的校验字节(错帧故障注入;各协议校验字节位置不同故下沉从站)
        /// </summary>
        byte[] Corrupt(byte[] frame);
    }
}
