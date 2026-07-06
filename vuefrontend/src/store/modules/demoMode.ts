import { defineStore } from "pinia";
import { storage } from "@/utils/storage";

// 双模式状态管理：实时模式、演示模式
export const useDemoModeStore = defineStore("demoMode", {
  state: () => ({
    currentMode: "demo", // 当前模式：realtime(实时) | demo(演示8月)
    demoYear: "2025", // 演示模式固定年份
    demoMonth: "08", // 演示模式固定月份（8月）
    demoDay: "31" // 演示模式固定日期（31号）
  }),

  getters: {
    // 获取当前模式显示文本
    currentModeText: state => {
      switch (state.currentMode) {
        case "realtime":
          return "实时模式";
        case "demo":
          return "演示模式";
        default:
          return "实时模式";
      }
    },

    // 获取演示日期显示文本（用于日模式）
    demoDateText: state =>
      `${state.demoYear}年${state.demoMonth}月${state.demoDay}日`,

    // 获取月份显示文本
    monthText: state => {
      if (state.currentMode === "demo") {
        return `${state.demoYear}年${state.demoMonth}月`;
      }
      return "实时数据";
    },
    // 获取日期选择器的日期范围
    datePickerRange: state => {
      if (state.currentMode === "demo") {
        return {
          min: `${state.demoYear}-${state.demoMonth}-01`,
          max: `${state.demoYear}-${state.demoMonth}-31`
        };
      }
      return null; // 实时模式不需要日期选择器
    },
    // 是否为演示模式（兼容性）
    isDemoMode: state => state.currentMode === "demo"
  },

  actions: {
    // 切换到下一个模式
    switchToNextMode() {
      const modes = ["realtime", "demo"];
      const currentIndex = modes.indexOf(this.currentMode);
      const nextIndex = (currentIndex + 1) % modes.length;
      this.currentMode = modes[nextIndex];
      storage.setItem("currentMode", this.currentMode);
      console.log(`已切换到: ${this.currentModeText}`);
    },
    // 设置当前模式
    setCurrentMode(mode: "realtime" | "demo") {
      this.currentMode = mode;
      storage.setItem("currentMode", mode);
      console.log(`模式已设置为: ${this.currentModeText}`);
    },

    // 切换演示模式（兼容性方法）
    toggleDemoMode() {
      if (this.currentMode === "demo") {
        this.setCurrentMode("realtime");
      } else {
        this.setCurrentMode("demo");
      }
    },
    // 初始化模式状态
    initDemoMode() {
      // 读取保存的当前模式
      const savedCurrentMode = storage.getItem("currentMode");
      if (
        savedCurrentMode &&
        (savedCurrentMode === "realtime" || savedCurrentMode === "demo")
      ) {
        this.currentMode = savedCurrentMode as "realtime" | "demo";
      }
      // 强制使用2025年作为演示年份，忽略本地存储的旧数据
      this.demoYear = "2025";
      this.demoMonth = "08";
      this.demoDay = "31";
      // 更新本地存储为新的2025年数据
      storage.setItem("demoYear", "2025");
      storage.setItem("demoMonth", "08");
      storage.setItem("demoDay", "31");

      console.log("演示模式已初始化为2025年8月31日");
    },

    // 获取演示模式的日期参数
    getDemoModeParams(dateType: string) {
      if (this.currentMode === "demo") {
        // 演示模式：使用2025年8月的固定日期
        const demoYear = this.demoYear;
        const demoMonth = this.demoMonth;
        switch (dateType) {
          case "day":
            return {
              StartTime: `${demoYear}-${demoMonth}-${this.demoDay} 00:00:00`,
              EndTime: `${demoYear}-${demoMonth}-${this.demoDay} 23:59:59`,
              DataType: 2
            };
          case "month":
            return {
              StartTime: `${demoYear}-${demoMonth}-01 00:00:00`,
              EndTime: `${demoYear}-${demoMonth}-31 23:59:59`,
              DataType: 4
            };
          case "year":
            return {
              StartTime: `${demoYear}-01-01 00:00:00`,
              EndTime: `${demoYear}-12-31 23:59:59`,
              DataType: 5
            };
          default:
            return {
              StartTime: `${demoYear}-${demoMonth}-01 00:00:00`,
              EndTime: `${demoYear}-${demoMonth}-31 23:59:59`,
              DataType: 4
            };
        }
      }
      return null; // 实时模式返回null
    },

    // 获取实时模式的时间参数
    getRealTimeParams(dateType: string) {
      const now = new Date();
      const year = now.getFullYear();
      const month = (now.getMonth() + 1).toString().padStart(2, "0");
      const day = now.getDate().toString().padStart(2, "0");

      switch (dateType) {
        case "day":
          // 日模式：传递当天的时间范围
          return {
            StartTime: `${year}-${month}-${day} 00:00:00`,
            EndTime: `${year}-${month}-${day} 23:59:59`,
            DataType: 2
          };
        case "month":
          // 月模式：传递当前月的时间范围
          const lastDay = new Date(year, now.getMonth() + 1, 0).getDate();
          return {
            StartTime: `${year}-${month}-01 00:00:00`,
            EndTime: `${year}-${month}-${lastDay.toString().padStart(2, "0")} 23:59:59`,
            DataType: 4
          };
        case "year":
          // 年模式：传递当前年的时间范围
          return {
            StartTime: `${year}-01-01 00:00:00`,
            EndTime: `${year}-12-31 23:59:59`,
            DataType: 5
          };
        default:
          // 默认月模式
          const defaultLastDay = new Date(
            year,
            now.getMonth() + 1,
            0
          ).getDate();
          return {
            StartTime: `${year}-${month}-01 00:00:00`,
            EndTime: `${year}-${month}-${defaultLastDay.toString().padStart(2, "0")} 23:59:59`,
            DataType: 4
          };
      }
    },

    // 根据日期类型获取DataType
    getDataTypeByDateType(dateType: string) {
      switch (dateType) {
        case "day":
          return 2;
        case "month":
          return 4;
        case "year":
          return 5;
        default:
          return 4;
      }
    },

    // 获取API参数（支持两种模式）
    getDemoApiParams(originalParams: any, dateType: string) {
      if (this.currentMode === "realtime") {
        // 实时模式：添加当前时间参数
        const realTimeParams = this.getRealTimeParams(dateType);
        return {
          ...originalParams,
          ...realTimeParams
        };
      }

      // 演示模式：使用时间范围参数
      const modeParams = this.getDemoModeParams(dateType);
      if (!modeParams) return originalParams;

      // 合并原始参数和模式参数
      return {
        ...originalParams,
        ...modeParams,
        // 确保使用正确的日期范围
        StartTime: modeParams.StartTime,
        EndTime: modeParams.EndTime,
        DataType: modeParams.DataType
      };
    },

    // 获取告警查询参数（支持两种模式）
    getDemoAlarmParams(originalParams: any, dateType: string = "month") {
      if (this.currentMode === "realtime") {
        // 实时模式：使用当前时间作为开始和结束时间
        const realTimeParams = this.getRealTimeParams(dateType);
        return {
          ...originalParams,
          starttime: realTimeParams.StartTime,
          endtime: realTimeParams.EndTime
        };
      }

      // 演示模式：使用时间范围
      const modeParams = this.getDemoModeParams(dateType);
      if (!modeParams) return originalParams;

      return {
        ...originalParams,
        starttime: modeParams.StartTime,
        endtime: modeParams.EndTime
      };
    },

    // 设置演示日期（可选功能，用于未来扩展）
    setDemoDate(year: string, month: string, day?: string) {
      this.demoYear = year;
      this.demoMonth = month;
      if (day) {
        this.demoDay = day;
      }
      storage.setItem("demoYear", year);
      storage.setItem("demoMonth", month);
      if (day) {
        storage.setItem("demoDay", day);
      }
    },

    // 设置演示日期（通过Date对象）
    setDemoDateByDate(date: Date) {
      const year = date.getFullYear().toString();
      const month = (date.getMonth() + 1).toString().padStart(2, "0");
      const day = date.getDate().toString().padStart(2, "0");

      this.demoYear = year;
      this.demoMonth = month;
      this.demoDay = day;

      storage.setItem("demoYear", year);
      storage.setItem("demoMonth", month);
      storage.setItem("demoDay", day);

      console.log(`演示日期已更新为: ${year}-${month}-${day}`);
    },

    // 仅设置演示日期（保持年月不变）
    setDemoDay(day: string) {
      // 确保日期在1-31范围内
      const dayNum = parseInt(day);
      if (dayNum >= 1 && dayNum <= 31) {
        this.demoDay = day.padStart(2, "0");
        storage.setItem("demoDay", this.demoDay);
        console.log(
          `演示日期已更新为: ${this.demoYear}-${this.demoMonth}-${this.demoDay}`
        );
      }
    },

    // 重置为默认演示日期
    resetDemoDate() {
      this.demoYear = "2024";
      this.demoMonth = "08";
      this.demoDay = "31";
      storage.removeItem("demoYear");
      storage.removeItem("demoMonth");
      storage.removeItem("demoDay");
    }
  }
});

// 导出hook函数供组件使用
export function useDemoModeStoreHook() {
  return useDemoModeStore();
}
