import { http } from "@/utils/http";
import type { ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 基础档案(建筑/组织) —— 供设备表单挂靠下拉复用，仅取分页只读列表 */

/** 建筑（对应后端 BuildInfoEntity；树形结构 ParentId=0 为顶级） */
export interface BuildInfoItem {
  BuildId: number;
  BuildName?: string;
  BuildCode?: string;
  SortBorder?: string;
  TreeLevel?: number;
  ParentId?: number;
  FullName?: string;
  FullCode?: string;
  HasChild?: boolean;
  UnitId?: number;
}

/** 组织（对应后端 DeptInfoEntity；树形结构 ParentId=0 为顶级） */
export interface DeptInfoItem {
  DeptId: number;
  DeptName?: string;
  DeptCode?: string;
  SortBorder?: string;
  TreeLevel?: number;
  ParentId?: number;
  FullName?: string;
  FullCode?: string;
  HasChild?: boolean;
  UnitId?: number;
}

/** 建筑分页查询(按SortBorder排序) */
export const getBuildListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询建筑");
  return http.request<ResultTable>("post", "/Buildinfo/GetListByPage", {
    data
  });
};

/** 组织分页查询(按SortBorder排序) */
export const getDeptListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询组织");
  return http.request<ResultTable>("post", "/Deptinfo/GetListByPage", {
    data
  });
};

export default {
  getBuildListByPage,
  getDeptListByPage
};
