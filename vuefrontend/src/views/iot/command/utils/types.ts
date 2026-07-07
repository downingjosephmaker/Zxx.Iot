// 产品命令白名单类型定义
import type { QueryTableParams } from "@/api/type";

export type { QueryTableParams };
export type { ProductCommandItem } from "@/api/iot/command";

export interface ProductCommandFormItemProps {
  title?: string;
  SnowId: string | number;
  DeviceTypeCode: string;
  CommandName: string;
  /** 下行控制类型(插件侧ClassName白名单) */
  ClassName: string;
  /** 参数JSON Schema(前端动态表单渲染依据) */
  ParamSchema: string;
  /** 下行内容模板({参数名}占位) */
  ConTemplate: string;
  /** 阀控等高危命令二次确认 */
  NeedConfirm: boolean;
  IsEnable: boolean;
}

export interface ProductCommandFormProps {
  formInline: ProductCommandFormItemProps;
}
