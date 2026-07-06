import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备类型 */

const button = "设备类型";
/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/DeviceType/GetListByPage", {
    data
  });
};
/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>("post", "/DeviceType/Delete?id=" + id);
};
/** 查询单个*/
export const GetInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>("get", "/DeviceType/GetInfoByPk?_Id=" + id);
};

/** 新增 */
export const insert = (data?: object) => {
  return http.request<Result>("post", "/DeviceType/Insert", {
    data
  });
};
/** 修改 */
export const update = (data?: object) => {
  return http.request<Result>("post", "/DeviceType/Update", {
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

/** 设备类型查询 */
export const GetMasterTypeList = (data: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("get", "/DeviceType/GetMasterTypeList", {
    params: data
  });
};

/** 设备类型查询 */
export const GetHistoryTypes = (data: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("get", "/DeviceType/GetHistoryTypes", {
    params: data
  });
};

/** 设备类型查询 */
export const GetReportTypes = (data: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("get", "/DeviceType/GetReportTypes", {
    params: data
  });
};
export default {
  getListByPage,
  deleteByPk,
  GetInfoByPk,
  insert,
  update,
  getListByPageCL,
  GetMasterTypeList,
  GetHistoryTypes,
  GetReportTypes
};
