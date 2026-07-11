import axios from "axios";
import type { App } from "vue";

let config: object = {};
const { VITE_PUBLIC_PATH } = import.meta.env;

const setConfig = (cfg?: unknown) => {
  config = Object.assign(config, cfg);
};

const getConfig = (key?: string): PlatformConfigs => {
  if (typeof key === "string") {
    const arr = key.split(".");
    if (arr && arr.length) {
      let data = config;
      arr.forEach(v => {
        if (data && typeof data[v] !== "undefined") {
          data = data[v];
        } else {
          data = null;
        }
      });
      return data;
    }
  }
  return config;
};

/** 获取项目动态全局配置 */
export const getPlatformConfig = async (app: App): Promise<undefined> => {
  app.config.globalProperties.$config = getConfig();
  return new Promise((resolve, reject) => {
    fetch(`${VITE_PUBLIC_PATH}platform-config.json`)
      .then(response => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
      })
      .then(data => {
        console.log("🚀 ~ returnnewPromise ~ data:", data);
        let $config = app.config.globalProperties.$config;
        // 自动注入项目配置
        if (app && $config && typeof data === "object") {
          $config = Object.assign($config, data);
          app.config.globalProperties.$config = $config;
          // 设置全局配置
          setConfig($config);
        }
        resolve($config);
        // 在这里可以使用从服务器获取的数据
      })
      .catch(error => {
        throw "请在public文件夹下添加serverConfig.json配置文件";
        reject(error);
      });
  });
  // return axios({
  //   method: "get",
  //   url: `${VITE_PUBLIC_PATH}platform-config.json`
  // })
  //   .then(({ data: config }) => {
  //     let $config = app.config.globalProperties.$config;
  //     // 自动注入系统配置
  //     if (app && $config && typeof config === "object") {
  //       $config = Object.assign($config, config);
  //       app.config.globalProperties.$config = $config;
  //       // 设置全局配置
  //       setConfig($config);
  //     }
  //     return $config;
  //   })
  //   .catch(() => {
  //     throw "请在public文件夹下添加platform-config.json配置文件";
  //   });
};

/** 本地响应式存储的命名空间 */
const responsiveStorageNameSpace = () => getConfig().ResponsiveStorageNameSpace;

/**
 * 服务端地址运行时配置（发布后免打包修改）：
 * 优先级 platform-config.json > 打包时 .env > 同源默认值。
 * platform-config.json 未加载完成时 getConfig() 为空对象，自然回退 .env，任意时机调用均安全。
 */

/** 后端 API 根地址 */
const getApiUrl = (): string =>
  getConfig().ApiUrl || import.meta.env.VITE_BASE_URL || "/Api";

/** 静态资源/地图瓦片服务地址 */
const getWapianUrl = (): string =>
  getConfig().WapianUrl || import.meta.env.VITE_BASE_URL_WAPIAN || "/htmlstatic/";

/** SignalR 实时推送地址 */
const getSignalRUrl = (): string =>
  getConfig().SignalRUrl ||
  import.meta.env.VITE_BASE_URL_WIRHURL ||
  "/signalr/chatHub";

/** 低代码报表（JimuReport 旁挂服务）地址，空=未部署（页面显示未配置提示） */
const getReportUrl = (): string => getConfig().ReportUrl || "";

export {
  getConfig,
  setConfig,
  responsiveStorageNameSpace,
  getApiUrl,
  getWapianUrl,
  getSignalRUrl,
  getReportUrl
};
