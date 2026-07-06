import { getPluginsList } from "./build/plugins";
import { include, exclude } from "./build/optimize";
import { type UserConfigExport, type ConfigEnv, loadEnv } from "vite";
import {
  root,
  alias,
  wrapperEnv,
  pathResolve,
  __APP_INFO__
} from "./build/utils";

export default ({ mode }: ConfigEnv): UserConfigExport => {
  const { VITE_CDN, VITE_PORT, VITE_COMPRESSION, VITE_PUBLIC_PATH } =
    wrapperEnv(loadEnv(mode, root));
  return {
    base: VITE_PUBLIC_PATH,
    root,
    resolve: {
      alias
    },
    // 服务端渲染
    server: {
      // 端口号
      port: VITE_PORT,
      host: "0.0.0.0",
      // 本地跨域代理 https://cn.vitejs.dev/config/server-options.html#server-proxy
      proxy: {
        "/Api": {
          // 这里填写后端地址（本地 IotWebApi，端口见 IotWebApi/appsettings.json 的 Urls）
          target: "http://127.0.0.1:13699/Api",
          changeOrigin: true,
          rewrite: path => path.replace(/^\/Api/, "/Api")
        }
      },
      // 预热文件以提前转换和缓存结果，降低启动期间的初始页面加载时长并防止转换瀑布
      warmup: {
        clientFiles: ["./index.html", "./src/{views,components}/*"]
      }
    },
    plugins: getPluginsList(VITE_CDN, VITE_COMPRESSION),
    // https://cn.vitejs.dev/config/dep-optimization-options.html#dep-optimization-options
    optimizeDeps: {
      include,
      exclude,
      esbuildOptions: {
        // 为 MQTT.js 的依赖项提供全局对象，在浏览器中兼容 Node.js modules
        define: {
          global: "globalThis"
        }
      }
    },
    // 静态资源处理
    assetsInclude: ["**/*.ttf", "**/*.woff", "**/*.woff2"],
    build: {
      // https://cn.vitejs.dev/guide/build.html#browser-compatibility
      target: "es2015",
      sourcemap: false,
      // 消除打包大小超过500kb警告
      chunkSizeWarningLimit: 4000,
      rollupOptions: {
        input: {
          index: pathResolve("./index.html", import.meta.url)
        },
        // 静态资源分类打包
        output: {
          chunkFileNames: "static/js/[name]-[hash].js",
          entryFileNames: "static/js/[name]-[hash].js",
          assetFileNames: assetInfo => {
            // 字体文件单独处理
            if (
              assetInfo.name?.endsWith(".ttf") ||
              assetInfo.name?.endsWith(".woff") ||
              assetInfo.name?.endsWith(".woff2")
            ) {
              return "static/fonts/[name]-[hash].[ext]";
            }
            return "static/[ext]/[name]-[hash].[ext]";
          }
        }
      }
    },
    define: {
      __INTLIFY_PROD_DEVTOOLS__: false,
      __APP_INFO__: JSON.stringify(__APP_INFO__),
      // 为 MQTT.js 提供全局对象
      global: "globalThis"
    }
  };
};
