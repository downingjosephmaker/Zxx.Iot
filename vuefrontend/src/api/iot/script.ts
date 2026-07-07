import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 协议脚本 */
const button = "协议脚本";

/** JS协议解析脚本（对应后端 ProtocolScript 实体） */
export interface ProtocolScriptItem {
  SnowId: number | string;
  /** 脚本名称 */
  ScriptName?: string;
  /** 挂靠产品类型编码(该产品的非JSON载荷/透传帧用此脚本解析) */
  DeviceTypeCode?: string;
  /** 脚本内容(JS,三段式函数) */
  ScriptContent?: string;
  /** 版本号(保存自增) */
  Version?: number;
  /** 试运行样例帧hex */
  SampleHex?: string;
  /** 试运行样例上下文JSON */
  SampleContext?: string;
  /** 是否启用(安全默认禁用) */
  IsEnable?: boolean;
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
}

/** 脚本版本历史（对应后端 ProtocolScriptHistory 实体） */
export interface ProtocolScriptHistoryItem {
  SnowId: number | string;
  ScriptId: number | string;
  Version: number;
  ScriptContent: string;
  CreateTime?: string;
  CreateName?: string;
}

/** 试运行参数（对应后端 ProtocolScriptDryRunPara） */
export interface ScriptDryRunPara {
  /** 脚本主键(0=直接用草稿内容) */
  ScriptId: number | string;
  /** 草稿脚本内容(非空时优先于库内内容) */
  ScriptContent: string;
  /** 函数名(decode/encode/splitFrames) */
  FuncName: string;
  /** 输入帧hex(decode/splitFrames用) */
  InputHex: string;
  /** 输入命令JSON(encode用) */
  InputJson: string;
  /** 模拟上下文JSON(deviceKey/variables等) */
  ContextJson: string;
}

/** 试运行结果（对应后端 ScriptRunResult） */
export interface ScriptRunResultItem {
  FuncName: string;
  Success: boolean;
  /** 失败原因(编译失败/未定义函数/执行异常/超时超限) */
  Error: string;
  /** 返回值JSON(null/undefined为空串) */
  ResultJson: string;
  /** console输出(最多200条) */
  ConsoleLogs: string[];
  /** 耗时(毫秒) */
  ElapsedMs: number;
}

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/ProtocolScript/GetListByPage", {
    data
  });
};

/** 批量保存（版本号自增+写历史快照，保存后沙箱缓存热重载） */
export const saveBatch = (data?: object) => {
  storage.setItem("button", "保存" + button);
  return http.request<Result>("post", "/ProtocolScript/SaveBatch", {
    data
  });
};

/** 根据主键删除（连带删除版本历史） */
export const deleteByPk = (_SnowId: number | string) => {
  storage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/ProtocolScript/DeleteByPk", {
    params: { _SnowId }
  });
};

/** 查询脚本版本历史（版本倒序） */
export const getHistoryList = (_SnowId: number | string) => {
  storage.setItem("button", "查询" + button + "历史");
  return http.request<Result>("get", "/ProtocolScript/GetHistoryList", {
    params: { _SnowId }
  });
};

/** 脚本试运行（干跑无副作用，草稿内容优先） */
export const postDryRun = (data: ScriptDryRunPara) => {
  storage.setItem("button", "调试" + button);
  return http.request<Result>("post", "/ProtocolScript/PostDryRun", {
    data
  });
};

export default {
  getListByPage,
  saveBatch,
  deleteByPk,
  getHistoryList,
  postDryRun
};
