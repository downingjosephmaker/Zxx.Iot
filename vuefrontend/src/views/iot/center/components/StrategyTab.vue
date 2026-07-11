<script setup lang="ts">
import { h, ref, reactive, computed, watch, onMounted } from "vue";
import { ElTag } from "element-plus";
import dayjs from "dayjs";
import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import type { PaginationProps } from "@pureadmin/table";
import type { QueryTableParams } from "@/api/type";
import type {
  CollectStrategyItem,
  PushStrategyItem
} from "@/api/iot/strategy";
import {
  getCollectListByPage,
  saveCollectBatch,
  deleteCollectByPk,
  getPushListByPage,
  savePushBatch,
  deletePushByPk
} from "@/api/iot/strategy";
import type {
  CollectStrategyFormItemProps,
  PushStrategyFormItemProps
} from "@/views/iot/strategy/utils/types";
import collectForm from "@/views/iot/strategy/collect-form.vue";
import pushForm from "@/views/iot/strategy/push-form.vue";
import AddFill from "~icons/ri/add-circle-line";
import Refresh from "~icons/ep/refresh";
import EditPen from "~icons/ep/edit-pen";
import Delete from "~icons/ep/delete";

defineOptions({
  name: "StrategyTab"
});

/** 挂靠范围：product=产品级(typecode) device=设备级(deviceId) */
interface StrategyScope {
  kind: "product" | "device";
  typecode: string;
  deviceId?: number;
}

const props = defineProps<{ scope: StrategyScope }>();

const scopeMap = {
  1: { text: "产品", type: "primary" },
  2: { text: "设备", type: "warning" },
  3: { text: "点位", type: "success" }
} as const;

const modeMap = {
  1: "收到即报",
  2: "变化上报",
  3: "定时上报",
  4: "变化+静默兜底"
} as const;

const deadbandMap = {
  0: "严格不等",
  1: "绝对死区",
  2: "百分比死区"
} as const;

type StrategyRow = CollectStrategyItem & PushStrategyItem;

const activeTab = ref<"collect" | "push">("collect");
const formRef = ref();
const dataList = ref<StrategyRow[]>([]);
const loading = ref(false);
const pagination = reactive<PaginationProps>({
  total: 0,
  pageSize: 10,
  pageSizes: [10, 20, 50, 100],
  currentPage: 1,
  background: true
});

const ModuleTitle = computed(() =>
  activeTab.value === "collect" ? "采集策略" : "推送策略"
);

const scopeValid = computed(() =>
  props.scope.kind === "product"
    ? !!props.scope.typecode
    : !!props.scope.deviceId
);

/** 写入/查询用的匹配键：必须与后端合并引擎一致(产品=类型编码,设备=设备ID纯数字字符串) */
const scopeId = computed(() =>
  props.scope.kind === "product"
    ? props.scope.typecode
    : String(props.scope.deviceId)
);

const scopeColumns = [
  {
    label: "序号",
    type: "index",
    width: 70,
    align: "center"
  },
  {
    label: "层级",
    prop: "ScopeType",
    align: "center",
    width: 90,
    cellRenderer: ({ row }) => {
      const item = scopeMap[row.ScopeType] ?? { text: "未知", type: "info" };
      return h(ElTag, { type: item.type, effect: "light" }, () => item.text);
    }
  },
  {
    label: "挂靠对象",
    prop: "ScopeId",
    align: "left",
    minWidth: 130,
    formatter: row =>
      row.ScopeType === 3 && row.ParamCode
        ? `${row.ScopeId} / ${row.ParamCode}`
        : row.ScopeId
  }
];

const timeAndOpColumns = [
  {
    label: "更新时间",
    prop: "UpdateTime",
    align: "center",
    width: 160,
    formatter: row =>
      row.UpdateTime ? dayjs(row.UpdateTime).format("YYYY-MM-DD HH:mm:ss") : "-"
  },
  {
    label: "操作",
    fixed: "right",
    width: 160,
    slot: "operation"
  }
];

const collectColumns = [
  ...scopeColumns,
  {
    label: "采集周期(毫秒)",
    prop: "CollectCycleMs",
    align: "right",
    width: 130,
    formatter: row => row.CollectCycleMs ?? "未设置"
  },
  {
    label: "采集cron",
    prop: "CollectCron",
    align: "left",
    minWidth: 130,
    formatter: row => row.CollectCron || "-"
  },
  {
    label: "上报周期(毫秒)",
    prop: "ReportCycleMs",
    align: "right",
    width: 130,
    formatter: row => row.ReportCycleMs ?? "未设置"
  },
  ...timeAndOpColumns
];

const pushColumns = [
  ...scopeColumns,
  {
    label: "推送模式",
    prop: "ReportMode",
    align: "center",
    width: 130,
    formatter: row => modeMap[row.ReportMode] ?? "未设置"
  },
  {
    label: "死区",
    prop: "DeadbandType",
    align: "center",
    width: 140,
    formatter: row => {
      const type = deadbandMap[row.DeadbandType];
      if (type === undefined) return "未设置";
      return row.DeadbandType === 0 ? type : `${type}：${row.DeadbandValue ?? 0}`;
    }
  },
  {
    label: "最小间隔(毫秒)",
    prop: "MinPushIntervalMs",
    align: "right",
    width: 120,
    formatter: row => row.MinPushIntervalMs ?? "未设置"
  },
  {
    label: "最大静默(毫秒)",
    prop: "MaxSilentMs",
    align: "right",
    width: 120,
    formatter: row => row.MaxSilentMs ?? "未设置"
  },
  ...timeAndOpColumns
];

const columns = computed(() =>
  activeTab.value === "collect" ? collectColumns : pushColumns
);

async function onSearch() {
  if (!scopeValid.value) return;
  loading.value = true;
  const params: QueryTableParams = {
    page: pagination.currentPage,
    pagesize: pagination.pageSize,
    sconlist:
      props.scope.kind === "product"
        ? [
            { ParamName: "ScopeType", ParamType: "=", ParamValue: "1" },
            {
              ParamName: "ScopeId",
              ParamType: "=",
              ParamValue: props.scope.typecode
            }
          ]
        : [
            // 设备视角同时看到设备级(2)与点位级(3)覆盖
            { ParamName: "ScopeType", ParamType: "in", ParamValue: "2,3" },
            {
              ParamName: "ScopeId",
              ParamType: "=",
              ParamValue: String(props.scope.deviceId)
            }
          ]
  };
  const data =
    activeTab.value === "collect"
      ? await getCollectListByPage(params)
      : await getPushListByPage(params);
  if (data.Status) {
    dataList.value = JSON.parse(data.Result);
    pagination.total = data.Total;
  } else {
    message(data.Message, { type: "error" });
  }
  loading.value = false;
}

function onTabChange() {
  pagination.currentPage = 1;
  dataList.value = [];
  onSearch();
}

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

async function openDialog(title = "新增", row?: StrategyRow) {
  const iscollect = activeTab.value === "collect";
  const formData: CollectStrategyFormItemProps & PushStrategyFormItemProps = {
    title,
    SnowId: 0,
    TenantId: 0,
    // 新增预填挂靠键，免手输防拼错：product=1/typecode，device=2/String(deviceId)
    ScopeType: props.scope.kind === "product" ? 1 : 2,
    ScopeId: scopeId.value,
    ParamCode: "",
    CollectCycleMs: null,
    CollectCron: "",
    ReportCycleMs: null,
    ReportMode: null,
    DeadbandType: null,
    DeadbandValue: null,
    MinPushIntervalMs: null,
    MaxSilentMs: null,
    DebounceIgnoreKeys: ""
  };
  if (row && row.SnowId) {
    Object.assign(formData, row, {
      ParamCode: row.ParamCode ?? "",
      CollectCron: row.CollectCron ?? "",
      DebounceIgnoreKeys: row.DebounceIgnoreKeys ?? ""
    });
  }

  addDialog({
    title: `${title}${ModuleTitle.value}`,
    props: {
      formInline: formData
    },
    width: "620px",
    draggable: true,
    fullscreenIcon: true,
    closeOnClickModal: false,
    contentRenderer: () =>
      h(iscollect ? collectForm : pushForm, {
        formInline: formData,
        ref: formRef
      }),
    beforeSure: (done, { options }) => {
      const FormRef = formRef.value.getRef();
      const curData = { ...options.props.formInline };
      FormRef.validate(async valid => {
        if (valid) {
          delete curData.title;
          if (iscollect) {
            // 双表单共用一个formData，提交前剔除对方表字段
            delete curData.ReportMode;
            delete curData.DeadbandType;
            delete curData.DeadbandValue;
            delete curData.MinPushIntervalMs;
            delete curData.MaxSilentMs;
            delete curData.DebounceIgnoreKeys;
          } else {
            delete curData.CollectCycleMs;
            delete curData.CollectCron;
            delete curData.ReportCycleMs;
          }
          const data = iscollect
            ? await saveCollectBatch([curData])
            : await savePushBatch([curData]);
          if (data.Status) {
            message(`${title}${ModuleTitle.value}成功`, { type: "success" });
            done();
            onSearch();
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

async function handleDelete(row: StrategyRow) {
  const data =
    activeTab.value === "collect"
      ? await deleteCollectByPk(row.SnowId.toString())
      : await deletePushByPk(row.SnowId.toString());
  if (data.Status) {
    message("删除成功", { type: "success" });
    onSearch();
  } else {
    message(data.Message, { type: "error" });
  }
}

watch(
  () => props.scope,
  () => {
    pagination.currentPage = 1;
    dataList.value = [];
    onSearch();
  },
  { deep: true }
);

onMounted(() => {
  onSearch();
});
</script>

<template>
  <div class="strategy-tab">
    <el-empty
      v-if="!scopeValid"
      description="请先选择产品或设备后再配置策略"
    />
    <template v-else>
      <el-tabs v-model="activeTab" @tab-change="onTabChange">
        <el-tab-pane label="采集策略" name="collect" />
        <el-tab-pane label="推送策略" name="push" />
      </el-tabs>
      <div class="mb-2 flex items-center justify-between">
        <span class="scope-tip">
          {{
            props.scope.kind === "product"
              ? `产品级策略，挂靠对象：${scopeId}`
              : `设备/点位级策略，挂靠对象：${scopeId}`
          }}
        </span>
        <div>
          <el-button
            type="primary"
            :icon="useRenderIcon(AddFill)"
            @click="openDialog()"
          >
            新增{{ ModuleTitle }}
          </el-button>
          <el-button :icon="useRenderIcon(Refresh)" @click="onSearch">
            刷新
          </el-button>
        </div>
      </div>
      <pure-table
        showOverflowTooltip
        align-whole="left"
        table-layout="auto"
        :loading="loading"
        :data="dataList"
        :columns="columns"
        :pagination="pagination"
        :header-cell-style="{
          background: 'var(--el-fill-color-light)',
          color: 'var(--el-text-color-primary)'
        }"
        @page-size-change="handleSizeChange"
        @page-current-change="handleCurrentChange"
      >
        <template #operation="{ row }">
          <el-button
            class="reset-margin"
            link
            type="primary"
            :icon="useRenderIcon(EditPen)"
            @click="openDialog('修改', row)"
          >
            修改
          </el-button>
          <el-popconfirm
            :title="`确定要删除该${ModuleTitle}吗？`"
            @confirm="handleDelete(row)"
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
    </template>
  </div>
</template>

<style scoped>
.scope-tip {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
</style>
