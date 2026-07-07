import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 采集策略（对应后端 CollectStrategy 实体） */
export interface CollectStrategyItem {
  SnowId: number | string;
  UnitId?: number;
  /** 挂靠层级(1=产品,2=设备,3=点位) */
  ScopeType?: number;
  /** 挂靠对象(产品=设备类型编码,设备/点位=设备ID) */
  ScopeId?: string;
  /** 参数编码(仅点位级使用) */
  ParamCode?: string;
  /** 采集周期毫秒(空=未设置回落下级) */
  CollectCycleMs?: number | null;
  /** 采集cron表达式(设置后优先于采集周期) */
  CollectCron?: string;
  /** 上报最大周期毫秒(空=未设置) */
  ReportCycleMs?: number | null;
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
}

/** 推送策略（对应后端 PushStrategy 实体） */
export interface PushStrategyItem {
  SnowId: number | string;
  UnitId?: number;
  /** 挂靠层级(1=产品,2=设备,3=点位) */
  ScopeType?: number;
  /** 挂靠对象(产品=设备类型编码,设备/点位=设备ID) */
  ScopeId?: string;
  /** 参数编码(仅点位级使用) */
  ParamCode?: string;
  /** 推送模式(1=收到即报,2=变化上报,3=定时上报,4=变化+静默兜底,空=未设置) */
  ReportMode?: number | null;
  /** 死区类型(0=严格不等,1=绝对死区,2=百分比死区,空=未设置) */
  DeadbandType?: number | null;
  /** 死区值 */
  DeadbandValue?: number | null;
  /** 最小推送间隔毫秒(窗口内只推最新) */
  MinPushIntervalMs?: number | null;
  /** 最大静默周期毫秒(强制上报兜底) */
  MaxSilentMs?: number | null;
  /** 关键属性点位清单(|分隔,变化立即冲刷) */
  DebounceIgnoreKeys?: string;
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
}

/** 采集策略：分页查询 */
export const getCollectListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询采集策略");
  return http.request<ResultTable>("post", "/CollectStrategy/GetListByPage", {
    data
  });
};

/** 采集策略：批量保存（保存后合并引擎热重载） */
export const saveCollectBatch = (data?: object) => {
  storage.setItem("button", "保存采集策略");
  return http.request<Result>("post", "/CollectStrategy/SaveBatch", {
    data
  });
};

/** 采集策略：根据主键删除 */
export const deleteCollectByPk = (_SnowId: number | string) => {
  storage.setItem("button", "删除采集策略");
  return http.request<Result>("post", "/CollectStrategy/DeleteByPk", {
    params: { _SnowId }
  });
};

/** 推送策略：分页查询 */
export const getPushListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询推送策略");
  return http.request<ResultTable>("post", "/PushStrategy/GetListByPage", {
    data
  });
};

/** 推送策略：批量保存（保存后合并引擎热重载） */
export const savePushBatch = (data?: object) => {
  storage.setItem("button", "保存推送策略");
  return http.request<Result>("post", "/PushStrategy/SaveBatch", {
    data
  });
};

/** 推送策略：根据主键删除 */
export const deletePushByPk = (_SnowId: number | string) => {
  storage.setItem("button", "删除推送策略");
  return http.request<Result>("post", "/PushStrategy/DeleteByPk", {
    params: { _SnowId }
  });
};

export default {
  getCollectListByPage,
  saveCollectBatch,
  deleteCollectByPk,
  getPushListByPage,
  savePushBatch,
  deletePushByPk
};
