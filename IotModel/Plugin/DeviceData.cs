using System.Collections.Generic;

namespace IotModel
{
    /// <summary>
    /// 设备数据上报
    /// </summary>
    public class DeviceData
    {
        /// <summary>
        /// 设备主键
        /// </summary>
        public int DeviceId { get; set; }
        /// <summary>
        /// 设备参数数据
        /// </summary>
        public List<Expand_DeviceParam> deviceparam { get; set; } = new();
        /// <summary>
        /// 设备信息
        /// </summary>
        public DeviceInfoEntity device { get; set; } = null;
        /// <summary>
        /// 上报数据类型0:实时数据,1:参数,2:故障状态,3:VRV网关数据,4:4G水表数据,5:4G空调更新人感状态
        /// </summary>
        public int paramtype { get; set; } = 0;
    }
}
