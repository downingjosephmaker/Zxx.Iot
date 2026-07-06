import { http } from "@/utils/http";
import type { ResultTable, QueryTableParams } from "../type";
/** 设备 */
const button = "电力数据";

export const GetParamTypeSelectBy = devid => {
  localStorage.setItem("button", "查询" + button + "设备参数类别下拉框");
  return http.request<ResultTable>(
    "get",
    "/HtkDeviceparam/GetParamTypeSelect?devid=" + devid
  );
};

export const GetDataTableBy = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "表格");
  return http.request<ResultTable>("post", "/DataReport/GetPeakDayReport", {
    data
  });
};
export const GetExportTable = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "表格");
  return http.request<ResultTable>(
    "post",
    "/DataReport/GetPeakDayReportExcel",
    {
      data
    }
  );
};

// 极值列表
export const GetPeakDayReport = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "极值列表");
  return http.request<ResultTable>(
    "post",
    "/HtkRecordDataPeakDay/GetPeakDayReport",
    { data }
  );
};

export default {
  GetDataTableBy,
  GetParamTypeSelectBy,
  GetExportTable,
  GetPeakDayReport
};
