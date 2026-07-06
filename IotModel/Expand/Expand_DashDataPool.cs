using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 请求头配置（直接作为数组元素）
    /// </summary>
    [DisplayName("请求头配置拓展类")]
    [Expand]
    public class Expand_DashDataPool_RequestHeaders
    {
        [DisplayName("头部键名")]
        public string Key { get; set; }

        [DisplayName("头部值")]
        public string Value { get; set; }

        [DisplayName("备注说明")]
        public string Remark { get; set; }
    }

    /// <summary>
    /// 查询参数配置（直接作为数组元素）
    /// </summary>
    [DisplayName("查询参数配置拓展类")]
    [Expand]
    public class Expand_DashDataPool_RequestParams
    {
        [DisplayName("参数键名")]
        public string Key { get; set; }

        [DisplayName("参数值/模板")]
        public string Value { get; set; }

        [DisplayName("参数类型")]
        [EnumRange(typeof(ParamTypeEnum), "参数类型值无效")]
        public ParamTypeEnum ParamType { get; set; }

        [DisplayName("是否必填")]
        public bool Required { get; set; }

        [DisplayName("备注说明")]
        public string Remark { get; set; }
    }

    /// <summary>
    /// 参数类型
    /// </summary>
    public enum ParamTypeEnum
    {
        字符串 = 1,
        整数 = 2,
        小数 = 3,
        布尔 = 4,
        日期 = 5
    }

    /// <summary>
    /// 响应数据映射（保留外层结构，含 DataPath + 字段映射数组）
    /// </summary>
    [DisplayName("响应数据映射拓展类")]
    [Expand]
    public class Expand_DashDataPool_ResponseMapping
    {
        [DisplayName("数据路径(JSONPath,如$.data.list)")]
        public string DataPath { get; set; }

        [DisplayName("字段映射集合")]
        public List<FieldMappingItem> Fields { get; set; } = new();
    }

    /// <summary>
    /// 单个字段映射
    /// </summary>
    [DisplayName("字段映射项")]
    public class FieldMappingItem
    {
        [DisplayName("源字段名(API响应中的字段)")]
        public string SourceField { get; set; }

        [DisplayName("目标字段名(输出到表格的字段)")]
        public string TargetField { get; set; }

        [DisplayName("字段类型")]
        [EnumRange(typeof(FieldTypeEnum), "字段类型值无效")]
        public FieldTypeEnum FieldType { get; set; }

        [DisplayName("转换公式(DynamicExpresso表达式)")]
        public string TransformExpression { get; set; }
    }

    /// <summary>
    /// 字段类型
    /// </summary>
    public enum FieldTypeEnum
    {
        字符串 = 1,
        整数 = 2,
        小数 = 3,
        布尔 = 4,
        日期 = 5
    }
}
