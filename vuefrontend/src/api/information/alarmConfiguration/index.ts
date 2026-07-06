import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备 */
/** 分页查询 */
const button = "告警相关配置";
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/AlarmConfig/GetListByPage", {
    data
  });
};
/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/AlarmConfig/DeleteByPk?_Id=" + id);
};
/** 查询单个*/
export const GetInfoByPk = (id?: any) => {
  localStorage.setItem("button", "查询单个" + button);
  return http.request<Result>("get", "/AlarmConfig/GetInfoByPk?_Id=" + id);
};
/** 批量保存/修改 */
export const saveBatch = (data?: object, isUpdate = false) => {
  if (!isUpdate) {
    localStorage.setItem("button", "保存" + button);
    return http.request<Result>("post", "/AlarmConfig/SaveBatch", {
      data
    });
  } else {
    localStorage.setItem("button", "修改" + button);
    return http.request<Result>("post", "/AlarmConfig/SaveBatch", {
      data
    });
  }
};
/** 新增 */
export const insert = (data?: object) => {
  return http.request<Result>("post", "/AlarmConfig/SaveBatch", {
    data
  });
};
/** 修改 */
export const update = (data?: object) => {
  return http.request<Result>("post", "/AlarmConfig/SaveBatch", {
    data
  });
};

/** 参数列表 */
export const deviceParamList = (data?: object) => {
  localStorage.setItem("button", "获取设备参数列表");
  return http.request<Result>("post", "/DeviceParam/GetListByPage", {
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
  deviceParamList
};
