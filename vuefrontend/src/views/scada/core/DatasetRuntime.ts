/**
 * DatasetRuntime.ts - IoT设备点位数据集执行器（编辑器预览与运行时共用）
 *
 * 实时模式：GetDeviceLatest 铺底 + SignalR 设备组增量推送
 * 历史模式：GetDeviceHistory 拉取曲线，取每点末样本作为当前值
 */
import {
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
  type HubConnection
} from "@microsoft/signalr";
import { getSignalRUrl } from "@/config";
import {
  getDeviceLatest,
  getDeviceHistory,
  type TelemetryPointItem,
  type HistoryResultItem
} from "@/api/iot/monitor";

/** 数据集勾选的点位 */
export interface IotDatasetPoint {
  ParamCode: string;
  ParamName?: string;
  ValueUnit?: string;
}

/** IoT设备点位数据集配置（DatasetPanel 保存进项目 datasets 的结构） */
export interface IotDatasetConfig {
  id: string;
  name?: string;
  type: "iot";
  /** realtime=最新值+SignalR增量，history=历史曲线 */
  mode: "realtime" | "history";
  deviceId: number;
  deviceName?: string;
  deviceTypeCode?: string;
  points: IotDatasetPoint[];
  /** 历史模式回看时长(小时)，默认24 */
  historyHours?: number;
  /** 历史模式聚合(auto/raw/hour)，默认auto */
  historyMode?: "auto" | "raw" | "hour";
}

/** 单点当前值 */
export interface IotPointValue {
  value: string;
  ts?: string;
  paramName?: string;
  unit?: string;
}

/** 数据回调：datasetId → 该数据集勾选点位的最新值表(键=ParamCode) */
export type IotDataHandler = (
  datasetId: string,
  values: Record<string, IotPointValue>
) => void;

function displayValue(p: TelemetryPointItem): string {
  if (p.ValueStr !== null && p.ValueStr !== undefined && p.ValueStr !== "") {
    return p.ValueStr;
  }
  if (p.Value !== null && p.Value !== undefined) return String(p.Value);
  return "";
}

function pad(n: number): string {
  return n < 10 ? `0${n}` : String(n);
}

function formatLocal(d: Date): string {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

/**
 * IoT数据集运行器：一次实例管理一批数据集共用一条SignalR连接。
 * 页面卸载须调用 stop()，否则连接与设备组订阅泄漏。
 */
export class IotDatasetRunner {
  private readonly datasets: IotDatasetConfig[];
  private readonly onData: IotDataHandler;
  private connection: HubConnection | null = null;
  private stopped = false;

  constructor(datasets: IotDatasetConfig[], onData: IotDataHandler) {
    this.datasets = datasets.filter(d => d && d.type === "iot" && d.deviceId);
    this.onData = onData;
  }

  async start() {
    const realtime = this.datasets.filter(d => d.mode !== "history");
    const history = this.datasets.filter(d => d.mode === "history");

    // 历史数据集一次性拉取（运行时文本绑定取末样本即可）
    await Promise.all(history.map(d => this.loadHistory(d)));

    if (!realtime.length) return;
    // 实时数据集：先铺底再建实时通道
    await Promise.all(realtime.map(d => this.bootstrap(d)));
    await this.connect(realtime);
  }

  async stop() {
    this.stopped = true;
    const conn = this.connection;
    this.connection = null;
    if (conn) {
      if (conn.state === HubConnectionState.Connected) {
        const deviceIds = this.distinctRealtimeDeviceIds();
        await Promise.all(
          deviceIds.map(id =>
            conn.invoke("LeaveDeviceGroup", id).catch(() => {})
          )
        );
      }
      await conn.stop().catch(() => {});
    }
  }

  private distinctRealtimeDeviceIds(): number[] {
    return [
      ...new Set(
        this.datasets
          .filter(d => d.mode !== "history")
          .map(d => Number(d.deviceId))
      )
    ];
  }

  /** 按数据集勾选点位过滤一批遥测点并回调 */
  private deliver(dataset: IotDatasetConfig, points: TelemetryPointItem[]) {
    const codes = new Set((dataset.points || []).map(p => p.ParamCode));
    const values: Record<string, IotPointValue> = {};
    points.forEach(p => {
      if (codes.size && !codes.has(p.ParamCode)) return;
      const meta = dataset.points?.find(x => x.ParamCode === p.ParamCode);
      values[p.ParamCode] = {
        value: displayValue(p),
        ts: p.Ts,
        paramName: p.ParamName || meta?.ParamName || p.ParamCode,
        unit: meta?.ValueUnit || ""
      };
    });
    if (Object.keys(values).length) this.onData(dataset.id, values);
  }

  /** 实时铺底：拉设备全部点位最新值 */
  private async bootstrap(dataset: IotDatasetConfig) {
    try {
      const data = await getDeviceLatest(dataset.deviceId);
      if (!data.Status) return;
      const points: TelemetryPointItem[] = JSON.parse(data.Result);
      this.deliver(dataset, points);
    } catch {
      /* 铺底失败不阻塞实时通道 */
    }
  }

  /** 历史模式：逐点拉曲线，投递末样本 */
  private async loadHistory(dataset: IotDatasetConfig) {
    const end = new Date();
    const start = new Date(
      end.getTime() - (dataset.historyHours || 24) * 3600 * 1000
    );
    const values: Record<string, IotPointValue> = {};
    await Promise.all(
      (dataset.points || []).map(async p => {
        try {
          const data = await getDeviceHistory(
            dataset.deviceId,
            p.ParamCode,
            formatLocal(start),
            formatLocal(end),
            dataset.historyMode || "auto"
          );
          if (!data.Status) return;
          const result: HistoryResultItem = JSON.parse(data.Result);
          const last = result.Points?.[result.Points.length - 1];
          if (!last) return;
          values[p.ParamCode] = {
            value:
              last.Value !== null && last.Value !== undefined
                ? String(last.Value)
                : (last.ValueStr ?? ""),
            ts: last.Ts,
            paramName: p.ParamName || p.ParamCode,
            unit: p.ValueUnit || ""
          };
        } catch {
          /* 单点失败不影响其余点位 */
        }
      })
    );
    if (Object.keys(values).length) this.onData(dataset.id, values);
  }

  /** 建立SignalR连接并加入全部实时设备组，断线5秒重连后自动重入组 */
  private async connect(realtime: IotDatasetConfig[]) {
    if (this.stopped) return;
    const conn = new HubConnectionBuilder()
      .withUrl(getSignalRUrl(), {
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets
      })
      .configureLogging(LogLevel.Warning)
      .build();
    conn.keepAliveIntervalInMilliseconds = 60 * 1000;
    conn.serverTimeoutInMilliseconds = 130 * 1000;

    // ReceiveDeviceData 收 List<TelemetryPoint> 的 JSON 串，按 DeviceId 路由到数据集
    conn.on("ReceiveDeviceData", (payload: string) => {
      try {
        const points: TelemetryPointItem[] = JSON.parse(payload);
        if (!points.length) return;
        const deviceId = Number(points[0].DeviceId);
        realtime
          .filter(d => Number(d.deviceId) === deviceId)
          .forEach(d => this.deliver(d, points));
      } catch {
        /* 忽略非法负载 */
      }
    });

    conn.onclose(() => {
      if (!this.stopped) setTimeout(() => this.connect(realtime), 5000);
    });

    try {
      await conn.start();
      this.connection = conn;
      await Promise.all(
        this.distinctRealtimeDeviceIds().map(id =>
          conn.invoke("JoinDeviceGroup", id).catch(() => {})
        )
      );
    } catch {
      if (!this.stopped) setTimeout(() => this.connect(realtime), 5000);
    }
  }
}
