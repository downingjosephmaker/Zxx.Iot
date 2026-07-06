import { http } from "@/utils/http";
import type { ResultTable } from "../../type";

/** 状态变化日志 */
export const getListByPage = (data?: any) => {
  localStorage.setItem("button", "状态变化日志");
  return http.request<ResultTable>("post", "/EventSignal/GetListByPage", {
    data
  });
};
export default {
  getListByPage
};
