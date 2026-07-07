// 通知渠道类型定义
import type { QueryTableParams } from "@/api/type";

export type { QueryTableParams };
export type { NotifyChannelItem } from "@/api/iot/notify";

export interface NotifyChannelFormItemProps {
  title?: string;
  SnowId: string | number;
  ChannelName: string;
  /** 渠道类型(1:邮件 2:Webhook 3:钉钉机器人 4:企微机器人 5:短信预留) */
  ChannelType: number;
  TargetUrl: string;
  Secret: string;
  Receivers: string;
  GradeFilter: string;
  /** 升级梯队(0=立即;1/2/3=按15/30/60分钟渐进升级) */
  EscalationLevel: number;
  IsEnable: boolean;
}

export interface NotifyChannelFormProps {
  formInline: NotifyChannelFormItemProps;
}
