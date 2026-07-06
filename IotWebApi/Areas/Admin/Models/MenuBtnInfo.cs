using System.ComponentModel;

namespace IotWebApi.Areas.Admin.Models
{
    public class MenuBtnInfo
    {
        /// <summary>
        /// 菜单ID
        ///</summary>
        [DisplayName("菜单ID")]
        public string MenuId { get; set; }
        /// <summary>
        /// 页面中按钮排序
        ///</summary>
        [DisplayName("页面中按钮排序")]
        public int MbSort { get; set; }
        /// <summary>
        /// 按钮ID
        ///</summary>
        [DisplayName("按钮ID")]
        public int ButtonId { get; set; }
        /// <summary>
        /// 按钮编码
        ///</summary>
        [DisplayName("按钮编码")]
        public string ButtonCode { get; set; }
        /// <summary>
        /// 按钮名称
        ///</summary>
        [DisplayName("按钮名称")]
        public string ButtonName { get; set; }
        /// <summary>
        /// 按钮Html
        ///</summary>
        [DisplayName("按钮Html")]
        public string ButtonHtml { get; set; }
        /// <summary>
        /// 1:页面按钮  2:表单按钮
        ///</summary>
        [DisplayName("1:页面按钮  2:表单按钮")]
        public int ButtonType { get; set; }
    }
}
