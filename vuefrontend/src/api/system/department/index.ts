import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 楼层 */ const button = "楼层";
/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  data.sconlist ? data.sconlist : (data.sconlist = []);
  data.sconlist.push({
    ParamName: "SortBorder", // 参数表字段
    // ParamType: "", // 查询规则(=|!=|>|>=|<|<=|in|notin|like|isnull|sort)
    // ParamValue: "", // 参数值
    ParamSort: 1 // 排序(0:不处理 1:正序 2:倒序)
  });
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/Deptinfo/GetListByPage", {
    data
  });
};
/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>("post", "/Deptinfo/Delete?id=" + id);
};
/** 查询单个*/
export const GetInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>("get", "/Deptinfo/GetInfoByPk?deptid=" + id);
};
/** 保存建筑额外信息*/
export const InsertDept = (data?: object) => {
  localStorage.setItem("button", "新增" + button + "");
  return http.request<Result>("post", "/Deptinfo/Insert", {
    data
  });
};
/** 保存建筑额外信息*/
export const UpdateDept = (data?: object) => {
  localStorage.setItem("button", "修改" + button + "");
  return http.request<Result>("post", "/Deptinfo/Update", {
    data
  });
};
/** 分页查询单位 */
export const getUnitListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "分页查询单位");
  return http.request<ResultTable>("post", "/Basicunitinfo/GetListByPage", {
    data
  });
};
export const NoteSaveBatch = (data?: object) => {
  localStorage.setItem("button", "消息通知设置");
  return http.request<Result>("post", "/Deptinfo/NoteSaveBatch", {
    data
  });
};

export default {
  getListByPage,
  deleteByPk,
  GetInfoByPk,
  InsertDept,
  UpdateDept,
  NoteSaveBatch,
  getUnitListByPage
};
