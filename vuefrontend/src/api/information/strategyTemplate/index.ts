import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";

/** 策略模版 */
const button = "策略模版";

/** 批量保存 */
export const saveBatch = (data?: any[]) => {
  localStorage.setItem("button", "批量保存" + button + "");
  return http.request<Result>("post", "/StrategyTemplate/SaveBatch", {
    data
  });
};

/** 根据主键删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>(
    "post",
    "/StrategyTemplate/DeleteByPk?_SnowId=" + id
  );
};

/** 根据主键查询单条数据 */
export const getInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>(
    "get",
    "/StrategyTemplate/GetInfoByPk?_SnowId=" + id
  );
};

/** 根据条件查询分页数据 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/StrategyTemplate/GetListByPage", {
    data
  });
};
