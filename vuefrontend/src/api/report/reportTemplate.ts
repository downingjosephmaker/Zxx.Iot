import { http } from "@/utils/http";
import type { uploadType, ResultLC, QueryTableParams } from "@/api/type";

type Result = {
  success: boolean;
  data: Array<any>;
};
/** 模板 */
const button = "模板";
export const getListByPage = (params?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>("post", "/custom/forms/template/list", {
    params
  });
};
export const getCharData = id => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>("get", "/custom/forms/getCharData?id=" + id);
};
export const buildReportFile = data => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>("post", "/custom/forms/generate", {
    data
  });
};
export const uploadTempFile = fileData => {
  console.log(
    "🚀 ~ uploadTempFil333333333333333333333333333333333333333333e ~ fileData:",
    fileData
  );
  localStorage.setItem("button", "保存" + button + "");
  const filename = "test";
  return http.request<Result>("post", "/oss/upload", fileData, {
    headers: {
      "Content-Type":
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
      // "Content-Disposition": `attachment; filename="${filename}"`
    }
  });
};

export const deleteByTemplate = id => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<uploadType>("get", "/custom/forms/template/" + id);
};
export const saveTemp = (data: object) => {
  localStorage.setItem("button", "保存" + button + "");
  return http.request<uploadType>("post", "/custom/forms/template/add", {
    data: data
  });
};

export const updateTemplate = (data: object) => {
  localStorage.setItem("button", "保存" + button + "");
  return http.request<uploadType>("post", "/custom/forms/template/update", {
    data: data
  });
};
export default {
  getListByPage,
  buildReportFile,
  uploadTempFile,
  saveTemp,
  updateTemplate,
  getCharData,
  deleteByTemplate
};
