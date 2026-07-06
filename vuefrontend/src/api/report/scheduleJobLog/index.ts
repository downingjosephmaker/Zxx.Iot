import { http } from "@/utils/http";
import type { ResultTable } from "../../type";

/** 任务调度日志 */
export const getListByPage = (data?: any) => {
  localStorage.setItem("button", "任务调度日志");
  return http.request<ResultTable>("post", "/ScheduleJobLog/GetListByPage", {
    data
  });
};
export default {
  getListByPage
};
