using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    public class BaseEntity
    {
        /// <summary>
        /// 创建用户ID
        ///</summary>
        [DisplayName("创建用户ID")]
        [SugarColumn(ColumnName = "create_id", ColumnDescription = "创建用户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int CreateId { get; set; }
        /// <summary>
        /// 创建时间
        ///</summary>
        [DisplayName("创建时间")]
        [SugarColumn(ColumnName = "create_time", IsNullable = true, Length = 20, ColumnDescription = "创建时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string CreateTime { get; set; }
        /// <summary>
        /// 创建用户名称
        ///</summary>
        [DisplayName("创建用户名称")]
        [SugarColumn(ColumnName = "create_name", IsNullable = true, Length = 50, ColumnDescription = "创建用户名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string CreateName { get; set; }
        /// <summary>
        /// 修改用户ID
        ///</summary>
        [DisplayName("修改用户ID")]
        [SugarColumn(ColumnName = "update_id", ColumnDescription = "修改用户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int UpdateId { get; set; }
        /// <summary>
        /// 修改时间
        ///</summary>
        [DisplayName("修改时间")]
        [SugarColumn(ColumnName = "update_time", IsNullable = true, Length = 20, ColumnDescription = "修改时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string UpdateTime { get; set; }
        /// <summary>
        /// 修改用户名称
        ///</summary>
        [DisplayName("修改用户名称")]
        [SugarColumn(ColumnName = "update_name", IsNullable = true, Length = 50, ColumnDescription = "修改用户名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string UpdateName { get; set; }
    }
}
