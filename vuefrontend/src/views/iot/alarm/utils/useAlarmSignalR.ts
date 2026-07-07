import { ref, shallowRef, onBeforeUnmount } from "vue";
import {
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
  type HubConnection
} from "@microsoft/signalr";
import { getSignalRUrl } from "@/config";
import type { EventSignalItem } from "@/api/iot/alarm";

type AlarmHandler = (alarm: EventSignalItem) => void;

/**
 * 告警实时订阅组合式函数
 * 独立连接，按单位 JoinAlarmGroup，断线自动重连并恢复订阅，页面卸载自动断开
 */
export function useAlarmSignalR() {
  const connection = shallowRef<HubConnection | null>(null);
  const connected = ref(false);
  /** 当前已加入的告警组(单位ID)，重连后自动重新 JoinAlarmGroup */
  const joinedUnitId = ref<number | null>(null);

  let onAlarm: AlarmHandler | null = null;
  let manualClosed = false;

  async function start() {
    manualClosed = false;
    const conn = new HubConnectionBuilder()
      .withUrl(getSignalRUrl(), {
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets
      })
      .configureLogging(LogLevel.Warning)
      .build();
    conn.keepAliveIntervalInMilliseconds = 60 * 1000;
    conn.serverTimeoutInMilliseconds = 130 * 1000;

    // ReceiveAlarm 收 EventSignal 的 JSON 串
    conn.on("ReceiveAlarm", (payload: string) => {
      if (!onAlarm) return;
      try {
        onAlarm(JSON.parse(payload));
      } catch {
        /* 忽略非法负载 */
      }
    });

    conn.onclose(() => {
      connected.value = false;
      if (!manualClosed) setTimeout(start, 5000);
    });

    try {
      await conn.start();
      connection.value = conn;
      connected.value = true;
      if (joinedUnitId.value != null) {
        await conn.invoke("JoinAlarmGroup", joinedUnitId.value);
      }
    } catch {
      connected.value = false;
      if (!manualClosed) setTimeout(start, 5000);
    }
  }

  /** 加入某单位的告警组 */
  async function joinUnit(unitId: number) {
    const conn = connection.value;
    if (
      joinedUnitId.value != null &&
      conn?.state === HubConnectionState.Connected
    ) {
      await conn.invoke("LeaveAlarmGroup", joinedUnitId.value).catch(() => {});
    }
    joinedUnitId.value = unitId;
    if (conn?.state === HubConnectionState.Connected) {
      await conn.invoke("JoinAlarmGroup", unitId).catch(() => {});
    }
  }

  function setHandler(handler: AlarmHandler) {
    onAlarm = handler;
  }

  async function stop() {
    manualClosed = true;
    const conn = connection.value;
    if (
      joinedUnitId.value != null &&
      conn?.state === HubConnectionState.Connected
    ) {
      await conn.invoke("LeaveAlarmGroup", joinedUnitId.value).catch(() => {});
    }
    if (conn) {
      await conn.stop().catch(() => {});
      connection.value = null;
    }
    connected.value = false;
  }

  onBeforeUnmount(() => {
    stop();
  });

  return {
    connected,
    joinedUnitId,
    start,
    stop,
    joinUnit,
    setHandler
  };
}
