namespace IotWebApi.Areas.Control.Models
{
    /// <summary>
    /// 烟感控制返回摘要
    /// </summary>
    public class SmokeControlResponse
    {
        /// <summary>
        /// 命令编号
        /// </summary>
        public string CommandId { get; set; } = "";

        /// <summary>
        /// 控制动作
        /// </summary>
        public string ActionName { get; set; } = "";

        /// <summary>
        /// 设备数量
        /// </summary>
        public int DeviceCount { get; set; }

        /// <summary>
        /// 设备名称列表
        /// </summary>
        public List<string> DeviceNames { get; set; } = new();

        /// <summary>
        /// 结果概览
        /// </summary>
        public string Overview { get; set; } = "";

        /// <summary>
        /// 成功数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// 设备结果
        /// </summary>
        public List<SmokeControlDeviceItem> Items { get; set; } = new();
    }

    /// <summary>
    /// 单设备控制结果摘要
    /// </summary>
    public class SmokeControlDeviceItem
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; } = "";

        /// <summary>
        /// 结果
        /// </summary>
        public string Result { get; set; } = "";

        /// <summary>
        /// 结果时间
        /// </summary>
        public string ResultTime { get; set; } = "";
    }
}
