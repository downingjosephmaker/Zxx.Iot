import { http } from "@/utils/http";
import {
  type ResultLC,
  type ResultTable,
  QueryTableParams
} from "../../wechart";
/** 建筑 */ const button = "建筑";
/** 分页查询 获取微信推送列表数据 */

export const GetWechartList = (params: any) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<ResultLC>("get", "/weChat/info/list", { params });
};
/**  获取微信推送详细信息 */

export const getListById = (id?: number) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("get", "/weChat/info/" + id);
};

/**  新增 */

export const addwechartList = (data?: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/weChat/info", { data });
};

/**  删除*/

export const delwechartList = (id?: string) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/weChat/info/" + id);
};
/**  修改 */

export const updatewechartList = (data?: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/weChat/info/update", { data });
};
/**  获取微信推送详细信息 */

// export const getListById = (id?: string) => {
//   localStorage.setItem("button", "查询" + button + "");
//   return http.request<ResultTable>("post", "/Sysrelated/GetListByPage"+ id,
//  );
// };
/** 删除 */

export default {
  GetWechartList,
  getListById,
  addwechartList,
  delwechartList,
  updatewechartList
};
