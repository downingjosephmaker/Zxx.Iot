namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 中台设备类型json
    /// </summary>
    public class ZtTypeJson
    {
        /// <summary>
        /// 类型id
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// 类型名称
        /// </summary>
        public string modelName { get; set; }
        /// <summary>
        /// 类型描述
        /// </summary>
        public string description { get; set; }
        /// <summary>
        /// 通讯方式(MODBUS_TCP)
        /// </summary>
        public string provider { get; set; }
        /// <summary>
        /// 设备类型参数集合
        /// </summary>
        public string metadata { get; set; }
        /// <summary>
        /// 创建人ID
        /// </summary>
        public string creatorId { get; set; }
        /// <summary>
        /// 创建时间戳
        /// </summary>
        public long createTime { get; set; }
    }

    public class PointEntity
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public String name;

        /// <summary>
        /// 参数编码
        /// </summary>
        public String code;

        /// <summary>
        /// 说明
        /// </summary>
        public String description;

        /// <summary>
        /// 通讯方式(MODBUS_TCP)
        /// </summary>
        public String provider;
        /// <summary>
        /// 点位地址
        /// </summary>
        public String pointKey;

        /// <summary>
        /// 配置(根据类型不同而不同)
        /// </summary>
        public Configuration configuration;

        /// <summary>
        /// 采样时间
        /// </summary>
        public long interval;

        /// <summary>
        /// 特征
        /// </summary>
        public String[] features;

        /// <summary>
        /// 
        /// </summary>
        public string creatorIdProperty;

        /// <summary>
        /// 操作类型
        /// </summary>
        public List<AccessModesItem> accessModes;
    }

    public class AccessModesItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string text { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string value { get; set; }
    }

    public class Configuration
    {
        /// <summary>
        /// 
        /// </summary>
        public Codec codec { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string function { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Parameter parameter { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int interval { get; set; }
    }

    public class Codec
    {
        /// <summary>
        /// 
        /// </summary>
        public Configuration configuration { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string provider { get; set; }
    }

    public class Parameter
    {
        /// <summary>
        /// 
        /// </summary>
        public int quantity { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int address { get; set; }
    }

}
