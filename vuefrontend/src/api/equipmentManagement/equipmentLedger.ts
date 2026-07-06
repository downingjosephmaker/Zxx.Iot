import { http } from "@/utils/http";
import type { uploadType, ResultLC, QueryTableParams } from "@/api/type";
import {
  type DataInfo,
  setToken,
  getToken,
  removeToken,
  sessionKey
} from "@/utils/auth";
type Result = {
  success: boolean;
  data: Array<any>;
};
/** 模板 */
const button = "设备台账";
export const addDevFlieLedger = (data: object) => {
  localStorage.setItem("button", "新增" + button + "");
  return http.request<ResultLC>("post", "/htkDeviceFile/add", { data });
};

export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>("post", "/htkDeviceFile/list", {
    data
  });
};
export const getListByPageBigScreen = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>("post", "/htkDeviceFile/list", {
    data
  });
};
export const getAreaRankingList = (params?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>(
    "get",
    "/alarmShielding/alarmShieldingAreaRanking",
    {
      params
    }
  );
};

export const uploadTempFile = (fileData: FormData) => {
  localStorage.setItem("button", "保存" + button + "");
  return http.request<Result>("post", "/oss/upload", fileData, {
    headers: {
      "Content-Type": "multipart/form-data"
    }
  });
};

export const getFileData = (params: object) => {
  localStorage.setItem("button", "获取" + button + "文件信息");
  return http.request<Result>("get", "/oss/download", { params });
};
export const delData = (params: object) => {
  localStorage.setItem("button", "取消" + button + "");
  return http.request<ResultLC>("get", "/alarmShielding/deleteAlarmShielding", {
    params
  });
};
export const deleteByLedger = id => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<uploadType>("get", "/htkDeviceFile/delete?snowId=" + id);
};
export const updateAlarmShield = (data: object) => {
  localStorage.setItem("button", "保存" + button + "");
  return http.request<ResultLC>(
    "post",
    "/alarmShielding/updateAlarmShielding",
    { data }
  );
};

export default {
  getListByPage,
  getAreaRankingList,
  getListByPageBigScreen,
  uploadTempFile,
  addDevFlieLedger,
  updateAlarmShield,
  deleteByLedger,
  delData,
  getFileData
};
