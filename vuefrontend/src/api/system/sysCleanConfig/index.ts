import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";

/** 业务模块名称 */
const moduleName = "数据清理配置管理";

// ==================== 类型定义 ====================

/**
 * 数据清理配置数据接口
 */
export interface SysCleanConfigData {
  /** 系统字段 */
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
  SnowId?: number;

  /** 业务字段 */
  CleanName: string; // 清理名称
  DataCode: string; // 数据类型编码
  RetentionDays?: number; // 保留天数
  IsAutoCleanup?: boolean; // 是否启用自动清理
  LastCleanupTime?: string; // 最后清理时间
}

/**
 * 数据清理配置查询参数
 */
export interface SysCleanConfigQueryParams extends QueryTableParams {
  CleanName?: string; // 清理名称
  DataCode?: string; // 数据类型编码
  IsAutoCleanup?: boolean; // 是否启用自动清理
}

// ==================== API函数 ====================

/**
 * 分页查询数据清理配置列表
 * @param params 查询参数
 * @returns 分页数据
 */
export const getListByPage = (params: SysCleanConfigQueryParams) => {
  return http.request<ResultTable<SysCleanConfigData[]>>(
    "post",
    "/SysCleanConfig/GetListByPage",
    { data: params },
    {
      headers: {
        "Content-Type": "application/json"
      }
    }
  );
};

/**
 * 根据主键查询单条数据
 * @param snowId 主键ID
 * @returns 单条数据
 */
export const getInfoByPk = (snowId: number) => {
  return http.request<Result<SysCleanConfigData>>(
    "get",
    `/SysCleanConfig/GetInfoByPk?_SnowId=${snowId}`
  );
};

/**
 * 批量保存数据清理配置
 * @param data 数据清理配置数据数组
 * @returns 操作结果
 */
export const saveBatch = (data: SysCleanConfigData[]) => {
  return http.request<Result<string>>(
    "post",
    "/SysCleanConfig/SaveBatch",
    { data },
    {
      headers: {
        "Content-Type": "application/json"
      }
    }
  );
};

/**
 * 根据主键删除数据清理配置
 * @param snowId 主键ID
 * @returns 操作结果
 */
export const deleteByPk = (snowId: number) => {
  return http.request<Result<string>>(
    "post",
    `/SysCleanConfig/DeleteByPk?_SnowId=${snowId}`
  );
};

/**
 * 数据类型选项
 */
export const DATA_TYPE_OPTIONS = [
  { label: "系统日志", value: "SYSTEM_LOG" },
  { label: "操作日志", value: "OPERATION_LOG" },
  { label: "错误日志", value: "ERROR_LOG" },
  { label: "访问日志", value: "ACCESS_LOG" },
  { label: "设备数据", value: "DEVICE_DATA" },
  { label: "能耗数据", value: "ENERGY_DATA" },
  { label: "报警数据", value: "ALARM_DATA" },
  { label: "历史数据", value: "HISTORY_DATA" }
];
