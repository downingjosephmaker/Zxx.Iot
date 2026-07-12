// 租户管理类型定义
import type { QueryTableParams } from "@/api/type";

export type { QueryTableParams };

/** 租户实体(对应后端 TenantInfo / tenant_info) */
export interface TenantItem {
  TenantId: number;
  /** 上级租户ID(0=根租户) */
  ParentId: number;
  /** 租户层级(1=顶级) */
  TreeLevel: number;
  /** 祖先链(形如 |1|3|7|) */
  FullCode: string;
  /** 租户全称(全路径) */
  FullName: string;
  HasChild: boolean;
  TenantName: string;
  Remark: string;
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
}

export interface TenantFormItemProps {
  title?: string;
  TenantId: number;
  ParentId: number;
  TreeLevel: number;
  FullCode: string;
  FullName: string;
  HasChild: boolean;
  TenantName: string;
  Remark: string;
}

export interface TenantFormProps {
  formInline: TenantFormItemProps;
}
