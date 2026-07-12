using Magicodes.ExporterAndImporter.Core;

namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 设备导入模板
    /// </summary>
    public class DeviceImportDto
    {
        /// <summary>
        /// 设备名称
        ///</summary>
        [ImporterHeader(Name = "设备名称")]
        public string DeviceName { get; set; }
        /// <summary>
        /// 设备类型
        ///</summary>
        [ImporterHeader(Name = "设备类型")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备编号
        ///</summary>
        [ImporterHeader(Name = "设备编号")]
        public string DeviceGuid { get; set; }
        /// <summary>
        /// 网关编号
        ///</summary>
        [ImporterHeader(Name = "网关编号")]
        public string DeviceGateway { get; set; }
        /// <summary>
        /// 父级设备名
        ///</summary>
        [ImporterHeader(Name = "父级设备名")]
        public string ParentName { get; set; }
        /// <summary>
        /// 设备IP地址
        ///</summary>
        [ImporterHeader(Name = "设备IP地址")]
        public string DeviceIp { get; set; }
        /// <summary>
        /// 设备端口号
        ///</summary>
        [ImporterHeader(Name = "设备端口号")]
        public string DevicePort { get; set; }
        /// <summary>
        /// 串口通道号
        ///</summary>
        [ImporterHeader(Name = "串口通道号")]
        public string DeviceCom { get; set; }
        /// <summary>
        /// 设备协议地址
        ///</summary>
        [ImporterHeader(Name = "设备协议地址")]
        public string DeviceAdr { get; set; }

        /// <summary>
        /// 单位名称(Excel 模板列头保持"单位名称"，属性映射由 ImporterHeader 特性承担)
        ///</summary>
        [ImporterHeader(Name = "单位名称")]
        public string TenantName { get; set; }
    }
}
