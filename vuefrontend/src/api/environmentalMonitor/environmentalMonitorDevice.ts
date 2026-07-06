import { http } from "@/utils/http";
import type { Result, ResultTable } from "../type";
const button = "环境监测";
export const DoorKgControl = (data?: any) => {
  localStorage.setItem("button", "门禁设备控制");
  return http.request<Result>("post", "/DeviceControl/DoorKgControl", { data });
};

export const WaterPumpControl = (data?: any) => {
  localStorage.setItem("button", "水泵设备控制");
  return http.request<Result>("post", "/DeviceControl/WaterPumpControl", {
    data
  });
};
/** 查询建筑点位信息 */
export const GetInfoByPk = (BuildId?: string) => {
  localStorage.setItem("button", "查询建筑点位信息");
  return http.request<ResultTable>(
    "get",
    "/BuildinfoDianweiMap/GetInfoByPk?_BuildId=" + BuildId
  );
};

/** 点位信息保存 */
export const UpdateMapConfig = (data?: object) => {
  localStorage.setItem("button", "点位信息保存");
  return http.request<ResultTable>(
    "post",
    "/BuildinfoDianweiMap/UpdateMapConfig",
    { data }
  );
};

/** 查询平面图参数数据 */
export const GetDianweiMapDataByBid = (BuildId?: string) => {
  localStorage.setItem("button", "查询平面图参数数据");
  return http.request<ResultTable>(
    "get",
    "/BuildinfoDianweiMap/GetDianweiMapDataByBid?_BuildId=" + BuildId
  );
};

export default {
  DoorKgControl,
  WaterPumpControl,
  GetInfoByPk,
  UpdateMapConfig,
  GetDianweiMapDataByBid
};
