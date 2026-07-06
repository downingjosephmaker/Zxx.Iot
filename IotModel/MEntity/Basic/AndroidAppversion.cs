using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 软件升级
    ///</summary>
    [DisplayName("软件升级")]
    [SugarTable(TableName = "android_appversion", TableDescription = "软件升级", IsDisabledUpdateAll = true)]
    public class AndroidAppversion
    {
        /// <summary>
        /// 自增主键
        ///</summary>
        [DisplayName("自增主键")]
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "自增主键", DefaultValue = "0", ColumnDataType = "int")]
        public int Id { get; set; }
        /// <summary>
        /// 版本号
        ///</summary>
        [DisplayName("版本号")]
        [SugarColumn(ColumnName = "app_version", IsNullable = true, Length = 10, ColumnDescription = "版本号", DefaultValue = "", ColumnDataType = "varchar")]
        public string AppVersion { get; set; }
        /// <summary>
        /// 升级标志(1:升级 0:无)
        ///</summary>
        [DisplayName("升级标志(1:升级 0:无)")]
        [SugarColumn(ColumnName = "app_status", ColumnDescription = "升级标志(1:升级 0:无)", DefaultValue = "0", ColumnDataType = "int")]
        public int AppStatus { get; set; }
        /// <summary>
        /// 更新包下载地址
        ///</summary>
        [DisplayName("更新包下载地址")]
        [SugarColumn(ColumnName = "app_url", IsNullable = true, Length = 100, ColumnDescription = "更新包下载地址", DefaultValue = "", ColumnDataType = "varchar")]
        public string AppUrl { get; set; }
        /// <summary>
        /// 包名
        ///</summary>
        [DisplayName("包名")]
        [SugarColumn(ColumnName = "app_package", IsNullable = true, Length = 100, ColumnDescription = "包名", DefaultValue = "", ColumnDataType = "varchar")]
        public string AppPackage { get; set; }
    }
}