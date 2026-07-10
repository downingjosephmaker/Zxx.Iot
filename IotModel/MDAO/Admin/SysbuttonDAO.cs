using System;
using System.Collections.Generic;

namespace IotModel
{
    public sealed partial class SysButtonDAO : DbContext<SysButton>
    {
        private static SysButtonDAO instance;
        public static SysButtonDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SysButtonDAO();
                }
                return instance;
            }
        }
        public override void Init()
        {
            try
            {
                // 种子数据：参数化插入(跨方言,PG/MySQL 通用)，替换原 MySQL 反引号原生 SQL(PG 下语法错误被吞)。
                // ButtonId 显式赋值原因：SysButton.ButtonId 是 IsIdentity 自增主键，但 sys_menu_btn 种子
                // (SysMenuBtnDAO.Init) 的 button_id 列硬编码引用了 2/3/4/9/10/11 等具体按钮ID，
                // 必须保持一致，否则菜单-按钮关系错位。
                // 关键：自增主键默认会被 SqlSugar 从 INSERT 列中剔除(由 DB 生成)，
                // 因此不能用基类 InsertRange(它走普通 Insertable)，必须 .OffIdentity() 强制写入显式ID。
                var list = new List<SysButton>
                {
                    new SysButton { ButtonId = 2,  ButtonCode = "btn_add",         ButtonName = "新增按钮",     ButtonHtml = null, ButtonSort = 0, ButtonRemark = "新增按钮",                     ButtonType = 1, CreateId = 1, CreateTime = "2024-08-06 16:36:42", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-08-06 16:36:42", UpdateName = "开发管理员" },
                    new SysButton { ButtonId = 3,  ButtonCode = "btn_del",         ButtonName = "删除按钮",     ButtonHtml = null, ButtonSort = 0, ButtonRemark = "删除按钮",                     ButtonType = 1, CreateId = 1, CreateTime = "2024-08-06 18:02:13", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-08-06 18:02:13", UpdateName = "开发管理员" },
                    new SysButton { ButtonId = 4,  ButtonCode = "btn_edit",        ButtonName = "修改按钮",     ButtonHtml = null, ButtonSort = 0, ButtonRemark = "修改按钮",                     ButtonType = 1, CreateId = 1, CreateTime = "2024-08-08 13:42:37", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-08-08 13:42:37", UpdateName = "开发管理员" },
                    new SysButton { ButtonId = 8,  ButtonCode = "btn_report",      ButtonName = "生成报表",     ButtonHtml = null, ButtonSort = 0, ButtonRemark = "数据模块页面上 生成报表 按钮",   ButtonType = 1, CreateId = 1, CreateTime = "2024-09-06 16:22:27", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-09-06 16:22:27", UpdateName = "开发管理员" },
                    new SysButton { ButtonId = 9,  ButtonCode = "btn_bind",        ButtonName = "绑定菜单按钮", ButtonHtml = null, ButtonSort = 0, ButtonRemark = "给菜单绑定按钮",               ButtonType = 1, CreateId = 1, CreateTime = "2024-09-06 19:09:37", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-09-06 19:09:37", UpdateName = "开发管理员" },
                    new SysButton { ButtonId = 10, ButtonCode = "btn_import",      ButtonName = "导入按钮",     ButtonHtml = null, ButtonSort = 0, ButtonRemark = "导入按钮",                     ButtonType = 1, CreateId = 1, CreateTime = "2024-09-09 13:40:48", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-09-09 13:40:48", UpdateName = "开发管理员" },
                    new SysButton { ButtonId = 11, ButtonCode = "btn_export",      ButtonName = "导出按钮",     ButtonHtml = null, ButtonSort = 0, ButtonRemark = "导出按钮",                     ButtonType = 1, CreateId = 1, CreateTime = "2024-08-08 13:42:37", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-12-05 13:51:37", UpdateName = "开发管理员" },
                    new SysButton { ButtonId = 12, ButtonCode = "btn_zhbj",        ButtonName = "组合报警",     ButtonHtml = null, ButtonSort = 0, ButtonRemark = "自定义告警页面组合告警",         ButtonType = 1, CreateId = 1, CreateTime = "2024-09-11 16:46:29", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-09-11 16:46:29", UpdateName = "开发管理员" },
                    new SysButton { ButtonId = 13, ButtonCode = "btn_association", ButtonName = "关联按钮",     ButtonHtml = null, ButtonSort = 0, ButtonRemark = "关联设备使用",                 ButtonType = 1, CreateId = 1, CreateTime = "2024-11-12 15:33:52", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-11-12 15:33:52", UpdateName = "开发管理员" },
                    new SysButton { ButtonId = 14, ButtonCode = "btn_update",      ButtonName = "上传按钮",     ButtonHtml = null, ButtonSort = 0, ButtonRemark = "上传按钮",                     ButtonType = 1, CreateId = 1, CreateTime = "2024-08-08 13:42:37", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-12-05 14:08:56", UpdateName = "开发管理员" },
                    new SysButton { ButtonId = 15, ButtonCode = "btn_apply",       ButtonName = "申请审批",     ButtonHtml = null, ButtonSort = 0, ButtonRemark = "申请审批任务相关操作",         ButtonType = 1, CreateId = 1, CreateTime = "2024-12-09 16:15:20", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-12-09 16:15:20", UpdateName = "开发管理员" },
                    new SysButton { ButtonId = 16, ButtonCode = "btn_span",        ButtonName = "跨度",         ButtonHtml = null, ButtonSort = 0, ButtonRemark = "数据跨度 查询按钮",             ButtonType = 1, CreateId = 1, CreateTime = "2024-08-08 13:42:37", CreateName = "开发管理员", UpdateId = 1, UpdateTime = "2024-12-12 10:35:56", UpdateName = "开发管理员" },
                };
                // SeedOffIdentity 强制写入显式主键值(2..16)保证与 sys_menu_btn 引用一致,并同步 PG 序列。
                SeedOffIdentity(list);
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
            }
        }

    }
}
