import { http } from "@/utils/http";
import type { Result } from "../type";
import { storage } from "@/utils/storage";

/** 实时监控 */
const button = "实时监控";

/** 遥测点位（对应后端 TelemetryPoint，SignalR ReceiveDeviceData 推送同结构） */
export interface TelemetryPointItem {
  DeviceId: number | string;
  /** 参数编码 */
  ParamCode: string;
  /** 参数名称 */
  ParamName?: string;
  /** 采集时间(UTC) */
  Ts: string;
  /** 数值型值 */
  Value?: number | null;
  /** 状态/字符串型值 */
  ValueStr?: string | null;
  /** 质量戳(0=正常) */
  Quality: number;
}

/** 查询设备全部点位最新值（首屏铺底，增量走 SignalR） */
export const getDeviceLatest = (deviceid: number | string) => {
  storage.setItem("button", "查询" + button);
  return http.request<Result>("get", "/DeviceControl/GetDeviceLatest", {
    params: { deviceid }
  });
};

/** 历史点（对应后端 TelemetryQueryService.HistoryPoint；raw=Value/ValueStr，hour=Value取均值另带Min/Max/Last/Cnt） */
export interface HistoryPointItem {
  /** 本地时间(原始=采集时刻，聚合=小时桶起点) */
  Ts: string;
  Value?: number | null;
  ValueStr?: string | null;
  Min?: number | null;
  Max?: number | null;
  Last?: number | null;
  Cnt?: number;
}

/** 历史查询结果（Result内层为 {Mode:"raw"|"hour", Points:HistoryPointItem[]}） */
export interface HistoryResultItem {
  Mode: "raw" | "hour";
  Points: HistoryPointItem[];
}

/** 查询单设备单参数历史曲线（mode:auto=跨度≤48h且在30天原始保留窗内走原始点，否则1h聚合；raw/hour=显式指定） */
export const getDeviceHistory = (
  deviceid: number | string,
  paramcode: string,
  starttime: string,
  endtime: string,
  mode: "auto" | "raw" | "hour" = "auto"
) => {
  storage.setItem("button", "查询" + button + "历史");
  return http.request<Result>("get", "/DeviceControl/GetDeviceHistory", {
    params: { deviceid, paramcode, starttime, endtime, mode }
  });
};

export default {
  getDeviceLatest,
  getDeviceHistory
};
