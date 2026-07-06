import { http } from "@/utils/http";
import type { Result } from "../type";

/**
 * 获取空调热力图数据
 * @param data 查询参数（时间段、空间ID、时间粒度等）
 */
export const getAcHeatmapData = (data: any) => {
  return http.request<Result<any>>("post", "/api/dataAnalysis/acHeatmap/data", {
    data
  });
};

/**
 * 获取空调热力图按时间点的温度区间数据
 * @param data 查询参数（时间段、空间ID、时间粒度等）
 */
export const getAcTemperatureRangeByTime = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acHeatmap/temperatureRange",
    {
      data
    }
  );
};

/**
 * 获取空调热力图总用能数据
 * @param data 查询参数（时间段、空间ID、时间粒度等）
 */
export const getAcTotalEnergyConsumption = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acHeatmap/totalEnergy",
    {
      data
    }
  );
};

/**
 * 获取空调合理用能数据
 * @param data 查询参数（时间段、空间ID、时间粒度等）
 */
export const getAcReasonableEnergyUsage = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acHeatmap/reasonableEnergy",
    {
      data
    }
  );
};

/**
 * 获取空调运行情况数据
 * @param data 查询参数（时间段、空间ID、时间粒度等）
 */
export const getAcOperationStatus = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acHeatmap/operationStatus",
    {
      data
    }
  );
};

/**
 * 获取设备开机率数据
 * @param data 查询参数（时间段、空间ID、时间粒度等）
 */
export const getAcDevicePowerOnRate = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acHeatmap/devicePowerOnRate",
    {
      data
    }
  );
};

/**
 * 获取空调运行数量流向数据（按空间、用途、组织）
 * @param data 查询参数（时间段、分类类型、时间粒度等）
 */
export const getAcOperationFlowData = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acHeatmap/operationFlow",
    {
      data
    }
  );
};

/**
 * 获取空调能耗流向数据（按空间、用途、组织）
 * @param data 查询参数（时间段、分类类型、时间粒度等）
 */
export const getAcEnergyFlowData = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acHeatmap/energyFlow",
    {
      data
    }
  );
};

/**
 * 导出空调热力图分析报表
 * @param data 查询参数（时间段、空间ID、时间粒度等）
 */
export const exportAcHeatmapReport = (data: any) => {
  return http.request<Blob>("post", "/api/dataAnalysis/acHeatmap/export", {
    data,
    responseType: "blob"
  });
};
