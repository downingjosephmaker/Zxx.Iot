-- sys_plugin 种子数据(B-1.6-⑤:新环境初始化口径)
-- 登记随仓 5 个协议插件:默认停用(plugin_status=0)、plugin_path 留空。

-- 【存量库迁移,必须先于 INSERT 执行】
-- CodeFirst 建表闸门以"表是否存在"为准(DbContext.cs InitDataBase:874),已建过 sys_plugin 的库
-- 升级本版后 plugin_manifest 列永远不会被自动创建,首次 sys_plugin 查询即报 42703 undefined column。
ALTER TABLE sys_plugin ADD COLUMN IF NOT EXISTS plugin_manifest text;
-- 部署路径二选一:
--   ① 插件管理页/Swagger 调 SysPlugin/UploadPluginFile 上传 zip/DLL —— 自动落版本化目录并回填 plugin_path/plugin_manifest/plugin_config;
--   ② 带依赖整目录拷入 files/plugins/{guid}/{时间戳}/ 后手工回填 plugin_path(相对路径,如 plugins/{guid}/{时间戳}/IotPlugin.Xxx.dll)。
-- plugin_manifest/plugin_config 由宿主加载时反射 ICenBoPlugin.PluginManifest 自动回写,无需手工维护。
-- 幂等:主键冲突跳过,可重复执行。

INSERT INTO sys_plugin (plugin_guid, plugin_name, plugin_type, plugin_desc, plugin_model_path, plugin_version, plugin_status, plugin_heart_status, plugin_heart_time, plugin_config, plugin_manifest, plugin_path, create_id, create_time, create_name, update_id, update_time, update_name)
VALUES
('b8c4d0e6f2a3517b4cd85e6f9012b345', 'ModbusPlugin', 'Modbus通用采集插件', 'Modbus RTU/TCP通用采集与控制(基于IotDriverCore驱动框架)', '', '1.0.0', 0, 0, '', '', '', '', 1, '', '系统初始化', 1, '', '系统初始化'),
('c9d5e1f7a3b4628c5de96f7a0123c456', 'Dlt645Plugin', 'DLT645电表采集插件', 'DL/T 645-2007/1997电表抄读与广播校时(基于IotDriverCore驱动框架)', '', '1.0.0', 0, 0, '', '', '', '', 1, '', '系统初始化', 1, '', '系统初始化'),
('d0e6f2a8b4c5739d6ef07a8b1234d567', 'Cjt188Plugin', 'CJT188水表采集插件', 'CJ/T 188-2004/2018水表抄读与阀控(基于IotDriverCore驱动框架)', '', '1.0.0', 0, 0, '', '', '', '', 1, '', '系统初始化', 1, '', '系统初始化'),
('e1f7a3b9c5d6840e7fa18b9c2345e678', 'S7Plugin', '西门子S7采集插件', '西门子S7系列PLC批量采集(基于IotDriverCore驱动框架,S7netplus)', '', '1.0.0', 0, 0, '', '', '', '', 1, '', '系统初始化', 1, '', '系统初始化'),
('f2a8b4c0d6e7951fa8b29cad3456f789', 'OpcUaPlugin', 'OPCUA采集插件', 'OPC UA订阅/批量读采集(基于IotDriverCore驱动框架,OPCFoundation官方栈)', '', '1.0.0', 0, 0, '', '', '', '', 1, '', '系统初始化', 1, '', '系统初始化')
ON CONFLICT (plugin_guid) DO NOTHING;
