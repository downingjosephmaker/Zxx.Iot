using SqlSugar;

namespace IotModel
{
    /// <summary>
    /// 用户搜索收藏实体
    /// </summary>
    [SugarTable("sys_user_search_favorite")]
    public class SysUserSearchFavorite : BaseEntity
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID")]
        public long Id { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 菜单编码
        /// </summary>
        [SugarColumn(ColumnName = "menu_url", Length = 100, IsNullable = false, ColumnDescription = "菜单编码")]
        public string MenuUrl { get; set; }

        /// <summary>
        /// 收藏名称
        /// </summary>
        [SugarColumn(ColumnName = "favorite_name", Length = 200, IsNullable = false, ColumnDescription = "收藏名称")]
        public string FavoriteName { get; set; }

        /// <summary>
        /// 搜索条件JSON
        /// </summary>
        [SugarColumn(ColumnName = "search_conditions", IsNullable = false, ColumnDataType = "TEXT", ColumnDescription = "搜索条件JSON")]
        public string SearchConditions { get; set; }

        /// <summary>
        /// 搜索使用次数
        /// </summary>
        [SugarColumn(ColumnName = "search_count", DefaultValue = "0", ColumnDescription = "搜索使用次数")]
        public int SearchCount { get; set; } = 0;

    }
}
