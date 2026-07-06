import { http } from "@/utils/http";
import type { ResultTable } from "../../type";

/** 系统操作日志 */
export const getListByPage = (data?: any) => {
  localStorage.setItem("button", "系统操作日志");
  return http.request<ResultTable>("post", "/SysyoptLogOS/GetListByPage", {
    data
  });
};
export default {
  getListByPage
};
