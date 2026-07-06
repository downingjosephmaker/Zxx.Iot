import { http } from "@/utils/http";
import type { Result } from "../type";

/**
 * 获取空调舒适度设置
 * @param deviceId 设备ID
 */
export const getAcComfortSettings = (deviceId: string) => {
  return http.request<Result<any>>(
    "get",
    `/api/dataAnalysis/acComfort/settings/${deviceId}`
  );
};

/**
 * 更新空调舒适度设置
 * @param data 舒适度设置数据
 */
export const updateAcComfortSettings = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acComfort/settings",
    {
      data
    }
  );
};

/**
 * 获取空调舒适度报表数据
 * @param data 查询参数（时间段、建筑ID等）
 */
export const getAcComfortReport = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acComfort/report",
    {
      data
    }
  );
};

/**
 * 按建筑获取空调舒适度分析
 * @param data 查询参数（时间段、建筑ID等）
 */
export const getAcComfortAnalysisByBuilding = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acComfort/analysis/building",
    {
      data
    }
  );
};

/**
 * 导出空调舒适度报表
 * @param data 查询参数（时间段、建筑ID等）
 */
export const exportAcComfortReport = (data: any) => {
  return http.request<Blob>(
    "post",
    "/api/dataAnalysis/acComfort/report/export",
    {
      data,
      responseType: "blob"
    }
  );
};
