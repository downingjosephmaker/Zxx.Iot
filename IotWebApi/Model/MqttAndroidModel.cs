using SqlSugar;
using System.ComponentModel;

namespace IotWebApi
{
    /// <summary>
    /// Mqtt通讯一体机模型
    /// </summary>
    [DisplayName("Mqtt通讯一体机模型")]
    public class MqttAndroidModel
    {
        /// <summary>
        /// 通讯类型(3:告警信息)
        ///</summary>
        [DisplayName("通讯类型")]
        public int DataType { get; set; }

        /// <summary>
        /// 操作分类(
        /// 1:新增告警
        /// 2:删除告警
        ///</summary>
        [DisplayName("操作分类")]
        public int OptType { get; set; }

        /// <summary>
        /// 具体内容(参照获取接口内容)
        ///</summary>
        [DisplayName("具体内容(参照获取接口内容)")]
        public string DataContent { get; set; }

    }
}
