namespace IotWebApi.Areas.Control.Models
{
    /// <summary>
    /// 参数下发入参
    /// </summary>
    public class PluginParameterDemoInput
    {
        /// <summary>
        /// 设备ID集合
        /// </summary>
        public List<int> DeviceIds { get; set; } = new();

        /// <summary>
        /// 参数编码
        /// </summary>
        public string ParamCode { get; set; } = "";

        /// <summary>
        /// 参数值
        /// </summary>
        public string ParamValue { get; set; } = "";

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = "";
    }
}
