using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 部门/建筑表拓展属性
    ///</summary>
    [DisplayName("部门/建筑表拓展属性")]
    [Expand]
    public class Expand_DeptBuild
    {
        /// <summary>
        /// 通知类型(短信|电话|邮件|微信)
        ///</summary>
        [DisplayName("通知类型(短信|电话|邮件|微信)")]
        public string NoteType { get; set; }
        /// <summary>
        /// 通知值集合
        ///</summary>
        [DisplayName("通知值集合")]
        public string NoteCollection { get; set; }
        /// <summary>
        /// 是否发送
        ///</summary>
        [DisplayName("是否发送")]
        public bool IsNoteSend { get; set; } = false;

    }
}
