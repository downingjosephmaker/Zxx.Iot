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
        /// 建筑一
        ///</summary>
        [ImporterHeader(Name = "建筑一")]
        public string BuildId1 { get; set; }
        /// <summary>
        /// 建筑二
        ///</summary>
        [ImporterHeader(Name = "建筑二")]
        public string BuildId2 { get; set; }
        /// <summary>
        /// 建筑三
        ///</summary>
        [ImporterHeader(Name = "建筑三")]
        public string BuildId3 { get; set; }
        /// <summary>
        /// 建筑四
        ///</summary>
        [ImporterHeader(Name = "建筑四")]
        public string BuildId4 { get; set; }
        /// <summary>
        /// 部门一
        ///</summary>
        [ImporterHeader(Name = "部门一")]
        public string DeptId1 { get; set; }
        /// <summary>
        /// 部门二
        ///</summary>
        [ImporterHeader(Name = "部门二")]
        public string DeptId2 { get; set; }
        /// <summary>
        /// 部门三"
        ///</summary>
        [ImporterHeader(Name = "部门三")]
        public string DeptId3 { get; set; }
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
        /// 单位名称
        ///</summary>
        [ImporterHeader(Name = "单位名称")]
        public string UnitName { get; set; }
    }
}
