import { http } from "@/utils/http";
import type { uploadType, ResultLC, QueryTableParams } from "@/api/type";

/** 模板 */
const button = "报表";
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>("post", "/custom/forms/generate/list", {
    data
  });
};
export const buildReportFile = data => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>("post", "/custom/forms/generate", { data });
};

export const deleteByReport = id => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<uploadType>(
    "get",
    "/custom/forms/generate/delete?id=" + id
  );
};
export const downloadFile = data => {
  localStorage.setItem("button", "保存" + button + "");
  return http.request(
    "post",
    "/oss/download",
    { data },
    { responseType: "blob" }
  );
};

export const saveTemp = (data: object) => {
  localStorage.setItem("button", "保存" + button + "");
  return http.request<uploadType>("post", "/custom/forms/template/add", {
    data: data
  });
};

export const getListByRecordData = (params?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>(
    "post",
    "/system/recordData/variousStatisticsTableDay",
    { params }
  );
};

export default {
  getListByPage,
  buildReportFile,
  downloadFile,
  saveTemp,
  deleteByReport,
  getListByRecordData
};
