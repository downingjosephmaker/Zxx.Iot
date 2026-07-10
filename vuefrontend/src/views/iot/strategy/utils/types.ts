// 采集/推送策略类型定义
import type { QueryTableParams } from "@/api/type";

export type { QueryTableParams };
export type { CollectStrategyItem, PushStrategyItem } from "@/api/iot/strategy";

/** 采集策略表单 */
export interface CollectStrategyFormItemProps {
  title?: string;
  SnowId: string | number;
  TenantId: number;
  /** 挂靠层级(1=产品,2=设备,3=点位) */
  ScopeType: number;
  ScopeId: string;
  ParamCode: string;
  /** 空=未设置回落下级 */
  CollectCycleMs: number | null;
  CollectCron: string;
  ReportCycleMs: number | null;
}

/** 推送策略表单 */
export interface PushStrategyFormItemProps {
  title?: string;
  SnowId: string | number;
  TenantId: number;
  /** 挂靠层级(1=产品,2=设备,3=点位) */
  ScopeType: number;
  ScopeId: string;
  ParamCode: string;
  /** 推送模式(1=收到即报,2=变化上报,3=定时上报,4=变化+静默兜底,null=未设置) */
  ReportMode: number | null;
  /** 死区类型(0=严格不等,1=绝对死区,2=百分比死区,null=未设置) */
  DeadbandType: number | null;
  DeadbandValue: number | null;
  MinPushIntervalMs: number | null;
  MaxSilentMs: number | null;
  /** 关键属性点位清单(|分隔) */
  DebounceIgnoreKeys: string;
}

export interface CollectStrategyFormProps {
  formInline: CollectStrategyFormItemProps;
}

export interface PushStrategyFormProps {
  formInline: PushStrategyFormItemProps;
}
