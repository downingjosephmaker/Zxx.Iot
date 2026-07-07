// 规则联动类型定义
import type { QueryTableParams } from "@/api/type";

export type { QueryTableParams };
export type { LinkageRuleItem } from "@/api/iot/linkage";

/** 下发命令动作（对应后端 LinkageActionCommand，DeviceIds表单持逗号分隔文本） */
export interface LinkageCmdConfigForm {
  PluginGuid: string;
  ClassName: string;
  ConContent: string;
  DeviceIdsText: string;
}

/** 写虚拟点位动作（对应后端 LinkageActionVirtualPoint） */
export interface LinkageVpConfigForm {
  DeviceId: number;
  ParamCode: string;
  ParamValue: string;
}

/** 发通知动作（对应后端 LinkageActionNotify） */
export interface LinkageNotifyConfigForm {
  Content: string;
}

/** Webhook动作（对应后端 LinkageActionWebhook） */
export interface LinkageWebhookConfigForm {
  Url: string;
  Body: string;
}

export interface LinkageRuleFormItemProps {
  title?: string;
  SnowId: string | number;
  RuleName: string;
  /** 触发类型(1:点位变化 2:告警产生 3:告警恢复 4:定时cron 5:设备上线 6:设备离线) */
  TriggerType: number;
  /** 触发设备ID(0=任意设备) */
  TriggerDeviceId: number;
  TriggerParamCode: string;
  TriggerCron: string;
  ConditionFormula: string;
  TimeRanges: string;
  /** 动作类型(1:下发命令 2:写虚拟点位 3:发通知 4:Webhook) */
  ActionType: number;
  /** 动作配置JSON（由表单辅助字段同步生成） */
  ActionConfig?: string;
  CooldownSeconds: number;
  IsEnable: boolean;
  /** 表单辅助字段，保存前剔除 */
  cmdConfig: LinkageCmdConfigForm;
  /** 表单辅助字段，保存前剔除 */
  vpConfig: LinkageVpConfigForm;
  /** 表单辅助字段，保存前剔除 */
  notifyConfig: LinkageNotifyConfigForm;
  /** 表单辅助字段，保存前剔除 */
  webhookConfig: LinkageWebhookConfigForm;
}

export interface LinkageRuleFormProps {
  formInline: LinkageRuleFormItemProps;
}
