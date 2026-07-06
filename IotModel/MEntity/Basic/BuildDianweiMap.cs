using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 建筑点位图
    ///</summary>
    [DisplayName("建筑点位图")]
    [EntityCache]
    [SugarTable(TableName = "build_dianwei_map", TableDescription = "建筑点位图", IsDisabledUpdateAll = true)]
    public class BuildDianweiMap : BaseEntity
    {
        /// <summary>
        /// 建筑ID
        ///</summary>
        [DisplayName("建筑ID")]
        [SugarColumn(ColumnName = "build_id", IsPrimaryKey = true, ColumnDescription = "建筑ID", DefaultValue = "0", ColumnDataType = "int")]
        public int BuildId { get; set; }
        /// <summary>
        /// 文件名称
        ///</summary>
        [DisplayName("文件名称")]
        [SugarColumn(ColumnName = "file_name", Length = 200, ColumnDescription = "文件名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string FileName { get; set; }
        /// <summary>
        /// 文件路径
        ///</summary>
        [DisplayName("文件路径")]
        [SugarColumn(ColumnName = "file_path", IsNullable = true, Length = 500, ColumnDescription = "文件路径", DefaultValue = "", ColumnDataType = "varchar")]
        public string FilePath { get; set; }
        /// <summary>
        /// 文件长度
        ///</summary>
        [DisplayName("文件长度")]
        [SugarColumn(ColumnName = "file_length", Length = 20, ColumnDescription = "文件长度", DefaultValue = "0", ColumnDataType = "bigint")]
        public long FileLength { get; set; }
        /// <summary>
        /// 点位内容
        ///</summary>
        [DisplayName("点位内容")]
        [JsonField(typeof(Expand_BuildDianweiMap))]
        [SugarColumn(ColumnName = "map_config", IsNullable = true, ColumnDescription = "点位内容", ColumnDataType = "text")]
        public string MapConfig { get; set; }
    }
}