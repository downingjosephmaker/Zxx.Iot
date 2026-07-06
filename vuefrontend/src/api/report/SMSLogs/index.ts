import { http } from "@/utils/http";
import type { ResultTable, QueryTableParams } from "../../type";
/** 设备 */
const button = "短息发送记录";

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/EventNoteRecord/GetListByPage", {
    data
  });
};

export default {
  getListByPage
};
