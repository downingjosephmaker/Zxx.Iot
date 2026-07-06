namespace IotWebApi.Areas.Control.Models
{
    /// <summary>
    /// 中控控制返回
    /// </summary>
    public class ZkResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// 失败说明
        /// </summary>
        public string Explain { get; set; }
    }
}
