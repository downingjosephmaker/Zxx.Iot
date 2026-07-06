import { http } from "@/utils/http";
import type { ResultTable, QueryTableParams } from "../../type";
/** 设备 */
const button = "告警";

/** 分页查询 */
export const getListByPage = (data?: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/EventAlarm/GetListByPage", {
    data
  });
};
/** 告警确认 */
export const saveRoleBatch = (data?: QueryTableParams) => {
  localStorage.setItem("button", "确认" + button + "");
  return http.request<ResultTable>("post", "/EventAlarm/PostHandleAlarm", {
    data
  });
};

/** 删除告警记录 */
export const deleteAlarm = (snowid: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<ResultTable>(
    "post",
    `/EventAlarm/Delete?snowid=${snowid}`
  );
};

export default {
  getListByPage,
  saveRoleBatch,
  deleteAlarm
};
