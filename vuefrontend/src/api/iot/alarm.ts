import { http } from "@/utils/http";
import type { ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 告警中心 */
const button = "告警记录";

/** 状态变化日志/告警记录（对应后端 EventSignal，SignalR ReceiveAlarm 推送同结构） */
export interface EventSignalItem {
  SnowId: number | string;
  /** 事件类型(状态变化/意外情况) */
  EventType?: string;
  /** 内容 */
  EventValue?: string;
  /** 详情 */
  EventContent?: string;
  TenantId?: number;
  UnitName?: string;
  BuildId?: number;
  BuildName?: string;
  DeptId?: number;
  DeptName?: string;
  DeviceTypeCode?: string;
  DeviceTypeName?: string;
  DeviceId?: number;
  DeviceName?: string;
  /** 记录时间 */
  EventTime?: string;
}

/** 分页查询告警历史（按月分表，走通用分页） */
export const getListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/EventSignalDb/GetListByPage", {
    data
  });
};

export default {
  getListByPage
};
