// 点表(设备类型参数)管理类型定义
import type { QueryTableParams } from "@/api/type";

export type { QueryTableParams };
export type {
  DeviceTypeParamItem,
  StatusValueItem
} from "@/api/iot/typeparam";

import type { StatusValueItem } from "@/api/iot/typeparam";

export interface DeviceTypeParamFormItemProps {
  title?: string;
  SnowId: string | number;
  DeviceTypeCode: string;
  SubChannel: string;
  ParamCode: string;
  ParamName: string;
  ParamTypeName: string;
  ParamAddr: number;
  ParamFormula: string;
  /** 数值|状态 */
  ValueType: string;
  ExpandStatusValues: StatusValueItem[];
  ValueUnit: string;
  DecimalDigit: number;
  ParamMaxValue: number;
  ParamMinValue: number;
  ParamChangeValue: number;
  RangeFilterEnable: boolean;
  AmplitudeFilterEnable: boolean;
  MaxAmplitudePercent: number;
  ContinuousFilterEnable: boolean;
  MaxContinuousCount: number;
  IsShow: boolean;
  IsMainShow: boolean;
  IsSet: boolean;
  IsPeak: boolean;
  IsReport: boolean;
  IsCustomAlarm: boolean;
  CollectFuncCode: number;
  CollectDataType: string;
  CollectByteOrder: string;
  CollectBitOffset: number;
  CollectRegLength: number;
  CollectWritable: boolean;
  CollectNodeId: string;
  IsAlarmSource: boolean;
  AlarmConfigId: number;
}

export interface DeviceTypeParamFormProps {
  formInline: DeviceTypeParamFormItemProps;
}
