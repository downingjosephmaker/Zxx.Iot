using System;

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
                string sql = @"
INSERT INTO `sys_button` VALUES (2, 'btn_add', '新增按钮', NULL, 0, '新增按钮', 1, 1, '2024-08-06 16:36:42', '开发管理员', 1, '2024-08-06 16:36:42', '开发管理员');
INSERT INTO `sys_button` VALUES (3, 'btn_del', '删除按钮', NULL, 0, '删除按钮', 1, 1, '2024-08-06 18:02:13', '开发管理员', 1, '2024-08-06 18:02:13', '开发管理员');
INSERT INTO `sys_button` VALUES (4, 'btn_edit', '修改按钮', NULL, 0, '修改按钮', 1, 1, '2024-08-08 13:42:37', '开发管理员', 1, '2024-08-08 13:42:37', '开发管理员');
INSERT INTO `sys_button` VALUES (8, 'btn_report', '生成报表', NULL, 0, '报表模板页面上 生成报表 按钮', 1, 1, '2024-09-06 16:22:27', '开发管理员', 1, '2024-09-06 16:22:27', '开发管理员');
INSERT INTO `sys_button` VALUES (9, 'btn_bind', '绑定菜单按钮', NULL, 0, '给菜单绑定按钮', 1, 1, '2024-09-06 19:09:37', '开发管理员', 1, '2024-09-06 19:09:37', '开发管理员');
INSERT INTO `sys_button` VALUES (10, 'btn_import', '导入按钮', NULL, 0, '导入按钮', 1, 1, '2024-09-09 13:40:48', '开发管理员', 1, '2024-09-09 13:40:48', '开发管理员');
INSERT INTO `sys_button` VALUES (11, 'btn_export', '导出按钮', NULL, 0, '导出按钮', 1, 1, '2024-08-08 13:42:37', '开发管理员', 1, '2024-12-05 13:51:37', '开发管理员');
INSERT INTO `sys_button` VALUES (12, 'btn_zhbj', '组合报警', NULL, 0, '自定义告警页面的组合告警', 1, 1, '2024-09-11 16:46:29', '开发管理员', 1, '2024-09-11 16:46:29', '开发管理员');
INSERT INTO `sys_button` VALUES (13, 'btn_association', '关联按钮', NULL, 0, '关联设备使用', 1, 1, '2024-11-12 15:33:52', '开发管理员', 1, '2024-11-12 15:33:52', '开发管理员');
INSERT INTO `sys_button` VALUES (14, 'btn_update', '上传按钮', NULL, 0, '上传按钮', 1, 1, '2024-08-08 13:42:37', '开发管理员', 1, '2024-12-05 14:08:56', '开发管理员');
INSERT INTO `sys_button` VALUES (15, 'btn_apply', '特殊情况处理', NULL, 0, '申请与审批的特殊情况处理', 1, 1, '2024-12-09 16:15:20', '开发管理员', 1, '2024-12-09 16:15:20', '开发管理员');
INSERT INTO `sys_button` VALUES (16, 'btn_span', '审批', NULL, 0, '审批管理 审批按钮', 1, 1, '2024-08-08 13:42:37', '开发管理员', 1, '2024-12-12 10:35:56', '开发管理员');
                ";
                Db.Ado.ExecuteCommand(sql);
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