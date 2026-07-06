import { http } from "@/utils/http";
import { Result, type ResultTable, type DeviceMonitorSearch } from "../../type";
/** 设备 */
const button = "设备监控";
const url = "/DeviceMonitor";
/** 分页查询 */
export const getListByPage = (data?: DeviceMonitorSearch) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", url + "/GetListByPage", {
    data
  });
};
export const GetDeviceStateCount = (data?: DeviceMonitorSearch) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", url + "/GetDeviceStateCount", {
    data
  });
};
export default {
  getListByPage,
  GetDeviceStateCount
};
