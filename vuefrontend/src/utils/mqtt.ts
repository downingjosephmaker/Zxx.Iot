import mqtt, { type MqttClient, type IClientOptions } from "mqtt";
import { ElMessage } from "element-plus";

// MQTT连接配置
export interface MqttConfig {
  host: string;
  port: number;
  protocol: "ws" | "wss" | "mqtt" | "mqtts";
  username?: string;
  password?: string;
  clientId?: string;
  keepalive?: number;
  reconnectPeriod?: number;
  connectTimeout?: number;
  clean?: boolean;
}

// 消息处理器接口
export interface MessageHandler {
  (topic: string, message: Buffer): void;
}

// MQTT客户端管理类
export class MqttManager {
  private client: MqttClient | null = null;
  private config: MqttConfig | null = null;
  private messageHandlers: Map<string, MessageHandler[]> = new Map();
  private isConnected: boolean = false;
  private reconnectAttempts: number = 0;
  private maxReconnectAttempts: number = 10;

  constructor() {
    this.setupEventHandlers();
  }

  /**
   * 连接MQTT服务器
   */
  async connect(config: MqttConfig): Promise<void> {
    return new Promise((resolve, reject) => {
      try {
        this.config = config;

        // 生成客户端ID
        const clientId =
          config.clientId ||
          `scada_client_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

        // MQTT连接选项
        const options: IClientOptions = {
          clientId,
          username: config.username,
          password: config.password,
          keepalive: config.keepalive || 60,
          reconnectPeriod: config.reconnectPeriod || 1000,
          connectTimeout: config.connectTimeout || 30000,
          clean: config.clean !== false,
          will: {
            topic: `scada/client/${clientId}/status`,
            payload: "offline",
            qos: 1,
            retain: true
          }
        };

        // 构建连接URL
        const url = `${config.protocol}://${config.host}:${config.port}`;

        console.log("正在连接MQTT服务器:", url);

        // 创建MQTT客户端
        this.client = mqtt.connect(url, options);

        // 设置消息处理器
        this.client.on("message", (topic, message) => {
          this.handleMessage(topic, message);
        });

        // 连接成功
        this.client.on("connect", () => {
          console.log("MQTT连接成功");
          this.isConnected = true;
          this.reconnectAttempts = 0;

          // 发布上线状态
          this.publish(`scada/client/${clientId}/status`, "online", {
            qos: 1,
            retain: true
          });

          ElMessage.success("MQTT连接成功");
          resolve();
        });

        // 连接失败
        this.client.on("error", error => {
          console.error("MQTT连接错误:", error);
          this.isConnected = false;
          ElMessage.error(`MQTT连接失败: ${error.message}`);
          reject(error);
        });

        // 连接断开
        this.client.on("close", () => {
          console.log("MQTT连接已断开");
          this.isConnected = false;

          if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            console.log(
              `尝试重连 (${this.reconnectAttempts}/${this.maxReconnectAttempts})`
            );
          } else {
            ElMessage.error("MQTT连接已断开，超过最大重连次数");
          }
        });

        // 重连事件
        this.client.on("reconnect", () => {
          console.log("正在重连MQTT服务器...");
        });

        // 离线事件
        this.client.on("offline", () => {
          console.log("MQTT客户端离线");
          this.isConnected = false;
        });
      } catch (error) {
        console.error("MQTT连接异常:", error);
        reject(error);
      }
    });
  }

  /**
   * 断开MQTT连接
   */
  disconnect(): void {
    if (this.client) {
      // 发布离线状态
      if (this.config?.clientId) {
        this.publish(
          `scada/client/${this.config.clientId}/status`,
          "offline",
          { qos: 1, retain: true }
        );
      }

      this.client.end();
      this.client = null;
      this.isConnected = false;
      this.messageHandlers.clear();

      console.log("MQTT连接已断开");
      ElMessage.info("MQTT连接已断开");
    }
  }

  /**
   * 订阅主题
   */
  subscribe(topic: string, handler: MessageHandler, qos: 0 | 1 | 2 = 0): void {
    if (!this.client || !this.isConnected) {
      console.error("MQTT客户端未连接");
      return;
    }

    this.client.subscribe(topic, { qos }, error => {
      if (error) {
        console.error(`订阅主题失败: ${topic}`, error);
        ElMessage.error(`订阅主题失败: ${topic}`);
        return;
      }

      console.log(`已订阅主题: ${topic}`);

      // 添加消息处理器
      if (!this.messageHandlers.has(topic)) {
        this.messageHandlers.set(topic, []);
      }
      this.messageHandlers.get(topic)!.push(handler);
    });
  }

  /**
   * 取消订阅主题
   */
  unsubscribe(topic: string, handler?: MessageHandler): void {
    if (!this.client || !this.isConnected) {
      console.error("MQTT客户端未连接");
      return;
    }

    if (handler) {
      // 移除特定的处理器
      const handlers = this.messageHandlers.get(topic);
      if (handlers) {
        const index = handlers.indexOf(handler);
        if (index > -1) {
          handlers.splice(index, 1);
        }

        // 如果没有处理器了，取消订阅
        if (handlers.length === 0) {
          this.messageHandlers.delete(topic);
          this.client.unsubscribe(topic);
          console.log(`已取消订阅主题: ${topic}`);
        }
      }
    } else {
      // 移除所有处理器并取消订阅
      this.messageHandlers.delete(topic);
      this.client.unsubscribe(topic);
      console.log(`已取消订阅主题: ${topic}`);
    }
  }

  /**
   * 发布消息
   */
  publish(
    topic: string,
    message: string | Buffer,
    options?: { qos?: 0 | 1 | 2; retain?: boolean }
  ): void {
    if (!this.client || !this.isConnected) {
      console.error("MQTT客户端未连接");
      return;
    }

    this.client.publish(topic, message, options || {}, error => {
      if (error) {
        console.error(`发布消息失败: ${topic}`, error);
        ElMessage.error(`发布消息失败: ${topic}`);
      } else {
        console.log(`消息已发布: ${topic}`);
      }
    });
  }

  /**
   * 设置消息处理器
   */
  private setupEventHandlers(): void {
    // 在connect方法中设置，避免重复设置
  }

  /**
   * 处理接收到的消息
   */
  private handleMessage(topic: string, message: Buffer): void {
    console.log(`收到消息: ${topic}`, message.toString());

    // 查找匹配的处理器
    for (const [subscribedTopic, handlers] of this.messageHandlers) {
      if (this.topicMatches(subscribedTopic, topic)) {
        handlers.forEach(handler => {
          try {
            handler(topic, message);
          } catch (error) {
            console.error(`消息处理器执行失败: ${topic}`, error);
          }
        });
      }
    }
  }

  /**
   * 检查主题是否匹配（支持通配符）
   */
  private topicMatches(pattern: string, topic: string): boolean {
    // 简单的通配符匹配实现
    if (pattern === topic) return true;

    // 支持 + 和 # 通配符
    const patternParts = pattern.split("/");
    const topicParts = topic.split("/");

    for (let i = 0; i < patternParts.length; i++) {
      const patternPart = patternParts[i];

      if (patternPart === "#") {
        // # 匹配剩余所有层级
        return true;
      } else if (patternPart === "+") {
        // + 匹配单个层级
        if (i >= topicParts.length) return false;
        continue;
      } else if (patternPart !== topicParts[i]) {
        return false;
      }
    }

    return patternParts.length === topicParts.length;
  }

  /**
   * 获取连接状态
   */
  isConnectedToMqtt(): boolean {
    return this.isConnected;
  }

  /**
   * 获取客户端信息
   */
  getClientInfo(): { connected: boolean; config: MqttConfig | null } {
    return {
      connected: this.isConnected,
      config: this.config
    };
  }
}

// 全局MQTT管理器实例
export const mqttManager = new MqttManager();

// 导出默认实例
export default mqttManager; 