// 导入 polyfills
import { Buffer } from "buffer";
import EventEmitter from "events";

// URL polyfill (为MQTT.js提供URL解析功能)
const urlPolyfill = {
  parse: (urlStr: string) => {
    try {
      const url = new URL(urlStr);
      return {
        protocol: url.protocol,
        hostname: url.hostname,
        port: url.port,
        pathname: url.pathname,
        query: Object.fromEntries(url.searchParams),
        hash: url.hash
      };
    } catch (error) {
      console.error("URL解析失败:", error);
      return {};
    }
  },
  format: (urlObj: any) => {
    try {
      const url = new URL("http://placeholder");
      url.protocol = urlObj.protocol || url.protocol;
      url.hostname = urlObj.hostname || url.hostname;
      url.port = urlObj.port || "";
      url.pathname = urlObj.pathname || "";
      url.hash = urlObj.hash || "";

      if (urlObj.query) {
        const searchParams = new URLSearchParams();
        for (const key in urlObj.query) {
          searchParams.append(key, urlObj.query[key]);
        }
        url.search = searchParams.toString();
      }

      return url.toString();
    } catch (error) {
      console.error("URL格式化失败:", error);
      return "";
    }
  }
};

// Node.js 核心模块的浏览器适配
const browserShims = {
  Buffer: Buffer,
  process: {
    env: {},
    nextTick: (fn: Function, ...args: any[]) => setTimeout(fn, 0, ...args),
    title: "browser"
  },
  EventEmitter: EventEmitter,
  url: urlPolyfill
};

// 添加全局对象
if (typeof window !== "undefined") {
  // 添加 Buffer
  window.Buffer = window.Buffer || Buffer;

  // 添加 process
  if (!window.process) {
    (window as any).process = {
      env: {},
      nextTick: (fn: Function, ...args: any[]) => setTimeout(fn, 0, ...args)
    };
  }

  // 添加 global 对象
  (window as any).global = window;

  // 添加 EventEmitter
  if (!(window as any).EventEmitter) {
    (window as any).EventEmitter = EventEmitter;
  }

  // 提供URL polyfill
  if (!(window as any).url) {
    (window as any).url = urlPolyfill;
  }
}

console.log("MQTT 浏览器环境适配已加载");

export default browserShims;
