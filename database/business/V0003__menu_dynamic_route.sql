-- ============================================================================
-- 菜单表改造：新增 component / meta_json 两列，并用与前端路由一一对应的真实菜单重建种子
--
-- 背景：
--   sys_menu 原有 15 条是从旧项目（楼宇/环境监测）带过来的幽灵数据，menu_url 指向
--   /record/alarmLog/index、/environmentalMonitor/environmentalMap/index 等
--   vuefrontend 里根本不存在的页面；menu_code 也与前端路由 name 对不上
--   （System vs SystemManage、buttonManagement vs SystemButton）。
--   这正是后端动态路由一直被注释掉的原因 —— 一旦开启，侧边栏会全线 404。
--
-- 本脚本：
--   1. 加 component（Vue 组件路径）/ meta_json（自定义路由 meta）两列
--   2. 重建 25 条真实菜单，与 router/modules 的 iot/system/scada/report 一一对应
--      （首页 / 与 /welcome 不入表：由 router/modules/home.ts 静态提供，是动态路由的挂载容器）
--   3. sys_menu_btn 里指向「菜单管理/按钮管理/角色管理/用户管理」的按钮绑定按新 menu_id
--      重映射保留；其余（指向已不存在的「设备类型管理」）删除
--   4. sys_role_menu_btn 清空：仅有的 3 条授权属于无人使用的角色 4，且全部指向幽灵菜单。
--      现存唯一用户 superadmin 是平台超管，GetMenuTree 对超管返回全量菜单，不会被锁死。
--
-- 执行后需知：
--   非超管角色的菜单授权需到「角色授权」页重新勾选（旧授权本就指向不存在的页面）。
--
-- 回滚：V0003__rollback_menu_dynamic_route.sql
-- ============================================================================

BEGIN;

-- 1. 新增两列（CodeFirst 建表时也会带上，此处保证存量库先行可用；幂等）
ALTER TABLE sys_menu ADD COLUMN IF NOT EXISTS component varchar(200) DEFAULT '';
ALTER TABLE sys_menu ADD COLUMN IF NOT EXISTS meta_json varchar(500) DEFAULT '';

COMMENT ON COLUMN sys_menu.component IS '组件路径(相对src/views,如 iot/center/index.vue;目录节点留空)';
COMMENT ON COLUMN sys_menu.meta_json IS '附加路由meta(JSON),下发时平铺合并进 meta';

-- 2. 按钮绑定重映射：旧菜单ID → 新菜单ID（只保留在新菜单表里仍有对应页面的）
UPDATE sys_menu_btn SET menu_id = '9004' WHERE menu_id = '10021';  -- 菜单管理
UPDATE sys_menu_btn SET menu_id = '9005' WHERE menu_id = '10022';  -- 按钮管理
UPDATE sys_menu_btn SET menu_id = '9003' WHERE menu_id = '10025';  -- 角色授权
UPDATE sys_menu_btn SET menu_id = '9002' WHERE menu_id = '10026';  -- 用户管理
-- 其余绑定指向新菜单表里不存在的页面（设备类型管理等），删除
DELETE FROM sys_menu_btn WHERE menu_id NOT IN ('9002', '9003', '9004', '9005');

-- 3. 清空旧授权（全部指向幽灵菜单；超管走全量菜单，不受影响）
DELETE FROM sys_role_menu_btn;

-- 4. 重建菜单
DELETE FROM sys_menu;

INSERT INTO sys_menu
  (menu_id, menu_code, menu_name, parent_id, menu_url, component, menu_icon, meta_json,
   is_show_link, sort_border, tree_level, full_name, full_code, has_child,
   create_id, create_name, create_time, update_id, update_name, update_time)
VALUES
  -- 组态管理
  ('2000','ProjectScada','组态管理','0','/projectscada','','ri:computer-line','',
   1,'A002',1,'组态管理','|2000|',true,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  -- 组态与报表共用 views/project 同一个页面，靠 meta.projectKind 区分读写哪套数据
  ('2001','Project','项目管理','2000','/project','project/index.vue','','{"projectKind":"scada"}',
   1,'B001',2,'组态管理|项目管理','|2000|2001|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),

  -- 物联管理
  ('3000','IotManage','物联管理','0','/iot','','ri:cpu-line','',
   1,'A003',1,'物联管理','|3000|',true,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3001','IotCenter','设备中心','3000','/iot/center','iot/center/index.vue','','',
   1,'B001',2,'物联管理|设备中心','|3000|3001|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3002','IotProduct','产品类型','3000','/iot/product','iot/product/index.vue','','',
   1,'B002',2,'物联管理|产品类型','|3000|3002|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3003','IotTypeParam','点表配置','3000','/iot/typeparam','iot/typeparam/index.vue','','',
   1,'B003',2,'物联管理|点表配置','|3000|3003|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3004','IotDevice','设备管理','3000','/iot/device','iot/device/index.vue','','',
   1,'B004',2,'物联管理|设备管理','|3000|3004|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3005','IotMonitor','实时监控','3000','/iot/monitor','iot/monitor/index.vue','','',
   1,'B005',2,'物联管理|实时监控','|3000|3005|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3006','IotStrategy','采集推送策略','3000','/iot/strategy','iot/strategy/index.vue','','',
   1,'B006',2,'物联管理|采集推送策略','|3000|3006|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3007','IotAlarm','告警中心','3000','/iot/alarm','iot/alarm/index.vue','','',
   1,'B007',2,'物联管理|告警中心','|3000|3007|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3008','IotAlarmMask','告警屏蔽','3000','/iot/alarmmask','iot/alarmmask/index.vue','','',
   1,'B008',2,'物联管理|告警屏蔽','|3000|3008|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3009','IotNotify','通知渠道','3000','/iot/notify','iot/notify/index.vue','','',
   1,'B009',2,'物联管理|通知渠道','|3000|3009|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3010','IotLinkage','规则联动','3000','/iot/linkage','iot/linkage/index.vue','','',
   1,'B010',2,'物联管理|规则联动','|3000|3010|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3011','IotNorthbound','北向转发','3000','/iot/northbound','iot/northbound/index.vue','','',
   1,'B011',2,'物联管理|北向转发','|3000|3011|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3012','IotScript','协议脚本','3000','/iot/script','iot/script/index.vue','','',
   1,'B012',2,'物联管理|协议脚本','|3000|3012|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3013','IotCommand','产品命令','3000','/iot/command','iot/command/index.vue','','',
   1,'B013',2,'物联管理|产品命令','|3000|3013|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('3014','IotPlugin','插件管理','3000','/iot/plugin','iot/plugin/index.vue','','',
   1,'B014',2,'物联管理|插件管理','|3000|3014|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),

  -- 报表中心
  ('4000','ReportCenter','报表中心','0','/report','','ri:file-chart-line','',
   1,'A004',1,'报表中心','|4000|',true,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('4001','ReportProject','报表项目','4000','/report/project','project/index.vue','','{"projectKind":"dash"}',
   1,'B001',2,'报表中心|报表项目','|4000|4001|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),

  -- 系统管理
  ('9000','SystemManage','系统管理','0','/system','','ri:settings-3-line','',
   1,'A999',1,'系统管理','|9000|',true,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('9001','SystemUnit','租户管理','9000','/system/unit','system/unit/index.vue','','',
   1,'B001',2,'系统管理|租户管理','|9000|9001|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('9002','SystemUser','用户管理','9000','/system/user','system/user/index.vue','','',
   1,'B002',2,'系统管理|用户管理','|9000|9002|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('9003','SystemRole','角色授权','9000','/system/role','system/role/index.vue','','',
   1,'B003',2,'系统管理|角色授权','|9000|9003|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('9004','SystemMenu','菜单管理','9000','/system/menu','system/menu/index.vue','','',
   1,'B004',2,'系统管理|菜单管理','|9000|9004|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS')),
  ('9005','SystemButton','按钮管理','9000','/system/button','system/button/index.vue','','',
   1,'B005',2,'系统管理|按钮管理','|9000|9005|',false,
   1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'),1,'系统管理员',to_char(now(),'YYYY-MM-DD HH24:MI:SS'));

COMMIT;
