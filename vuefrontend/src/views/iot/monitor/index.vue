<script setup lang="ts">
import { ref, reactive, computed, onMounted, onBeforeUnmount } from "vue";
import dayjs from "dayjs";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import Search from "~icons/ep/search";
import Connection from "~icons/ep/connection";
import { getListByPage, type DeviceInfoItem } from "@/api/iot/device";
import { getDeviceLatest, type TelemetryPointItem } from "@/api/iot/monitor";
import { useDeviceSignalR } from "./utils/useDeviceSignalR";

defineOptions({
  name: "IotMonitor"
});

/** 点位行(最新值 + 高亮闪烁标记) */
interface PointRow extends TelemetryPointItem {
  /** 最近一次更新的时间戳(毫秒)，驱动行高亮 */
  flashAt?: number;
}

const deviceKeyword = ref("");
const deviceOptions = ref<DeviceInfoItem[]>([]);
const selectedDeviceId = ref<number | null>(null);
const loading = ref(false);
/** 参数编码 → 点位行 */
const pointMap = reactive<Record<string, PointRow>>({});
/** 触发高亮重渲染的时钟 */
const flashTick = ref(0);

const {
  connected,
  start: startSignalR,
  joinDevice,
  setHandlers
} = useDeviceSignalR();

const selectedDevice = computed(() =>
  deviceOptions.value.find(d => d.DeviceId === selectedDeviceId.value)
);

const pointRows = computed(() => {
  // 依赖 flashTick 使高亮到期后重算
  void flashTick.value;
  return Object.values(pointMap).sort((a, b) =>
    (a.ParamName || a.ParamCode).localeCompare(b.ParamName || b.ParamCode)
  );
});

/** 展示值：优先字符串型，其次数值 */
function displayValue(row: PointRow): string {
  if (row.ValueStr !== null && row.ValueStr !== undefined && row.ValueStr !== "") {
    return row.ValueStr;
  }
  if (row.Value !== null && row.Value !== undefined) {
    return String(row.Value);
  }
  return "-";
}

function isFlashing(row: PointRow): boolean {
  return !!row.flashAt && Date.now() - row.flashAt < 1500;
}

/** 合并一批推送点位，打高亮标记 */
function mergePoints(points: TelemetryPointItem[]) {
  const now = Date.now();
  points.forEach(p => {
    pointMap[p.ParamCode] = { ...pointMap[p.ParamCode], ...p, flashAt: now };
  });
  flashTick.value = now;
}

async function loadDevices() {
  const data = await getListByPage({
    page: 1,
    pagesize: 500,
    sconlist: deviceKeyword.value
      ? [
          {
            ParamName: "DeviceName",
            ParamType: "like",
            ParamValue: deviceKeyword.value
          }
        ]
      : []
  });
  if (data.Status) {
    deviceOptions.value = JSON.parse(data.Result);
  }
}

async function onSelectDevice(deviceId: number) {
  if (!deviceId) return;
  loading.value = true;
  // 清空旧设备点位
  Object.keys(pointMap).forEach(k => delete pointMap[k]);
  try {
    // 首屏铺底：拉最新值
    const data = await getDeviceLatest(deviceId);
    if (data.Status) {
      const points: TelemetryPointItem[] = JSON.parse(data.Result);
      points.forEach(p => (pointMap[p.ParamCode] = { ...p }));
    }
    // 订阅增量
    await joinDevice(deviceId);
  } finally {
    loading.value = false;
  }
}

// 每秒推进时钟，让高亮到期自动消退
let flashTimer: ReturnType<typeof setInterval> | null = null;

onMounted(async () => {
  setHandlers(mergePoints);
  await startSignalR();
  await loadDevices();
  flashTimer = setInterval(() => {
    // 仅在有活跃高亮时触发重算
    if (Object.values(pointMap).some(r => isFlashing(r))) {
      flashTick.value = Date.now();
    }
  }, 500);
});

onBeforeUnmount(() => {
  if (flashTimer) clearInterval(flashTimer);
});
</script>

<template>
  <div class="main">
    <el-card shadow="never" class="mb-3">
      <div class="toolbar">
        <el-select
          v-model="selectedDeviceId"
          filterable
          remote
          :remote-method="
            (kw: string) => {
              deviceKeyword = kw;
              loadDevices();
            }
          "
          placeholder="选择要监控的设备"
          class="!w-[280px]"
          @change="onSelectDevice"
        >
          <el-option
            v-for="item in deviceOptions"
            :key="item.DeviceId"
            :label="`${item.DeviceName}（${item.DeviceTypeName || item.DeviceTypeCode}）`"
            :value="item.DeviceId"
          />
        </el-select>
        <el-button
          :icon="useRenderIcon(Search)"
          :loading="loading"
          :disabled="!selectedDeviceId"
          @click="onSelectDevice(selectedDeviceId!)"
        >
          刷新
        </el-button>
        <div class="conn-status">
          <el-icon :class="connected ? 'ok' : 'off'">
            <Connection />
          </el-icon>
          <span :class="connected ? 'ok' : 'off'">
            {{ connected ? "实时连接已建立" : "连接断开，重连中…" }}
          </span>
        </div>
      </div>
    </el-card>

    <el-card v-if="selectedDevice" shadow="never">
      <template #header>
        <div class="card-header">
          <span class="device-title">{{ selectedDevice.DeviceName }}</span>
          <el-tag type="info" effect="light" size="small">
            {{ selectedDevice.DeviceTypeName || selectedDevice.DeviceTypeCode }}
          </el-tag>
          <el-tag
            :type="
              selectedDevice.DeviceState === 2
                ? 'success'
                : selectedDevice.DeviceState === 1
                  ? 'warning'
                  : 'info'
            "
            effect="light"
            size="small"
          >
            {{
              selectedDevice.DeviceState === 2
                ? "在线"
                : selectedDevice.DeviceState === 1
                  ? "掉电"
                  : "离线"
            }}
          </el-tag>
          <span class="point-count">共 {{ pointRows.length }} 个点位</span>
        </div>
      </template>

      <el-empty
        v-if="pointRows.length === 0"
        description="暂无实时数据（设备未上报或未采集）"
        :image-size="90"
      />
      <div v-else class="point-grid">
        <div
          v-for="row in pointRows"
          :key="row.ParamCode"
          class="point-card"
          :class="{ flash: isFlashing(row), bad: row.Quality !== 0 }"
        >
          <div class="point-name">{{ row.ParamName || row.ParamCode }}</div>
          <div class="point-value">{{ displayValue(row) }}</div>
          <div class="point-ts">
            {{ row.Ts ? dayjs(row.Ts).format("HH:mm:ss") : "-" }}
            <el-tag v-if="row.Quality !== 0" type="danger" size="small">
              质量异常
            </el-tag>
          </div>
        </div>
      </div>
    </el-card>

    <el-empty
      v-else
      description="请选择一台设备开始实时监控"
      :image-size="120"
    />
  </div>
</template>

<style scoped lang="scss">
.toolbar {
  display: flex;
  gap: 12px;
  align-items: center;
}

.conn-status {
  display: flex;
  gap: 4px;
  align-items: center;
  margin-left: auto;
  font-size: 13px;

  .ok {
    color: var(--el-color-success);
  }

  .off {
    color: var(--el-color-info);
  }
}

.card-header {
  display: flex;
  gap: 10px;
  align-items: center;

  .device-title {
    font-size: 16px;
    font-weight: 600;
  }

  .point-count {
    margin-left: auto;
    font-size: 13px;
    color: var(--el-text-color-secondary);
  }
}

.point-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 12px;
}

.point-card {
  padding: 14px 16px;
  background: var(--el-fill-color-lighter);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  transition:
    background 0.6s ease,
    border-color 0.6s ease;

  &.flash {
    background: var(--el-color-primary-light-8);
    border-color: var(--el-color-primary);
  }

  &.bad {
    border-color: var(--el-color-danger-light-5);
  }
}

.point-name {
  margin-bottom: 6px;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.point-value {
  font-size: 24px;
  font-weight: 600;
  line-height: 1.2;
  color: var(--el-text-color-primary);
  word-break: break-all;
}

.point-ts {
  display: flex;
  gap: 6px;
  align-items: center;
  margin-top: 6px;
  font-size: 12px;
  color: var(--el-text-color-placeholder);
}
</style>
