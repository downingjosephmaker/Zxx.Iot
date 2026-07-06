namespace IotWebApi.Areas.Admin.Models
{
    public class AlarmWebInfo
    {
        /// <summary>
        /// 雪花主键
        /// </summary>
        public string snowId { get; set; }
        /// <summary>
        /// 设备id
        /// </summary>
        public int deviceId { get; set; }
        /// <summary>
        /// 设备名称
        /// </summary>
        public string deviceName { get; set; }
        /// <summary>
        /// 建筑id
        /// </summary>
        public int buildId { get; set; }
        /// <summary>
        /// 建筑名称
        /// </summary>
        public string buildName { get; set; }
        /// <summary>
        /// 报警等级
        /// </summary>
        public int alarmLevel { get; set; }
        /// <summary>
        /// 报警类型
        /// </summary>
        public int alarmType { get; set; }
        /// <summary>
        /// 事件类型
        /// </summary>
        public string eventType { get; set; }
        /// <summary>
        /// 参数id
        /// </summary>
        public int paramId { get; set; }
        /// <summary>
        /// 参数编码
        /// </summary>
        public string alarmCode { get; set; }
        /// <summary>
        /// 参数名称
        /// </summary>
        public string alarmName { get; set; }
        /// <summary>
        /// 报警时间
        /// </summary>
        public string alarmTime { get; set; }
        /// <summary>
        /// 报警内容
        /// </summary>
        public string alarmValue { get; set; }
    }

}
