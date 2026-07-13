<script setup lang="ts">
import { ref, reactive, computed, onBeforeUnmount } from "vue";
import { message } from "@/utils/message";
import { ElMessageBox } from "element-plus";
import {
  HubConnectionBuilder,
  HttpTransportType,
  LogLevel,
  type HubConnection
} from "@microsoft/signalr";
import { getSignalRUrl } from "@/config";
import {
  getDeviceSimMeta,
  startSim,
  stopSim,
  listSims,
  injectFault
} from "@/api/simulator";
import type { DeviceInfoItem } from "./utils/types";

defineOptions({
  name: "SimDialog"
});

/** 点表快照(GetDeviceSimMeta.device.Points) */
interface SimPointItem {
  ParamCode: string;
  Di: string;
  FuncCode: number;
  Length: number;
  DataType: string;
  Generator: {
    Type: string;
    Base: number;
    Min: number;
    Max: number;
    Amp: number;
    PeriodS: number;
    Step: number;
    StepEveryS: number;
  };
}

interface SimStatusItem {
  SimId: string;
  DeviceId: number;
  Mode: number;
  Running: boolean;
  Port: number;
  StartedAt: string;
  Message: string;
}

interface SimLogEntryItem {
  SimId: string;
  Time: string;
  Direction: string;
  Hex: string;
  Note: string;
}

const dialogVisible = ref(false);
const activeTab = ref("self");
const loading = ref(false);
const starting = ref(false);

const curRow = ref<DeviceInfoItem | null>(null);
const supportSim = ref(false);
const pluginName = ref("");
const capability = ref<{
  SupportSlave?: boolean;
  SupportSelfTest?: boolean;
  DefaultPort?: number;
  Protocol?: string;
} | null>(null);
const deviceAddress = ref("");
const points = ref<SimPointItem[]>([]);

const port = ref(0);
const running = ref(false);
const curSimId = ref("");

/** 故障注入表单 */
const fault = reactive({
  wrongcs: false,
  timeout: false,
  split: false,
  delayMs: 50
});

const logs = ref<SimLogEntryItem[]>([]);
const MAX_LOG = 200;

const runningList = ref<SimStatusItem[]>([]);
let runningTimer: ReturnType<typeof setInterval> | null = null;

let connection: HubConnection | null = null;

const generatorOptions = [
  { label: "随机游走", value: "random" },
  { label: "正弦", value: "sine" },
  { label: "斜坡", value: "step" },
  { label: "固定值", value: "constant" }
];

const protocolLabel = computed(() => capability.value?.Protocol || "-");

/** 建立SignalR连接，订阅帧日志(参照useDeviceSignalR连接方式) */
async function ensureSignalR() {
  if (connection) return;
  const conn = new HubConnectionBuilder()
    .withUrl(getSignalRUrl(), {
      skipNegotiation: true,
      transport: HttpTransportType.WebSockets
    })
    .configureLogging(LogLevel.Warning)
    .build();
  conn.on("ReceiveSimLog", (payload: string) => {
    try {
      const entry: SimLogEntryItem = JSON.parse(payload);
      if (entry.SimId !== curSimId.value) return;
      logs.value.push(entry);
      if (logs.value.length > MAX_LOG) logs.value.shift();
    } catch {
      /* 忽略非法负载 */
    }
  });
  try {
    await conn.start();
    connection = conn;
  } catch {
    connection = null;
  }
}

async function closeSignalR() {
  if (connection) {
    await connection.stop().catch(() => {});
    connection = null;
  }
}

/** 打开弹窗：拉点表+capability渲染 */
async function open(row: DeviceInfoItem) {
  curRow.value = row;
  dialogVisible.value = true;
  activeTab.value = "self";
  running.value = false;
  curSimId.value = "";
  logs.value = [];
  loading.value = true;
  try {
    const data: any = await getDeviceSimMeta(row.DeviceId);
    if (!data?.ok) {
      message(data?.msg || "获取模拟元信息失败", { type: "error" });
      supportSim.value = false;
      return;
    }
    supportSim.value = !!data.supportSim;
    pluginName.value = data.pluginName || "";
    capability.value = data.capability || null;
    deviceAddress.value = data.device?.Address || "";
    points.value = (data.device?.Points || []).map((p: SimPointItem) => ({
      ...p,
      Generator: { ...p.Generator }
    }));
    port.value = capability.value?.DefaultPort || 0;
  } finally {
    loading.value = false;
  }
  await ensureSignalR();
}

/** 启动模拟 */
async function onStartSim() {
  if (!curRow.value) return;
  starting.value = true;
  try {
    const body = {
      DeviceId: curRow.value.DeviceId,
      Port: port.value,
      Generators: points.value.map(p => ({
        ParamCode: p.ParamCode,
        Generator: p.Generator
      }))
    };
    const data: any = await startSim(body);
    if (data?.ok) {
      curSimId.value = data.status?.SimId || "";
      running.value = true;
      logs.value = [];
      if (curSimId.value)
        await connection?.invoke("JoinSimGroup", curSimId.value).catch(() => {});
      message("模拟已启动", { type: "success" });
    } else {
      message(data?.msg || "启动模拟失败", { type: "error" });
    }
  } finally {
    starting.value = false;
  }
}

/** 停止本设备模拟 */
async function onStopSim() {
  if (!curSimId.value) {
    running.value = false;
    return;
  }
  await stopSim(curSimId.value);
  await connection?.invoke("LeaveSimGroup", curSimId.value).catch(() => {});
  running.value = false;
  message("模拟已停止", { type: "success" });
}

/** 故障注入变更即时下发到运行中实例 */
async function onFaultChange() {
  if (!curSimId.value) return;
  let kind = "";
  if (fault.wrongcs) kind = "wrongcs";
  else if (fault.timeout) kind = "timeout";
  else if (fault.split) kind = "split";
  await injectFault({
    SimId: curSimId.value,
    Kind: kind,
    Probability: 1,
    DelayMs: fault.delayMs
  });
}

function hexClass(dir: string) {
  return dir === "→" ? "log-out" : "log-in";
}

/** 全部运行中：刷新列表 */
async function refreshRunningList() {
  const data: any = await listSims();
  if (data?.ok) runningList.value = data.sims || [];
}

async function onStopOne(row: SimStatusItem) {
  await stopSim(row.SimId);
  message("已停止", { type: "success" });
  await refreshRunningList();
}

async function onStopAll() {
  if (runningList.value.length === 0) return;
  try {
    await ElMessageBox.confirm("确定要停止所有运行中的模拟吗？", "提示", {
      type: "warning",
      confirmButtonText: "确定",
      cancelButtonText: "取消"
    });
  } catch {
    return;
  }
  await Promise.all(runningList.value.map(r => stopSim(r.SimId)));
  message("已全部停止", { type: "success" });
  await refreshRunningList();
}

function onTabChange(name: string | number) {
  if (name === "all") {
    refreshRunningList();
    if (!runningTimer) {
      runningTimer = setInterval(refreshRunningList, 5000);
    }
  } else if (runningTimer) {
    clearInterval(runningTimer);
    runningTimer = null;
  }
}

function onDialogClose() {
  if (runningTimer) {
    clearInterval(runningTimer);
    runningTimer = null;
  }
  if (curSimId.value) connection?.invoke("LeaveSimGroup", curSimId.value).catch(() => {});
  closeSignalR();
}

onBeforeUnmount(() => {
  if (runningTimer) clearInterval(runningTimer);
  closeSignalR();
});

defineExpose({ open });
</script>

<template>
  <el-dialog
    v-model="dialogVisible"
    :title="`设备模拟 - ${curRow?.DeviceName ?? ''}`"
    width="900px"
    draggable
    destroy-on-close
    @close="onDialogClose"
  >
    <el-tabs v-model="activeTab" @tab-change="onTabChange">
      <el-tab-pane label="本设备模拟" name="self">
        <div v-loading="loading">
          <el-empty
            v-if="!loading && !supportSim"
            description="该设备所属插件不支持模拟"
            :image-size="80"
          />
          <template v-else>
            <el-form label-width="90px" inline>
              <el-form-item label="监听端口">
                <el-input-number
                  v-model="port"
                  :min="1"
                  :max="65535"
                  :disabled="running"
                  controls-position="right"
                />
              </el-form-item>
              <el-form-item label="从站号">
                <el-tag type="info" effect="light">{{ deviceAddress }}</el-tag>
              </el-form-item>
              <el-form-item label="协议">
                <el-tag type="info" effect="light">{{ protocolLabel }}</el-tag>
              </el-form-item>
              <el-form-item label="运行状态">
                <el-tag :type="running ? 'success' : 'info'" effect="light">
                  <span :class="running ? 'run-dot on' : 'run-dot'" />
                  {{ running ? "运行中" : "未运行" }}
                </el-tag>
              </el-form-item>
            </el-form>

            <el-divider content-position="left">点表</el-divider>
            <el-table :data="points" size="small" max-height="260" border>
              <el-table-column prop="ParamCode" label="参数" min-width="120" />
              <el-table-column prop="DataType" label="类型" width="90" />
              <el-table-column prop="Di" label="地址" width="90" />
              <el-table-column label="生成器" width="130">
                <template #default="{ row }">
                  <el-select v-model="row.Generator.Type" size="small">
                    <el-option
                      v-for="opt in generatorOptions"
                      :key="opt.value"
                      :label="opt.label"
                      :value="opt.value"
                    />
                  </el-select>
                </template>
              </el-table-column>
              <el-table-column label="覆盖参数" min-width="280">
                <template #default="{ row }">
                  <template v-if="row.Generator.Type === 'random'">
                    <el-input-number
                      v-model="row.Generator.Min"
                      size="small"
                      :controls="false"
                      placeholder="Min"
                      class="gen-input"
                    />
                    <el-input-number
                      v-model="row.Generator.Max"
                      size="small"
                      :controls="false"
                      placeholder="Max"
                      class="gen-input"
                    />
                  </template>
                  <template v-else-if="row.Generator.Type === 'sine'">
                    <el-input-number
                      v-model="row.Generator.Base"
                      size="small"
                      :controls="false"
                      placeholder="Base"
                      class="gen-input"
                    />
                    <el-input-number
                      v-model="row.Generator.Amp"
                      size="small"
                      :controls="false"
                      placeholder="Amp"
                      class="gen-input"
                    />
                    <el-input-number
                      v-model="row.Generator.PeriodS"
                      size="small"
                      :controls="false"
                      placeholder="周期(s)"
                      class="gen-input"
                    />
                  </template>
                  <template v-else-if="row.Generator.Type === 'step'">
                    <el-input-number
                      v-model="row.Generator.Base"
                      size="small"
                      :controls="false"
                      placeholder="Base"
                      class="gen-input"
                    />
                    <el-input-number
                      v-model="row.Generator.Step"
                      size="small"
                      :controls="false"
                      placeholder="步长"
                      class="gen-input"
                    />
                    <el-input-number
                      v-model="row.Generator.StepEveryS"
                      size="small"
                      :controls="false"
                      placeholder="间隔(s)"
                      class="gen-input"
                    />
                  </template>
                  <template v-else>
                    <el-input-number
                      v-model="row.Generator.Base"
                      size="small"
                      :controls="false"
                      placeholder="固定值"
                      class="gen-input"
                    />
                  </template>
                </template>
              </el-table-column>
            </el-table>

            <el-divider content-position="left">故障注入</el-divider>
            <el-form inline>
              <el-form-item>
                <el-checkbox v-model="fault.wrongcs" @change="onFaultChange">
                  错帧
                </el-checkbox>
              </el-form-item>
              <el-form-item>
                <el-checkbox v-model="fault.timeout" @change="onFaultChange">
                  超时丢弃
                </el-checkbox>
              </el-form-item>
              <el-form-item>
                <el-checkbox v-model="fault.split" @change="onFaultChange">
                  半包延迟
                </el-checkbox>
              </el-form-item>
              <el-form-item label="延迟(ms)">
                <el-input-number
                  v-model="fault.delayMs"
                  :min="0"
                  :max="60000"
                  size="small"
                  @change="onFaultChange"
                />
              </el-form-item>
            </el-form>

            <el-divider content-position="left">实时帧日志</el-divider>
            <div class="log-box">
              <el-empty
                v-if="logs.length === 0"
                description="暂无帧数据"
                :image-size="60"
              />
              <div v-for="(l, idx) in logs" :key="idx" :class="hexClass(l.Direction)">
                <span class="log-time">{{ l.Time }}</span>
                <span class="log-dir">{{ l.Direction }}</span>
                <span class="log-hex">{{ l.Hex }}</span>
                <span v-if="l.Note" class="log-note">{{ l.Note }}</span>
              </div>
            </div>
          </template>
        </div>
      </el-tab-pane>

      <el-tab-pane label="全部运行中" name="all">
        <div class="mb-2 text-right">
          <el-button type="danger" plain size="small" @click="onStopAll">
            全部停止
          </el-button>
        </div>
        <el-table :data="runningList" size="small" border>
          <el-table-column prop="DeviceId" label="设备" width="90" />
          <el-table-column prop="Port" label="端口" width="90" />
          <el-table-column prop="Mode" label="模式" width="90" />
          <el-table-column prop="StartedAt" label="启动时间" min-width="160" />
          <el-table-column label="状态" width="100">
            <template #default="{ row }">
              <el-tag :type="row.Running ? 'success' : 'info'" effect="light">
                {{ row.Running ? "运行中" : "已停止" }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="90" fixed="right">
            <template #default="{ row }">
              <el-button link type="danger" size="small" @click="onStopOne(row)">
                停止
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-tab-pane>
    </el-tabs>

    <template #footer>
      <el-button
        v-if="supportSim"
        :type="running ? 'danger' : 'primary'"
        :loading="starting"
        @click="running ? onStopSim() : onStartSim()"
      >
        {{ running ? "停止" : "启动模拟" }}
      </el-button>
      <el-button @click="dialogVisible = false">关闭</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.gen-input {
  width: 96px;
  margin-right: 6px;
}

.run-dot {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--el-color-info);
  margin-right: 4px;
}

.run-dot.on {
  background: var(--el-color-success);
}

.log-box {
  height: 180px;
  overflow-y: auto;
  background: var(--el-fill-color-light);
  border-radius: 4px;
  padding: 6px 10px;
  font-family: Consolas, Monaco, "Courier New", monospace;
  font-size: 12px;
}

.log-in,
.log-out {
  white-space: nowrap;
}

.log-out {
  color: var(--el-color-primary);
}

.log-in {
  color: var(--el-color-success);
}

.log-time {
  color: var(--el-text-color-secondary);
  margin-right: 6px;
}

.log-dir {
  margin-right: 6px;
  font-weight: bold;
}

.log-note {
  margin-left: 8px;
  color: var(--el-text-color-secondary);
}
</style>
