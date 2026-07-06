import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";

/** 节能率管理 */
const button = "节能率管理";

// 节能率数据类型定义
export interface EnergySavingRateData {
  // 以下字段新增时不需要传递，后端会处理
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
  SnowId?: number;
  UnitId?: number;

  // 业务字段
  DeviceIds?: string; // 设备ID集合(|)
  StrategyStart?: string; // 策略开始日期
  StrategyEnd?: string; // 策略结束日期
  IsStrategy?: boolean; // 策略下发情况
  StrategyJson?: string; // 策略内容(json)
  StrategyEnergy?: number; // 策略总能耗
  StrategyEnergyJson?: string; // 策略日能耗(json)
  UnStrategyStart?: string; // 无策略开始日期
  UnStrategyEnd?: string; // 无策略结束日期
  UnIsStrategy?: boolean; // 无策略下发情况
  UnStrategyEnergy?: number; // 无策略总能耗
  UnStrategyEnergyJson?: string; // 无策略日能耗(json)
  IsEnable?: boolean; // 是否启用
  OperatorNum?: number; // 执行步骤序号
  OperatorLog?: string; // 执行日志
  ExpandObjects?: EnergySavingRateExpand[];
}

// 节能率计算日能耗类
export interface EnergySavingRateExpand {
  DeviceId?: number;
  EventTime: string;
  TotalValue: string;
}

// 策略保存参数
export interface StrategySaveParams {
  WorkModel: string;
  RefrigStartTemp?: number;
  RefrigOpenTemp?: number;
  RefrigCloseTemp?: number;
  HotStartTemp?: number;
  HotOpenTemp?: number;
  HotCloseTemp?: number;
  HumanNum?: number;
  HumanTime?: number;
  TemporaryTime?: number;
  SummerTemp?: number;
  WinterTemp?: number;
  OpenTempEnable?: number;
  OperatModeTemp?: number;
  OpenCloseTemp?: number;
  SummerOpenTemp?: number;
  WinterOpenTemp?: number;
  DeviceInfoList: DeviceInfo[];
  TimeInfoList: TimeInfo[];
}

// 设备信息
export interface DeviceInfo {
  DeviceId?: number;
}

// 时间策略
export interface TimeInfo {
  DayType?: number;
  TimeNum?: number;
  IsHuman?: number;
  StartHour?: number;
  StartMinute?: number;
  EndHour?: number;
  EndMinute?: number;
}

/**
 * 批量保存节能率数据
 * @param data 节能率数据集合
 * @returns 保存结果
 */
export const saveBatch = (data: EnergySavingRateData[]) => {
  localStorage.setItem("button", "批量保存" + button);
  return http.request<Result>("post", "/EnergySavingRate/SaveBatch", {
    data
  });
};

/**
 * 根据主键删除节能率数据
 * @param snowId 主键ID
 * @returns 删除结果
 */
export const deleteByPk = (snowId: number) => {
  localStorage.setItem("button", "删除" + button);
  return http.request<Result>(
    "post",
    `/EnergySavingRate/DeleteByPk?_SnowId=${snowId}`
  );
};

/**
 * 根据主键查询单条数据
 * @param snowId 主键ID
 * @returns 节能率数据
 */
export const getInfoByPk = (snowId: number) => {
  localStorage.setItem("button", "查询" + button);
  return http.request<Result>(
    "get",
    `/EnergySavingRate/GetInfoByPk?_SnowId=${snowId}`
  );
};

/**
 * 策略保存
 * @param snowId 节能率ID
 * @param data 策略数据
 * @returns 保存结果
 */
export const strategySave = (snowId: number, data: StrategySaveParams) => {
  localStorage.setItem("button", "策略保存" + button);
  return http.request<Result>(
    "post",
    `/EnergySavingRate/StrategySave?_SnowId=${snowId}`,
    { data }
  );
};

/**
 * 根据条件查询分页数据
 * @param params 查询参数
 * @returns 分页数据
 */
export const getListByPage = (params: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/EnergySavingRate/GetListByPage", {
    data: params
  });
};

export default {
  saveBatch,
  deleteByPk,
  getInfoByPk,
  strategySave,
  getListByPage
};
