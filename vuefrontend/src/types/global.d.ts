// 全局类型声明
declare global {
  interface Window {
    // 已有的全局属性
    __MICRO_APP_ENVIRONMENT__?: boolean;
    __MICRO_APP_UMD_MODE__?: boolean;
    microApp?: any;
    __POWERED_BY_WUJIE__?: boolean;
    $wujie?: any;
    // 添加Buffer类型
    Buffer: typeof Buffer;
  }
}

export {};
