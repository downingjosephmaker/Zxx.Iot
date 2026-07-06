import { http } from "@/utils/http";
import type { Result, QueryTableParams } from "../type";

export const GetDataChartBy = (data?: QueryTableParams) => {
  localStorage.setItem("button", "根据条件曲线查询");
  return http.request<Result>("post", "/EventPeakDay/GetDataChartBy", {
    data
  });
};

export const GetDataTableBy = (data?: QueryTableParams) => {
  localStorage.setItem("button", "根据条件表格查询");
  return http.request<Result>("post", "/EventPeakDay/GetDataTableBy", {
    data
  });
};

export const GetPeakDayReport = (data?: QueryTableParams) => {
  localStorage.setItem("button", "获取逐日极值数据");
  return http.request<Result>("post", "/EventPeakDay/GetPeakDayReport", {
    data
  });
};
// 获取报表类型极值数据
export const GetParamPeakSelect = (typecode?: any) => {
  localStorage.setItem("button", "根据条件表格查询");
  return http.request<Result>(
    "get",
    "/DeviceTypeParam/GetParamPeakSelect?typecode=" + typecode
  );
};
// 获取报表类型统计数据
export const GetParamReportSelect = (typecode?: any) => {
  localStorage.setItem("button", "根据条件表格查询");
  return http.request<Result>(
    "get",
    "/DeviceTypeParam/GetParamReportSelect?typecode=" + typecode
  );
};

// 获取报表类型统计数据
export const GetParamReportSelectByBuild = (typecode?: any) => {
  localStorage.setItem("button", "根据条件表格查询");
  return http.request<Result>(
    "get",
    "/DeviceTypeParam/GetParamReportSelectByBuild?buildid=" + typecode
  );
};

// GetParamTypeSelectBy;

export default {
  GetDataChartBy,
  GetDataTableBy,
  GetPeakDayReport,
  GetParamPeakSelect,
  GetParamReportSelect,
  GetParamReportSelectByBuild
};
