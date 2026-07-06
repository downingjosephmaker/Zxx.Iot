import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";
const button = "同比分析";

export const GetEnergyTbAnalysis = (data?: QueryTableParams) => {
  localStorage.setItem("button", "同比分析按日");
  return http.request<Result>("post", "/EnergyAnalysis/GetEnergyTbAnalysis", {
    data
  });
};

export const GetEnergyTbAnalysisExcel = (data?: QueryTableParams) => {
  localStorage.setItem("button", "同比分析导出");
  return http.request<Result>(
    "post",
    "/EnergyAnalysis/GetEnergyTbAnalysisExcel",
    {
      data
    }
  );
};
export const GetReportAnalysis = (data?: QueryTableParams) => {
  localStorage.setItem("button", "趋势分析");
  return http.request<Result>("post", "/EventReportDay/GetDataChartBy", {
    data
  });
};

export const GetReportataTableBy = (data?: QueryTableParams) => {
  localStorage.setItem("button", "趋势分析表格");
  return http.request<Result>("post", "/EventReportDay/GetDataTableBy", {
    data
  });
};

export const GetDataTableExcelBy = (data?: QueryTableParams) => {
  localStorage.setItem("button", "趋势分析导出");
  return http.request<Result>("post", "/EventReportDay/GetDataTableExcelBy", {
    data
  });
};

export const GetEnergyHbAnalysis = (data?: QueryTableParams) => {
  localStorage.setItem("button", "环比分析");
  return http.request<Result>("post", "/EnergyAnalysis/GetEnergyHbAnalysis", {
    data
  });
};

export const GetEnergyHbAnalysisExcel = (data?: QueryTableParams) => {
  localStorage.setItem("button", "环比导出日报");
  return http.request<Result>(
    "post",
    "/EnergyAnalysis/GetEnergyHbAnalysisExcel",
    {
      data
    }
  );
};

export default {
  GetEnergyTbAnalysis,
  GetEnergyTbAnalysisExcel,
  GetEnergyHbAnalysis,
  GetEnergyHbAnalysisExcel,
  GetReportAnalysis,
  GetReportataTableBy,
  GetDataTableExcelBy
};
