import { store } from "@/store";
import { defineStore } from "pinia";
import MQTT from "mqtt";
import { emitter } from "@/utils/mitt";

export const useMqttStore = defineStore({
  id: "pure-mqtt",
  state: () => ({
    client: null,
    isConnected: false,
    WebMap: "",
    WebCdReal: "",
    subscriptions: {}
  }),
  getters: {},
  actions: {
    init(data) {
      // 该MQTT模块已被禁用，以避免与plugins/mqtt.ts中的MQTT客户端重复处理告警
      // 所有MQTT功能现在由plugins/mqtt.ts处理
      console.log("MQTT Store已被禁用，使用plugins/mqtt.ts替代");

      // 保留以下代码以避免类型错误，但不执行实际操作
      const { MqttUrl, MqttUser, MqttPass, WebAlarm, WebMap, WebCdReal } = data;
      this.WebMap = WebMap;
      this.WebCdReal = WebCdReal;

      // 不再连接MQTT或订阅主题
      // this.connectToBroker(MqttUrl, MqttUser, MqttPass);
    },
    utf8ArrayToString(uint8Array) {
      const decoder = new TextDecoder("utf-8");
      return decoder.decode(uint8Array);
    },
    async connectToBroker(url, username, password, pendingSubscription?) {
      this.client = MQTT.connect(url, {
        username,
        password,
        reconnectPeriod: 3000, // 可选，设置自动重连间隔（以毫秒为单位）
        keepalive: 30
      });

      // 只有当提供了pendingSubscription参数时才处理订阅
      if (
        pendingSubscription &&
        typeof pendingSubscription.callback === "function"
      ) {
        this.subscribe(pendingSubscription.topic, pendingSubscription.callback);
      }

      // 监听连接事件
      this.client.on("connect", () => {
        console.log("MQTT连接成功connect");
        this.isConnected = true;
        for (const key in this.subscriptions) {
          if (Object.prototype.hasOwnProperty.call(this.subscriptions, key)) {
            const topicCallback = this.subscriptions[key];
            console.log(key);
            this.client.subscribe(key, err => {
              if (err) {
                console.error("Failed to subscribe:", err);
                delete this.subscriptions[key]; // 订阅失败则移除记录
              }
            });
          }
        }
      });
      this.client.on("close", error => {
        if (error) {
          console.error("MQTT断开连接:", error);
        } else {
          console.log("MQTT正常断开连接");
        }
      });
      // 处理断开连接和其他错误情况
      this.client.on("error", err => {
        console.error("MQTT Error:", err);
        this.isConnected = false; // 连接断开
      });
      this.client.on("disconnect", () => {
        console.log("disconnect");
        this.isConnected = false;
      });
      this.client.on("message", (receivedTopic, message) => {
        // console.log("===========>订阅的主题", this.subscriptions);
        // 检查是否是当前订阅的主题
        if (this.subscriptions[receivedTopic]) {
          let result;
          // 根据你的MQTT库实现，解析接收到的消息
          // 这里假设message是一个Buffer对象，需要转换为字符串
          if (typeof message === "object" && message instanceof ArrayBuffer) {
            result = message.toString();
          } else if (message instanceof Uint8Array) {
            result = this.utf8ArrayToString(message);
          } else {
            result = message;
          }
          // 调用用户提供的回调函数来处理消息
          this.subscriptions[receivedTopic](result);
        }
      });
    },
    unsubscribe(topic) {
      console.log("unsubscribe", topic);
      if (this.isConnected && this.client && this.subscriptions[topic]) {
        this.client.unsubscribe(topic, err => {
          if (err) console.error("Failed to unsubscribe:", err);
          delete this.subscriptions[topic]; // 移除已1取消订阅主题的记录
        });
      }
    },
    subscribe(topic, callback) {
      this.subscriptions[topic] = callback; // 存储订阅关系
      console.log("🚀 ~ subscribe ~  this.subscriptions:", this.subscriptions);
    },

    publish(topic, message) {
      if (this.isConnected && this.client) {
        this.client.publish(topic, message);
      }
    },
    disconnect() {
      if (this.isConnected && this.client) {
        this.client.end();
        this.subscriptions = [];
        this.isConnected = false;
      }
    },
    unsubscribeAll() {
      // if (this.isConnected && this.client) {
      for (const topic in this.subscriptions) {
        // if (topic !== "zhpd/gsdx/receive/webalarm/") {
        if (Object.prototype.hasOwnProperty.call(this.subscriptions, topic)) {
          this.unsubscribe(topic);
        }
        // }
      }
    }
  }
});

export function useMqttStoreHook() {
  return useMqttStore(store);
}
