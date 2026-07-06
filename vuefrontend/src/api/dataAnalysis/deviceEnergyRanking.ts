import { http } from "@/utils/http";
import type { Result } from "../interface";

/**
 * 获取设备能耗排名数据
 * @param data 查询参数（时间段、设备类型、数量限制等）
 */
export const getDeviceEnergyRanking = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/deviceEnergyRanking",
    { data }
  );
};

/**
 * 获取设备待机能耗排名数据
 * @param data 查询参数（时间段、设备类型、数量限制等）
 */
export const getDeviceStandbyEnergyRanking = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/deviceStandbyEnergyRanking",
    { data }
  );
};

/**
 * 获取设备运行时长排名数据
 * @param data 查询参数（时间段、设备类型、数量限制等）
 */
export const getDeviceRuntimeRanking = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/deviceRuntimeRanking",
    { data }
  );
};

/**
 * 获取设备最长持续运行时长排名数据
 * @param data 查询参数（时间段、设备类型、数量限制等）
 */
export const getDeviceContinuousRuntimeRanking = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/deviceContinuousRuntimeRanking",
    { data }
  );
};
