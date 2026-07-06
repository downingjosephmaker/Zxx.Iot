using System;
using System.ComponentModel;

namespace IotWebApi.Model
{
    /// <summary>
    /// 空调信息推送模型
    /// </summary>
    public class MqttAirWebInfo
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        [DisplayName("设备ID")]
        public int DeviceId { get; set; }

        /// <summary>
        /// 采集时间
        ///</summary>
        [DisplayName("采集时间")]
        public string CreateTime { get; set; }

        /// <summary>
        /// 空调模式(自动,制冷,除湿,送风,制热)
        ///</summary>
        [DisplayName("空调模式(自动,制冷,除湿,送风,制热)")]
        public string AirModel { get; set; }

        /// <summary>
        /// 空调设定温度
        ///</summary>
        [DisplayName("空调设定温度")]
        public string AirModelTemp { get; set; }

        /// <summary>
        /// 空调风速(自动,低速,中速,高速)
        ///</summary>
        [DisplayName("空调风速(自动,低速,中速,高速)")]
        public string AirSpeed { get; set; }

        /// <summary>
        /// 空调风向(自动摆风,手动摆风)
        ///</summary>
        [DisplayName("空调风向(自动摆风,手动摆风)")]
        public string AirDirection { get; set; }

        /// <summary>
        /// 开关机状态(关机,开机)
        ///</summary>
        [DisplayName("开关机状态(关机,开机)")]
        public string AirSwitch { get; set; }

        /// <summary>
        /// 控制模式(手动,强制关,强制开)
        ///</summary>
        [DisplayName("控制模式(手动,强制关,强制开)")]
        public string SwitchModel { get; set; }

        /// <summary>
        /// 环境温度
        ///</summary>
        [DisplayName("环境温度")]
        public string EnvirTemp { get; set; }

        /// <summary>
        /// 环境舒适度
        ///</summary>
        [DisplayName("环境舒适度")]
        public string EnvirComfort { get; set; }

        /// <summary>
        /// 视在功率
        ///</summary>
        [DisplayName("视在功率")]
        public string ApparentPower { get; set; }

        /// <summary>
        /// 功率因素
        ///</summary>
        [DisplayName("功率因素")]
        public string PowerFactor { get; set; }

        /// <summary>
        /// 工作电流
        ///</summary>
        [DisplayName("工作电流")]
        public string AirElec { get; set; }

        /// <summary>
        /// 空调功率
        ///</summary>
        [DisplayName("空调功率")]
        public string AirPower { get; set; }

        /// <summary>
        /// 空调锁定(解锁,锁定)
        ///</summary>
        [DisplayName("空调锁定(解锁,锁定)")]
        public string AirLock { get; set; }

        /// <summary>
        /// 人感信息(无人,有人,故障,不显示)
        ///</summary>
        [DisplayName("人感信息(无人,有人,故障,不显示)")]
        public string Human { get; set; } = "不显示";

        /// <summary>
        /// 温度开机允许(不允许,允许)
        ///</summary>
        [DisplayName("温度开机允许(不允许,允许)")]
        public string TempEnable { get; set; }

        /// <summary>
        /// 工作模式(调温,人感,温度,时间,手动,计量,断电,省电,定时关机,冬季)"
        ///</summary>
        [DisplayName("工作模式(调温,人感,温度,时间,手动,计量,断电,省电,定时关机,冬季)")]
        public string WorkModel { get; set; }

    }

    /// <summary>
    /// 智慧插座信息推送模型(集合)
    /// </summary>
    public class MqttZhczWebInfo
    {
        /// <summary>
        /// 设备路数
        ///</summary>
        [DisplayName("设备路数")]
        public int DeviceChannel { get; set; }
        /// <summary>
        /// 采集时间
        ///</summary>
        [DisplayName("采集时间")]
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 设备开关(0:关闭1:开启)
        ///</summary>
        [DisplayName("设备开关(0:关闭1:开启)")]
        public int SwitchStatus { get; set; }
        /// <summary>
        /// 电流
        /// </summary>
        [DisplayName("电流")]
        public string Elecity { get; set; }
        /// <summary>
        /// 功率
        /// </summary>
        [DisplayName("功率")]
        public string Power { get; set; }
        /// <summary>
        /// 环境温度
        /// </summary>
        [DisplayName("环境温度")]
        public string AmbientTemp { get; set; }
        /// <summary>
        /// 板载温度
        /// </summary>
        [DisplayName("板载温度")]
        public string BoardTemp { get; set; }
        /// <summary>
        /// 功率因数
        /// </summary>
        [DisplayName("功率因数")]
        public string PowerFactor { get; set; }
    }

    /// <summary>
    /// 智能电气控制器信息推送模型(集合)
    /// </summary>
    public class MqttZhkkWebInfo
    {
        /// <summary>
        /// 设备路数
        ///</summary>
        [DisplayName("设备路数")]
        public int DeviceChannel { get; set; }
        /// <summary>
        /// 采集时间
        ///</summary>
        [DisplayName("采集时间")]
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 设备开关(0:关闭1:开启)
        ///</summary>
        [DisplayName("设备开关(0:关闭1:开启)")]
        public int SwitchStatus { get; set; }
        /// <summary>
        /// 电压
        /// </summary>
        public string Voltage { get; set; }
        /// <summary>
        /// 电流
        /// </summary>
        [DisplayName("电流")]
        public string Elecity { get; set; }
        /// <summary>
        /// 功率
        /// </summary>
        [DisplayName("功率")]
        public string Power { get; set; }
        /// <summary>
        /// 温度
        /// </summary>
        [DisplayName("温度")]
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

}
