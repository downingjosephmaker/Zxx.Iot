import { http } from "@/utils/http";
/** 月度能耗报表 */
const button = "月度能耗报表";
// 定义接口响应类型
interface ReportResponse {
  Result: any;
  Total?: number;
  Success?: boolean;
  Code?: number;
  Message?: string;
}
/**
 * 获取月度能耗报表数据
 * @param firstday 日期 格式：YYYY-MM-DD
 * @returns 月度能耗报表数据
 */
export const GetMonthEnergyReport = (firstday: string) => {
  localStorage.setItem("button", "查询" + button);
  return http.request<ReportResponse>(
    "get",
    `/ReportForm/GetMonthEnergyReport?firstday=${firstday}`
  );
};
/**
 * 获取年度能耗报表数据
 * @param firstday 日期 格式：YYYY-MM-DD (年度报告使用年份的1月1日)
 * @returns 年度能耗报表数据
 */
export const GetYearEnergyReport = (firstday: string) => {
  localStorage.setItem("button", "查询年度能耗报表");
  return http.request<ReportResponse>(
    "get",
    `/ReportForm/GetYearEnergyReport?firstday=${firstday}`
  );
};

export default {
  GetMonthEnergyReport,
  GetYearEnergyReport
};
