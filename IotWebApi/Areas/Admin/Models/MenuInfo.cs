using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace IotWebApi.Areas.Admin.Models
{
    /// <summary>
    /// 菜单模型
    /// </summary>
    public class MenuInfo
    {
        /// <summary>
        /// 菜单ID
        ///</summary>
        public string menuid { get; set; }
        /// <summary>
        /// 菜单地址
        ///</summary>
        public string path { get; set; }
        /// <summary>
        /// 菜单编码
        ///</summary>
        public string name { get; set; }
        /// <summary>
        /// 组件路径(相对src/views,如 iot/center/index.vue)。
        /// 目录节点为null:前端 addAsyncRoutes 会把动态路由拍平后统一挂到根路由(Layout)下,目录只用于菜单树渲染。
        ///</summary>
        public string component { get; set; }
        /// <summary>
        /// 菜单属性
        ///</summary>
        public MetaInfo meta { get; set; } = new MetaInfo();
        /// <summary>
        /// 子菜单
        ///</summary>
        public List<MenuInfo> children { get; set; } = null;
    }

    /// <summary>
    /// 菜单属性
    /// </summary>
    public class MetaInfo
    {
        /// <summary>
        /// 菜单标题
        ///</summary>
        public string title { get; set; }
        /// <summary>
        /// 菜单图标
        ///</summary>
        public string icon { get; set; }
        /// <summary>
        /// 菜单排序。必须是数字:前端 ascending() 做的是 a.meta.rank - b.meta.rank,
        /// 下发 SortBorder 原值("A002")会得到 NaN,顶级菜单排序静默失效。
        ///</summary>
        public int? rank { get; set; }
        /// <summary>
        /// 是否显示菜单中
        ///</summary>
        public bool showLink { get; set; }
        /// <summary>
        /// 菜单按钮权限
        ///</summary>
        public List<string> auths { get; set; } = new List<string>();
        /// <summary>
        /// 菜单按钮
        ///</summary>
        public List<BtnInfo> btns { get; set; } = new List<BtnInfo>();
        /// <summary>
        /// sys_menu.meta_json 里的自定义路由元数据(如 projectKind)。
        /// [JsonExtensionData] 使其在序列化时平铺进 meta 而非嵌套成子对象。
        ///</summary>
        [JsonExtensionData]
        public Dictionary<string, object> extra { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 权限菜单和按钮
    /// </summary>
    public class RoleMenuBtn
    {
        /// <summary>
        /// 角色ID
        ///</summary>
        [DisplayName("角色ID")]
        public int RoleId { get; set; }
        /// <summary>
        /// 菜单ID
        ///</summary>
        [DisplayName("菜单ID")]
        public string MenuId { get; set; }
        /// <summary>
        /// 按钮ID
        ///</summary>
        [DisplayName("按钮ID")]
        public int ButtonId { get; set; }
    }

    /// <summary>
    /// 权限菜单和按钮
    /// </summary>
    public class BtnInfo
    {
        /// <summary>
        /// 按钮ID
        ///</summary>
        [DisplayName("按钮ID")]
        public int ButtonId { get; set; }
        /// <summary>
        /// 按钮名称
        ///</summary>
        [DisplayName("按钮名称")]
        public string ButtonName { get; set; }
    }

}
