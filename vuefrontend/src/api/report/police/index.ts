import { http } from "@/utils/http";
import { Result, type ResultTable, type QueryTableParams } from "../../type";
/** 设备 */
const button = "签到记录";

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/PoliceEventSign/GetListByPage", {
    data
  });
};

export default {
  getListByPage
};
