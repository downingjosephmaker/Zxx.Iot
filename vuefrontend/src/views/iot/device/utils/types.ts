// 设备管理类型定义
import type { QueryTableParams } from "@/api/type";

export type { QueryTableParams };
export type { DeviceInfoItem, ExpandDeviceInfo } from "@/api/iot/device";

export interface TreeSelectOption {
  value: string;
  label: string;
  children?: TreeSelectOption[];
}

/** 数字主键树节点(建筑/组织挂靠下拉，value=BuildId/DeptId) */
export interface TreeNumberOption {
  value: number;
  label: string;
  children?: TreeNumberOption[];
}

export interface DeviceFormItemProps {
  title?: string;
  DeviceId: number;
  DeviceName: string;
  DeviceTypeCode: string;
  DeviceGuid: string;
  DeviceGateway: string;
  ParentId: number;
  BuildId: number;
  DeptId: number;
  SortBorder: string;
  DeviceIp: string;
  DevicePort: number;
  DeviceCom: number;
  DeviceAdr: number;
  IsCollection: number;
  IsVirtual: number;
  /** 以下为拓展属性(ExpandObject)拍平可编辑字段 */
  EnergyType: string;
  LineNum: string;
  DeviceIMEI: string;
  DeviceSim: string;
  CurrentTransformer: number;
  VoltageTransformer: number;
  /** 编辑透传字段(不渲染表单,防止整行写回时清零运行状态) */
  passthrough?: Record<string, unknown>;
}

export interface DeviceFormProps {
  formInline: DeviceFormItemProps;
  typeOptions?: TreeSelectOption[];
  buildOptions?: TreeNumberOption[];
  deptOptions?: TreeNumberOption[];
}
