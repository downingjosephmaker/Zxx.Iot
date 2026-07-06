// SCADA组态系统类型定义
import type { QueryTableParams } from "@/api/type";

// 导出QueryTableParams类型供hook.tsx使用
export type { QueryTableParams };

export interface ScadaProjectFormItemProps {
  title?: string;
  SnowId?: string | number;
  ProjectName: string;
  ProjectDesc?: string; // 项目描述
  ProjectStatus: number; // 发布状态(0:未发布 1:发布)
  Thumbnail?: string; // 缩略图路径
  ProjectDefault?: number; // 默认状态(0:未设置 1:默认)
  UnitId?: number; // 单位ID
  ExpandJson?: string; // 拓展属性(json)
}

export interface ScadaProjectItem {
  SnowId: string | number;
  ProjectName: string;
  ProjectDesc?: string; // 项目描述
  ProjectStatus: number; // 发布状态(0:未发布 1:发布)
  Thumbnail?: string; // 缩略图路径
  ProjectDefault?: number; // 默认状态(0:未设置 1:默认)
  IsDeleted?: number;
  UnitId?: number; // 单位ID
  ExpandJson?: string; // 拓展属性(json)
  CreateTime?: string;
  UpdateTime?: string;
  CreateBy?: string;
  UpdateBy?: string;
  CreateId?: number;
  CreateName?: string;
  UpdateId?: number;
  UpdateName?: string;
}

export interface ScadaProjectFormProps {
  formInline: ScadaProjectFormItemProps;
}
