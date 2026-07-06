import type { App } from "vue";
import { Buffer } from "buffer";
import MQTT from "mqtt";
import type { MqttClient as MqttClientType } from "mqtt";
import { emitter } from "@/utils/mitt";
import { getMqttConfig } from "@/api/mqtt";
import { useUserStoreHook } from "@/store/modules/user";

// 确保全局可访问 Buffer
if (typeof window !== "undefined" && !window.Buffer) {
  window.Buffer = Buffer;
}

// MQTT连接状态
export enum MqttConnectionState {
  CONNECTED = "已连接",
  CONNECTING = "连接中",
  RECONNECTING = "重连中",
  DISCONNECTED = "已断开"
}

// MQTT配置信息接口
export interface MqttInfo {
  MqttUrl: string;
  MqttUser: string;
  MqttPass: string;
  WebReal: string;
  WebAlarm: string;
  WebMap?: string;
  WebCdReal?: string;
}

// MQTT客户端和状态
class MqttClient {
  private status: MqttConnectionState = MqttConnectionState.DISCONNECTED;
  private mqttInfo: MqttInfo | null = null;
  private client: MqttClientType | null = null;
  private subscriptions: Map<string, ((message: any) => void)[]> = new Map();
  private isConnected = false;

  // 获取当前连接状态
  getStatus(): MqttConnectionState {
    return this.status;
  }
  // 获取MQTT配置信息
  async getMqttInfo(): Promise<MqttInfo | null> {
    try {
      const response = await getMqttConfig();
      if (!response || !response.Status) {
        // console.error("获取MQTT配置信息失败:", response?.Message || "未知错误");
        return null;
      }
      // 解析Result字段中的JSON字符串
      try {
        const mqttConfig: MqttInfo = JSON.parse(response.Result);
        // console.log("获取MQTT配置成功:", mqttConfig);
        this.mqttInfo = mqttConfig;
        return mqttConfig;
      } catch (_error) {
        // console.error("解析MQTT配置失败:", error);
        return null;
      }
    } catch (_error) {
      // console.error("请求MQTT配置接口失败:", error);
      return null;
    }
  }

  // 连接MQTT服务器
  async connect(): Promise<boolean> {
    try {
      // console.log("正在连接MQTT服务...");

      // 检查是否已连接
      if (this.isConnected && this.client) {
        // console.log("MQTT已连接，无需重连");
        return true;
      }

      // 获取MQTT配置信息
      if (!this.mqttInfo) {
        // console.log("获取MQTT配置信息...");
        this.mqttInfo = await this.getMqttInfo();
        if (!this.mqttInfo || !this.mqttInfo.MqttUrl) {
          // console.error("无法获取MQTT配置信息或配置无效");
          return false;
        }
      }

      // 更新状态为连接中
      this.status = MqttConnectionState.CONNECTING;
      emitter.emit("mqttStatusChange", this.status);

      // 连接选项
      const options: MQTT.IClientOptions = {
        clean: true,
        connectTimeout: 30000,
        clientId: `web_client_${Math.random().toString(16).substring(2, 10)}`,
        reconnectPeriod: 5000 // 自动重连间隔
      };

      // 添加认证信息
      if (this.mqttInfo.MqttUser) {
        options.username = this.mqttInfo.MqttUser;
      }

      if (this.mqttInfo.MqttPass) {
        options.password = this.mqttInfo.MqttPass;
      }

      // console.log("初始化MQTT连接...", {
      //   url: this.mqttInfo.MqttUrl,
      //   user: this.mqttInfo.MqttUser ? "已配置" : "未配置",
      //   topics: {
      //     alarm: this.mqttInfo.WebAlarm,
      //     real: this.mqttInfo.WebReal
      //   }
      // });

      // 创建MQTT客户端并连接
      return new Promise<boolean>(resolve => {
        try {
          // 连接MQTT服务器
          this.client = MQTT.connect(this.mqttInfo!.MqttUrl, options);
          // 连接成功事件
          this.client.on("connect", () => {
            // console.log("MQTT连接成功");
            this.isConnected = true;
            this.status = MqttConnectionState.CONNECTED;
            emitter.emit("mqttStatusChange", this.status);

            // 从用户存储中获取单位ID
            const unitId = useUserStoreHook()?.unitId || "";
            // 恢复之前的订阅
            this.resubscribe();

            // 订阅实时数据主题
            if (this.mqttInfo?.WebReal) {
              const realTopic = unitId
                ? `${this.mqttInfo.WebReal}${unitId}`
                : this.mqttInfo.WebReal;
              // console.log(`订阅实时数据主题: ${realTopic}`);
              this.subscribe(realTopic, message => {
                try {
                  const payload = JSON.parse(message);
                  // console.log("实时数据:", payload);
                  emitter.emit("mqttRealData", payload);
                } catch (_error) {
                  // console.error("解析实时数据失败:", error);
                  emitter.emit("mqttRealData", message);
                }
              });
            }

            // 订阅告警数据主题
            if (this.mqttInfo?.WebAlarm) {
              const alarmTopic = unitId
                ? `${this.mqttInfo.WebAlarm}${unitId}`
                : this.mqttInfo.WebAlarm;
              // console.log(`订阅告警数据主题: ${alarmTopic}`);
              this.subscribe(alarmTopic, message => {
                try {
                  // 解析消息内容
                  let payload;
                  try {
                    // 尝试解析JSON
                    payload = JSON.parse(message);
                    // 添加处理跟踪标记
                    // 为消息添加唯一标识符以帮助调试
                    const msgId = `msg-${Date.now()}-${Math.floor(Math.random() * 1000)}`;
                    if (Array.isArray(payload)) {
                      // 如果是数组，给每个元素添加处理标记
                      payload.forEach((item, index) => {
                        if (typeof item === "object" && item !== null) {
                          item._mqttProcessed = false; // 初始设为未处理
                          item._msgId = `${msgId}-${index}`; // 添加消息ID
                        }
                      });
                      // console.log(
                      //   `[MQTT] 收到告警数据数组，长度: ${payload.length}`
                      // );
                    } else if (payload && typeof payload === "object") {
                      // 如果是单个对象
                      payload._mqttProcessed = false; // 初始设为未处理
                      payload._msgId = msgId; // 添加消息ID
                      // console.log(`[MQTT] 收到告警数据对象: ${msgId}`);
                    }

                    // 发送告警数据事件
                    // console.log(
                    //   `[MQTT] 发送mqttAlarmData事件: ${typeof payload === "object" ? payload._msgId : "未知ID"}`
                    // );
                    emitter.emit("mqttAlarmData", payload);
                  } catch (_error) {
                    // console.error("[MQTT] 解析告警数据失败:", error);
                    // 即使解析失败，也发送原始消息
                    emitter.emit("mqttAlarmData", message);
                  }
                } catch (_error) {
                  // console.error("[MQTT] 处理MQTT告警数据失败:", error);
                }
              });
            }

            resolve(true);
          });
          // 重连事件
          this.client.on("reconnect", () => {
            // console.log("MQTT正在重连...");
            this.status = MqttConnectionState.RECONNECTING;
            emitter.emit("mqttStatusChange", this.status);
          });

          // 断开连接事件
          this.client.on("close", () => {
            if (this.isConnected) {
              // console.log("MQTT连接已断开");
              this.isConnected = false;
              this.status = MqttConnectionState.DISCONNECTED;
              emitter.emit("mqttStatusChange", this.status);
            }
          });

          // 错误事件
          this.client.on("error", _err => {
            // console.error("MQTT连接错误:", err);
            this.status = MqttConnectionState.DISCONNECTED;
            emitter.emit("mqttStatusChange", this.status);
            resolve(false);
          });

          // 消息事件
          this.client.on("message", (topic, message) => {
            const handlers = this.subscriptions.get(topic);
            if (handlers && handlers.length > 0) {
              const messageStr = message.toString();
              handlers.forEach(handler => {
                handler(messageStr);
              });
            }
          });
        } catch (_error) {
          // console.error("创建MQTT客户端失败:", error);
          this.status = MqttConnectionState.DISCONNECTED;
          emitter.emit("mqttStatusChange", this.status);
          resolve(false);
        }
      });
    } catch (error) {
      // console.error("MQTT连接失败:", error);
      this.status = MqttConnectionState.DISCONNECTED;
      emitter.emit("mqttStatusChange", this.status);
      return false;
    }
  }

  // 重新订阅之前的主题
  private resubscribe() {
    if (!this.client || !this.isConnected) return;

    for (const topic of this.subscriptions.keys()) {
      // console.log(`重新订阅主题: ${topic}`);
      this.client.subscribe(topic);
    }
  }

  // 重新连接MQTT服务器，用于单位切换时更新订阅
  async reconnect(): Promise<boolean> {
    // console.log("重新连接MQTT服务，更新单位ID...");
    if (this.client && this.isConnected) {
      // 先断开当前连接
      this.disconnect();
    }
    // 重新连接
    return await this.connect();
  }

  // 断开连接
  disconnect() {
    if (this.client && this.isConnected) {
      this.client.end();
      this.isConnected = false;
      this.status = MqttConnectionState.DISCONNECTED;
      emitter.emit("mqttStatusChange", this.status);
      // console.log("MQTT连接已断开");
    }
  }

  // 订阅主题
  subscribe(topic: string, callback: (message: any) => void) {
    if (!this.client || !this.isConnected) {
      // console.error("MQTT未连接，无法订阅主题");
      return;
    }

    try {
      // 保存回调函数
      let handlers = this.subscriptions.get(topic);
      if (!handlers) {
        handlers = [];
        this.subscriptions.set(topic, handlers);
      }
      handlers.push(callback);

      // 订阅主题
      this.client.subscribe(topic, err => {
        if (err) {
          // console.error(`订阅主题失败: ${topic}`, err);
          return;
        }
        // console.log(`已订阅主题: ${topic}`);
        emitter.emit("mqttSubscribed", topic);
      });
    } catch (error) {
      // console.error(`订阅主题失败: ${topic}`, error);
    }
  }

  // 取消订阅
  unsubscribe(topic: string) {
    if (!this.client || !this.isConnected) {
      // console.error("MQTT未连接，无法取消订阅主题");
      return;
    }

    try {
      // 移除回调函数
      this.subscriptions.delete(topic);

      // 取消订阅主题
      this.client.unsubscribe(topic, err => {
        if (err) {
          // console.error(`取消订阅主题失败: ${topic}`, err);
          return;
        }
        // console.log(`已取消订阅主题: ${topic}`);
        emitter.emit("mqttUnsubscribed", topic);
      });
    } catch (error) {
      // console.error(`取消订阅主题失败: ${topic}`, error);
    }
  }

  // 发布消息
  publish(topic: string, message: any) {
    if (!this.client || !this.isConnected) {
      // console.error("MQTT未连接，无法发布消息");
      return false;
    }

    try {
      const payload =
        typeof message === "object" ? JSON.stringify(message) : message;

      this.client.publish(
        topic,
        payload,
        {
          qos: 1,
          retain: false
        },
        err => {
          if (err) {
            // console.error(`发布消息失败: ${topic}`, err);
            return;
          }
          // console.log(`消息已发布到主题: ${topic}`);
          emitter.emit("mqttPublished", { topic, message });
        }
      );

      return true;
    } catch (error) {
      // console.error(`处理消息发布失败:`, error);
      return false;
    }
  }
}

// 创建MQTT实例
export const mqttClient = new MqttClient();

// Vue插件安装方法
export const useMQTT = {
  install(app: App) {
    // 将MQTT客户端实例添加到Vue全局属性中
    app.config.globalProperties.$mqtt = mqttClient;

    // 注意：不再自动连接，改为在导航栏组件中手动连接
    // console.log("MQTT插件已注册，等待手动连接");
  }
};

// 导出一个hook供组件使用
export const useMqttClient = () => {
  return {
    connect: mqttClient.connect.bind(mqttClient),
    disconnect: mqttClient.disconnect.bind(mqttClient),
    reconnect: mqttClient.reconnect.bind(mqttClient),
    subscribe: mqttClient.subscribe.bind(mqttClient),
    unsubscribe: mqttClient.unsubscribe.bind(mqttClient),
    publish: mqttClient.publish.bind(mqttClient),
    getStatus: mqttClient.getStatus.bind(mqttClient)
  };
};
