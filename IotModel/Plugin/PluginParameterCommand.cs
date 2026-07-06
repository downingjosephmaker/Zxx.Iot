using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 参数下发命令，继承自 <see cref="PluginCommandBase"/>。
    /// 在通用命令字段基础上扩展参数编码、参数值和备注。
    /// </summary>
    public class PluginParameterCommand : PluginCommandBase
    {
        /// <summary>
        /// 参数编码
        /// </summary>
        [DisplayName("参数编码")]
        public string ParamCode { get; set; } = "";

        /// <summary>
        /// 参数值
        /// </summary>
        [DisplayName("参数值")]
        public string ParamValue { get; set; } = "";

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        public string Remark { get; set; } = "";
    }
}
