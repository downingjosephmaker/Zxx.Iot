import { http } from "@/utils/http";
import type { Result, QueryTableParams } from "../type";

export const GetDataChartBy = (data?: QueryTableParams) => {
  localStorage.setItem("button", "根据条件分项曲线查询");
  return http.request<Result>(
    "post",
    "/EventReportDay/GetSubEneryDataChartBy",
    {
      data
    }
  );
};

export const GetDataTableBy = (data?: QueryTableParams) => {
  localStorage.setItem("button", "根据条件表格查询");
  return http.request<Result>(
    "post",
    "/EventReportDay/GetSubEneryDataTableBy",
    {
      data
    }
  );
};

export const GetSubDataTableExcelBy = (data?: QueryTableParams) => {
  localStorage.setItem("button", "环比导出日报");
  return http.request<Result>(
    "post",
    "/EventReportDay/GetSubEneryDataTableExcelBy",
    {
      data
    }
  );
};

export default {
  GetDataChartBy,
  GetDataTableBy,
  GetSubDataTableExcelBy
};
