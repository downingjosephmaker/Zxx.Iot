import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 产品命令 */
const button = "产品命令";

/** 产品命令白名单（对应后端 ProductCommand 实体） */
export interface ProductCommandItem {
  SnowId: number | string;
  /** 所属产品类型编码 */
  DeviceTypeCode?: string;
  /** 命令显示名称 */
  CommandName?: string;
  /** 下行控制类型(插件侧ClassName:netmodbuswrite/nets7write/netopcuawrite/netcjt188valve等) */
  ClassName?: string;
  /** 参数JSON Schema(前端动态表单渲染依据) */
  ParamSchema?: string;
  /** 下行内容模板(ConContent JSON模板,表单值填充占位) */
  ConTemplate?: string;
  /** 是否二次确认(阀控等高危命令) */
  NeedConfirm?: boolean;
  /** 是否启用 */
  IsEnable?: boolean;
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
}

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/ProductCommand/GetListByPage", {
    data
  });
};

/** 批量保存 */
export const saveBatch = (data?: object) => {
  storage.setItem("button", "保存" + button);
  return http.request<Result>("post", "/ProductCommand/SaveBatch", {
    data
  });
};

/** 根据主键删除 */
export const deleteByPk = (_SnowId: number | string) => {
  storage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/ProductCommand/DeleteByPk", {
    params: { _SnowId }
  });
};

/** 按产品类型编码查询启用命令（设备详情指令下发表单数据源） */
export const getListByTypeCode = (typecode: string) => {
  storage.setItem("button", "查询" + button);
  return http.request<Result>("get", "/ProductCommand/GetListByTypeCode", {
    params: { typecode }
  });
};

/** 指令下发参数 */
export interface DeviceCommandSendPara {
  /** 产品命令主键 */
  CommandId: number | string;
  /** 目标设备ID集合 */
  DeviceIds: number[];
  /** 下行内容(按ConTemplate占位填充后的最终JSON串) */
  ConContent: string;
}

/** 手动下发设备命令（白名单校验+广播全部插件） */
export const sendCommand = (data: DeviceCommandSendPara) => {
  storage.setItem("button", "下发" + button);
  return http.request<Result>("post", "/DeviceControl/SendCommand", {
    data
  });
};

export default {
  getListByPage,
  saveBatch,
  deleteByPk,
  getListByTypeCode,
  sendCommand
};
