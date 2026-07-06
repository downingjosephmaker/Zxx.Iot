import { http } from "@/utils/http";

/** 单位概览报表 */
const button = "单位概览报表";

// 定义接口响应类型
interface ReportResponse {
  Result: any;
  Total?: number;
  Success?: boolean;
  Code?: number;
  Message?: string;
}

/**
 * 月总电量
 */
export const GetEneryOne = (firstday: string) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ReportResponse>(
    "get",
    "/ReportForm/GetEneryOne?firstday=" + firstday
  );
};

/**
 * 月总用水
 */
export const GetEneryTwo = (firstday: string) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ReportResponse>(
    "get",
    "/ReportForm/GetEneryTwo?firstday=" + firstday
  );
};

/**
 * 月总用氢
 */
export const GetEneryThree = (firstday: string) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ReportResponse>(
    "get",
    "/ReportForm/GetEneryThree?firstday=" + firstday
  );
};

/**
 * 月单位面积电耗
 */
export const GetEneryFour = (firstday: string) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ReportResponse>(
    "get",
    "/ReportForm/GetEneryFour?firstday=" + firstday
  );
};

/**
 * （月）仓库区用电/堆高区用电
 */
export const GetEneryFive = (firstday: string) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ReportResponse>(
    "post",
    "/ReportForm/GetEneryFive?firstday=" + firstday
  );
};

/**
 * 表格数据
 * 返回格式：
 * {
 *   "ReportColumns": [
 *     {
 *       "ColumnEn": "",
 *       "ColumnCn": ""
 *     }
 *   ],
 *   "ReportDatas": [
 *     "string"
 *   ]
 * }
 */
export const GetEnerySix = (firstday: string) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ReportResponse>(
    "get",
    "/ReportForm/GetEnerySix?firstday=" + firstday
  );
};

export default {
  GetEneryOne,
  GetEneryTwo,
  GetEneryThree,
  GetEneryFour,
  GetEneryFive,
  GetEnerySix
};
