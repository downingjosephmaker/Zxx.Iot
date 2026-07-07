import { ref, shallowRef, onBeforeUnmount } from "vue";
import {
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
  type HubConnection
} from "@microsoft/signalr";
import { getSignalRUrl } from "@/config";
import type { TelemetryPointItem } from "@/api/iot/monitor";

/** 设备状态推送(对应后端 EventRun，ReceiveDeviceState) */
export interface DeviceStateItem {
  DeviceId?: number | string;
  DeviceState?: number;
  [key: string]: unknown;
}

type DataHandler = (points: TelemetryPointItem[]) => void;
type StateHandler = (state: DeviceStateItem) => void;

/**
 * 设备实时数据 SignalR 订阅组合式函数
 * 独立连接(与登录页连接互不干扰)，页面卸载自动断开
 */
export function useDeviceSignalR() {
  const connection = shallowRef<HubConnection | null>(null);
  const connected = ref(false);
  /** 当前已加入的设备组，重连后自动重新 JoinDeviceGroup */
  const joinedDeviceId = ref<number | null>(null);

  let onData: DataHandler | null = null;
  let onState: StateHandler | null = null;
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

    // ReceiveDeviceData 收 List<TelemetryPoint> 的 JSON 串
    conn.on("ReceiveDeviceData", (payload: string) => {
      if (!onData) return;
      try {
        const points: TelemetryPointItem[] = JSON.parse(payload);
        onData(points);
      } catch {
        /* 忽略非法负载 */
      }
    });
    // ReceiveDeviceState 收 EventRun 的 JSON 串
    conn.on("ReceiveDeviceState", (payload: string) => {
      if (!onState) return;
      try {
        onState(JSON.parse(payload));
      } catch {
        /* 忽略 */
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
      // 重连后恢复设备组订阅
      if (joinedDeviceId.value != null) {
        await conn.invoke("JoinDeviceGroup", joinedDeviceId.value);
      }
    } catch {
      connected.value = false;
      if (!manualClosed) setTimeout(start, 5000);
    }
  }

  /** 切换订阅的设备：离开旧组、加入新组 */
  async function joinDevice(deviceId: number) {
    const conn = connection.value;
    if (
      joinedDeviceId.value != null &&
      conn?.state === HubConnectionState.Connected
    ) {
      await conn.invoke("LeaveDeviceGroup", joinedDeviceId.value).catch(() => {});
    }
    joinedDeviceId.value = deviceId;
    if (conn?.state === HubConnectionState.Connected) {
      await conn.invoke("JoinDeviceGroup", deviceId).catch(() => {});
    }
  }

  async function leaveDevice() {
    const conn = connection.value;
    if (
      joinedDeviceId.value != null &&
      conn?.state === HubConnectionState.Connected
    ) {
      await conn.invoke("LeaveDeviceGroup", joinedDeviceId.value).catch(() => {});
    }
    joinedDeviceId.value = null;
  }

  function setHandlers(dataHandler: DataHandler, stateHandler?: StateHandler) {
    onData = dataHandler;
    onState = stateHandler ?? null;
  }

  async function stop() {
    manualClosed = true;
    await leaveDevice();
    if (connection.value) {
      await connection.value.stop().catch(() => {});
      connection.value = null;
    }
    connected.value = false;
  }

  onBeforeUnmount(() => {
    stop();
  });

  return {
    connected,
    joinedDeviceId,
    start,
    stop,
    joinDevice,
    leaveDevice,
    setHandlers
  };
}
