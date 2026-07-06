import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 建筑 */ const button = "建筑";
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
  return http.request<ResultTable>("post", "/Buildinfo/GetListByPage", {
    data
  });
};
/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>("post", "/Buildinfo/Delete?id=" + id);
};
/** 查询单个*/
export const GetInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>("get", "/Buildinfo/GetInfoByPk?_BuildId=" + id);
};
/** 保存建筑额外信息*/
export const saveBatch = (data?: object) => {
  localStorage.setItem("button", "保存建筑额外信息" + button + "");
  return http.request<Result>("post", "/BuildinfoAttribute/SaveBatch", {
    data
  });
};

/** 保存建筑告警配置信息*/
export const NoteSaveBatch = (data?: object) => {
  localStorage.setItem("button", "保存建筑额外信息" + button + "");
  return http.request<Result>("post", "/Buildinfo/NoteSaveBatch", {
    data
  });
};
/** 新增 */
export const insert = (data?: object) => {
  localStorage.setItem("button", "新增" + button + "");
  return http.request<Result>("post", "/Buildinfo/Insert", {
    data
  });
};
/** 修改 */
export const update = (data?: object) => {
  localStorage.setItem("button", "修改" + button + "");
  return http.request<Result>("post", "/Buildinfo/Update", {
    data
  });
};

/** 查询建筑额外信息*/
export const GetBuilInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询建筑额外信息");
  return http.request<Result>(
    "get",
    "/BuildinfoAttribute/GetInfoByPk?_BuildId=" + id
  );
};

/** 删除平面图数据 */
export const deleteByPkWaPian = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "平面图");
  return http.request<Result>(
    "post",
    "/BuildDianweiMap/DeleteByPk?_BuildId=" + id
  );
};

export default {
  getListByPage,
  deleteByPk,
  GetInfoByPk,
  saveBatch,
  insert,
  update,
  GetBuilInfoByPk,
  NoteSaveBatch,
  deleteByPkWaPian
};
