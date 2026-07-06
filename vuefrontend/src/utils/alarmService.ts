import { useAlarmStoreHook } from "@/store/modules/alarm";
import { emitter } from "@/utils/mitt";
import type { AlarmItem } from "@/store/modules/alarm";

/**
 * 告警服务 - 用于在任何地方处理告警功能
 */
export class AlarmService {
  private static instance: AlarmService;
  private alarmStore: ReturnType<typeof useAlarmStoreHook> | null = null;
  private initialized = false;

  /**
   * 获取AlarmService实例
   */
  public static getInstance(): AlarmService {
    if (!AlarmService.instance) {
      AlarmService.instance = new AlarmService();
    }
    return AlarmService.instance;
  }

  /**
   * 延迟获取store
   */
  private getStore(): ReturnType<typeof useAlarmStoreHook> {
    try {
      if (!this.alarmStore) {
        this.alarmStore = useAlarmStoreHook();
      }
      return this.alarmStore;
    } catch (error) {
      console.error("获取告警存储失败:", error);
      // 抛出异常以便上层处理
      throw new Error("获取告警存储失败");
    }
  }

  /**
   * 初始化告警服务
   */
  public init(): boolean {
    if (this.initialized) return true;

    try {
      // 尝试获取存储以验证其可用性
      const store = this.getStore();

      // 验证存储是否具有必要的方法
      if (typeof store.initAlarmListener !== "function") {
        console.error("告警存储不包含必要的方法");
        return false;
      }
      // 初始化告警监听
      store.initAlarmListener();
      this.initialized = true;
      console.log("告警服务初始化成功");
      return true;
    } catch (error) {
      console.error("告警服务初始化失败:", error);
      return false;
    }
  }

  /**
   * 获取所有告警
   */
  public getAllAlarms(): AlarmItem[] {
    const store = this.getStore();
    return typeof store.getAllAlarms === "function"
      ? store.getAllAlarms()
      : store.getAllAlarms;
  }

  /**
   * 获取未读告警
   */
  public getUnreadAlarms(): AlarmItem[] {
    const store = this.getStore();
    const allAlarms =
      typeof store.getAllAlarms === "function"
        ? store.getAllAlarms()
        : store.getAllAlarms;

    // 过滤未处理的告警
    return allAlarms.filter(
      alarm =>
        alarm.detail &&
        (alarm.detail.CheckResult === "未处理" ||
          alarm.detail.CheckResult === "0" ||
          alarm.detail.CheckResult === 0)
    );
  }

  /**
   * 获取未读告警数量
   */
  public getUnreadCount(): number {
    return this.getUnreadAlarms().length;
  }

  /**
   * 获取高级别告警
   */
  public getHighLevelAlarms(): AlarmItem[] {
    const store = this.getStore();
    return typeof store.getHighLevelAlarms === "function"
      ? store.getHighLevelAlarms()
      : store.getHighLevelAlarms;
  }

  /**
   * 标记告警为已读
   * @param id 告警ID
   */
  public markAsRead(id: string): void {
    console.log(`告警标记已读功能已移除: ${id}`);
  }

  /**
   * 标记所有告警为已读
   */
  public markAllAsRead(): void {
    console.log(`标记所有告警已读功能已移除`);
  }

  /**
   * 清空所有告警
   */
  public clearAlarms(): void {
    this.getStore().clearAlarms();
  }

  /**
   * 手动添加一条告警
   * @param alarm 告警内容
   */
  public addAlarm(alarm: Partial<AlarmItem>): void {
    const defaultAlarm: AlarmItem = {
      id: `alarm-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
      type: "system",
      level: 3,
      message: "系统告警",
      timestamp: Date.now(),
      detail: {
        CheckResult: "未处理"
      }
    };

    const newAlarm = { ...defaultAlarm, ...alarm };
    this.getStore().handleAlarm(newAlarm);
  }

  /**
   * 处理MQTT告警数据
   * @param data MQTT订阅接收到的告警数据
   */
  public processAlarmData(data: any): void {
    try {
      // 如果是字符串格式，尝试解析JSON
      let alarmData = data;
      if (typeof data === "string") {
        try {
          alarmData = JSON.parse(data);
        } catch {
          console.error("告警数据不是有效的JSON格式");
          return;
        }
      }

      // 处理特定格式的MQTT告警数据 [{ DataType: 3, OptType: 2, DataContent: "..." }]
      if (Array.isArray(alarmData)) {
        // 检查是否已处理过的消息
        const unprocessedItems = alarmData.filter(item => !item._processed);
        if (unprocessedItems.length === 0) {
          console.log("所有消息已被处理过，跳过处理");
          return;
        }

        // 标记所有消息为已处理
        alarmData.forEach(item => {
          item._processed = true;
        });

        alarmData.forEach(item => {
          // 修改条件，同时支持 OptType 为 1 或 2 的消息
          if (
            item.DataType === 3 &&
            (item.OptType === 2 || item.OptType === 1) &&
            item.DataContent
          ) {
            try {
              // 解析DataContent
              const dataContent =
                typeof item.DataContent === "string"
                  ? JSON.parse(item.DataContent)
                  : item.DataContent;

              // 构建告警对象
              const alarm: Partial<AlarmItem> = {
                id: `mqtt-alarm-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
                type: dataContent.DeviceTypeCode || "设备告警",
                // 根据告警内容设置级别
                level: this.parseAlarmLevel(dataContent),
                message: `${dataContent.DeviceName || "未知设备"}: ${dataContent.AlarmValue || "未知告警"}`,
                timestamp: new Date(
                  dataContent.EventTime || Date.now()
                ).getTime(),
                detail: {
                  ...dataContent,
                  rawData: item,
                  CheckResult: "未处理" // 添加未处理状态
                }
              };

              // 添加告警 - 让 addAlarm 方法负责处理通知，避免重复
              this.addAlarm(alarm);
            } catch (error) {
              console.error("解析DataContent失败:", error);
            }
          }
        });
        return; // 已处理数组格式，直接返回
      }

      // 检查非数组格式的消息是否已处理
      if (alarmData && typeof alarmData === "object" && alarmData._processed) {
        console.log("该消息已被处理过，跳过处理");
        return;
      }

      // 标记消息为已处理
      if (alarmData && typeof alarmData === "object") {
        alarmData._processed = true;
      }

      // 处理常规格式的告警数据
      const alarm: Partial<AlarmItem> = {
        id: alarmData.id || alarmData.AlarmId || `mqtt-alarm-${Date.now()}`,
        type: alarmData.type || alarmData.AlarmType || "mqtt",
        level: this.parseAlarmLevel(alarmData),
        message:
          alarmData.message ||
          alarmData.AlarmContent ||
          alarmData.AlarmValue ||
          "MQTT告警",
        timestamp: alarmData.timestamp || alarmData.AlarmTime || Date.now(),
        detail: {
          ...alarmData,
          CheckResult: "未处理" // 确保添加未处理状态
        }
      };

      // 使用告警处理机制处理 - 让 addAlarm 方法负责处理通知，避免重复
      this.addAlarm(alarm);
    } catch (error) {
      console.error("处理MQTT告警数据失败:", error);
    }
  }

  /**
   * 解析告警级别
   * @param data 告警数据
   * @returns 告警级别
   */
  private parseAlarmLevel(data: any): number {
    try {
      // 优先使用明确的级别字段
      if (data.level) {
        const level = parseInt(data.level);
        // 确保级别在1-3范围内
        return Math.min(Math.max(level, 1), 3);
      }

      if (data.AlarmLevel) {
        const level = parseInt(data.AlarmLevel);
        // 确保级别在1-3范围内
        return Math.min(Math.max(level, 1), 3);
      }

      // 根据告警内容判断级别
      if (data.AlarmValue) {
        // 离线告警通常是最严重的
        if (data.AlarmValue.includes("离线")) {
          return 3; // 严重
        }
        // 异常或报警通常是中等级别
        if (
          data.AlarmValue.includes("异常") ||
          data.AlarmValue.includes("报警")
        ) {
          return 2; // 事故
        }
      }

      // 没有特殊情况，默认为普通告警
      return 1;
    } catch (error) {
      console.error("解析告警级别失败:", error);
      return 1; // 出错时默认为普通级别
    }
  }

  /**
   * 打开告警中心
   */
  public openAlarmCenter(): void {
    emitter.emit("openPanel", "alarm-center");
  }

  /**
   * 判断是否有未读告警
   */
  public hasUnread(): boolean {
    return this.getUnreadCount() > 0;
  }

  /**
   * 判断是否有高级别告警
   */
  public hasHighLevelAlarm(): boolean {
    return this.getStore().getHighLevelAlarms.length > 0;
  }

  /**
   * 添加告警处理器
   * @param handler 处理函数
   */
  public addAlarmHandler(handler: (alarm: AlarmItem) => void): void {
    emitter.on("newAlarm", handler);
  }

  /**
   * 移除告警处理器
   * @param handler 处理函数
   */
  public removeAlarmHandler(handler: (alarm: AlarmItem) => void): void {
    emitter.off("newAlarm", handler);
  }
}

// 导出单例
export const alarmService = AlarmService.getInstance();
