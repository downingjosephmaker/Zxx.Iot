import { http } from "@/utils/http";
import type { ResultTable } from "../../type";

/** 系统状态检查记录 */
export const getListByPage = (data?: any) => {
  localStorage.setItem("button", "系统状态检查记录");
  return http.request<ResultTable>("post", "/SysStatusCheckOS/GetListByPage", {
    data
  });
};
export default {
  getListByPage
};
