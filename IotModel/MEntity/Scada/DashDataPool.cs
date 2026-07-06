using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 通用数据池
    /// </summary>
    [DisplayName("通用数据池")]
    [EntityCache]
    [SugarTable(TableName = "dash_data_pool", TableDescription = "通用数据池", IsDisabledUpdateAll = true)]
    public class DashDataPool : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        /// </summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }

        /// <summary>
        /// 数据集名称
        /// </summary>
        [DisplayName("数据集名称")]
        [SugarColumn(ColumnName = "data_name", Length = 100, ColumnDescription = "数据集名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string DataName { get; set; }

        /// <summary>
        /// 数据集路径/API地址
        /// </summary>
        [DisplayName("数据集路径")]
        [SugarColumn(ColumnName = "data_url", IsNullable = true, Length = 500, ColumnDescription = "数据集路径/API地址", DefaultValue = "", ColumnDataType = "varchar")]
        public string DataUrl { get; set; }

        /// <summary>
        /// 请求方法
        /// </summary>
        [DisplayName("请求方法")]
        [EnumRange(typeof(HttpMethodEnum), "请求方法值无效")]
        [SugarColumn(ColumnName = "request_method", IsNullable = false, ColumnDescription = "请求方法(1:GET 2:POST 3:PUT 4:DELETE)", DefaultValue = "1", ColumnDataType = "int")]
        public HttpMethodEnum RequestMethod { get; set; }

        /// <summary>
        /// 请求头配置
        /// </summary>
        [DisplayName("请求头配置")]
        [JsonField(typeof(Expand_DashDataPool_RequestHeaders))]
        [SugarColumn(ColumnName = "request_headers", IsNullable = true, ColumnDescription = "请求头配置", ColumnDataType = "text")]
        public string RequestHeaders { get; set; }

        /// <summary>
        /// 查询参数配置
        /// </summary>
        [DisplayName("查询参数配置")]
        [JsonField(typeof(Expand_DashDataPool_RequestParams))]
        [SugarColumn(ColumnName = "request_params", IsNullable = true, ColumnDescription = "查询参数配置", ColumnDataType = "text")]
        public string RequestParams { get; set; }

        /// <summary>
        /// POST请求体
        /// </summary>
        [DisplayName("POST请求体")]
        [SugarColumn(ColumnName = "request_body", IsNullable = true, ColumnDescription = "POST请求体", ColumnDataType = "text")]
        public string RequestBody { get; set; }

        /// <summary>
        /// 响应数据映射
        /// </summary>
        [DisplayName("响应数据映射")]
        [JsonField(typeof(Expand_DashDataPool_ResponseMapping))]
        [SugarColumn(ColumnName = "response_mapping", IsNullable = true, ColumnDescription = "响应数据映射", ColumnDataType = "text")]
        public string ResponseMapping { get; set; }

        /// <summary>
        /// 刷新间隔(毫秒)
        /// </summary>
        [DisplayName("刷新间隔")]
        [SugarColumn(ColumnName = "refresh_interval", IsNullable = false, ColumnDescription = "刷新间隔(毫秒)", DefaultValue = "60000", ColumnDataType = "int")]
        public int RefreshInterval { get; set; }

        /// <summary>
        /// 数据集描述
        /// </summary>
        [DisplayName("数据集描述")]
        [SugarColumn(ColumnName = "data_desc", IsNullable = true, Length = 500, ColumnDescription = "数据集描述", DefaultValue = "", ColumnDataType = "varchar")]
        public string DataDesc { get; set; }

        /// <summary>
        /// 单位ID
        /// </summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "unit_id", ColumnDescription = "单位ID", DefaultValue = "0", ColumnDataType = "int")]
        public int UnitId { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [DisplayName("是否启用")]
        [SugarColumn(ColumnName = "is_enabled", Length = 1, ColumnDescription = "是否启用", DefaultValue = "1", ColumnDataType = "bit")]
        public bool IsEnabled { get; set; }
    }

    /// <summary>
    /// HTTP请求方法
    /// </summary>
    public enum HttpMethodEnum
    {
        GET = 1,
        POST = 2,
        PUT = 3,
        DELETE = 4
    }
}
