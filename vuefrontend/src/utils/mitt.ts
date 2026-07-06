import type { Emitter } from "mitt";
import mitt from "mitt";
import type { AlarmItem } from "@/store/modules/alarm";

/** 全局公共事件需要在此处添加类型 */
type Events = {
  openPanel: string;
  tagOnClick: string;
  logoChange: boolean;
  tagViewsChange: string;
  changLayoutRoute: string;
  tagViewsShowModel: string;
  imageInfo: {
    img: HTMLImageElement;
    height: number;
    width: number;
    x: number;
    y: number;
  };
  // MQTT相关事件
  mqttConnected: void;
  mqttDisconnected: void;
  mqttStatusChange: string;
  mqttMessage: { topic: string; payload: any };
  mqttRealData: any;
  mqttAlarmData: any;
  mqttError: Error;
  mqttSubscribed: string;
  mqttUnsubscribed: string;
  mqttPublished: { topic: string; message: any };
  changAlarm: any;
  // 新增告警相关事件
  newAlarm: AlarmItem;
  alarmRead: string;
  allAlarmsRead: void;
  alarmsCleared: void;
  // 未处理告警数量更新事件
  unprocessedAlarmCountUpdated: number;
};

export const emitter: Emitter<Events> = mitt<Events>();
