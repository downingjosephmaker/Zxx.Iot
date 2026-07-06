import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备类型 */
const button = "设备类型参数";
/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/DeviceTypeParam/GetListByPage", {
    data
  });
};
/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>(
    "post",
    "/DeviceTypeParam/DeleteByPk?_SnowId=" + id
  );
};
/** 删除 */
export const deleteByIds = (id?: string) => {
  localStorage.setItem("button", "批量删除" + button + "");
  return http.request<Result>("post", "/DeviceTypeParam/DeleteByIds?ids=" + id);
};
/** 查询单个*/
export const GetInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>("get", "/DeviceTypeParam/GetInfoByPk?_Id=" + id);
};
/** 批量保存/修改 */
export const saveBatch = (data?: object) => {
  localStorage.setItem("button", "保存/修改" + button + "");
  return http.request<Result>("post", "/DeviceTypeParam/SaveBatch", {
    data
  });
};

export default {
  getListByPage,
  deleteByPk,
  GetInfoByPk,
  saveBatch,
  deleteByIds
};
