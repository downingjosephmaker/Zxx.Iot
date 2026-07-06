import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备 */
const button = "告警";

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/EventSignSet/GetListByPage", {
    data
  });
};

/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>("post", "/EventSignSet/DeleteByPk?_Id=" + id);
};

/** 批量保存/修改 */
export const saveBatch = (data?: any) => {
  if (data[0].Id && data[0].Id != "") {
    localStorage.setItem("button", "修改" + button + "");
  } else {
    localStorage.setItem("button", "新增" + button + "");
  }
  return http.request<Result>("post", "/EventSignSet/SaveBatch", {
    data
  });
};

export default {
  getListByPage,
  saveBatch,
  deleteByPk
};
