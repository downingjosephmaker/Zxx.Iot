// 北向转发目的地类型定义
import type { QueryTableParams } from "@/api/type";

export type { QueryTableParams };
export type { NorthboundSinkItem } from "@/api/iot/northbound";

/** MQTT目的地连接配置（对应后端 SinkMqttConfig） */
export interface SinkMqttConfigForm {
  Host: string;
  Port: number;
  ClientId: string;
  UserName: string;
  Password: string;
  DataTopic: string;
  EventTopic: string;
}

/** HTTP Webhook目的地连接配置（表单持有Headers的JSON文本，保存时序列化为对象） */
export interface SinkHttpConfigForm {
  Url: string;
  HeadersJson: string;
}

export interface NorthboundSinkFormItemProps {
  title?: string;
  SnowId: string | number;
  SinkName: string;
  /** 目的地类型(1:MQTT 2:HTTP Webhook 3:Kafka预留) */
  SinkType: number;
  /** 连接配置JSON（由表单的mqttConfig/httpConfig同步生成） */
  ConnConfig?: string;
  /** 转发内容(1:仅遥测 2:仅告警 3:遥测+告警) */
  ContentMode: number;
  /** 推送范围(0:全部 1:按产品类型编码 2:按设备ID) */
  ScopeType: number;
  ScopeJson?: string;
  IsEnable: boolean;
  /** 表单辅助字段，保存前剔除 */
  mqttConfig: SinkMqttConfigForm;
  /** 表单辅助字段，保存前剔除 */
  httpConfig: SinkHttpConfigForm;
}

export interface NorthboundSinkFormProps {
  formInline: NorthboundSinkFormItemProps;
}
