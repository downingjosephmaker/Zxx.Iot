import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 建筑 */ const button = "建筑";
/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/Sysrelated/GetListByPage", {
    data
  });
};
/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>("post", "/Sysrelated/DeleteByPk?_SnowId=" + id);
};
/** 查询单个*/
export const GetInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>("get", "/Sysrelated/GetInfoByPk?_SnowId=" + id);
};
/** 批量保存/修改 */
export const saveBatch = (data?: object, type?: string) => {
  localStorage.setItem("button", "保存/修改" + button + "");
  return http.request<Result>("post", "/Sysrelated/SaveBatch?qxtype=" + type, {
    data
  });
};

/** 分页查询 */
export const GetQxListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "分页查询单位");
  return http.request<ResultTable>("post", "/Basicunitinfo/GetQxListByPage", {
    data
  });
};

export default {
  getListByPage,
  deleteByPk,
  GetInfoByPk,
  saveBatch,
  GetQxListByPage
};
