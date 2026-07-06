import { http } from "@/utils/http";

/** 请求载荷：获取设备能耗详情 */
export interface GetSraptDevEnergyDetailPayload {
  targetdate: string;
}

/** 监控卡片展示参数 */
export interface ShowParam {
  SubChannel: string | null;
  ParamCode: string | null;
  ParamName: string | null;
  ValueUnit: string | null;
  ParamValue: string | null;
}

/** 设备能耗详情 */
export interface DeviceMonitorDetail {
  DeviceId: number;
  DeviceName: string | null;
  DeviceGuid: string | null;
  DeviceAdr: number;
  DeviceSwitch?: string | null; // 设备开关状态：充电中/未充电
  YRealList: ShowParam[] | null;
  MRealList: ShowParam[] | null;
  DRealList: ShowParam[] | null;
}

/** 接口响应：大屏设备能耗（Result 字段内的结构） */
export interface DeviceSecondMonitor {
  YTotalList: ShowParam[] | null;
  MTotalList: ShowParam[] | null;
  DTotalList: ShowParam[] | null;
  DeviceDetailList: DeviceMonitorDetail[] | null;
}

/** 完整的 API 响应体结构 */
export interface ApiResponse {
  Message: string;
  Result: string; // Result 是一个 stringified JSON
  Status: boolean;
  Timestamp: string;
  Total: number;
}

/**
 * @description 获取大屏设备能耗详情
 * @param data
 * @returns
 */
export const getSraptDevEnergyDetail = (
  data: GetSraptDevEnergyDetailPayload
) => {
  return http.request<ApiResponse>("post", "/Monitor/GetSraptDevEnergyDetail", {
    params: { targetdate: data.targetdate }
  });
};

/** 获取设备能耗监测详情 */
export const getDevEnergyDetail = (data: {
  targetdate: string;
  devtype: number;
}) => {
  return http.request<ApiResponse>("post", "/Monitor/GetDevEnergyDetail", {
    params: data
  });
};
