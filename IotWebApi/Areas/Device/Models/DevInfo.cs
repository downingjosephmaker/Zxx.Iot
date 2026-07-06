using System.ComponentModel;

namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 监控设备
    /// </summary>
    public class DevInfo
    {
        /// <summary>
        /// 区域编码
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// 区域名称
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 在线离线 1:在线 0:离线
        /// </summary>
        public string state { get; set; }
        /// <summary>
        /// 异常状态 1:正常 0:异常
        /// </summary>
        public string status { get; set; }
        /// <summary>
        /// 设备id
        /// </summary>
        public string devid { get; set; }
        /// <summary>
        /// 设备id
        /// </summary>
        public string airid { get; set; }
        /// <summary>
        /// 协议类型(1：正常 2：风机盘管3：中央空调4：NB风机盘管5：NB空调)
        /// </summary>
        public string charging { get; set; }
        /// <summary>
        /// 通道数
        /// </summary>
        public string route { get; set; }
        /// <summary>
        /// 设备编码
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// 设备名称
        /// </summary>
        public string devname { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        public int equiptype { get; set; }
        /// <summary>
        /// 设备最后通讯时间
        /// </summary>
        public string time { get; set; }

        /// <summary>
        /// 开关状态
        /// </summary>
        public List<int> switchList = new List<int>();

        /// <summary>
        /// 空调强开关状态
        /// </summary>
        public string airswitch { get; set; } = "";

        /// <summary>
        /// 开关状态（用来查询）1：关 2：开 3：强关 4：强开
        /// </summary>
        public int airswitchint { get; set; }

        /// <summary>
        /// 空调模式(0：自动1：制冷2：除湿3：送风4：制热5：睡眠)
        /// </summary>
        public string model { get; set; }
        /// <summary>
        /// 空调温度
        /// </summary>
        public string temperature { get; set; }
        /// <summary>
        /// 空调风速
        /// </summary>
        public string speed { get; set; }
        /// <summary>
        /// 空调风向
        /// </summary>
        public string direct { get; set; }

        /// <summary>
        /// 控制设备(uncontrol:不控制)
        /// </summary>
        public string deviceremark { get; set; }

        /// <summary>
        /// 实时状态
        /// </summary>
        public string realinfo { get; set; }
    }

    /// <summary>
    /// 设备基本信息
    /// </summary>
    public class DevInfoBase
    {
        /// <summary>
        /// 设备ID
        ///</summary>
        [DisplayName("设备ID")]
        public long EquipId { get; set; }
        /// <summary>
        /// 物联网平台设备id
        /// </summary>
        [DisplayName("物联网平台设备id")]
        public string IotDevId { get; set; }
        /// <summary>
        /// 设备名称
        /// </summary>
        [DisplayName("设备名称")]
        public string EquipName { get; set; }
        /// <summary>
        /// 设备编号
        ///</summary>
        [DisplayName("设备编号")]
        public string EquipCode { get; set; }
        /// <summary>
        /// 设备类型ID
        ///</summary>
        [DisplayName("设备类型ID")]
        public int EquipType { get; set; }
        /// <summary>
        /// 设备类型名称
        /// </summary>
        [DisplayName("设备类型名称")]
        public string EquipTypeName { get; set; }
        /// <summary>
        /// 设备型号名称
        /// </summary>
        [DisplayName("设备型号名称")]
        public string EquipTypeModel { get; set; }
        /// <summary>
        /// 设备子类型
        ///</summary>
        [DisplayName("设备子类型")]
        public int SubType { get; set; }
        /// <summary>
        /// 建筑ID
        ///</summary>
        [DisplayName("建筑ID")]
        public int BuildId { get; set; }
        /// <summary>
        /// 建筑名称
        ///</summary>
        [DisplayName("建筑名称")]
        public string BuildName { get; set; }
        /// <summary>
        /// 部门ID
        ///</summary>
        [DisplayName("部门ID")]
        public int DepartId { get; set; }
        /// <summary>
        /// 部门名称
        /// </summary>
        [DisplayName("部门名称")]
        public string DepartName { get; set; }

        /// <summary>
        /// 采集时间
        ///</summary>
        [DisplayName("采集时间")]
        public string CollectTime { get; set; }
        /// <summary>
        /// 设备状态0:正常 1:离线 
        ///</summary>
        [DisplayName("设备状态0:正常 1:离线 2:掉电 ")]
        public int EquipStatus { get; set; } = 1;
        /// <summary>
        /// 异常状态 0:正常 1:异常
        /// </summary>
        public int EquipAlarm { get; set; }
        /// <summary>
        /// 通道数量
        /// </summary>
        public int SubChannel { get; set; } = 1;
        /// <summary>
        /// 开关状态,多支路顺序表示，0关1开
        /// </summary>
        public List<string> SwitchList { get; set; } = new List<string>();

    }

    /// <summary>
    /// 电器控制器现况
    /// </summary>
    public class DxkzqRealData : DevInfoBase
    {
        /// <summary>
        /// 1路开关状态（1：开0：关）
        ///</summary>
        [DisplayName("1路开关状态（1：开0：关）")]
        public string StatusA { get; set; }
        /// <summary>
        /// 2路开关状态（1：开0：关）
        ///</summary>
        [DisplayName("2路开关状态（1：开0：关）")]
        public string StatusB { get; set; }
        /// <summary>
        /// 3路开关状态（1：开0：关）
        ///</summary>
        [DisplayName("3路开关状态（1：开0：关）")]
        public string StatusC { get; set; }
        /// <summary>
        /// 节点1电压V
        ///</summary>
        [DisplayName("节点1电压V")]
        public string VoltageA { get; set; }
        /// <summary>
        /// 节点2电压V
        ///</summary>
        [DisplayName("节点2电压V")]
        public string VoltageB { get; set; }
        /// <summary>
        /// 节点3电压V
        ///</summary>
        [DisplayName("节点3电压V")]
        public string VoltageC { get; set; }
        /// <summary>
        /// 节点1电流A
        ///</summary>
        [DisplayName("节点1电流A")]
        public string ElectricityA { get; set; }
        /// <summary>
        /// 节点2电流A
        ///</summary>
        [DisplayName("节点2电流A")]
        public string ElectricityB { get; set; }
        /// <summary>
        /// 节点3电流A
        ///</summary>
        [DisplayName("节点3电流A")]
        public string ElectricityC { get; set; }
        /// <summary>
        /// 总电流A
        ///</summary>
        [DisplayName("总电流A")]
        public string ElectricityTotal { get; set; }
        /// <summary>
        /// 节点1功率w
        ///</summary>
        [DisplayName("节点1功率w")]
        public string PowerA { get; set; }
        /// <summary>
        /// 节点2功率w
        ///</summary>
        [DisplayName("节点2功率w")]
        public string PowerB { get; set; }
        /// <summary>
        /// 节点3功率w
        ///</summary>
        [DisplayName("节点3功率w")]
        public string PowerC { get; set; }
        /// <summary>
        /// 总功率w
        ///</summary>
        [DisplayName("总功率w")]
        public string PowerTotal { get; set; }
        /// <summary>
        /// 节点1能耗kwh
        ///</summary>
        [DisplayName("节点1能耗kwh")]
        public string EnergyA { get; set; }
        /// <summary>
        /// 节点2能耗kwh
        ///</summary>
        [DisplayName("节点2能耗kwh")]
        public string EnergyB { get; set; }
        /// <summary>
        /// 节点3能耗kwh
        ///</summary>
        [DisplayName("节点3能耗kwh")]
        public string EnergyC { get; set; }
        /// <summary>
        /// 总能耗kwh
        ///</summary>
        [DisplayName("总能耗kwh")]
        public string EnergyTotal { get; set; }
        /// <summary>
        /// 节点1温度
        ///</summary>
        [DisplayName("节点1温度")]
        public string TempA { get; set; }
        /// <summary>
        /// 节点2温度
        ///</summary>
        [DisplayName("节点2温度")]
        public string TempB { get; set; }
        /// <summary>
        /// 节点3温度
        ///</summary>
        [DisplayName("节点3温度")]
        public string TempC { get; set; }

        ///// <summary>
        ///// 设备子类型（1：单相(基础版)2：三相(基础版)3：三路单相(基础版)）
        /////</summary>
        //[DisplayName("设备子类型")]
        //public int SubType { get; set; }

        /// <summary>
        /// 面板锁定状态（0，解锁 ；1，锁定）
        ///</summary>
        [DisplayName("面板锁定状态（0，解锁 ；1，锁定）")]
        public string LockedStatus { get; set; } = "--";
        /// <summary>
        /// 工作模式
        /// </summary>
        [DisplayName("工作模式")]
        public string WorkingMode { get; set; } = "--";

        /// <summary>
        /// 设备故障
        /// </summary>
        [DisplayName("设备故障")]
        public string FaultInfo { get; set; } = "--";

        /// <summary>
        /// 支路现况
        /// </summary>
        [DisplayName("支路现况")]
        public List<DxkzqSubRealData> SubDataList { get; set; } = new List<DxkzqSubRealData>();
    }

    /// <summary>
    /// 电气控制器支路数据
    /// </summary>
    public class DxkzqSubRealData
    {
        /// <summary>
        /// 支路id
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        /// 支路名称
        /// </summary>
        public string LineName { get; set; }
        /// <summary>
        /// 开关状态 0断开 1闭合
        /// </summary>
        public string SwitchStatus { get; set; }
        /// <summary>
        /// 电压
        /// </summary>
        public string Voltage { get; set; }
        /// <summary>
        /// 电流
        /// </summary>
        public string Elecity { get; set; }
        /// <summary>
        /// 功率
        /// </summary>
        public string Power { get; set; }
        /// <summary>
        /// 温度
        /// </summary>
        public string Temp { get; set; }
        /// <summary>
        /// 电量
        /// </summary>
        public string Energy { get; set; }
        /// <summary>
        /// 漏电阈值
        /// </summary>
        public string ElecLeakage { get; set; }
    }

    /// <summary>
    /// 雷达人感现况
    /// </summary>
    public class LdrgRealData : DevInfoBase
    {
        /// <summary>
        /// 人感状态 0无人 1有人
        /// </summary>
        public string HumanStatus { get; set; } = "--";
        //public int HumanCount { get; set; }
        /// <summary>
        /// 人感关联的设备名称集合
        /// </summary>
        public string EquipNameSet { get; set; }
    }

    /// <summary>
    /// 开关现况
    /// </summary>
    public class SwitchRealData : DevInfoBase
    {
        /// <summary>
        /// 支路实况
        /// </summary>
        public List<SwitchSubReal> SubRealList { get; set; } = new List<SwitchSubReal>();
    }

    public class SwitchSubReal
    {
        /// <summary>
        /// 支路id 支路1 2 3 4 
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        /// 支路名称
        /// </summary>
        public string LineName { get; set; }
        /// <summary>
        /// 开关状态 0断开 1闭合
        /// </summary>
        public int SwitchStatus { get; set; }
    }

    /// <summary>
    /// 插座现况
    /// </summary>
    public class SocketRealData : DevInfoBase
    {
        /// <summary>
        /// 电流
        /// </summary>
        public string Elecity { get; set; }
        /// <summary>
        /// 功率
        /// </summary>
        public string Power { get; set; }
        /// <summary>
        /// 环境温度
        /// </summary>
        public string AmbientTemp { get; set; }
        /// <summary>
        /// 板载温度
        /// </summary>
        public string BoardTemp { get; set; }
        /// <summary>
        /// 功率因数
        /// </summary>
        public string PowerFactor { get; set; }
    }

    /// <summary>
    /// 断路器现况
    /// </summary>
    public class CircuitRealData : DevInfoBase
    {

        /// <summary>
        /// 合闸状态 0分闸 1合闸
        /// </summary>
        public int SwitchStatus { get; set; }

        /// <summary>
        /// 远程合闸状态 0-启用 1-禁用
        /// </summary>
        public int RemoteSwitch { get; set; }

        /// <summary>
        /// 故障信息
        /// </summary>
        public string FaultInfo { get; set; }
        /// <summary>
        /// 电压
        /// </summary>
        public string Voltage { get; set; }
        /// <summary>
        /// 电流
        /// </summary>
        public string Elecity { get; set; }
        /// <summary>
        /// 功率
        /// </summary>
        public string Power { get; set; }
        /// <summary>
        /// 功率
        /// </summary>
        public string Temperature { get; set; }
        /// <summary>
        /// 电量
        /// </summary>
        public string Energy { get; set; }
        /// <summary>
        /// 漏电流
        /// </summary>
        public string ElecLeakage { get; set; }

        /// <summary>
        /// 三相现况
        /// </summary>
        public List<CircuitSubReal> SubRealList { get; set; } = new List<CircuitSubReal>();
    }

    /// <summary>
    /// 三相现况
    /// </summary>
    public class CircuitSubReal
    {
        /// <summary>
        /// 相位名称
        /// </summary>
        public string LineName { get; set; }
        /// <summary>
        /// 故障信息
        /// </summary>
        public string FaultInfo { get; set; }
        /// <summary>
        /// 电压
        /// </summary>
        public string Voltage { get; set; }
        /// <summary>
        /// 电流
        /// </summary>
        public string Elecity { get; set; }
        /// <summary>
        /// 功率
        /// </summary>
        public string Power { get; set; }
        /// <summary>
        /// 电量
        /// </summary>
        public string Energy { get; set; }
        /// <summary>
        /// 功率
        /// </summary>
        public string Temperature { get; set; }
        /// <summary>
        /// 漏电流
        /// </summary>
        public string ElecLeakage { get; set; }

    }

    /// <summary>
    /// 开关现况
    /// </summary>
    public class FansRealData : DevInfoBase
    {

        /// <summary>
        /// 变档 0关   123456挡  7全速 )
        /// </summary>
        public int SwitchStatus { get; set; }
    }

    /// <summary>
    /// 设备实时数据
    /// </summary>
    public class DevRealData
    {
        ///// <summary>
        ///// 节能实时数据
        ///// </summary>
        //public List<EquipmentDataReal> EquipRealList { get; set; } = new List<EquipmentDataReal>();
        ///// <summary>
        ///// 空调实时数据
        ///// </summary>
        //public List<Airrealinfo> AirRealList { get; set; } = new List<Airrealinfo>();
        /// <summary>
        /// 空调设备状态
        /// </summary>
        public List<StatusReal> AirStatusRealList { get; set; } = new List<StatusReal>();
        /// <summary>
        /// 节能设备状态
        /// </summary>
        public List<StatusReal> EquipStatusRealList { get; set; } = new List<StatusReal>();
    }

    /// <summary>
    /// 设备状态
    /// </summary>
    public class StatusReal
    {
        /// <summary>
        /// 空调编号
        ///</summary>
        [DisplayName("空调编号")]
        public int AirId { get; set; }
        /// <summary>
        /// 设备ID
        ///</summary>
        [DisplayName("设备ID")]
        public long EquipId { get; set; }
        /// <summary>
        /// 设备状态0:正常 1:离线 2:告警 
        ///</summary>
        [DisplayName("设备状态0:正常 1:离线 2:告警 ")]
        public int Status { get; set; }
    }

    /// <summary>
    /// 设备类型下拉框
    /// </summary>
    public class DeviceTypeSelect
    {
        /// <summary>
        /// 设备大类编码
        ///</summary>
        [DisplayName("设备大类编码")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备大类名称
        ///</summary>
        [DisplayName("设备大类名称")]
        public string DeviceTypeName { get; set; }
    }
}
