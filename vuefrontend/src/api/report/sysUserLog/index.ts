import { http } from "@/utils/http";
import type { ResultTable } from "../../type";

/** 查询用户日志 */
export const getListByPage = (data?: any) => {
  localStorage.setItem("button", "查询用户日志");
  return http.request<ResultTable>("post", "/Sysuserlog/GetListByPage", {
    data
  });
};
export default {
  getListByPage
};
