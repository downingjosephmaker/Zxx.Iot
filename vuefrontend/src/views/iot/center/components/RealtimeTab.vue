<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted, onBeforeUnmount } from "vue";
import dayjs from "dayjs";
import { message } from "@/utils/message";
import { getDeviceLatest, type TelemetryPointItem } from "@/api/iot/monitor";
import { useDeviceSignalR } from "@/views/iot/monitor/utils/useDeviceSignalR";

defineOptions({
  name: "RealtimeTab"
});

const props = defineProps<{
  device: any | null;
}>();

/** 点位行(最新值 + 高亮闪烁标记) */
interface PointRow extends TelemetryPointItem {
  /** 最近一次增量到达的时间戳(毫秒)，驱动行高亮 */
  flashAt?: number;
}

const loading = ref(false);
/** 参数编码 → 点位行(单设备内 ParamCode 唯一，作合并键) */
const pointMap = reactive<Record<string, PointRow>>({});
/** 触发高亮重渲染的时钟 */
const flashTick = ref(0);

// 必须在 setup 顶层调用：内部 onBeforeUnmount 兜底断开连接
const {
  connected,
  start: startSignalR,
  joinDevice,
  leaveDevice,
  setHandlers
} = useDeviceSignalR();

const pointRows = computed(() => {
  // 依赖 flashTick 使高亮到期后重算
  void flashTick.value;
  return Object.values(pointMap).sort((a, b) =>
    (a.ParamName || a.ParamCode).localeCompare(b.ParamName || b.ParamCode)
  );
});

/** 展示值：优先数值型，其次字符串型 */
function displayValue(row: PointRow): string {
  if (row.Value !== null && row.Value !== undefined) {
    return String(row.Value);
  }
  if (row.ValueStr !== null && row.ValueStr !== undefined && row.ValueStr !== "") {
    return row.ValueStr;
  }
  return "-";
}

function isFlashing(row: PointRow): boolean {
  return !!row.flashAt && Date.now() - row.flashAt < 2000;
}

function rowClassName({ row }: { row: PointRow }): string {
  return isFlashing(row) ? "flash-row" : "";
}

/** 合并一批推送点位，打高亮标记 */
function mergePoints(points: TelemetryPointItem[]) {
  const now = Date.now();
  points.forEach(p => {
    pointMap[p.ParamCode] = { ...pointMap[p.ParamCode], ...p, flashAt: now };
  });
  flashTick.value = now;
}

function clearPoints() {
  Object.keys(pointMap).forEach(k => delete pointMap[k]);
}

async function loadDevice(deviceId: number | string) {
  loading.value = true;
  clearPoints();
  try {
    // 首屏铺底：拉最新值，之后增量走 SignalR
    const data = await getDeviceLatest(deviceId);
    if (data.Status) {
      const points: TelemetryPointItem[] = JSON.parse(data.Result);
      points.forEach(p => (pointMap[p.ParamCode] = { ...p }));
    } else {
      message(data.Message, { type: "error" });
    }
    // joinDevice 自动退旧组；连接未就绪时 start 成功后会补 Join
    await joinDevice(deviceId as number);
  } finally {
    loading.value = false;
  }
}

watch(
  () => props.device?.DeviceId,
  async deviceId => {
    if (deviceId !== null && deviceId !== undefined) {
      await loadDevice(deviceId);
    } else {
      await leaveDevice();
      clearPoints();
    }
  },
  { immediate: true }
);

// 每 500ms 推进时钟，让高亮到期自动消退
let flashTimer: ReturnType<typeof setInterval> | null = null;

onMounted(async () => {
  setHandlers(mergePoints);
  await startSignalR();
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
  <div class="realtime-tab">
    <div class="toolbar">
      <span class="conn-dot" :class="connected ? 'ok' : 'off'" />
      <span class="conn-text" :class="connected ? 'ok' : 'off'">
        {{ connected ? "实时连接已建立" : "连接断开，重连中…" }}
      </span>
      <span class="point-count">共 {{ pointRows.length }} 个点位</span>
    </div>

    <el-table
      v-loading="loading"
      :data="pointRows"
      row-key="ParamCode"
      :row-class-name="rowClassName"
      border
      stripe
    >
      <el-table-column
        prop="ParamName"
        label="参数名称"
        min-width="140"
        show-overflow-tooltip
      />
      <el-table-column
        prop="ParamCode"
        label="参数编码"
        min-width="120"
        show-overflow-tooltip
      />
      <el-table-column label="值" min-width="120">
        <template #default="{ row }">
          <span class="point-value">{{ displayValue(row) }}</span>
        </template>
      </el-table-column>
      <el-table-column label="采集时间" min-width="160">
        <template #default="{ row }">
          {{ row.Ts ? dayjs(row.Ts).format("YYYY-MM-DD HH:mm:ss") : "-" }}
        </template>
      </el-table-column>
      <el-table-column label="质量" width="90" align="center">
        <template #default="{ row }">
          <el-tag :type="row.Quality === 0 ? 'success' : 'danger'" size="small">
            {{ row.Quality === 0 ? "正常" : "异常" }}
          </el-tag>
        </template>
      </el-table-column>
      <template #empty>
        <el-empty
          :description="
            device ? '暂无实时数据（设备未上报或未采集）' : '请先选择设备'
          "
          :image-size="80"
        />
      </template>
    </el-table>
  </div>
</template>

<style scoped lang="scss">
.toolbar {
  display: flex;
  gap: 6px;
  align-items: center;
  margin-bottom: 12px;
  font-size: 13px;

  .conn-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;

    &.ok {
      background: var(--el-color-success);
    }

    &.off {
      background: var(--el-color-info);
    }
  }

  .conn-text {
    &.ok {
      color: var(--el-color-success);
    }

    &.off {
      color: var(--el-color-info);
    }
  }

  .point-count {
    margin-left: auto;
    color: var(--el-text-color-secondary);
  }
}

.point-value {
  font-weight: 600;
  color: var(--el-text-color-primary);
}

:deep(.flash-row) > td {
  background: var(--el-color-primary-light-8) !important;
  transition: background 0.6s ease;
}
</style>
