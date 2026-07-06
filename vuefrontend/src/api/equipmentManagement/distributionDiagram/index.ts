import { http } from "@/utils/http";
import { type Result, type ResultTable, QueryTableParams } from "../../type";
/** 配电图参数*/

const button = "配电图参数";
/** 分页查询 */
export const getListByPage = (devicetype?: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>(
    "get",
    "/HtkDevice/GetPdtParamList?devicetype=" + devicetype
  );
};

/** 配电图数据保存*/

export const SavePdtTypeParam = (data?: object) => {
  localStorage.setItem("button", "配电图数据保存");
  return http.request<Result>("post", `/HtkDevice/SavePdtTypeParam`, { data });
};

export default {
  getListByPage,
  SavePdtTypeParam
};
