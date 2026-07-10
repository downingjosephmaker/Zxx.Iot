import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, onMounted, computed } from "vue";
import { ElMessage, ElTag } from "element-plus";
import dayjs from "dayjs";
import type {
  QueryTableParams,
  CollectStrategyItem,
  PushStrategyItem,
  CollectStrategyFormItemProps,
  PushStrategyFormItemProps
} from "./types";
import {
  getCollectListByPage,
  saveCollectBatch,
  deleteCollectByPk,
  getPushListByPage,
  savePushBatch,
  deletePushByPk
} from "@/api/iot/strategy";
import collectForm from "../collect-form.vue";
import pushForm from "../push-form.vue";

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

export function useStrategy(tableRef: Ref) {
  /** 当前Tab：collect=采集策略 push=推送策略 */
  const activeTab = ref<"collect" | "push">("collect");
  const form = reactive({
    keyword: "",
    scopetype: ""
  });
  const formRef = ref();
  const dataList = ref<StrategyRow[]>([]);
  const loading = ref(true);
  const selectedNum = ref(0);
  const selectedRows = ref<StrategyRow[]>([]);
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
    loading.value = true;
    const params: QueryTableParams = {
      page: pagination.currentPage,
      pagesize: pagination.pageSize,
      sconlist: []
    };
    if (form.keyword !== "") {
      params.sconlist.push({
        ParamName: "ScopeId",
        ParamType: "like",
        ParamValue: form.keyword
      });
    }
    if (form.scopetype !== "") {
      params.sconlist.push({
        ParamName: "ScopeType",
        ParamType: "=",
        ParamValue: form.scopetype
      });
    }
    const data =
      activeTab.value === "collect"
        ? await getCollectListByPage(params)
        : await getPushListByPage(params);
    if (data.Status) {
      dataList.value = JSON.parse(data.Result);
      pagination.total = data.Total;
    }
    loading.value = false;
  }

  function onTabChange() {
    pagination.currentPage = 1;
    dataList.value = [];
    onSelectionCancel();
    onSearch();
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

  function handleSelectionChange(val: StrategyRow[]) {
    selectedNum.value = val.length;
    selectedRows.value = val;
    tableRef.value.setAdaptive();
  }

  function onSelectionCancel() {
    selectedNum.value = 0;
    selectedRows.value = [];
    tableRef.value?.getTableRef?.()?.clearSelection();
  }

  async function openDialog(title = "新增", row?: StrategyRow) {
    const iscollect = activeTab.value === "collect";
    const formData: CollectStrategyFormItemProps & PushStrategyFormItemProps = {
      title,
      SnowId: 0,
      TenantId: 0,
      ScopeType: 1,
      ScopeId: "",
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

  async function onbatchDel() {
    if (selectedRows.value.length === 0) {
      ElMessage.warning("请先选择要删除的策略");
      return;
    }
    const deleteapi =
      activeTab.value === "collect" ? deleteCollectByPk : deletePushByPk;
    const results = await Promise.all(
      selectedRows.value.map(row => deleteapi(row.SnowId.toString()))
    );
    const successCount = results.filter(result => result.Status).length;
    const failCount = results.length - successCount;
    message(
      `成功删除 ${successCount} 条策略${failCount > 0 ? `，失败 ${failCount} 条` : ""}`,
      { type: failCount === 0 ? "success" : "warning" }
    );
    onSearch();
    onSelectionCancel();
  }

  onMounted(() => {
    onSearch();
  });

  return {
    activeTab,
    form,
    loading,
    columns,
    dataList,
    selectedNum,
    pagination,
    ModuleTitle,
    onTabChange,
    handleSizeChange,
    handleCurrentChange,
    handleSelectionChange,
    onSearch,
    resetForm,
    openDialog,
    handleDelete,
    onbatchDel,
    onSelectionCancel
  };
}
