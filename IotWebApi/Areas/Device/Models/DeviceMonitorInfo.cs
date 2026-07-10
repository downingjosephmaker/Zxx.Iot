using System.ComponentModel;

namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 设备监控卡片展示
    /// </summary>
    public class DeviceMonitorInfo : MqttWebModel
    {
        /// <summary>
        /// 单位名称
        ///</summary>
        [DisplayName("单位名称")]
        public string UnitName { get; set; }
        /// <summary>
        /// 设备类型名称
        ///</summary>
        [DisplayName("设备类型名称")]
        public string DeviceTypeName { get; set; }
        /// <summary>
        /// 设备大类名称
        ///</summary>
        [DisplayName("设备大类名称")]
        public string DeviceMasterTypeName { get; set; }
        /// <summary>
        /// 设备大类编号
        ///</summary>
        [DisplayName("设备大类编号")]
        public string DeviceMasterTypeCode { get; set; }
        /// <summary>
        /// 电流互感器变比
        ///</summary>
        [DisplayName("电流互感器变比")]
        public int CurrentTransformer { get; set; } = 1;
        /// <summary>
        /// 电压互感器变比
        ///</summary>
        [DisplayName("电压互感器变比")]
        public int VoltageTransformer { get; set; } = 1;
    }

    /// <summary>
    /// 设备监控查询条件
    /// </summary>
    public class DeviceMonitorSearch
    {
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        public int TenantId { get; set; }
        /// <summary>
        /// 设备名称
        /// </summary>
        [DisplayName("设备名称")]
        public string DevName { get; set; }

        /// <summary>
        /// 设备大类编号
        /// </summary>
        [DisplayName("设备大类编号")]
        public string DevMasterTypeCode { get; set; }
        /// <summary>
        /// 设备类型集合
        /// </summary>
        [DisplayName("设备类型集合")]
        public List<string> DevTypeCodeList { get; set; } = new List<string>();
        /// <summary>
        /// 在线状态(0:离线 1:掉电 2:在线 3:异常)
        /// </summary>
        [DisplayName("在线状态(0:离线 1:掉电 2:在线 3:异常)")]
        [DefaultValue("-1")]
        public int DevLine { get; set; } = -1;
        /// <summary>
        /// 开关状态(1:关闭 2:开启 3:强关 4:强开)
        /// </summary>
        [DisplayName("开关状态(1:关闭 2:开启 3:强关 4:强开)")]
        [DefaultValue("-1")]
        public int DevSwitch { get; set; } = -1;

        /// <summary>
        /// 当前页
        ///</summary>
        [DisplayName("当前页")]
        public int page { get; set; }
        /// <summary>
        /// 每页记录数
        ///</summary>
        [DisplayName("每页记录数")]
        public int pagesize { get; set; } = 20;

    }

    /// <summary>
    /// 设备能耗详情
    /// </summary>
    public class DeviceMonitorDetail
    {
        /// <summary>
        /// 设备ID
        ///</summary>
        [DisplayName("设备ID")]
        public int DeviceId { get; set; }
        /// <summary>
        /// 设备名称
        ///</summary>
        [DisplayName("设备名称")]
        public string DeviceName { get; set; }
        /// <summary>
        /// 设备类型编号
        ///</summary>
        [DisplayName("设备类型编号")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备类型名称
        ///</summary>
        [DisplayName("设备类型名称")]
        public string DeviceTypeName { get; set; }

        /// <summary>
        /// 设备编号
        ///</summary>
        [DisplayName("设备编号")]
        public string DeviceGuid { get; set; }

        /// <summary>
        /// 设备从机地址
        ///</summary>
        [DisplayName("设备从机地址")]
        public int DeviceAdr { get; set; }

        /// <summary>
        /// 充电状态
        ///</summary>
        [DisplayName("充电状态")]
        public string DeviceSwitch { get; set; }

        /// <summary>
        /// 设备能耗参数集合--年
        /// </summary>
        [DisplayName("设备能耗参数集合--年")]
        public List<ShowParam> YRealList { get; set; } = new();
        /// <summary>
        /// 设备能耗参数集合--月
        /// </summary>
        [DisplayName("设备能耗参数集合--月")]
        public List<ShowParam> MRealList { get; set; } = new();
        /// <summary>
        /// 设备能耗参数集合--日
        /// </summary>
        [DisplayName("设备能耗参数集合--日")]
        public List<ShowParam> DRealList { get; set; } = new();

    }

    /// <summary>
    /// 大屏设备能耗
    /// </summary>
    public class DeviceSecondMonitor
    {
        /// <summary>
        /// 能耗参数集合--年总
        /// </summary>
        [DisplayName("能耗参数集合--年总")]
        public List<ShowParam> YTotalList { get; set; } = new();
        /// <summary>
        /// 能耗参数集合--月总
        /// </summary>
        [DisplayName("能耗参数集合--月总")]
        public List<ShowParam> MTotalList { get; set; } = new();
        /// <summary>
        /// 能耗参数集合--日总
        /// </summary>
        [DisplayName("能耗参数集合--日总")]
        public List<ShowParam> DTotalList { get; set; } = new();

        /// <summary>
        /// 设备能耗参数集合
        /// </summary>
        [DisplayName("设备能耗参数集合")]
        public List<DeviceMonitorDetail> DeviceDetailList { get; set; } = new();

    }

    /// <summary>
    /// 大屏二级数据查询参数
    /// </summary>
    public class DeviceSecondMonitorSearch
    {

    }
}
