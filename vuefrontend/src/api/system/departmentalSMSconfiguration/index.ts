import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 楼层 */ const button = "楼层";
/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  data.sconlist ? data.sconlist : (data.sconlist = []);
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/DeptinfoNote/GetListByPage", {
    data
  });
};
/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>(
    "post",
    "/DeptinfoNote/DeleteByPk?_DeptCode=" + id
  );
};
/** 查询单个*/
export const GetInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>(
    "get",
    "/DeptinfoNote/GetInfoByPk?_DeptCode=" + id
  );
};
/** 保存建筑额外信息*/
export const SaveBatch = (data?: any) => {
  localStorage.setItem("button", "新增" + button + "");
  return http.request<Result>("post", "/DeptinfoNote/SaveBatch", {
    data
  });
};

export default {
  getListByPage,
  deleteByPk,
  GetInfoByPk,
  SaveBatch
};
