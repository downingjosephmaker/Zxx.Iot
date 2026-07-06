import { http } from "@/utils/http";
import type { ReportResponse } from "../type";

/**
 * 按日期获取配电房抄表数据
 * @param date 日期 格式：YYYY-MM-DD (每月的第1天，代表整个月的数据)
 * @returns 返回包含文件路径的响应
 */
export const getInfoByDate = (date: string) => {
  return http.request<ReportResponse>("get", `/EventYmdReport/GetInfoByDate`, {
    params: { date }
  });
};

/**
 * 根据日期区间生成抄表文件
 * @param bdate 开始日期 格式：YYYY-MM-DD
 * @param edate 结束日期 格式：YYYY-MM-DD
 * @returns 返回包含文件路径的响应
 */
export const getChaobiaoByDateRange = (bdate?: string, edate?: string) => {
  return http.request<ReportResponse>(
    "get",
    `/EventYmdReport/GetChaobiaoByDateRange`,
    {
      params: { bdate, edate }
    }
  );
};

/**
 * 根据日期区间生成用电明细文件
 * @param bdate 开始日期 格式：YYYY-MM-DD
 * @param edate 结束日期 格式：YYYY-MM-DD
 * @returns 返回包含文件路径的响应
 */
export const getYdChaobiaoByDateRange = (bdate?: string, edate?: string) => {
  return http.request<ReportResponse>(
    "get",
    `/EventYmdReport/GetYdChaobiaoByDateRange`,
    {
      params: { bdate, edate }
    }
  );
};

export default {
  getInfoByDate,
  getChaobiaoByDateRange,
  getYdChaobiaoByDateRange
};
