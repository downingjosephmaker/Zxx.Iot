// 告警屏蔽类型定义
import type { QueryTableParams } from "@/api/type";

export type { QueryTableParams };
export type { AlarmMaskItem } from "@/api/iot/alarmmask";

export interface AlarmMaskFormItemProps {
  title?: string;
  SnowId: string | number;
  /** 屏蔽对象类型(1:全局 2:租户 3:建筑 4:设备类型 5:单设备 6:告警等级) */
  MaskScopeType: number;
  ScopeId: string;
  /** 屏蔽模式(1:永久 2:一次性时间段 3:周期性时间窗) */
  MaskMode: number;
  StartTime: string;
  EndTime: string;
  TimeRanges: string;
  /** 屏蔽动作(1:完全屏蔽不入库 2:静默入库打标不通知 3:降级) */
  MaskAction: number;
  DowngradeGrade: string;
  Reason: string;
  OperatorName: string;
  ExpireAt: string;
  IsEnable: boolean;
}

export interface AlarmMaskFormProps {
  formInline: AlarmMaskFormItemProps;
}
