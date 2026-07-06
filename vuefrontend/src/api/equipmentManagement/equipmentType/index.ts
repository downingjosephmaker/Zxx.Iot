import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备类型 */

const button = "设备类型";
/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/HtkDevicetype/GetListByPage", {
    data
  });
};
/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>("post", "/HtkDevicetype/DeleteByPk?_Id=" + id);
};
/** 查询单个*/
export const GetInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>("get", "/HtkDevicetype/GetInfoByPk?_Id=" + id);
};
/** 批量保存/修改 */
export const saveBatch = (data?: object) => {
  localStorage.setItem("button", "保存/修改" + button + "");
  return http.request<Result>("post", "/HtkDevicetype/SaveBatch", {
    data
  });
};
/** 新增 */
export const insert = (data?: object) => {
  return http.request<Result>("post", "/HtkDevicetype/InsertBuild", {
    data
  });
};
/** 修改 */
export const update = (data?: object) => {
  return http.request<Result>("post", "/HtkDevicetype/UpdateBuild", {
    data
  });
};
/** 建筑查询 */
export const getListByPageCL = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/Buildinfo/GetListByPage", {
    data
  });
};
export default {
  getListByPage,
  deleteByPk,
  GetInfoByPk,
  saveBatch,
  insert,
  update,
  getListByPageCL
};
