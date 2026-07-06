// 导入 Buffer polyfill 和 EventEmitter
import { Buffer } from "buffer";
import EventEmitter from "events";

// 为 MQTT.js 5.0.2 配置浏览器环境
const configureMqtt = () => {
  // 全局注入 Buffer
  if (typeof window !== "undefined") {
    window.Buffer = Buffer;

    // 确保 EventEmitter 可用
    if (!(window as any).EventEmitter) {
      (window as any).EventEmitter = EventEmitter;
    }

    // 确保 process 对象可用
    if (!window.process) {
      (window as any).process = {
        env: {},
        nextTick: (fn: Function, ...args: any[]) => setTimeout(fn, 0, ...args),
        title: "browser"
      };
    }

    // 确保 global 对象可用
    if (!window.global) {
      (window as any).global = window;
    }
  }

  console.log("MQTT 配置已完成");
};

// 立即执行配置
configureMqtt();

// 导出配置模块
export default { configure: configureMqtt };
