<script setup lang="ts">
import { ref, reactive, computed, onMounted } from "vue";
import dayjs from "dayjs";
import type { PaginationProps } from "@pureadmin/table";
import { PureTableBar } from "@/components/RePureTableBar";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { useUserStoreHook } from "@/store/modules/user";
import Search from "~icons/ep/search";
import Refresh from "~icons/ep/refresh";
import Connection from "~icons/ep/connection";
import Bell from "~icons/ep/bell";
import { getListByPage, type EventSignalItem } from "@/api/iot/alarm";
import { useAlarmSignalR } from "./utils/useAlarmSignalR";

defineOptions({
  name: "IotAlarm"
});

/** 实时告警条(带唯一key供列表动画) */
interface LiveAlarm extends EventSignalItem {
  _key: string;
  _at: number;
}

const searchForm = reactive({
  deviceName: "",
  eventType: ""
});
const formRef = ref();
const loading = ref(true);
const dataList = ref<EventSignalItem[]>([]);
const pagination = reactive<PaginationProps>({
  total: 0,
  pageSize: 10,
  pageSizes: [10, 20, 50, 100],
  currentPage: 1,
  background: true
});

/** 实时告警流（最多保留50条，新的在顶部） */
const liveAlarms = ref<LiveAlarm[]>([]);
const liveSeq = ref(0);

const { connected, start, joinUnit, setHandler } = useAlarmSignalR();

const currentUnitId = computed(() => {
  const raw = useUserStoreHook().unitId;
  const n = Number(raw);
  return Number.isFinite(n) ? n : 0;
});

/** 告警类型着色：意外情况=红，状态变化=橙 */
function alarmTagType(eventType?: string): "danger" | "warning" | "info" {
  if (eventType === "意外情况") return "danger";
  if (eventType === "状态变化") return "warning";
  return "info";
}

const columns = [
  { label: "序号", type: "index", width: 60, align: "center" },
  { label: "事件类型", prop: "EventType", align: "center", width: 100 },
  { label: "设备名称", prop: "DeviceName", align: "left", minWidth: 140 },
  {
    label: "产品类型",
    prop: "DeviceTypeName",
    align: "left",
    minWidth: 110,
    formatter: row => row.DeviceTypeName || row.DeviceTypeCode || "-"
  },
  { label: "内容", prop: "EventValue", align: "left", minWidth: 160 },
  { label: "详情", prop: "EventContent", align: "left", minWidth: 180 },
  {
    label: "建筑",
    prop: "BuildName",
    align: "left",
    minWidth: 120,
    formatter: row => row.BuildName || "-"
  },
  {
    label: "发生时间",
    prop: "EventTime",
    align: "center",
    width: 160,
    formatter: row =>
      row.EventTime ? dayjs(row.EventTime).format("YYYY-MM-DD HH:mm:ss") : "-"
  }
];

async function onSearch() {
  loading.value = true;
  const sconlist = [];
  if (searchForm.deviceName !== "") {
    sconlist.push({
      ParamName: "DeviceName",
      ParamType: "like",
      ParamValue: searchForm.deviceName
    });
  }
  if (searchForm.eventType !== "") {
    sconlist.push({
      ParamName: "EventType",
      ParamType: "=",
      ParamValue: searchForm.eventType
    });
  }
  const data = await getListByPage({
    page: pagination.currentPage,
    pagesize: pagination.pageSize,
    sconlist
  });
  if (data.Status) {
    dataList.value = JSON.parse(data.Result);
    pagination.total = data.Total;
  }
  loading.value = false;
}

const resetForm = formEl => {
  if (!formEl) return;
  formEl.resetFields();
  onSearch();
};

function handleSizeChange(val: number) {
  if (pagination.pageSize !== val) {
    pagination.pageSize = val;
    onSearch();
  }
}

function handleCurrentChange(val: number) {
  if (pagination.currentPage !== val) {
    pagination.currentPage = val;
    onSearch();
  }
}

/** 新告警推入实时流，顶部插入并限长 */
function onIncomingAlarm(alarm: EventSignalItem) {
  liveSeq.value += 1;
  liveAlarms.value.unshift({
    ...alarm,
    _key: `${alarm.SnowId ?? liveSeq.value}-${liveSeq.value}`,
    _at: Date.now()
  });
  if (liveAlarms.value.length > 50) {
    liveAlarms.value = liveAlarms.value.slice(0, 50);
  }
}

function clearLive() {
  liveAlarms.value = [];
}

onMounted(async () => {
  setHandler(onIncomingAlarm);
  await start();
  if (currentUnitId.value > 0) {
    await joinUnit(currentUnitId.value);
  }
  await onSearch();
});
</script>

<template>
  <div class="main">
    <!-- 实时告警流 -->
    <el-card shadow="never" class="mb-3 live-card">
      <template #header>
        <div class="live-header">
          <el-icon class="bell"><Bell /></el-icon>
          <span class="live-title">实时告警</span>
          <div class="conn-status">
            <el-icon :class="connected ? 'ok' : 'off'"><Connection /></el-icon>
            <span :class="connected ? 'ok' : 'off'">
              {{ connected ? "已连接" : "重连中…" }}
            </span>
          </div>
          <el-button
            v-if="liveAlarms.length"
            link
            type="primary"
            @click="clearLive"
          >
            清空
          </el-button>
        </div>
      </template>
      <el-empty
        v-if="liveAlarms.length === 0"
        description="暂无实时告警，新告警将在此实时滚入"
        :image-size="70"
      />
      <transition-group v-else name="alarm-slide" tag="div" class="live-list">
        <div
          v-for="item in liveAlarms"
          :key="item._key"
          class="live-item"
          :class="alarmTagType(item.EventType)"
        >
          <el-tag :type="alarmTagType(item.EventType)" effect="dark" size="small">
            {{ item.EventType || "告警" }}
          </el-tag>
          <span class="live-device">{{ item.DeviceName || "-" }}</span>
          <span class="live-value">{{ item.EventValue }}</span>
          <span class="live-content">{{ item.EventContent }}</span>
          <span class="live-time">
            {{
              item.EventTime
                ? dayjs(item.EventTime).format("HH:mm:ss")
                : dayjs(item._at).format("HH:mm:ss")
            }}
          </span>
        </div>
      </transition-group>
    </el-card>

    <!-- 历史告警 -->
    <div class="flex-1">
      <el-form
        ref="formRef"
        :inline="true"
        :model="searchForm"
        class="search-form bg-bg_color w-[99/100] pl-8 pt-[12px]"
      >
        <el-form-item label="设备名称" prop="deviceName">
          <el-input
            v-model="searchForm.deviceName"
            placeholder="请输入设备名称"
            clearable
            class="!w-[180px]"
            @keyup.enter="onSearch"
          />
        </el-form-item>
        <el-form-item label="事件类型" prop="eventType">
          <el-select
            v-model="searchForm.eventType"
            placeholder="全部"
            clearable
            class="!w-[140px]"
          >
            <el-option label="状态变化" value="状态变化" />
            <el-option label="意外情况" value="意外情况" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button
            type="primary"
            :icon="useRenderIcon(Search)"
            :loading="loading"
            @click="onSearch"
          >
            搜索
          </el-button>
          <el-button :icon="useRenderIcon(Refresh)" @click="resetForm(formRef)">
            重置
          </el-button>
        </el-form-item>
      </el-form>
      <PureTableBar title="历史告警" :columns="columns" @refresh="onSearch">
        <template v-slot="{ size, dynamicColumns }">
          <pure-table
            adaptive
            showOverflowTooltip
            align-whole="left"
            table-layout="auto"
            :loading="loading"
            :size="size"
            :data="dataList"
            :columns="dynamicColumns"
            :pagination="pagination"
            :paginationSmall="size === 'small' ? true : false"
            :header-cell-style="{
              background: 'var(--el-fill-color-light)',
              color: 'var(--el-text-color-primary)'
            }"
            @page-size-change="handleSizeChange"
            @page-current-change="handleCurrentChange"
          />
        </template>
      </PureTableBar>
    </div>
  </div>
</template>

<style scoped lang="scss">
.live-header {
  display: flex;
  gap: 8px;
  align-items: center;

  .bell {
    color: var(--el-color-warning);
  }

  .live-title {
    font-size: 15px;
    font-weight: 600;
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
}

.live-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: 260px;
  overflow-y: auto;
}

.live-item {
  display: flex;
  gap: 10px;
  align-items: center;
  padding: 8px 12px;
  font-size: 13px;
  border-left: 3px solid var(--el-color-info);
  border-radius: 4px;
  background: var(--el-fill-color-lighter);

  &.danger {
    border-left-color: var(--el-color-danger);
  }

  &.warning {
    border-left-color: var(--el-color-warning);
  }

  .live-device {
    font-weight: 600;
  }

  .live-value {
    color: var(--el-text-color-primary);
  }

  .live-content {
    color: var(--el-text-color-secondary);
  }

  .live-time {
    margin-left: auto;
    font-size: 12px;
    color: var(--el-text-color-placeholder);
  }
}

.alarm-slide-enter-active {
  transition: all 0.4s ease;
}

.alarm-slide-enter-from {
  opacity: 0;
  transform: translateY(-12px);
}

.search-form {
  :deep(.el-form-item) {
    margin-bottom: 12px;
  }
}
</style>
