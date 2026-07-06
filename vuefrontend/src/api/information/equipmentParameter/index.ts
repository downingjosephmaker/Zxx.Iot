import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备参数 */
/** 分页查询 */
const button = "设备参数";
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/DeviceParam/getListByPage", {
    data
  });
};
/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>(
    "post",
    "/DeviceParam/DeleteByPk?_DeviceId=" + id
  );
};
/** 查询单个*/
export const GetInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>(
    "get",
    "/DeviceParam/GetInfoByPk?_DeviceId=" + id
  );
};
/** 批量保存/修改 */
export const saveBatch = (data?: object) => {
  localStorage.setItem("button", "保存/修改" + button + "");
  return http.request<Result>("post", "/DeviceParam/SaveBatch", {
    data
  });
};
export default {
  getListByPage,
  deleteByPk,
  GetInfoByPk,
  saveBatch
};
