import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
export const GetDataChartBy = (data?: object) => {
  localStorage.setItem("button", "查询设备设备曲线日");
  return http.request<r>("post", "/EventReportDay/GetDataChartBy", {
    data
  });
};

export const GetDataTableBy = (data?: object) => {
  localStorage.setItem("button", "查询设备设备曲线日");
  return http.request<r>("post", "/EventReportDay/GetDataTableBy", {
    data
  });
};

export const GetHourDataChartBy = (data?: object) => {
  localStorage.setItem("button", "查询设备设备曲线时");
  return http.request<r>("post", "/EventReportDay/GetHourDataChartBy", {
    data
  });
};

export const GetHourDataTableBy = (data?: object) => {
  localStorage.setItem("button", "查询设备曲线表格时");
  return http.request<r>("post", "/EventReportDay/GetDataTableBy", {
    data
  });
};

export const GetListByPage = (data?: object) => {
  localStorage.setItem("button", "查询设备日报表数据");
  return http.request<Result>("post", "/EventReportDay/GetListByPage", {
    data
  });
};

export default {
  GetDataTableBy,
  GetDataChartBy,
  GetHourDataChartBy,
  GetHourDataTableBy,
  GetListByPage
};
