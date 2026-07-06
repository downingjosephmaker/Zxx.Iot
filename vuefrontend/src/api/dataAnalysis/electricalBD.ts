import { http } from "@/utils/http";
import type { Result } from "../type";

/**
 * 获取平衡数据列表
 * @param type 平衡类型 1:电平衡 2:水平衡
 * @param date 查询日期，格式为 YYYY-MM-DD
 * @returns 平衡数据
 */
export const GetBalanceList = (type: number = 1, date?: string) => {
  const buttonText = type === 1 ? "电平衡曲线查询" : "水平衡曲线查询";
  localStorage.setItem("button", buttonText);

  let url = `/Basicunitinfo/GetBalanceList?balancetype=${type}`;
  if (date) {
    url += `&date=${date}`;
  }

  return http.request<Result>("get", url);
};

export default {
  GetBalanceList
};
