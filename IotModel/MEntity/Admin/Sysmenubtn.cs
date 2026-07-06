using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 菜单按钮关系表
    ///</summary>
    [DisplayName("菜单按钮关系表")]
    [EntityCache]
    [SugarTable(TableName = "sys_menu_btn", TableDescription = "菜单按钮关系表", IsDisabledUpdateAll = true)]
    public class SysMenuBtn : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 菜单ID
        ///</summary>
        [DisplayName("菜单ID")]
        [SugarColumn(ColumnName = "menu_id", Length = 20, ColumnDescription = "菜单ID", DefaultValue = "", ColumnDataType = "varchar")]
        public string MenuId { get; set; }
        /// <summary>
        /// 按钮ID
        ///</summary>
        [DisplayName("按钮ID")]
        [SugarColumn(ColumnName = "button_id", ColumnDescription = "按钮ID", DefaultValue = "0", ColumnDataType = "int")]
        public int ButtonId { get; set; }
        /// <summary>
        /// 页面中按钮排序
        ///</summary>
        [DisplayName("页面中按钮排序")]
        [SugarColumn(ColumnName = "mb_sort", ColumnDescription = "页面中按钮排序", DefaultValue = "1", ColumnDataType = "int")]
        public int MbSort { get; set; }
    }
}