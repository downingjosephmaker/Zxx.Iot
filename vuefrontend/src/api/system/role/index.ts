import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备类型 */

const button = "设备类型";
/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "分页查询角色");
  return http.request<ResultTable>("post", "/Sysrole/GetListByPage", {
    data
  });
};

/** 查询单个角色 */
export const GetInfoByPk = (data?: object) => {
  localStorage.setItem("button", "查询单个角色");
  return http.request<Result>("get", "/Sysrole/GetInfoByPk", {
    data
  });
};

/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>("post", "/Sysrole/Delete?id=" + id);
};

/** 新增 */
export const insert = (data?: object) => {
  return http.request<Result>("post", "/Sysrole/Insert", {
    data
  });
};
/** 修改 */
export const update = (data?: object) => {
  return http.request<Result>("post", "/Sysrole/Update", {
    data
  });
};

export default {
  getListByPage,
  deleteByPk,
  GetInfoByPk,
  insert,
  update
};
