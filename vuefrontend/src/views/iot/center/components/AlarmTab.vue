<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted, h } from "vue";
import dayjs from "dayjs";
import { ElTag } from "element-plus";
import type { PaginationProps } from "@pureadmin/table";
import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import Refresh from "~icons/ep/refresh";
import Delete from "~icons/ep/delete";
import AddFill from "~icons/ri/add-circle-line";
import type { Sconlist } from "@/api/type";
import { getListByPage as getAlarmListByPage, type EventSignalItem } from "@/api/iot/alarm";
import {
  getListByPage as getMaskListByPage,
  saveBatch as saveMaskBatch,
  deleteByPk as deleteMaskByPk,
  type AlarmMaskItem
} from "@/api/iot/alarmmask";
import maskForm from "@/views/iot/alarmmask/form.vue";
import type { AlarmMaskFormItemProps } from "@/views/iot/alarmmask/utils/types";

defineOptions({
  name: "CenterAlarmTab"
});

const props = defineProps<{
  scope: { kind: "product" | "device"; typecode: string; deviceId?: number };
}>();

const activeTab = ref("record");

/* ---------------- 告警记录 ---------------- */

const recordLoading = ref(false);
const recordList = ref<EventSignalItem[]>([]);
const recordPagination = reactive<PaginationProps>({
  total: 0,
  pageSize: 10,
  pageSizes: [10, 20, 50, 100],
  currentPage: 1,
  background: true
});

/** EventSignal按月分表，无时间窗会全表扫，故时间范围必填(不可清空)，默认近7天 */
const recordRange = ref<[string, string]>([
  dayjs().subtract(7, "day").format("YYYY-MM-DD HH:mm:ss"),
  dayjs().format("YYYY-MM-DD HH:mm:ss")
]);

/** 告警类型着色：意外情况=红，状态变化=橙 */
function alarmTagType(eventType?: string): "danger" | "warning" | "info" {
  if (eventType === "意外情况") return "danger";
  if (eventType === "状态变化") return "warning";
  return "info";
}

const recordColumns = [
  { label: "序号", type: "index", width: 60, align: "center" },
  {
    label: "发生时间",
    prop: "EventTime",
    align: "center",
    width: 160,
    formatter: row =>
      row.EventTime ? dayjs(row.EventTime).format("YYYY-MM-DD HH:mm:ss") : "-"
  },
  { label: "设备名称", prop: "DeviceName", align: "left", minWidth: 140 },
  {
    label: "事件类型",
    prop: "EventType",
    align: "center",
    width: 100,
    cellRenderer: ({ row }) =>
      h(
        ElTag,
        { type: alarmTagType(row.EventType), effect: "light" },
        () => row.EventType || "-"
      )
  },
  { label: "内容", prop: "EventValue", align: "left", minWidth: 160 },
  { label: "详情", prop: "EventContent", align: "left", minWidth: 180 }
];

function recordSconlist(): Sconlist[] {
  return props.scope.kind === "product"
    ? [
        {
          ParamName: "DeviceTypeCode",
          ParamType: "=",
          ParamValue: props.scope.typecode
        }
      ]
    : [
        {
          ParamName: "DeviceId",
          ParamType: "=",
          ParamValue: String(props.scope.deviceId)
        }
      ];
}

async function onRecordSearch() {
  recordLoading.value = true;
  const data = await getAlarmListByPage({
    page: recordPagination.currentPage,
    pagesize: recordPagination.pageSize,
    starttime: recordRange.value[0],
    endtime: recordRange.value[1],
    sconlist: recordSconlist()
  });
  if (data.Status) {
    recordList.value = JSON.parse(data.Result);
    recordPagination.total = data.Total;
  } else {
    message(data.Message, { type: "error" });
  }
  recordLoading.value = false;
}

function handleRecordSizeChange(val: number) {
  if (recordPagination.pageSize !== val) {
    recordPagination.pageSize = val;
    onRecordSearch();
  }
}

function handleRecordCurrentChange(val: number) {
  if (recordPagination.currentPage !== val) {
    recordPagination.currentPage = val;
    onRecordSearch();
  }
}

/* ---------------- 告警屏蔽 ---------------- */

const modeMap = {
  1: { text: "永久", type: "danger" },
  2: { text: "一次性", type: "warning" },
  3: { text: "周期窗", type: "primary" }
} as const;

const actionMap = {
  1: { text: "完全屏蔽", type: "danger" },
  2: { text: "静默", type: "warning" },
  3: { text: "降级", type: "info" }
} as const;

const maskLoading = ref(false);
const maskList = ref<AlarmMaskItem[]>([]);
const maskFormRef = ref();
const maskPagination = reactive<PaginationProps>({
  total: 0,
  pageSize: 10,
  pageSizes: [10, 20, 50, 100],
  currentPage: 1,
  background: true
});

/** MaskScopeType: 4=设备类型(ScopeId=类型编码) 5=单设备(ScopeId=设备ID) */
const maskScopeType = computed(() => (props.scope.kind === "product" ? 4 : 5));
const maskScopeId = computed(() =>
  props.scope.kind === "product"
    ? props.scope.typecode
    : String(props.scope.deviceId)
);

const maskColumns = [
  { label: "序号", type: "index", width: 60, align: "center" },
  {
    label: "模式",
    prop: "MaskMode",
    align: "center",
    width: 90,
    cellRenderer: ({ row }) => {
      const item = modeMap[row.MaskMode] ?? { text: "未知", type: "warning" };
      return h(ElTag, { type: item.type, effect: "light" }, () => item.text);
    }
  },
  {
    label: "动作",
    prop: "MaskAction",
    align: "center",
    width: 100,
    cellRenderer: ({ row }) => {
      const item = actionMap[row.MaskAction] ?? { text: "未知", type: "warning" };
      return h(ElTag, { type: item.type, effect: "light" }, () => item.text);
    }
  },
  {
    label: "屏蔽原因",
    prop: "Reason",
    align: "left",
    minWidth: 160,
    showOverflowTooltip: true
  },
  {
    label: "自动失效",
    prop: "ExpireAt",
    align: "center",
    width: 160,
    formatter: row => row.ExpireAt || "不失效"
  },
  {
    label: "启用",
    prop: "IsEnable",
    align: "center",
    width: 80,
    cellRenderer: ({ row }) =>
      h(
        ElTag,
        { type: row.IsEnable ? "success" : "info", effect: "light" },
        () => (row.IsEnable ? "启用" : "停用")
      )
  },
  {
    label: "更新时间",
    prop: "UpdateTime",
    align: "center",
    width: 160,
    formatter: row =>
      row.UpdateTime ? dayjs(row.UpdateTime).format("YYYY-MM-DD HH:mm:ss") : "-"
  },
  { label: "操作", fixed: "right", width: 100, slot: "operation" }
];

async function onMaskSearch() {
  maskLoading.value = true;
  const data = await getMaskListByPage({
    page: maskPagination.currentPage,
    pagesize: maskPagination.pageSize,
    sconlist: [
      {
        ParamName: "MaskScopeType",
        ParamType: "=",
        ParamValue: String(maskScopeType.value)
      },
      {
        ParamName: "ScopeId",
        ParamType: "=",
        ParamValue: maskScopeId.value
      }
    ]
  });
  if (data.Status) {
    maskList.value = JSON.parse(data.Result);
    maskPagination.total = data.Total;
  } else {
    message(data.Message, { type: "error" });
  }
  maskLoading.value = false;
}

function handleMaskSizeChange(val: number) {
  if (maskPagination.pageSize !== val) {
    maskPagination.pageSize = val;
    onMaskSearch();
  }
}

function handleMaskCurrentChange(val: number) {
  if (maskPagination.currentPage !== val) {
    maskPagination.currentPage = val;
    onMaskSearch();
  }
}

function openMaskDialog() {
  const formData: AlarmMaskFormItemProps = {
    title: "新增",
    SnowId: 0,
    MaskScopeType: maskScopeType.value,
    ScopeId: maskScopeId.value,
    MaskMode: 2,
    StartTime: "",
    EndTime: "",
    TimeRanges: "",
    MaskAction: 2,
    DowngradeGrade: "",
    Reason: "",
    OperatorName: "",
    ExpireAt: "",
    IsEnable: true
  };

  addDialog({
    title: "新增告警屏蔽",
    props: {
      formInline: formData
    },
    width: "620px",
    draggable: true,
    fullscreenIcon: true,
    closeOnClickModal: false,
    contentRenderer: () => h(maskForm, { formInline: formData, ref: maskFormRef }),
    beforeSure: (done, { options }) => {
      const FormRef = maskFormRef.value.getRef();
      const curData = { ...options.props.formInline };
      FormRef.validate(async valid => {
        if (valid) {
          delete curData.title;
          const data = await saveMaskBatch([curData]);
          if (data.Status) {
            message("新增告警屏蔽成功", { type: "success" });
            done();
            onMaskSearch();
          } else {
            message(data.Message, { type: "error" });
          }
        } else {
          message("表单验证失败，请检查输入", { type: "warning" });
        }
      });
    }
  });
}

async function handleMaskDelete(row: AlarmMaskItem) {
  const data = await deleteMaskByPk(row.SnowId.toString());
  if (data.Status) {
    message("删除成功", { type: "success" });
    onMaskSearch();
  } else {
    message(data.Message, { type: "error" });
  }
}

/* ---------------- 刷新 ---------------- */

function refreshAll() {
  recordPagination.currentPage = 1;
  maskPagination.currentPage = 1;
  onRecordSearch();
  onMaskSearch();
}

watch(() => props.scope, refreshAll, { deep: true });

onMounted(refreshAll);
</script>

<template>
  <el-tabs v-model="activeTab">
    <el-tab-pane label="告警记录" name="record">
      <div class="mb-2 flex items-center gap-2">
        <el-date-picker
          v-model="recordRange"
          type="datetimerange"
          range-separator="至"
          start-placeholder="开始时间"
          end-placeholder="结束时间"
          format="YYYY-MM-DD HH:mm:ss"
          value-format="YYYY-MM-DD HH:mm:ss"
          :clearable="false"
          class="!w-[380px]"
          @change="onRecordSearch"
        />
        <el-button
          :icon="useRenderIcon(Refresh)"
          :loading="recordLoading"
          @click="onRecordSearch"
        >
          刷新
        </el-button>
      </div>
      <pure-table
        showOverflowTooltip
        align-whole="left"
        table-layout="auto"
        :loading="recordLoading"
        :data="recordList"
        :columns="recordColumns"
        :pagination="recordPagination"
        :header-cell-style="{
          background: 'var(--el-fill-color-light)',
          color: 'var(--el-text-color-primary)'
        }"
        @page-size-change="handleRecordSizeChange"
        @page-current-change="handleRecordCurrentChange"
      />
    </el-tab-pane>

    <el-tab-pane label="告警屏蔽" name="mask">
      <div class="mb-2 flex items-center gap-2">
        <el-button
          type="primary"
          :icon="useRenderIcon(AddFill)"
          @click="openMaskDialog"
        >
          新增屏蔽
        </el-button>
        <el-button
          :icon="useRenderIcon(Refresh)"
          :loading="maskLoading"
          @click="onMaskSearch"
        >
          刷新
        </el-button>
      </div>
      <pure-table
        showOverflowTooltip
        align-whole="left"
        table-layout="auto"
        :loading="maskLoading"
        :data="maskList"
        :columns="maskColumns"
        :pagination="maskPagination"
        :header-cell-style="{
          background: 'var(--el-fill-color-light)',
          color: 'var(--el-text-color-primary)'
        }"
        @page-size-change="handleMaskSizeChange"
        @page-current-change="handleMaskCurrentChange"
      >
        <template #operation="{ row }">
          <el-popconfirm
            title="确定要删除该屏蔽规则吗？"
            @confirm="handleMaskDelete(row)"
          >
            <template #reference>
              <el-button
                class="reset-margin"
                link
                type="danger"
                :icon="useRenderIcon(Delete)"
              >
                删除
              </el-button>
            </template>
          </el-popconfirm>
        </template>
      </pure-table>
    </el-tab-pane>
  </el-tabs>
</template>
