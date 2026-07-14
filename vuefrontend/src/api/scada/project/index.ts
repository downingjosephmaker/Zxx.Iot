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

/* ───────────── 项目类型二元化：组态项目(scada) / 报表项目(dash) ─────────────
 * 两类项目共用同一套引擎与页面，只是落在不同的表(scada_project / dash_project)。
 * 后端两套控制器端点基本同构，唯二差异在保存与删除：
 *   ScadaProject: SaveBatch(List) / DeleteById(projectId)
 *   DashProject : Insert(info)+Update(info) / DeleteByIds(List<long>)
 * 此处把 Dash 磨成与 Scada 相同的接口形状，页面与编辑器便只需一套实现。
 */

export type ProjectKind = "scada" | "dash";

export interface ProjectApi {
  getListByPage: (data?: QueryTableParams) => Promise<ResultTable>;
  deleteByPk: (projectId: number | string) => Promise<Result>;
  saveBatch: (list: any[]) => Promise<Result>;
  buildRuntimeUrl: (projectId: number | string) => string;
  dashPublish: (
    projectId: number | string,
    status: number,
    runtimeUrl?: string
  ) => Promise<Result>;
  getDataInfo: (projectId: number | string) => Promise<Result>;
  saveProjectData: (data: {
    ProjectId: number | string;
    ContentData?: string;
    Thumbnail?: string;
  }) => Promise<Result>;
  uploadBase64Image: typeof uploadBase64Image;
}

/** 报表项目：DashProject 端点适配层 */
const dashApi: ProjectApi = {
  getListByPage: data => {
    storage.setItem("button", "查询报表项目");
    return http.request<ResultTable>("post", "/DashProject/GetListByPage", {
      data
    });
  },
  deleteByPk: projectId => {
    storage.setItem("button", "删除报表项目");
    return http.request<Result>("post", "/DashProject/DeleteByIds", {
      data: [projectId]
    });
  },
  // 无批量端点：按 SnowId 分派到 Insert / Update（页面一次只提交一条）
  saveBatch: async list => {
    storage.setItem("button", "保存报表项目");
    let last: Result;
    for (const info of list) {
      const isNew = !info.SnowId || Number(info.SnowId) === 0;
      last = await http.request<Result>(
        "post",
        isNew ? "/DashProject/Insert" : "/DashProject/Update",
        { data: info }
      );
      if (!last.Status) return last;
    }
    return last;
  },
  buildRuntimeUrl: projectId =>
    `${location.origin}${location.pathname}#/scada/runtime/${projectId}?kind=dash`,
  dashPublish: (projectId, status, runtimeUrl = "") => {
    storage.setItem("button", status === 1 ? "发布报表项目" : "取消发布报表项目");
    return http.request<Result>("get", "/DashProject/DashPublish", {
      params: { projectId, status, runtimeUrl }
    });
  },
  getDataInfo: projectId => {
    storage.setItem("button", "查询报表项目");
    return http.request<Result>("get", "/DashProject/GetDataInfo", {
      params: { projectId }
    });
  },
  saveProjectData: data => {
    storage.setItem("button", "保存报表项目");
    return http.request<Result>("post", "/DashProject/SaveProjectData", {
      data
    });
  },
  uploadBase64Image
};

/** 组态项目：ScadaProject 原生端点 */
const scadaApi: ProjectApi = {
  getListByPage,
  deleteByPk,
  saveBatch: list => saveBatch(list),
  buildRuntimeUrl,
  dashPublish,
  getDataInfo,
  saveProjectData,
  uploadBase64Image
};

/** 按项目类型取 API（默认组态项目） */
export const createProjectApi = (kind: ProjectKind = "scada"): ProjectApi =>
  kind === "dash" ? dashApi : scadaApi;

export default {
  getListByPage,
  deleteByPk,
  saveBatch,
  buildRuntimeUrl,
  dashPublish,
  getDataInfo,
  saveProjectData,
  uploadBase64Image,
  createProjectApi
};
