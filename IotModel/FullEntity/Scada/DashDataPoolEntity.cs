using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 通用数据池完整类
    /// </summary>
    [DisplayName("通用数据池完整类")]
    [FullEntity]
    public class DashDataPoolEntity : DashDataPool
    {
        [DisplayName("请求头集合")]
        public List<Expand_DashDataPool_RequestHeaders> ExpandRequestHeaders { get; set; } = new();

        [DisplayName("查询参数集合")]
        public List<Expand_DashDataPool_RequestParams> ExpandRequestParams { get; set; } = new();

        [DisplayName("响应映射对象")]
        public Expand_DashDataPool_ResponseMapping ExpandResponseMapping { get; set; } = new();
    }
}
