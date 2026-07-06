import { http } from "@/utils/http";
import type { Result } from "../type";

/**
 * 获取空调温度区间分析数据
 * @param data 查询参数（时间段、设备ID等）
 */
export const getAcTemperatureAnalysis = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acTemperatureAnalysis",
    {
      data
    }
  );
};

/**
 * 获取空调能耗分析数据
 * @param data 查询参数（时间段、设备ID等）
 */
export const getAcEnergyAnalysis = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acEnergyAnalysis",
    {
      data
    }
  );
};

/**
 * 获取空调运行情况分析数据
 * @param data 查询参数（时间段、设备ID等）
 */
export const getAcOperationAnalysis = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acOperationAnalysis",
    {
      data
    }
  );
};

/**
 * 获取空调设备开机率分析数据
 * @param data 查询参数（时间段、设备ID等）
 */
export const getAcPowerOnRateAnalysis = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acPowerOnRateAnalysis",
    {
      data
    }
  );
};

/**
 * 获取空调合理用能分析数据
 * @param data 查询参数（时间段、设备ID等）
 */
export const getAcReasonableEnergyAnalysis = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acReasonableEnergyAnalysis",
    {
      data
    }
  );
};

/**
 * 获取空调运行数量流向分析数据
 * @param data 查询参数（时间段、设备ID等）
 */
export const getAcOperationFlowAnalysis = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acOperationFlowAnalysis",
    {
      data
    }
  );
};

/**
 * 获取空调能耗流向分析数据
 * @param data 查询参数（时间段、设备ID等）
 */
export const getAcEnergyFlowAnalysis = (data: any) => {
  return http.request<Result<any>>(
    "post",
    "/api/dataAnalysis/acEnergyFlowAnalysis",
    {
      data
    }
  );
};
