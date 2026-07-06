namespace IotWebApi.Areas.Basic.Models
{
    /// <summary>
    /// 水电平衡树形结构
    /// </summary>
    public class BalanceTree
    {
        /// <summary>
        /// 节点唯一编码
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// 节点名称
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 节点文本
        /// </summary>
        public string value { get; set; }
        /// <summary>
        /// 节点数值
        /// </summary>
        public double valueNum { get; set; }
        /// <summary>
        /// 级别
        /// </summary>
        public int treeLevel { get; set; }
        /// <summary>
        /// 平衡率
        /// </summary>
        public string balanceValue { get; set; }
        /// <summary>
        /// 节点上级编码
        /// </summary>
        public string pid { get; set; }

    }

    /// <summary>
    /// 水电平衡结果
    /// </summary>
    public class BalanceChart
    {
        /// <summary>
        /// 节点名称
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 节点文本
        /// </summary>
        public string value { get; set; }
        /// <summary>
        /// 级别
        /// </summary>
        public int treeLevel { get; set; }
        /// <summary>
        /// 平衡率
        /// </summary>
        public string balanceValue { get; set; }
        /// <summary>
        /// 设备名称集合
        /// </summary>
        public string devnames { get; set; }
        /// <summary>
        /// 设备名称集合
        /// </summary>
        public string subdevnames { get; set; }

    }
}
