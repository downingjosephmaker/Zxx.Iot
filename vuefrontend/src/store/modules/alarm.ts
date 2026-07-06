import { defineStore } from "pinia";
import { store } from "@/store";

export interface AlarmItem {
  id: string;
  type: string;
  level: number;
  message: string;
  timestamp: number;
  detail?: any;
}

export interface AlarmState {
  alarms: AlarmItem[]; // 所有告警
  unprocessedCount: number; // 未处理告警数
  latestAlarm: AlarmItem | null; // 最新告警
  isInitialized: boolean; // 是否已初始化
}

// 创建一个空的告警store，实际功能已移到AlarmBadge.vue组件中
export const useAlarmStore = defineStore("pure-alarm", {
  state: (): AlarmState => ({
    alarms: [],
    unprocessedCount: 0,
    latestAlarm: null,
    isInitialized: false
  }),
  getters: {},
  actions: {
    // 初始化告警监听 - 仅为保持接口兼容，实际无操作
    initAlarmListener() {
      console.log("告警功能已迁移到AlarmBadge.vue组件中");
      this.isInitialized = true;
    },

    // 空方法，保持接口兼容
    removeAlarmListener() {
      this.isInitialized = false;
    },

    // 空方法，保持接口兼容
    handleAlarm(data: any) {
      console.log("告警功能已迁移到AlarmBadge.vue组件中");
    }
  }
});

export function useAlarmStoreHook() {
  return useAlarmStore(store);
}
