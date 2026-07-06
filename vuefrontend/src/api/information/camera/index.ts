import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备 */
const button = "摄像头";
const url = "/VideoDevice";
/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", url + "/GetListByPage", {
    data
  });
};
/** 查询单个*/
export const GetInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>("get", url + "/GetInfoByPk?_Id=" + id);
};
/** 查询视频播放流*/
export const VideoControl = (data?: object) => {
  localStorage.setItem("button", "查询视频播放流");
  return http.request<Result>("post", "/VideoControl/DevControl", { data });
};

export const SaveBatch = (data?: object) => {
  localStorage.setItem("button", "查询视频播放流");
  return http.request<Result>("post", "/VideoDevice/SaveBatch", { data });
};

export default {
  getListByPage,
  GetInfoByPk,
  SaveBatch,
  VideoControl
};
