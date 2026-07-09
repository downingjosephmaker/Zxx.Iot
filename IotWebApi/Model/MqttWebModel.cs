using SqlSugar;
using System.Collections.Generic;
using System.ComponentModel;
using IotModel;

namespace IotWebApi
{
    /// <summary>
    /// Mqtt通讯Web和APP模型
    /// </summary>
    [DisplayName("Mqtt通讯Web和APP模型")]
    public class MqttWebModel
    {
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        public int UnitId { get; set; }
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        public string DeviceTypeCode { get; set; }
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
        /// 记录时间
        ///</summary>
        [DisplayName("记录时间")]
        public string EventTime { get; set; }
        /// <summary>
        /// 设备通讯状态(2:在线;1:掉电;0:离线)
        ///</summary>
        [DisplayName("设备通讯状态(2:在线;1:掉电;0:离线)")]
        public int DeviceState { get; set; }
        /// <summary>
        /// 设备状态：开关(0:关闭 1:开启)/有无人(0无人 1有人)
        ///</summary>
        [DisplayName("设备状态：开关(0:关闭 1:开启 2: 强关 3:强开)/有无人(0无人 1有人)")]
        public int DeviceSwitch { get; set; }
        /// <summary>
        /// 告警状态(0:正常1:告警)
        ///</summary>
        [DisplayName("告警状态")]
        public int AlarmStatus { get; set; }
        /// <summary>
        /// 支路开关状态（0断开 1闭合）
        /// </summary>
        [DisplayName("支路开关状态（0断开 1闭合）")]
        public List<SubChannelStatus> OnOffList { get; set; } = new List<SubChannelStatus>();

        /// <summary>
        /// 设备实时状态参数集合
        /// </summary>
        [DisplayName("设备实时状态参数集合")]
        public List<ShowParam> DevRealList { get; set; } = new();
        /// <summary>
        /// 额外推送内容
        ///</summary>
        [DisplayName("额外推送内容")]
        public string EwContent { get; set; }
    }

    /// <summary>
    /// 支路开关状态
    /// </summary>
    public class SubChannelStatus
    {
        /// <summary>
        /// 支路序号 1-1路 2-2路 3-3路 4-4路
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        /// 开关支路状态 0关 1开 -1不变
        /// </summary>
        public string OnOff { get; set; } = "不变";


        /// <summary>
        /// 新建状态
        /// </summary>
        public SubChannelStatus()
        {
            
        }

        /// <summary>
        /// 新建状态
        /// </summary>
        /// <param name="line">支路序号</param>
        /// <param name="onoff">开关状态</param>
        public SubChannelStatus(int line, string onoff)
        {
            this.Line = line;
            this.OnOff = onoff;
        }
    }

    /// <summary>
    /// 监控卡片展示参数
    /// </summary>
    public class ShowParam
    {
        /// <summary>
        /// 设备路数(总路,1路/A,2路/B,3路/C)
        ///</summary>
        [DisplayName("设备路数(总路,1路/A,2路/B,3路/C)")]
        public string SubChannel { get; set; } = "总路";
        /// <summary>
        /// 参数编码
        ///</summary>
        [DisplayName("参数编码")]
        public string ParamCode { get; set; }
        /// <summary>
        /// 参数名称
        ///</summary>
        [DisplayName("参数名称")]
        public string ParamName { get; set; }
        /// <summary>
        /// 值单位
        ///</summary>
        [DisplayName("值单位")]
        public string ValueUnit { get; set; }
        /// <summary>
        /// 参数值
        ///</summary>
        [DisplayName("参数值")]
        public string ParamValue { get; set; }
        /// <summary>
        /// 是否主显示(0:否1:是)
        ///</summary>
        [DisplayName("是否主显示(0:否1:是)")]
        public bool IsMainShow { get; set; }
    }
}
