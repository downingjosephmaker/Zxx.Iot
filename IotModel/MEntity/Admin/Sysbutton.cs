using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 平台按钮管理
    ///</summary>
    [DisplayName("平台按钮管理")]
    [EntityCache]
    [SugarTable(TableName = "sys_button", TableDescription = "平台按钮管理", IsDisabledUpdateAll = true)]
    public class SysButton : BaseEntity
    {
        /// <summary>
        /// 按钮ID
        ///</summary>
        [DisplayName("按钮ID")]
        [SugarColumn(ColumnName = "button_id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "按钮ID", DefaultValue = "0", ColumnDataType = "int")]
        public int ButtonId { get; set; }
        /// <summary>
        /// 按钮编码
        ///</summary>
        [DisplayName("按钮编码")]
        [SugarColumn(ColumnName = "button_code", IsNullable = true, Length = 50, ColumnDescription = "按钮编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string ButtonCode { get; set; }
        /// <summary>
        /// 按钮名称
        ///</summary>
        [DisplayName("按钮名称")]
        [SugarColumn(ColumnName = "button_name", IsNullable = true, Length = 100, ColumnDescription = "按钮名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string ButtonName { get; set; }
        /// <summary>
        /// 按钮Html
        ///</summary>
        [DisplayName("按钮Html")]
        [SugarColumn(ColumnName = "button_html", IsNullable = true, Length = 2000, ColumnDescription = "按钮Html", DefaultValue = "", ColumnDataType = "varchar")]
        public string ButtonHtml { get; set; }
        /// <summary>
        /// 按钮排序
        ///</summary>
        [DisplayName("按钮排序")]
        [SugarColumn(ColumnName = "button_sort", ColumnDescription = "按钮排序", DefaultValue = "0", ColumnDataType = "int")]
        public int ButtonSort { get; set; }
        /// <summary>
        /// 按钮备注
        ///</summary>
        [DisplayName("按钮备注")]
        [SugarColumn(ColumnName = "button_remark", IsNullable = true, Length = 500, ColumnDescription = "按钮备注", DefaultValue = "", ColumnDataType = "varchar")]
        public string ButtonRemark { get; set; }
        /// <summary>
        /// 1:页面按钮  2:表单按钮
        ///</summary>
        [DisplayName("1:页面按钮  2:表单按钮")]
        [SugarColumn(ColumnName = "button_type", ColumnDescription = "1:页面按钮  2:表单按钮", DefaultValue = "1", ColumnDataType = "int")]
        public int ButtonType { get; set; }
    }
}