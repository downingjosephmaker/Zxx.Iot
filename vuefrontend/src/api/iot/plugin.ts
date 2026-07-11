import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 插件管理(后端SysPluginController;除GetSupportedCommands外均仅超级管理员可用) */
const button = "插件";

/** 插件(对应后端 SysPluginEntity) */
export interface SysPluginItem {
  /** 插件Guid(主键) */
  PluginGuid: string;
  /** 插件名称 */
  PluginName?: string;
  /** 插件类型(系统插件,业务插件) */
  PluginType?: string;
  /** 插件描述 */
  PluginDesc?: string;
  /** 插件模型路径 */
  PluginModelPath?: string;
  /** 插件版本 */
  PluginVersion?: string;
  /** 插件状态(0:禁用,1:启用) */
  PluginStatus?: number;
  /** 插件通讯状态(0:正常,1:异常) */
  PluginHeartStatus?: number;
  /** 插件通讯状态时间 */
  PluginHeartTime?: string;
  /** 插件参数(JSON) */
  PluginConfig?: string;
  /** 插件自描述清单(JSON:configSchema/defaultConfig/commands/addressing) */
  PluginManifest?: string;
  /** 插件路径(相对插件根 {guid}/{时间戳}/x.dll) */
  PluginPath?: string;
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
}

/** 已加载插件声明的控制命令(对应后端 PluginService.PluginCommandInfo) */
export interface PluginCommandInfo {
  /** 控制类名 */
  ClassName: string;
  /** 命令说明 */
  Description: string;
  /** 所属插件Guid */
  PluginGuid: string;
  /** 所属插件名称 */
  PluginName: string;
}

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/SysPlugin/GetListByPage", {
    data
  });
};

/** 根据插件Guid获取详情 */
export const getInfoByGuid = (guid: string) => {
  storage.setItem("button", "查询" + button + "详情");
  return http.request<Result>("post", "/SysPlugin/GetInfoByGuid", {
    params: { guid }
  });
};

/** 配置表单Schema(读plugin_manifest.configSchema,与ProductCommand.ParamSchema同构) */
export const getConfigSchema = (guid: string) => {
  storage.setItem("button", "查询" + button + "配置Schema");
  return http.request<Result>("post", "/SysPlugin/GetConfigSchema", {
    params: { guid }
  });
};

/** 全部已加载插件声明的控制命令(command/linkage表单下拉共用,登录即可读) */
export const getSupportedCommands = () => {
  storage.setItem("button", "查询" + button + "命令");
  return http.request<Result>("post", "/SysPlugin/GetSupportedCommands");
};

/** 插件驱动认领(对应后端 PluginDriverClaim,C-3点表按驱动裁剪数据源,仅元数据登录即可读) */
export interface PluginDriverClaim {
  PluginGuid: string;
  PluginName: string;
  /** 采集字段子集标识(modbus|dlt645|cjt188|s7|opcua,空=未声明不裁剪) */
  FieldGroup: string;
  /** 点表寻址说明(表单提示文本) */
  Addressing: string;
  /** 认领的产品类型编码清单 */
  DeviceTypeCodes: string[];
}

/** 已启用插件的驱动认领清单(产品类型编码→采集字段子集,点表表单按驱动裁剪依据) */
export const getDriverClaims = () => {
  storage.setItem("button", "查询" + button + "驱动认领");
  return http.request<Result>("post", "/SysPlugin/GetDriverClaims");
};

/** 启用/禁用插件(启用即加载并启动,停用即Stop并卸载) */
export const enablePlugin = (guid: string, pluginstatus: number) => {
  storage.setItem("button", (pluginstatus === 1 ? "启用" : "停用") + button);
  return http.request<Result>("post", "/SysPlugin/EnablePlugin", {
    params: { guid, pluginstatus }
  });
};

/** 保存插件配置(已启用插件即时重载生效,无需重启进程) */
export const saveConfig = (guid: string, configjson: string) => {
  storage.setItem("button", "保存" + button + "配置");
  return http.request<Result>("post", "/SysPlugin/SaveConfig", {
    params: { guid, configjson }
  });
};

/** 根据插件Guid删除(先删登记再卸载运行实例) */
export const deletePlugin = (guid: string) => {
  storage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/SysPlugin/Delete", {
    params: { guid }
  });
};

/** 插件新增(手工登记场景,如带依赖的整目录部署后补记录) */
export const insertPlugin = (data: SysPluginItem) => {
  storage.setItem("button", "新增" + button);
  return http.request<Result>("post", "/SysPlugin/Insert", {
    data
  });
};

/** 插件修改(元数据维护;配置走SaveConfig,Manifest由上传/加载反射回写) */
export const updatePlugin = (data: SysPluginItem) => {
  storage.setItem("button", "修改" + button);
  return http.request<Result>("post", "/SysPlugin/Update", {
    data
  });
};

/** 上传插件(zip整包或单DLL;zip内插件Guid决定登记/更新对象,新插件默认停用) */
export const uploadPluginFile = (file: File) => {
  storage.setItem("button", "上传" + button);
  const formData = new FormData();
  formData.append("file", file);
  return http.request<Result>("post", "/SysPlugin/UploadPluginFile", {
    data: formData,
    headers: { "Content-Type": "multipart/form-data" }
  });
};

export default {
  getListByPage,
  getInfoByGuid,
  getConfigSchema,
  getSupportedCommands,
  getDriverClaims,
  enablePlugin,
  saveConfig,
  deletePlugin,
  insertPlugin,
  updatePlugin,
  uploadPluginFile
};
