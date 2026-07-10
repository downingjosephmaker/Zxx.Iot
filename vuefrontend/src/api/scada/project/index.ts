import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";

import { storage } from "@/utils/storage";
/** SCADA项目 */
const button = "SCADA项目";

/** SCADA项目基本信息（对应后端 ScadaProject 实体 / ProjectInfo DTO） */
export interface ScadaProjectInfo {
  SnowId: number | string;
  ProjectName?: string;
  ProjectDesc?: string;
  ProjectStatus?: number;
  Thumbnail?: string;
  ProjectDefault?: number;
  RuntimeUrl?: string;
  TenantId?: number;
  ExpandJson?: string;
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
  /** GetDataInfo 返回时附带的组态内容 JSON */
  ContentData?: string;
}

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/ScadaProject/GetListByPage", {
    data
  });
};

/** 根据项目ID删除项目信息 */
export const deleteByPk = (projectId: number | string) => {
  storage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/ScadaProject/DeleteById", {
    params: {
      projectId
    }
  });
};

/** 批量保存/修改 */
export const saveBatch = (data?: object) => {
  storage.setItem("button", "保存" + button);
  return http.request<Result>("post", "/ScadaProject/SaveBatch", {
    data
  });
};

/** 构建运行时页地址（hash 路由），发布时写入后端 RuntimeUrl 字段 */
export const buildRuntimeUrl = (projectId: number | string) =>
  `${location.origin}${location.pathname}#/scada/runtime/${projectId}`;

/** 发布/取消发布项目（后端为 GET，发布时需带运行时地址） */
export const dashPublish = (
  projectId: number | string,
  status: number,
  runtimeUrl = ""
) => {
  storage.setItem(
    "button",
    status === 1 ? "发布" + button : "取消发布" + button
  );
  return http.request<Result>("get", "/ScadaProject/DashPublish", {
    params: {
      projectId,
      status,
      runtimeUrl
    }
  });
};

/** 查询项目完整数据（基本信息 + ContentData 组态内容） */
export const getDataInfo = (projectId: number | string) => {
  storage.setItem("button", "查询" + button);
  return http.request<Result>("get", "/ScadaProject/GetDataInfo", {
    params: {
      projectId
    }
  });
};

/** 保存组态内容与缩略图 */
export const saveProjectData = (data: {
  ProjectId: number | string;
  ContentData?: string;
  Thumbnail?: string;
}) => {
  storage.setItem("button", "保存" + button);
  return http.request<Result>("post", "/ScadaProject/SaveProjectData", {
    data
  });
};

/** 画布截图 Base64 上传（复用附件模块），返回的 Message 为文件访问路径 */
export const uploadBase64Image = (data: {
  Base64String: string;
  ImageType: string;
  ImageName: string;
}) => {
  storage.setItem("button", "上传缩略图");
  return http.request<Result>("post", "/AttachFile/UploadBybBase64", {
    data
  });
};

export default {
  getListByPage,
  deleteByPk,
  saveBatch,
  buildRuntimeUrl,
  dashPublish,
  getDataInfo,
  saveProjectData,
  uploadBase64Image
};
