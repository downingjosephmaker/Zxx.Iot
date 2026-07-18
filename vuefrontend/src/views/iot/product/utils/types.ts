// 产品类型(设备类型)管理类型定义
import type { QueryTableParams } from "@/api/type";

export type { QueryTableParams };
export type { DeviceTypeItem, ExpandDeviceType } from "@/api/iot/devicetype";

export interface DeviceTypeFormItemProps {
  title?: string;
  /** 主键编码，修改时禁改 */
  TypeCode: string;
  TypeName: string;
  /** 上级类型编码，空为顶级 */
  ParentId: string;
  /** 排序号，留空服务端自动生成 */
  SortBorder: string;
  IsEnable: boolean;
  /** 修改时透传，服务端Update会整行写回 */
  HasChild?: boolean;
  /** 以下为拓展属性(ExpandObject)拍平字段，提交前组装 */
  OfflineMinute: number;
  SubChannels: number;
  SbjgType: boolean;
  MqttKey: string;
}

export interface TreeSelectOption {
  value: string;
  label: string;
  disabled?: boolean;
  children?: TreeSelectOption[];
}

export interface DeviceTypeFormProps {
  formInline: DeviceTypeFormItemProps;
  typeOptions?: TreeSelectOption[];
}
