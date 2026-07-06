import { http } from "@/utils/http";
import { Result, type ResultTable, type QueryTableParams } from "../../type";
/** 设备 */
const button = "签到记录";
const url = "/EventSign";
/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", url + "/GetListByPage", {
    data
  });
};

export const getListByPage2 = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", url + "/GetNotSignedOffListByPage", {
    data
  });
};

export const PostTemplate = (data?: QueryTableParams) => {
  localStorage.setItem("button", "下载导入模板");
  return http.request<ResultTable>("post", url + "/DownloadBookTemplate", {
    data
  });
};

export const PostListExcel = (data?: QueryTableParams) => {
  localStorage.setItem("button", "导入签到记录");
  return http.request<ResultTable>("post", url + "/LobbySignImport", {
    data
  });
};

export default {
  getListByPage,
  getListByPage2,
  PostTemplate,
  PostListExcel
};
