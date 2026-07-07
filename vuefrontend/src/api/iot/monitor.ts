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

export default {
  getDeviceLatest
};
