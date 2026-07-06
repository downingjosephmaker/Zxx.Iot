import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";

/** 业务模块名称 */
const button = "系统区域管理";

// ==================== 类型定义 ====================

/**
 * 系统区域数据接口
 */
export interface SysAreaData {
  /** 系统字段 */
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
  SnowId?: number;
  UnitId?: number;

  /** 业务字段 */
  AreaId: string; // 行政区划Id
  AreaName: string; // 行政区划名称
  DivisionName: string; // 区域名称
  SortBorder: string; // 排序序号
  TreeLevel?: number; // 角色级别
  ParentId: string; // 上级ID
  FullName: string; // 行政区划名称(全)
  FullCode: string; // 行政区划ID(全)
  HasChild?: boolean; // 是否有子集
  ExpandJson: string; // 拓展属性(json)
  ExpandObject?: any; // 行政区划拓展类

  /** 树形结构扩展字段 */
  children?: SysAreaData[];
  label?: string; // 用于树形组件显示
  value?: string; // 用于树形组件值
}

/**
 * 区域查询参数
 */
export interface SysAreaQueryParams extends QueryTableParams {
  parentId?: string; // 父级ID
  treeLevel?: number; // 树形级别
}

// ==================== API函数 ====================

/**
 * 分页查询区域数据
 * @param params 查询参数
 * @returns 分页数据
 */
export const getListByPage = (params: SysAreaQueryParams) => {
  localStorage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/SysArea/GetListByPage", {
    data: params
  });
};

/**
 * 根据主键查询区域信息
 * @param id 区域ID
 * @returns 区域信息
 */
export const getInfoByPk = (id: string) => {
  localStorage.setItem("button", "查询" + button);
  return http.request<Result>("get", `/SysArea/GetInfoByPk?id=${id}`);
};

/**
 * 新增区域
 * @param data 区域数据
 * @returns 新增结果
 */
export const insertArea = (data: SysAreaData) => {
  localStorage.setItem("button", "新增" + button);
  return http.request<Result>("post", "/SysArea/Insert", {
    data
  });
};

/**
 * 修改区域
 * @param data 区域数据
 * @returns 修改结果
 */
export const updateArea = (data: SysAreaData) => {
  localStorage.setItem("button", "修改" + button);
  return http.request<Result>("post", "/SysArea/Update", {
    data
  });
};

/**
 * 根据区域ID删除区域信息(包含子区域)
 * @param id 区域ID
 * @returns 删除结果
 */
export const deleteArea = (id: number) => {
  localStorage.setItem("button", "删除" + button);
  return http.request<Result>("post", `/SysArea/Delete?id=${id}`);
};

/**
 * 根据国家行政区划刷新区域数据
 * @returns 刷新结果
 */
export const initArea = () => {
  localStorage.setItem("button", "刷新" + button);
  return http.request<Result>("post", "/SysArea/InitArea");
};

/**
 * 条件保存（新增或修改）
 * @param data 区域数据
 * @returns 保存结果
 */
export const saveArea = (data: SysAreaData) => {
  if (!data.SnowId || data.SnowId === 0) {
    return insertArea(data);
  } else {
    return updateArea(data);
  }
};

// ==================== 默认导出 ====================

export default {
  getListByPage,
  getInfoByPk,
  insertArea,
  updateArea,
  deleteArea,
  initArea,
  saveArea
};
