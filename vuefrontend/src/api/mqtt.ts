import { http } from "@/utils/http";

/**
 * MQTT配置信息接口
 */
export interface MqttInfo {
  MqttUrl: string;
  MqttUser: string;
  MqttPass: string;
  WebReal: string;
  WebAlarm: string;
  WebMap?: string;
  WebCdReal?: string;
}

// API返回的数据格式
export interface ApiResponse {
  Status: boolean;
  Result: string;
  Timestamp: string;
  Message: string;
  Total: number;
}

/**
 * 获取MQTT配置信息
 * @returns MQTT配置接口响应
 */
export const getMqttConfig = () => {
  return http.request<ApiResponse>("get", "/AdminMqttparam/GetMqtt");
};

/**
 * 连接MQTT服务器
 * @param config MQTT连接配置
 * @returns 连接结果
 */
export const connectMqtt = (config: any) => {
  return http.request<ApiResponse>("post", "/AdminMqttparam/Connect", { data: config });
};

/**
 * 断开MQTT连接
 * @returns 断开连接结果
 */
export const disconnectMqtt = () => {
  return http.request<ApiResponse>("post", "/AdminMqttparam/Disconnect");
};

/**
 * 订阅MQTT主题
 * @param topic 主题名称
 * @param qos 服务质量等级
 * @returns 订阅结果
 */
export const subscribeTopic = (topic: string, qos: number = 0) => {
  return http.request<ApiResponse>("post", "/AdminMqttparam/Subscribe", { 
    data: { topic, qos } 
  });
};

/**
 * 发布MQTT消息
 * @param topic 主题名称
 * @param message 消息内容
 * @param qos 服务质量等级
 * @returns 发布结果
 */
export const publishMessage = (topic: string, message: string, qos: number = 0) => {
  return http.request<ApiResponse>("post", "/AdminMqttparam/Publish", { 
    data: { topic, message, qos } 
  });
};
